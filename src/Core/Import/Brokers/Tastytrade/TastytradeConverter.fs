namespace Binnaculum.Core.Import

open System
open System.Threading
open Binnaculum.Core.Database
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging
open TastytradeModels

/// <summary>
/// Converts Tastytrade-specific transaction models to broker-agnostic domain models
/// Extracted from DatabasePersistence for multi-broker support
/// </summary>
module internal TastytradeConverter =

    /// <summary>
    /// Get or create currency ID for the specified currency code
    /// </summary>
    let private getCurrencyId (currencyCode: string) =
        task {
            let! currencyOption = CurrencyExtensions.Do.getByCode (currencyCode) |> Async.AwaitTask

            match currencyOption with
            | Some currency -> return currency.Id
            | None ->
                // Create currency if it doesn't exist
                let newCurrency =
                    { Id = 0
                      Name = $"{currencyCode} Currency"
                      Code = currencyCode
                      Symbol = "$" // Default symbol, could be enhanced with proper lookup
                    }

                do! CurrencyExtensions.Do.save (newCurrency) |> Async.AwaitTask
                let! createdCurrency = CurrencyExtensions.Do.getByCode (currencyCode) |> Async.AwaitTask

                match createdCurrency with
                | Some c -> return c.Id
                | None -> return failwith $"Failed to create currency: {currencyCode}"
        }

    /// <summary>
    /// Get or create ticker ID for a given symbol
    /// </summary>
    let private getOrCreateTickerId (symbol: string) =
        task {
            let! tickers = TickerExtensions.Do.getAll () |> Async.AwaitTask
            let existingTicker = tickers |> List.tryFind (fun t -> t.Symbol = symbol)

            match existingTicker with
            | Some ticker -> return ticker.Id
            | None ->
                // Create new ticker
                let newTicker =
                    { Id = 0
                      Symbol = symbol
                      Image = None
                      Name = Some symbol
                      Audit = AuditableEntity.FromDateTime(DateTime.UtcNow) }

                do! TickerExtensions.Do.save (newTicker) |> Async.AwaitTask
                let! allTickers = TickerExtensions.Do.getAll () |> Async.AwaitTask
                let createdTicker = allTickers |> List.find (fun t -> t.Symbol = symbol)

                return createdTicker.Id
        }

    /// <summary>
    /// Convert TastytradeTransaction to BrokerMovement domain object
    /// </summary>
    let private createBrokerMovementFromTransaction
        (transaction: TastytradeTransaction)
        (brokerAccountId: int)
        (currencyId: int)
        : BrokerMovement option =
        match transaction.TransactionType with
        | MoneyMovement(movementSubType) ->
            // Dividend transactions should NOT create BrokerMovement records
            // They are handled separately as Dividend/DividendTax records for ticker-level tracking
            if movementSubType = Dividend then
                None
            else
                let movementType =
                    match movementSubType with
                    | Deposit -> BrokerMovementType.Deposit
                    | Withdrawal -> BrokerMovementType.Withdrawal
                    | BalanceAdjustment -> BrokerMovementType.Fee // Regulatory fees map to Fee type
                    | CreditInterest -> BrokerMovementType.InterestsGained
                    | DebitInterest -> BrokerMovementType.InterestsPaid
                    | Transfer -> BrokerMovementType.Deposit // Default transfers to deposits
                    | Lending -> BrokerMovementType.Lending
                    | Dividend -> failwith "Dividend should not create BrokerMovement"

                let timeStamp = DateTimePattern.FromDateTime(transaction.Date)

                // Special handling for BalanceAdjustment (regulatory fees):
                // CSV Value field contains the fee amount (negative = you pay, positive = refund)
                // Store it in Fees field with proper sign convention:
                //   - Negative CSV value (-0.02) → Positive Fees (+0.02) = fee paid
                //   - Positive CSV value (+0.50) → Negative Fees (-0.50) = fee refund
                let amount, fees =
                    match movementSubType with
                    | BalanceAdjustment ->
                        // For balance adjustments, the Value field contains the fee amount
                        // Negate the CSV value to convert to our fee convention
                        let feeAmount = -transaction.Value // Flip sign: negative CSV → positive fee
                        Money.FromAmount(0m), Money.FromAmount(feeAmount)
                    | _ ->
                        // For other movement types, use standard handling
                        Money.FromAmount(Math.Abs(transaction.Value)), Money.FromAmount(Math.Abs(transaction.Fees))

                let commissions = Money.FromAmount(Math.Abs(transaction.Commissions))
                let audit = AuditableEntity.FromDateTime(DateTime.UtcNow)

                let brokerMovement =
                    { Id = 0 // Will be set by database
                      TimeStamp = timeStamp
                      Amount = amount // Store as positive amount
                      CurrencyId = currencyId
                      BrokerAccountId = brokerAccountId
                      Commissions = commissions
                      Fees = fees
                      MovementType = movementType
                      Notes = Some transaction.Description
                      FromCurrencyId = None // TODO: Handle currency conversions
                      AmountChanged = None
                      TickerId = None
                      Quantity = None
                      Audit = audit }

                Some brokerMovement
        | _ -> None

    /// <summary>
    /// Convert TastytradeTransaction to OptionTrade domain object
    /// </summary>
    let private createOptionTradeFromTransaction
        (transaction: TastytradeTransaction)
        (brokerAccountId: int)
        (currencyId: int)
        (tickerId: int)
        : OptionTrade list =
        match transaction.TransactionType, transaction.InstrumentType with
        | Trade(_, _), Some "Equity Option"
        | Trade(_, _), Some "Future Option" ->
            // Validate quantity
            if transaction.Quantity <= 0m then
                [] // Invalid quantity - return empty list
            else
                let optionCode =
                    match transaction.TransactionType with
                    | Trade(BuyToOpen, _) -> OptionCode.BuyToOpen
                    | Trade(SellToOpen, _) -> OptionCode.SellToOpen
                    | Trade(BuyToClose, _) -> OptionCode.BuyToClose
                    | Trade(SellToClose, _) -> OptionCode.SellToClose
                    | _ -> OptionCode.BuyToOpen // Default fallback

                let optionType =
                    match transaction.CallOrPut with
                    | Some "CALL" -> OptionType.Call
                    | Some "PUT" -> OptionType.Put
                    | _ -> OptionType.Put // Default fallback

                let expirationDate =
                    match transaction.ExpirationDate with
                    | Some date -> DateTimePattern.FromDateTime(date)
                    | None -> DateTimePattern.FromDateTime(transaction.Date.AddDays(30)) // Default fallback

                // Premium should preserve sign: positive for SELL, negative for BUY
                let premium = transaction.Value
                let commissionCost = Math.Abs(transaction.Commissions)
                let feeCost = Math.Abs(transaction.Fees)

                // NetPremium calculation:
                // For SELL trades: Premium (positive) - Commissions - Fees = Net income
                // For BUY trades: Premium (negative) - Commissions - Fees = Net cost (more negative)
                let netPremium = premium - commissionCost - feeCost

                let multiplier = transaction.Multiplier |> Option.defaultValue 100m
                let strike = transaction.StrikePrice |> Option.defaultValue 0m

                // CRITICAL: Expand trades with quantity > 1 into multiple records with quantity = 1
                // This ensures consistency with UI behavior and enables proper FIFO matching
                let netPremiumPerContract = netPremium / transaction.Quantity

                let baseOptionTrade =
                    { Id = 0 // Will be set by database
                      TimeStamp = DateTimePattern.FromDateTime(transaction.Date)
                      ExpirationDate = expirationDate
                      Premium = Money.FromAmount(premium / transaction.Quantity)
                      NetPremium = Money.FromAmount(netPremiumPerContract)
                      TickerId = tickerId
                      BrokerAccountId = brokerAccountId
                      CurrencyId = currencyId
                      OptionType = optionType
                      Code = optionCode
                      Strike = Money.FromAmount(strike)
                      Commissions = Money.FromAmount(Math.Abs(transaction.Commissions) / transaction.Quantity)
                      Fees = Money.FromAmount(Math.Abs(transaction.Fees) / transaction.Quantity)
                      IsOpen = OptionTradeExtensions.isOpeningCode optionCode
                      ClosedWith = None
                      Multiplier = multiplier
                      Notes = Some transaction.Description
                      Audit = AuditableEntity.FromDateTime(DateTime.UtcNow) }

                // Expand into multiple records (one per contract)
                [ for _ in 1m .. transaction.Quantity -> baseOptionTrade ]
        | _ -> []

    /// <summary>
    /// Convert TastytradeTransaction to Trade domain object (stocks)
    /// </summary>
    let private createTradeFromTransaction
        (transaction: TastytradeTransaction)
        (brokerAccountId: int)
        (currencyId: int)
        (tickerId: int)
        : Trade option =
        match transaction.TransactionType, transaction.InstrumentType with
        | Trade(_, _), Some "Equity" ->
            let tradeCode =
                match transaction.TransactionType with
                | Trade(BuyToOpen, _) -> TradeCode.BuyToOpen
                | Trade(SellToOpen, _) -> TradeCode.SellToOpen
                | Trade(BuyToClose, _) -> TradeCode.BuyToClose
                | Trade(SellToClose, _) -> TradeCode.SellToClose
                | _ -> TradeCode.BuyToOpen // Default fallback

            let tradeType =
                match transaction.TransactionType with
                | Trade(BuyToOpen, _)
                | Trade(BuyToClose, _) -> TradeType.Long
                | Trade(SellToOpen, _)
                | Trade(SellToClose, _) -> TradeType.Short
                | _ -> TradeType.Long // Default fallback

            let price =
                transaction.AveragePrice
                |> Option.map Math.Abs // Ensure price is always positive
                |> Option.defaultValue (Math.Abs(transaction.Value / transaction.Quantity))

            Some
                { Id = 0 // Will be set by database
                  TimeStamp = DateTimePattern.FromDateTime(transaction.Date)
                  TickerId = tickerId
                  BrokerAccountId = brokerAccountId
                  CurrencyId = currencyId
                  Quantity = Math.Abs(transaction.Quantity)
                  Price = Money.FromAmount(price)
                  Commissions = Money.FromAmount(Math.Abs(transaction.Commissions))
                  Fees = Money.FromAmount(Math.Abs(transaction.Fees))
                  TradeCode = tradeCode
                  TradeType = tradeType
                  Leveraged = 1.0m // Default to no leverage
                  Notes = Some transaction.Description
                  Audit = AuditableEntity.FromDateTime(DateTime.UtcNow) }
        | _ -> None

    /// <summary>
    /// Create a Dividend record from a MoneyMovement(Dividend) transaction with positive amount
    /// </summary>
    let private createDividendFromTransaction
        (transaction: TastytradeTransaction)
        (brokerAccountId: int)
        (currencyId: int)
        (tickerId: int)
        : Dividend =
        let timeStamp = DateTimePattern.FromDateTime(transaction.Date)
        let amount = Money.FromAmount(Math.Abs(transaction.Value))
        let audit = AuditableEntity.FromDateTime(DateTime.UtcNow)

        { Id = 0
          TimeStamp = timeStamp
          DividendAmount = amount
          TickerId = tickerId
          CurrencyId = currencyId
          BrokerAccountId = brokerAccountId
          Audit = audit }

    /// <summary>
    /// Create a DividendTax record from a MoneyMovement(Dividend) transaction with negative amount
    /// </summary>
    let private createDividendTaxFromTransaction
        (transaction: TastytradeTransaction)
        (brokerAccountId: int)
        (currencyId: int)
        (tickerId: int)
        : DividendTax =
        let timeStamp = DateTimePattern.FromDateTime(transaction.Date)
        let taxAmount = Money.FromAmount(Math.Abs(transaction.Value))
        let audit = AuditableEntity.FromDateTime(DateTime.UtcNow)

        { Id = 0
          TimeStamp = timeStamp
          DividendTaxAmount = taxAmount
          TickerId = tickerId
          CurrencyId = currencyId
          BrokerAccountId = brokerAccountId
          Audit = audit }

    /// <summary>
    /// Create ACAT equity transfer trade record
    /// </summary>
    let private createAcatTradeFromTransaction
        (transaction: TastytradeTransaction)
        (brokerAccountId: int)
        (currencyId: int)
        (tickerId: int)
        : Trade =
        { Id = 0
          TimeStamp = DateTimePattern.FromDateTime(transaction.Date)
          TickerId = tickerId
          BrokerAccountId = brokerAccountId
          CurrencyId = currencyId
          Quantity = Math.Abs(transaction.Quantity)
          Price = Money.FromAmount(0m) // ACAT transfers have no cost
          Commissions = Money.FromAmount(Math.Abs(transaction.Commissions))
          Fees = Money.FromAmount(Math.Abs(transaction.Fees))
          TradeCode = TradeCode.BuyToOpen // ACAT is like opening a position
          TradeType = TradeType.Long // Receiving shares is a long position
          Leveraged = 1.0m
          Notes = Some transaction.Description
          Audit = AuditableEntity.FromDateTime(DateTime.UtcNow) }

    /// <summary>
    /// Apply strike adjustment to a single option trade if a matching adjustment exists.
    /// This is called per-trade BEFORE saving to ensure strikes are correct before FIFO matching.
    /// </summary>
    let private applyAdjustmentToSingleTrade
        (trade: DatabaseModel.OptionTrade)
        (adjustments: SpecialDividendAdjustmentDetector.DetectedAdjustment list)
        : DatabaseModel.OptionTrade =

        let matchingAdjustment =
            adjustments
            |> List.tryFind (fun adj ->
                // Match by expiration date
                let sameExpiration =
                    match adj.ClosingTransaction.ExpirationDate with
                    | Some expDate -> trade.ExpirationDate = DateTimePattern.FromDateTime(expDate)
                    | None -> false

                // Match by original strike (what we're adjusting FROM)
                let sameStrike = trade.Strike.Value = adj.OriginalStrike

                // Match by option type
                let sameType =
                    match adj.OptionType with
                    | "CALL" -> trade.OptionType = OptionType.Call
                    | "PUT" -> trade.OptionType = OptionType.Put
                    | _ -> false

                sameExpiration && sameStrike && sameType)

        match matchingAdjustment with
        | Some adj ->
            // Apply the adjustment
            let adjustmentNote =
                SpecialDividendAdjustmentDetector.formatAdjustmentNote
                    adj.OriginalStrike
                    adj.NewStrike
                    adj.DividendAmount

            { trade with
                Strike = Money.FromAmount(adj.NewStrike)
                Notes = Some adjustmentNote
                Audit =
                    { trade.Audit with
                        UpdatedAt = Some(DateTimePattern.FromDateTime(DateTime.UtcNow)) } }
        | None ->
            // No adjustment needed - return unchanged
            trade

    /// <summary>
    /// Convert Tastytrade transactions to domain models
    /// Main entry point for Tastytrade-to-domain conversion
    /// Supports session tracking for resumable imports (integrates with PR #420)
    /// </summary>
    let convertToDomainModels
        (transactions: TastytradeTransaction list)
        (brokerAccountId: int)
        (sessionId: int option)
        (cancellationToken: CancellationToken)
        : Threading.Tasks.Task<ImportDomainTypes.PersistenceInput> =
        task {
            let mutable brokerMovements = []
            let mutable optionTrades = []
            let mutable stockTrades = []
            let mutable dividends = []
            let mutable dividendTaxes = []

            // Sort transactions by date (chronological order)
            let sortedTransactions = transactions |> List.sortBy (fun t -> t.Date)

            // Detect all strike adjustments BEFORE processing transactions
            // This allows us to apply adjustments to option trades as they're created,
            // ensuring strikes are correct BEFORE FIFO matching
            let detectedAdjustments =
                SpecialDividendAdjustmentDetector.detectAdjustments sortedTransactions

            // Process each transaction
            for transaction in sortedTransactions do
                cancellationToken.ThrowIfCancellationRequested()

                try
                    // Get currency ID for this transaction (with USD fallback)
                    let currencyCode =
                        if String.IsNullOrWhiteSpace(transaction.Currency) then
                            "USD"
                        else
                            transaction.Currency

                    let! currencyId = getCurrencyId currencyCode

                    match transaction.TransactionType with
                    | MoneyMovement(_) ->
                        match createBrokerMovementFromTransaction transaction brokerAccountId currencyId with
                        | Some brokerMovement -> brokerMovements <- brokerMovement :: brokerMovements
                        | None ->
                            // Dividend transactions return None from createBrokerMovementFromTransaction
                            // Process them as ticker-level Dividend/DividendTax records
                            match transaction.TransactionType with
                            | MoneyMovement(Dividend) ->
                                let tickerSymbol = transaction.Symbol |> Option.defaultValue "UNKNOWN"

                                if tickerSymbol <> "UNKNOWN" && tickerSymbol <> "" then
                                    let! tickerId = getOrCreateTickerId tickerSymbol

                                    if transaction.Value > 0m then
                                        // Positive amount = Dividend received
                                        let dividend =
                                            createDividendFromTransaction
                                                transaction
                                                brokerAccountId
                                                currencyId
                                                tickerId

                                        dividends <- dividend :: dividends
                                    else
                                        // Negative amount = Dividend tax withheld
                                        let dividendTax =
                                            createDividendTaxFromTransaction
                                                transaction
                                                brokerAccountId
                                                currencyId
                                                tickerId

                                        dividendTaxes <- dividendTax :: dividendTaxes
                            | _ -> ()

                    | Trade(_, _) when transaction.InstrumentType = Some "Equity Option" ->
                        // Get ticker ID for the underlying symbol
                        let underlyingSymbol = transaction.UnderlyingSymbol |> Option.defaultValue "UNKNOWN"
                        let! tickerId = getOrCreateTickerId underlyingSymbol

                        let expandedOptionTrades =
                            createOptionTradeFromTransaction transaction brokerAccountId currencyId tickerId

                        // Apply strike adjustments to each option trade (if applicable)
                        let adjustedOptionTrades =
                            expandedOptionTrades
                            |> List.map (fun trade -> applyAdjustmentToSingleTrade trade detectedAdjustments)

                        optionTrades <- List.append optionTrades adjustedOptionTrades

                    | Trade(_, _) when transaction.InstrumentType = Some "Equity" ->
                        // Get ticker ID for the stock symbol
                        let stockSymbol = transaction.Symbol |> Option.defaultValue "UNKNOWN"
                        let! tickerId = getOrCreateTickerId stockSymbol

                        match createTradeFromTransaction transaction brokerAccountId currencyId tickerId with
                        | Some stockTrade -> stockTrades <- stockTrade :: stockTrades
                        | None -> ()

                    | ReceiveDeliver(_) when transaction.InstrumentType = Some "Equity" ->
                        // ACAT equity transfers
                        let stockSymbol = transaction.Symbol |> Option.defaultValue "UNKNOWN"
                        let! tickerId = getOrCreateTickerId stockSymbol

                        let acatTrade =
                            createAcatTradeFromTransaction transaction brokerAccountId currencyId tickerId

                        stockTrades <- acatTrade :: stockTrades

                    | ReceiveDeliver(_) when transaction.InstrumentType = Some "Equity Option" ->
                        // Option expirations/assignments/exercises - informational only, skip
                        ()

                    | Trade(_, _) when transaction.InstrumentType = Some "Future Option" ->
                        // Future option trades - get ticker ID for the underlying future symbol
                        let underlyingSymbol = transaction.UnderlyingSymbol |> Option.defaultValue "UNKNOWN"
                        let! tickerId = getOrCreateTickerId underlyingSymbol

                        let expandedOptionTrades =
                            createOptionTradeFromTransaction transaction brokerAccountId currencyId tickerId

                        // Apply strike adjustments to each option trade (if applicable)
                        let adjustedOptionTrades =
                            expandedOptionTrades
                            |> List.map (fun trade -> applyAdjustmentToSingleTrade trade detectedAdjustments)

                        optionTrades <- List.append optionTrades adjustedOptionTrades

                    | ReceiveDeliver(_) when transaction.InstrumentType = Some "Future Option" ->
                        // Future option expirations/assignments/exercises - informational only, skip
                        ()

                    | _ -> ()

                with ex ->
                    CoreLogger.logWarningf
                        "TastytradeConverter"
                        "Error converting transaction line %d: %s"
                        transaction.LineNumber
                        ex.Message

            return
                { ImportDomainTypes.PersistenceInput.BrokerMovements = brokerMovements
                  OptionTrades = optionTrades
                  StockTrades = stockTrades
                  Dividends = dividends
                  DividendTaxes = dividendTaxes
                  SessionId = sessionId }
        }
