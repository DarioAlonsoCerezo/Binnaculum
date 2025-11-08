namespace Binnaculum.Core.Import

open System
open System.Threading
open System.Diagnostics
open Binnaculum.Core
open Binnaculum.Core.Database
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging
open Binnaculum.Core.Storage
open OptionTradeExtensions
open TastytradeModels

/// <summary>
/// Database persistence module for converting parsed import data into domain objects and saving to SQLite
/// </summary>
module DatabasePersistence =

    /// <summary>
    /// Result of database persistence operations with counts and import metadata
    /// </summary>
    type PersistenceResult =
        {
            BrokerMovementsCreated: int
            OptionTradesCreated: int
            StockTradesCreated: int
            DividendsCreated: int
            ErrorsCount: int
            Errors: string list
            /// Metadata collected during persistence for targeted snapshot updates
            ImportMetadata: ImportMetadata
        }

    let private getTransactionProcessingPriority (transaction: TastytradeTransaction) =
        match transaction.TransactionType with
        | MoneyMovement _ -> 0
        | Trade(subType, _) ->
            match subType with
            | BuyToOpen
            | SellToOpen -> 1
            | BuyToClose
            | SellToClose -> 2
        | ReceiveDeliver _ -> 3

    let internal orderTransactionsForPersistence (transactions: TastytradeTransaction list) =
        // CoreLogger.logDebugf
        //     "DatabasePersistence"
        //     "orderTransactionsForPersistence: Starting to sort %d transactions by date"
        //     transactions.Length

        let sorted = transactions |> List.sortBy (fun t -> t.Date)

        // CoreLogger.logDebugf
        //     "DatabasePersistence"
        //     "orderTransactionsForPersistence: Sorting completed by date, returning %d transactions in chronological order"
        //     sorted.Length

        sorted

    /// <summary>
    /// Convert TastytradeTransaction to BrokerMovement domain object
    /// </summary>
    let private createBrokerMovementFromTransaction
        (transaction: TastytradeTransaction)
        (brokerAccountId: int)
        (currencyId: int)
        : BrokerMovement option =
        // CoreLogger.logDebugf
        //     "DatabasePersistence"
        //     "createBrokerMovementFromTransaction: Starting with transaction type: %A"
        //     transaction.TransactionType

        match transaction.TransactionType with
        | MoneyMovement(movementSubType) ->
            // CoreLogger.logDebugf
            //     "DatabasePersistence"
            //     "createBrokerMovementFromTransaction: Processing MoneyMovement subtype: %A"
            //     movementSubType

            // Dividend transactions should NOT create BrokerMovement records
            // They are handled separately as Dividend/DividendTax records for ticker-level tracking
            if movementSubType = Dividend then
                None
            else
                let movementType =
                    match movementSubType with
                    | Deposit ->
                        // CoreLogger.logDebug
                        //     "DatabasePersistence"
                        //     "createBrokerMovementFromTransaction: Mapping to BrokerMovementType.Deposit"

                        BrokerMovementType.Deposit
                    | Withdrawal ->
                        // CoreLogger.logDebug
                        //     "DatabasePersistence"
                        //     "createBrokerMovementFromTransaction: Mapping to BrokerMovementType.Withdrawal"

                        BrokerMovementType.Withdrawal
                    | BalanceAdjustment ->
                        // CoreLogger.logDebug
                        //     "DatabasePersistence"
                        //     "createBrokerMovementFromTransaction: Mapping to BrokerMovementType.Fee"

                        BrokerMovementType.Fee // Regulatory fees map to Fee type
                    | CreditInterest ->
                        // CoreLogger.logDebug
                        //     "DatabasePersistence"
                        //     "createBrokerMovementFromTransaction: Mapping to BrokerMovementType.InterestsGained"

                        BrokerMovementType.InterestsGained
                    | DebitInterest -> BrokerMovementType.InterestsPaid
                    | Transfer ->
                        // CoreLogger.logDebug
                        //     "DatabasePersistence"
                        //     "createBrokerMovementFromTransaction: Mapping to BrokerMovementType.Deposit"

                        BrokerMovementType.Deposit // Default transfers to deposits
                    | Lending -> BrokerMovementType.Lending
                    | Dividend ->
                        // This should never be reached due to early return above
                        failwith "Dividend should not create BrokerMovement"

                // CoreLogger.logDebug
                //     "DatabasePersistence"
                //     "createBrokerMovementFromTransaction: About to create BrokerMovement object"

                // CoreLogger.logDebugf
                //     "DatabasePersistence"
                //     "createBrokerMovementFromTransaction: Transaction date: %A"
                //     transaction.Date

                // CoreLogger.logDebugf
                //     "DatabasePersistence"
                //     "createBrokerMovementFromTransaction: Transaction value: %M"
                //     transaction.Value

                // CoreLogger.logDebugf
                //     "DatabasePersistence"
                //     "createBrokerMovementFromTransaction: Transaction commissions: %M"
                //     transaction.Commissions

                // CoreLogger.logDebugf
                //     "DatabasePersistence"
                //     "createBrokerMovementFromTransaction: Transaction fees: %M"
                //     transaction.Fees

                // CoreLogger.logDebug
                //     "DatabasePersistence"
                //     "createBrokerMovementFromTransaction: Creating DateTimePattern from transaction date"

                let timeStamp = DateTimePattern.FromDateTime(transaction.Date)

                // CoreLogger.logDebug
                //     "DatabasePersistence"
                //     "createBrokerMovementFromTransaction: DateTimePattern created successfully"

                // CoreLogger.logDebug "DatabasePersistence" "createBrokerMovementFromTransaction: Creating Money objects"

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

                // CoreLogger.logDebug "DatabasePersistence" "createBrokerMovementFromTransaction: Amount Money object created"
                let commissions = Money.FromAmount(Math.Abs(transaction.Commissions))

                // CoreLogger.logDebug
                //     "DatabasePersistence"
                //     "createBrokerMovementFromTransaction: Commissions Money object created"

                // CoreLogger.logDebug "DatabasePersistence" "createBrokerMovementFromTransaction: Fees Money object created"

                // CoreLogger.logDebug "DatabasePersistence" "createBrokerMovementFromTransaction: Creating AuditableEntity"
                let audit = AuditableEntity.FromDateTime(DateTime.UtcNow)

                // CoreLogger.logDebug
                //     "DatabasePersistence"
                //     "createBrokerMovementFromTransaction: AuditableEntity created successfully"

                // CoreLogger.logDebug
                //     "DatabasePersistence"
                //     "createBrokerMovementFromTransaction: Creating BrokerMovement record"

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

                // CoreLogger.logDebug
                //     "DatabasePersistence"
                //     "createBrokerMovementFromTransaction: BrokerMovement record created successfully"

                Some brokerMovement
        | _ ->
            // CoreLogger.logDebug
            //     "DatabasePersistence"
            //     "createBrokerMovementFromTransaction: Non-MoneyMovement transaction type, returning None"

            None

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
        | Trade(_, _), Some "Equity Option" ->
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
                      IsOpen = isOpeningCode optionCode
                      ClosedWith = None
                      Multiplier = multiplier
                      Notes = Some transaction.Description
                      Audit = AuditableEntity.FromDateTime(DateTime.UtcNow) }

                // Expand into multiple records (one per contract)
                [ for _ in 1m .. transaction.Quantity -> baseOptionTrade ]
        | _ -> []

    /// <summary>
    /// Convert TastytradeTransaction to Trade domain object
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

                // Skip initial snapshot creation - batch processing will handle all snapshots including current date
                // This avoids reactive collection caching issues where old snapshot objects aren't updated
                // do!
                //     TickerSnapshotManager.handleNewTicker (createdTicker)
                //     |> Async.AwaitTask
                //     |> Async.Ignore

                return createdTicker.Id
        }

    /// <summary>
    /// Create a Dividend record from a MoneyMovement(Dividend) transaction with positive amount
    /// </summary>
    let private createDividendFromTransaction
        (transaction: TastytradeTransaction)
        (brokerAccountId: int)
        (currencyId: int)
        (tickerId: int)
        : Dividend =
        // CoreLogger.logDebugf
        //     "DatabasePersistence"
        //     "createDividendFromTransaction: Creating dividend record for ticker %d with amount %M"
        //     tickerId
        //     transaction.Value

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
        // CoreLogger.logDebugf
        //     "DatabasePersistence"
        //     "createDividendTaxFromTransaction: Creating dividend tax record for ticker %d with amount %M"
        //     tickerId
        //     (Math.Abs(transaction.Value))

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
    /// Apply strike adjustments from special dividend transactions to option trades.
    /// Updates the Strike and Notes fields of affected OptionTrade records.
    /// </summary>
    let private applyStrikeAdjustments
        (transactions: TastytradeTransaction list)
        (optionTrades: DatabaseModel.OptionTrade list)
        : DatabaseModel.OptionTrade list =
        try
            // Detect all adjustment pairs from transactions
            let adjustments: SpecialDividendAdjustmentDetector.DetectedAdjustment list =
                SpecialDividendAdjustmentDetector.detectAdjustments transactions

            if List.isEmpty adjustments then
                // CoreLogger.logDebugf "DatabasePersistence" "No strike adjustments detected - returning option trades unchanged"

                optionTrades
            else
                // CoreLogger.logInfof "DatabasePersistence" "Applying %d detected strike adjustments to option trades" adjustments.Length

                // Apply each adjustment to matching option trades
                let mutable updatedTrades = optionTrades

                for (adjustment: SpecialDividendAdjustmentDetector.DetectedAdjustment) in adjustments do
                    // Find option trades that match this adjustment
                    // Matching criteria: same expiration, option type, and ORIGINAL strike
                    let matchingTrades =
                        updatedTrades
                        |> List.filter (fun (trade: DatabaseModel.OptionTrade) ->
                            // Same expiration date
                            let sameExpiration =
                                match adjustment.ClosingTransaction.ExpirationDate with
                                | Some closeExp -> trade.ExpirationDate = DateTimePattern.FromDateTime(closeExp)
                                | None -> false

                            // Same original strike (this is what we're updating FROM)
                            let sameOriginalStrike = trade.Strike.Value = adjustment.OriginalStrike

                            // Same option type
                            let sameOptionType =
                                match adjustment.OptionType with
                                | "CALL" -> trade.OptionType = OptionType.Call
                                | "PUT" -> trade.OptionType = OptionType.Put
                                | _ -> false

                            // Is open (not already closed)
                            let isOpen = trade.IsOpen

                            sameExpiration && sameOriginalStrike && sameOptionType && isOpen)

                    // Update each matching trade
                    for matchingTrade in matchingTrades do
                        try
                            // Format adjustment note
                            let adjustmentNote =
                                SpecialDividendAdjustmentDetector.formatAdjustmentNote
                                    adjustment.OriginalStrike
                                    adjustment.NewStrike
                                    adjustment.DividendAmount

                            // Create updated trade record
                            let updatedTrade =
                                { matchingTrade with
                                    Strike = Money.FromAmount(adjustment.NewStrike)
                                    Notes = Some adjustmentNote
                                    Audit =
                                        { matchingTrade.Audit with
                                            UpdatedAt = Some(DateTimePattern.FromDateTime(DateTime.UtcNow)) } }

                            // Replace in the list
                            updatedTrades <-
                                updatedTrades
                                |> List.map (fun t -> if t.Id = matchingTrade.Id then updatedTrade else t)

                        // CoreLogger.logDebugf "DatabasePersistence" "Updated option trade ID=%d: strike %.2f -> %.2f, adjustment note added" matchingTrade.Id adjustment.OriginalStrike adjustment.NewStrike
                        with ex ->
                            CoreLogger.logWarningf
                                "DatabasePersistence"
                                "Failed to apply adjustment to option trade ID=%d: %s"
                                matchingTrade.Id
                                ex.Message

                    if List.isEmpty matchingTrades then
                        CoreLogger.logWarningf
                            "DatabasePersistence"
                            "No matching open option trades found for adjustment: %s %s strike=%.2f"
                            adjustment.TickerSymbol
                            adjustment.OptionType
                            adjustment.OriginalStrike

                updatedTrades
        with ex ->
            CoreLogger.logErrorf "DatabasePersistence" "Error applying strike adjustments: %s" ex.Message

            optionTrades

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
    /// Persist domain models to database (broker-agnostic)
    /// Supports all brokers by accepting pre-converted domain models
    /// Integrates with session tracking from PR #420 for resumable imports
    /// </summary>
    let internal persistDomainModelsToDatabase
        (input: ImportDomainTypes.PersistenceInput)
        (brokerAccountId: int)
        (cancellationToken: CancellationToken)
        =
        task {
            let mutable errors = []

            // Collect metadata for targeted snapshot updates
            let mutable affectedTickerSymbols = Set.empty<string>
            let mutable movementDates = []

            try
                let totalItems =
                    input.BrokerMovements.Length
                    + input.OptionTrades.Length
                    + input.StockTrades.Length
                    + input.Dividends.Length
                    + input.DividendTaxes.Length

                let mutable processedCount = 0

                // Persist BrokerMovements
                for brokerMovement in input.BrokerMovements do
                    cancellationToken.ThrowIfCancellationRequested()

                    try
                        do! BrokerMovementExtensions.Do.save brokerMovement |> Async.AwaitTask
                        movementDates <- brokerMovement.TimeStamp.Value :: movementDates
                        processedCount <- processedCount + 1

                        let progress = float processedCount / float totalItems

                        ImportState.updateStatus (
                            SavingToDatabase($"Saving broker movements {processedCount} of {totalItems}", progress)
                        )
                    with ex ->
                        errors <- $"Error saving BrokerMovement ID={brokerMovement.Id}: {ex.Message}" :: errors

                // Persist OptionTrades
                for optionTrade in input.OptionTrades do
                    cancellationToken.ThrowIfCancellationRequested()

                    try
                        let! persistedTrade = OptionTradeExtensions.Do.saveAndReturn optionTrade
                        movementDates <- optionTrade.TimeStamp.Value :: movementDates

                        // Collect ticker symbol for metadata (need to look it up)
                        let! tickerOption = TickerExtensions.Do.getById optionTrade.TickerId |> Async.AwaitTask

                        match tickerOption with
                        | Some ticker -> affectedTickerSymbols <- Set.add ticker.Symbol affectedTickerSymbols
                        | None -> ()

                        // Link closing trades
                        if isClosingCode persistedTrade.Code then
                            let! linkResult = OptionTradeExtensions.Do.linkClosingTrade persistedTrade

                            match linkResult with
                            | Ok _ -> ()
                            | Error _ -> () // Non-critical linking failure

                        processedCount <- processedCount + 1
                        let progress = float processedCount / float totalItems

                        ImportState.updateStatus (
                            SavingToDatabase($"Saving option trades {processedCount} of {totalItems}", progress)
                        )
                    with ex ->
                        errors <- $"Error saving OptionTrade ID={optionTrade.Id}: {ex.Message}" :: errors

                // Persist StockTrades
                for stockTrade in input.StockTrades do
                    cancellationToken.ThrowIfCancellationRequested()

                    try
                        do! TradeExtensions.Do.save stockTrade |> Async.AwaitTask
                        movementDates <- stockTrade.TimeStamp.Value :: movementDates

                        // Collect ticker symbol for metadata
                        let! tickerOption = TickerExtensions.Do.getById stockTrade.TickerId |> Async.AwaitTask

                        match tickerOption with
                        | Some ticker -> affectedTickerSymbols <- Set.add ticker.Symbol affectedTickerSymbols
                        | None -> ()

                        processedCount <- processedCount + 1
                        let progress = float processedCount / float totalItems

                        ImportState.updateStatus (
                            SavingToDatabase($"Saving stock trades {processedCount} of {totalItems}", progress)
                        )
                    with ex ->
                        errors <- $"Error saving Trade ID={stockTrade.Id}: {ex.Message}" :: errors

                // Persist Dividends
                for dividend in input.Dividends do
                    cancellationToken.ThrowIfCancellationRequested()

                    try
                        do! DividendExtensions.Do.save dividend |> Async.AwaitTask
                        movementDates <- dividend.TimeStamp.Value :: movementDates

                        // Collect ticker symbol for metadata
                        let! tickerOption = TickerExtensions.Do.getById dividend.TickerId |> Async.AwaitTask

                        match tickerOption with
                        | Some ticker -> affectedTickerSymbols <- Set.add ticker.Symbol affectedTickerSymbols
                        | None -> ()

                        processedCount <- processedCount + 1
                        let progress = float processedCount / float totalItems

                        ImportState.updateStatus (
                            SavingToDatabase($"Saving dividends {processedCount} of {totalItems}", progress)
                        )
                    with ex ->
                        errors <- $"Error saving Dividend ID={dividend.Id}: {ex.Message}" :: errors

                // Persist DividendTaxes
                for dividendTax in input.DividendTaxes do
                    cancellationToken.ThrowIfCancellationRequested()

                    try
                        do! DividendTaxExtensions.Do.save dividendTax |> Async.AwaitTask
                        movementDates <- dividendTax.TimeStamp.Value :: movementDates

                        // Collect ticker symbol for metadata
                        let! tickerOption = TickerExtensions.Do.getById dividendTax.TickerId |> Async.AwaitTask

                        match tickerOption with
                        | Some ticker -> affectedTickerSymbols <- Set.add ticker.Symbol affectedTickerSymbols
                        | None -> ()

                        processedCount <- processedCount + 1
                        let progress = float processedCount / float totalItems

                        ImportState.updateStatus (
                            SavingToDatabase($"Saving dividend taxes {processedCount} of {totalItems}", progress)
                        )
                    with ex ->
                        errors <- $"Error saving DividendTax ID={dividendTax.Id}: {ex.Message}" :: errors

                // Final progress update
                ImportState.updateStatus (SavingToDatabase("Database save completed", 1.0))

                // Create import metadata for targeted snapshot updates
                let oldestMovementDate =
                    if List.isEmpty movementDates then
                        None
                    else
                        Some(movementDates |> List.min)

                let importMetadata =
                    { OldestMovementDate = oldestMovementDate
                      AffectedBrokerAccountIds = Set.singleton brokerAccountId
                      AffectedTickerSymbols = affectedTickerSymbols
                      TotalMovementsImported = totalItems }

                return
                    { BrokerMovementsCreated = input.BrokerMovements.Length
                      OptionTradesCreated = input.OptionTrades.Length
                      StockTradesCreated = input.StockTrades.Length
                      DividendsCreated = input.Dividends.Length + input.DividendTaxes.Length
                      ErrorsCount = errors.Length
                      Errors = List.rev errors
                      ImportMetadata = importMetadata }

            with
            | :? OperationCanceledException ->
                ImportState.updateStatus (SavingToDatabase("Database save cancelled", 0.0))

                return
                    { BrokerMovementsCreated = 0
                      OptionTradesCreated = 0
                      StockTradesCreated = 0
                      DividendsCreated = 0
                      ErrorsCount = 1
                      Errors = [ "Operation was cancelled" ]
                      ImportMetadata = ImportMetadata.createEmpty () }
            | ex ->
                errors <- $"Database persistence failed: {ex.Message}" :: errors

                return
                    { BrokerMovementsCreated = 0
                      OptionTradesCreated = 0
                      StockTradesCreated = 0
                      DividendsCreated = 0
                      ErrorsCount = errors.Length
                      Errors = List.rev errors
                      ImportMetadata = ImportMetadata.createEmpty () }
        }

    /// <summary>
    /// Convert parsed TastytradeTransactions to domain objects and persist to database
    /// </summary>
    let persistTransactionsToDatabase
        (transactions: TastytradeTransaction list)
        (brokerAccountId: int)
        (cancellationToken: CancellationToken)
        =
        task {
            // do
            //     CoreLogger.logInfof
            //         "DatabasePersistence"
            //         "Persisting %d transactions for account %d"
            //         transactions.Length
            //         brokerAccountId

            let mutable brokerMovements = []
            let mutable optionTrades = []
            let mutable stockTrades = []
            let mutable dividends = [] // Dividend records for ticker-level dividend tracking
            let mutable dividendTaxes = [] // DividendTax records for ticker-level tax tracking
            let mutable errors = []

            // Collect metadata for targeted snapshot updates
            let mutable affectedTickerSymbols = Set.empty<string>
            let mutable movementDates = []

            try
                // CoreLogger.logDebugf
                //     "DatabasePersistence"
                //     "About to order %d transactions for persistence"
                //     transactions.Length

                let orderedTransactions = orderTransactionsForPersistence transactions
                let totalTransactions = orderedTransactions.Length

                // Detect all strike adjustments BEFORE processing transactions
                // This allows us to apply adjustments to option trades as they're created,
                // ensuring strikes are correct BEFORE linkClosingTrade attempts FIFO matching
                let detectedAdjustments =
                    SpecialDividendAdjustmentDetector.detectAdjustments orderedTransactions

                // CoreLogger.logDebugf
                //     "DatabasePersistence"
                //     "Transactions ordered successfully; total=%d"
                //     totalTransactions

                // CoreLogger.logDebug "DatabasePersistence" "Starting transaction processing loop"

                // Process each transaction
                for (index, transaction) in orderedTransactions |> List.mapi (fun i t -> i, t) do
                    cancellationToken.ThrowIfCancellationRequested()

                    // Update progress
                    let progress =
                        if totalTransactions = 0 then
                            0.0
                        else
                            float index / float totalTransactions

                    ImportState.updateStatus (
                        SavingToDatabase($"Saving transaction {index + 1} of {totalTransactions}", progress)
                    )

                    try
                        // CoreLogger.logDebugf
                        //     "DatabasePersistence"
                        //     "Processing transaction %d/%d: line=%d, type=%A"
                        //     (index + 1)
                        //     totalTransactions
                        //     transaction.LineNumber
                        //     transaction.TransactionType
                        // Get currency ID for this transaction (with USD fallback)
                        let currencyCode =
                            if String.IsNullOrWhiteSpace(transaction.Currency) then
                                "USD"
                            else
                                transaction.Currency

                        // CoreLogger.logDebugf "DatabasePersistence" "Getting currency ID for: %s" currencyCode
                        let! currencyId = getCurrencyId (currencyCode)
                        // CoreLogger.logDebugf "DatabasePersistence" "Got currency ID: %d for %s" currencyId currencyCode

                        // Collect movement date for metadata
                        movementDates <- transaction.Date :: movementDates

                        // CoreLogger.logDebugf
                        //     "DatabasePersistence"
                        //     "Processing transaction type: %A"
                        //     transaction.TransactionType

                        match transaction.TransactionType with
                        | MoneyMovement(_) ->
                            // CoreLogger.logDebug "DatabasePersistence" "Creating BrokerMovement from transaction"

                            match createBrokerMovementFromTransaction transaction brokerAccountId currencyId with
                            | Some brokerMovement ->
                                // CoreLogger.logDebug "DatabasePersistence" "Saving BrokerMovement to database"
                                do! BrokerMovementExtensions.Do.save (brokerMovement) |> Async.AwaitTask
                                // CoreLogger.logDebug "DatabasePersistence" "BrokerMovement saved successfully"
                                brokerMovements <- brokerMovement :: brokerMovements

                                // CoreLogger.logDebug
                                //     "DatabasePersistence"
                                //     "BrokerMovement added to collection, continuing to next step"

                                // For non-dividend money movements, no ticker-level records needed
                                // (Dividends return None from createBrokerMovementFromTransaction and are handled separately)
                                match transaction.TransactionType with
                                | MoneyMovement(_) -> ()
                                | _ -> ()
                            | None ->
                                // Dividend transactions intentionally return None from createBrokerMovementFromTransaction
                                // because they're processed as ticker-level Dividend/DividendTax records, not BrokerMovements
                                match transaction.TransactionType with
                                | MoneyMovement(Dividend) ->
                                    // Handle dividend transactions by creating Dividend/DividendTax records for tickers
                                    let tickerSymbol = transaction.Symbol |> Option.defaultValue "UNKNOWN"

                                    if tickerSymbol <> "UNKNOWN" && tickerSymbol <> "" then
                                        let! tickerId = getOrCreateTickerId (tickerSymbol)

                                        // Add to affected tickers for metadata
                                        affectedTickerSymbols <- Set.add tickerSymbol affectedTickerSymbols

                                        if transaction.Value > 0m then
                                            // Positive amount = Dividend received
                                            let dividend =
                                                createDividendFromTransaction
                                                    transaction
                                                    brokerAccountId
                                                    currencyId
                                                    tickerId

                                            do! DividendExtensions.Do.save (dividend) |> Async.AwaitTask
                                            dividends <- dividend :: dividends
                                        else
                                            // Negative amount = Dividend tax withheld
                                            let dividendTax =
                                                createDividendTaxFromTransaction
                                                    transaction
                                                    brokerAccountId
                                                    currencyId
                                                    tickerId

                                            do! DividendTaxExtensions.Do.save (dividendTax) |> Async.AwaitTask
                                            dividendTaxes <- dividendTax :: dividendTaxes
                                    else
                                        CoreLogger.logWarning
                                            "DatabasePersistence"
                                            "Dividend transaction has no ticker symbol, skipping dividend record creation"
                                | _ ->
                                    // Other transaction types returning None is an actual error
                                    errors <-
                                        $"Failed to create BrokerMovement from line {transaction.LineNumber}" :: errors

                        | Trade(_, _) when transaction.InstrumentType = Some "Equity Option" ->
                            // Get ticker ID for the underlying symbol
                            let underlyingSymbol = transaction.UnderlyingSymbol |> Option.defaultValue "UNKNOWN"
                            let! tickerId = getOrCreateTickerId (underlyingSymbol)

                            // Add to affected tickers for metadata
                            affectedTickerSymbols <- Set.add underlyingSymbol affectedTickerSymbols

                            let expandedOptionTrades =
                                createOptionTradeFromTransaction transaction brokerAccountId currencyId tickerId

                            if expandedOptionTrades.Length = 0 then
                                errors <- $"Failed to create OptionTrade from line {transaction.LineNumber}" :: errors
                            else
                                // Process each expanded option trade (normally will be Quantity=1 per trade)
                                for optionTrade in expandedOptionTrades do
                                    // Apply strike adjustment BEFORE saving (if applicable)
                                    // This ensures strikes are correct BEFORE linkClosingTrade attempts FIFO matching
                                    let adjustedTrade = applyAdjustmentToSingleTrade optionTrade detectedAdjustments

                                    let! persistedTrade = OptionTradeExtensions.Do.saveAndReturn (adjustedTrade)
                                    optionTrades <- persistedTrade :: optionTrades

                                    if isClosingCode persistedTrade.Code then
                                        // Now linkClosingTrade works with the CORRECT adjusted strike
                                        let! linkResult = OptionTradeExtensions.Do.linkClosingTrade (persistedTrade)

                                        match linkResult with
                                        | Ok _ -> ()
                                        | Error message ->
                                            // Log the linking error but don't count it as a persistence error
                                            // Linking can fail due to strike adjustments from dividends, stock splits, etc.
                                            // The trade is still persisted successfully - this is just metadata linking
                                            // CoreLogger.logDebugf
                                            //     "DatabasePersistence"
                                            //     "Option trade linking failed (non-critical): %s"
                                            //     message
                                            ()

                        | Trade(_, _) when transaction.InstrumentType = Some "Equity" ->
                            // Get ticker ID for the stock symbol
                            let stockSymbol = transaction.Symbol |> Option.defaultValue "UNKNOWN"
                            let! tickerId = getOrCreateTickerId (stockSymbol)

                            // Add to affected tickers for metadata
                            affectedTickerSymbols <- Set.add stockSymbol affectedTickerSymbols

                            match createTradeFromTransaction transaction brokerAccountId currencyId tickerId with
                            | Some stockTrade ->
                                do! TradeExtensions.Do.save (stockTrade) |> Async.AwaitTask
                                stockTrades <- stockTrade :: stockTrades
                            | None -> errors <- $"Failed to create Trade from line {transaction.LineNumber}" :: errors

                        | ReceiveDeliver(_) when transaction.InstrumentType = Some "Equity" ->
                            // ACAT equity transfers - shares received from another broker
                            // These are treated as trades with $0 cost basis (value comes from the transfer itself)
                            // CoreLogger.logDebugf
                            //     "DatabasePersistence"
                            //     "Processing ACAT equity transfer for %A"
                            //     transaction.Symbol

                            let stockSymbol = transaction.Symbol |> Option.defaultValue "UNKNOWN"
                            let! tickerId = getOrCreateTickerId (stockSymbol)

                            // Add to affected tickers for metadata
                            affectedTickerSymbols <- Set.add stockSymbol affectedTickerSymbols

                            // Create a trade record for the ACAT transfer with $0 price
                            let acatTrade =
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

                            do! TradeExtensions.Do.save (acatTrade) |> Async.AwaitTask
                            stockTrades <- acatTrade :: stockTrades

                        // CoreLogger.logDebugf
                        //     "DatabasePersistence"
                        //     "ACAT equity transfer saved: %s, quantity=%M"
                        //     stockSymbol
                        //     transaction.Quantity

                        | ReceiveDeliver(_) when transaction.InstrumentType = Some "Equity Option" ->
                            // Option expirations/assignments/exercises - informational only, no tradeable record
                            // These are silently skipped as they don't represent new positions
                            // CoreLogger.logDebugf
                            //     "DatabasePersistence"
                            //     "Skipping ReceiveDeliver option event (informational only): %s"
                            //     (transaction.Description)
                            ()

                        | _ ->
                            errors <-
                                $"Unsupported transaction type on line {transaction.LineNumber}: {transaction.TransactionType}"
                                :: errors

                    with ex ->
                        do
                            // CoreLogger.logErrorf
                            //     "DatabasePersistence"
                            //     "Error processing transaction line %d: %s"
                            //     transaction.LineNumber
                            //     ex.Message
                            ()

                        errors <-
                            $"Error processing transaction on line {transaction.LineNumber}: {ex.Message}"
                            :: errors

                    // CoreLogger.logDebugf
                    //     "DatabasePersistence"
                    //     "Completed processing transaction %d/%d"
                    //     (index + 1)
                    //     totalTransactions
                    ()

                // CoreLogger.logDebug "DatabasePersistence" "All transactions processed, finalizing"

                // Strike adjustments are already applied during trade creation (per-trade before save/link)
                // No post-processing needed - all trades in optionTrades already have adjusted strikes
                let adjustedOptionTrades = optionTrades

                // Final progress update
                ImportState.updateStatus (SavingToDatabase("Database save completed", 1.0))

                // CoreLogger.logInfof
                //     "DatabasePersistence"
                //     "Persistence complete. BrokerMovements=%d, OptionTrades=%d, StockTrades=%d, Dividends=%d, DividendTaxes=%d, Errors=%d"
                //     brokerMovements.Length
                //     adjustedOptionTrades.Length
                //     stockTrades.Length
                //     dividends.Length
                //     dividendTaxes.Length
                //     errors.Length

                // Create import metadata for targeted snapshot updates
                let oldestMovementDate =
                    if List.isEmpty movementDates then
                        None
                    else
                        Some(movementDates |> List.min)

                let importMetadata =
                    { OldestMovementDate = oldestMovementDate
                      AffectedBrokerAccountIds = Set.singleton brokerAccountId
                      AffectedTickerSymbols = affectedTickerSymbols
                      TotalMovementsImported =
                        brokerMovements.Length
                        + adjustedOptionTrades.Length
                        + stockTrades.Length
                        + dividends.Length
                        + dividendTaxes.Length }

                return
                    { BrokerMovementsCreated = brokerMovements.Length
                      OptionTradesCreated = adjustedOptionTrades.Length
                      StockTradesCreated = stockTrades.Length
                      DividendsCreated = dividends.Length + dividendTaxes.Length
                      ErrorsCount = errors.Length
                      Errors = List.rev errors
                      ImportMetadata = importMetadata }

            with
            | :? OperationCanceledException ->
                // CoreLogger.logWarning "DatabasePersistence" "Persistence cancelled by request"
                ImportState.updateStatus (SavingToDatabase("Database save cancelled", 0.0))

                return
                    { BrokerMovementsCreated = brokerMovements.Length
                      OptionTradesCreated = optionTrades.Length
                      StockTradesCreated = stockTrades.Length
                      DividendsCreated = dividends.Length + dividendTaxes.Length
                      ErrorsCount = 1
                      Errors = [ "Operation was cancelled" ]
                      ImportMetadata = ImportMetadata.createEmpty () }
            | ex ->
                // CoreLogger.logErrorf "DatabasePersistence" "Persistence failed: %s" ex.Message
                errors <- $"Database persistence failed: {ex.Message}" :: errors

                return
                    { BrokerMovementsCreated = brokerMovements.Length
                      OptionTradesCreated = optionTrades.Length
                      StockTradesCreated = stockTrades.Length
                      DividendsCreated = dividends.Length + dividendTaxes.Length
                      ErrorsCount = errors.Length
                      Errors = List.rev errors
                      ImportMetadata = ImportMetadata.createEmpty () }
        }
