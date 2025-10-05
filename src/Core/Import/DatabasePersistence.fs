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
        CoreLogger.logDebugf
            "DatabasePersistence"
            "orderTransactionsForPersistence: Starting to sort %d transactions"
            transactions.Length

        let sorted =
            transactions
            |> List.sortBy (fun t -> t.Date, getTransactionProcessingPriority t, t.LineNumber)

        CoreLogger.logDebugf
            "DatabasePersistence"
            "orderTransactionsForPersistence: Sorting completed, returning %d transactions"
            sorted.Length

        sorted

    /// <summary>
    /// Convert TastytradeTransaction to BrokerMovement domain object
    /// </summary>
    let private createBrokerMovementFromTransaction
        (transaction: TastytradeTransaction)
        (brokerAccountId: int)
        (currencyId: int)
        : BrokerMovement option =
        CoreLogger.logDebugf
            "DatabasePersistence"
            "createBrokerMovementFromTransaction: Starting with transaction type: %A"
            transaction.TransactionType

        match transaction.TransactionType with
        | MoneyMovement(movementSubType) ->
            CoreLogger.logDebugf
                "DatabasePersistence"
                "createBrokerMovementFromTransaction: Processing MoneyMovement subtype: %A"
                movementSubType

            let movementType =
                match movementSubType with
                | Deposit ->
                    CoreLogger.logDebug
                        "DatabasePersistence"
                        "createBrokerMovementFromTransaction: Mapping to BrokerMovementType.Deposit"

                    BrokerMovementType.Deposit
                | Withdrawal ->
                    CoreLogger.logDebug
                        "DatabasePersistence"
                        "createBrokerMovementFromTransaction: Mapping to BrokerMovementType.Withdrawal"

                    BrokerMovementType.Withdrawal
                | BalanceAdjustment ->
                    CoreLogger.logDebug
                        "DatabasePersistence"
                        "createBrokerMovementFromTransaction: Mapping to BrokerMovementType.Fee"

                    BrokerMovementType.Fee // Regulatory fees map to Fee type
                | CreditInterest ->
                    CoreLogger.logDebug
                        "DatabasePersistence"
                        "createBrokerMovementFromTransaction: Mapping to BrokerMovementType.InterestsGained"

                    BrokerMovementType.InterestsGained
                | Transfer ->
                    CoreLogger.logDebug
                        "DatabasePersistence"
                        "createBrokerMovementFromTransaction: Mapping to BrokerMovementType.Deposit"

                    BrokerMovementType.Deposit // Default transfers to deposits

            CoreLogger.logDebug
                "DatabasePersistence"
                "createBrokerMovementFromTransaction: About to create BrokerMovement object"

            CoreLogger.logDebugf
                "DatabasePersistence"
                "createBrokerMovementFromTransaction: Transaction date: %A"
                transaction.Date

            CoreLogger.logDebugf
                "DatabasePersistence"
                "createBrokerMovementFromTransaction: Transaction value: %M"
                transaction.Value

            CoreLogger.logDebugf
                "DatabasePersistence"
                "createBrokerMovementFromTransaction: Transaction commissions: %M"
                transaction.Commissions

            CoreLogger.logDebugf
                "DatabasePersistence"
                "createBrokerMovementFromTransaction: Transaction fees: %M"
                transaction.Fees

            CoreLogger.logDebug
                "DatabasePersistence"
                "createBrokerMovementFromTransaction: Creating DateTimePattern from transaction date"

            let timeStamp = DateTimePattern.FromDateTime(transaction.Date)

            CoreLogger.logDebug
                "DatabasePersistence"
                "createBrokerMovementFromTransaction: DateTimePattern created successfully"

            CoreLogger.logDebug "DatabasePersistence" "createBrokerMovementFromTransaction: Creating Money objects"
            let amount = Money.FromAmount(Math.Abs(transaction.Value))
            CoreLogger.logDebug "DatabasePersistence" "createBrokerMovementFromTransaction: Amount Money object created"
            let commissions = Money.FromAmount(Math.Abs(transaction.Commissions))

            CoreLogger.logDebug
                "DatabasePersistence"
                "createBrokerMovementFromTransaction: Commissions Money object created"

            let fees = Money.FromAmount(Math.Abs(transaction.Fees))
            CoreLogger.logDebug "DatabasePersistence" "createBrokerMovementFromTransaction: Fees Money object created"

            CoreLogger.logDebug "DatabasePersistence" "createBrokerMovementFromTransaction: Creating AuditableEntity"
            let audit = AuditableEntity.FromDateTime(DateTime.UtcNow)

            CoreLogger.logDebug
                "DatabasePersistence"
                "createBrokerMovementFromTransaction: AuditableEntity created successfully"

            CoreLogger.logDebug
                "DatabasePersistence"
                "createBrokerMovementFromTransaction: Creating BrokerMovement record"

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

            CoreLogger.logDebug
                "DatabasePersistence"
                "createBrokerMovementFromTransaction: BrokerMovement record created successfully"

            Some brokerMovement
        | _ ->
            CoreLogger.logDebug
                "DatabasePersistence"
                "createBrokerMovementFromTransaction: Non-MoneyMovement transaction type, returning None"

            None

    /// <summary>
    /// Convert TastytradeTransaction to OptionTrade domain object
    /// </summary>
    let private createOptionTradeFromTransaction
        (transaction: TastytradeTransaction)
        (brokerAccountId: int)
        (currencyId: int)
        (tickerId: int)
        : OptionTrade option =
        match transaction.TransactionType, transaction.InstrumentType with
        | Trade(_, _), Some "Equity Option" ->
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

            Some
                { Id = 0 // Will be set by database
                  TimeStamp = DateTimePattern.FromDateTime(transaction.Date)
                  ExpirationDate = expirationDate
                  Premium = Money.FromAmount(premium)
                  NetPremium = Money.FromAmount(netPremium)
                  TickerId = tickerId
                  BrokerAccountId = brokerAccountId
                  CurrencyId = currencyId
                  OptionType = optionType
                  Code = optionCode
                  Strike = Money.FromAmount(strike)
                  Commissions = Money.FromAmount(Math.Abs(transaction.Commissions))
                  Fees = Money.FromAmount(Math.Abs(transaction.Fees))
                  IsOpen = isOpeningCode optionCode
                  ClosedWith = None
                  Multiplier = multiplier
                  Notes = Some transaction.Description
                  Audit = AuditableEntity.FromDateTime(DateTime.UtcNow) }
        | _ -> None

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
    /// Convert parsed TastytradeTransactions to domain objects and persist to database
    /// </summary>
    let persistTransactionsToDatabase
        (transactions: TastytradeTransaction list)
        (brokerAccountId: int)
        (cancellationToken: CancellationToken)
        =
        task {
            do
                CoreLogger.logInfof
                    "DatabasePersistence"
                    "Persisting %d transactions for account %d"
                    transactions.Length
                    brokerAccountId

            let mutable brokerMovements = []
            let mutable optionTrades = []
            let mutable stockTrades = []
            let mutable dividends = [] // Not implemented yet for Tastytrade
            let mutable errors = []

            // Collect metadata for targeted snapshot updates
            let mutable affectedTickerSymbols = Set.empty<string>
            let mutable movementDates = []

            try
                CoreLogger.logDebugf
                    "DatabasePersistence"
                    "About to order %d transactions for persistence"
                    transactions.Length

                let orderedTransactions = orderTransactionsForPersistence transactions
                let totalTransactions = orderedTransactions.Length

                CoreLogger.logDebugf
                    "DatabasePersistence"
                    "Transactions ordered successfully; total=%d"
                    totalTransactions

                CoreLogger.logDebug "DatabasePersistence" "Starting transaction processing loop"

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
                        CoreLogger.logDebugf
                            "DatabasePersistence"
                            "Processing transaction %d/%d: line=%d, type=%A"
                            (index + 1)
                            totalTransactions
                            transaction.LineNumber
                            transaction.TransactionType
                        // Get currency ID for this transaction (with USD fallback)
                        let currencyCode =
                            if String.IsNullOrWhiteSpace(transaction.Currency) then
                                "USD"
                            else
                                transaction.Currency

                        CoreLogger.logDebugf "DatabasePersistence" "Getting currency ID for: %s" currencyCode
                        let! currencyId = getCurrencyId (currencyCode)
                        CoreLogger.logDebugf "DatabasePersistence" "Got currency ID: %d for %s" currencyId currencyCode

                        // Collect movement date for metadata
                        movementDates <- transaction.Date :: movementDates

                        CoreLogger.logDebugf
                            "DatabasePersistence"
                            "Processing transaction type: %A"
                            transaction.TransactionType

                        match transaction.TransactionType with
                        | MoneyMovement(_) ->
                            CoreLogger.logDebug "DatabasePersistence" "Creating BrokerMovement from transaction"

                            match createBrokerMovementFromTransaction transaction brokerAccountId currencyId with
                            | Some brokerMovement ->
                                CoreLogger.logDebug "DatabasePersistence" "Saving BrokerMovement to database"
                                do! BrokerMovementExtensions.Do.save (brokerMovement) |> Async.AwaitTask
                                CoreLogger.logDebug "DatabasePersistence" "BrokerMovement saved successfully"
                                brokerMovements <- brokerMovement :: brokerMovements

                                CoreLogger.logDebug
                                    "DatabasePersistence"
                                    "BrokerMovement added to collection, continuing to next step"
                            | None ->
                                errors <-
                                    $"Failed to create BrokerMovement from line {transaction.LineNumber}" :: errors

                        | Trade(_, _) when transaction.InstrumentType = Some "Equity Option" ->
                            // Get ticker ID for the underlying symbol
                            let underlyingSymbol = transaction.UnderlyingSymbol |> Option.defaultValue "UNKNOWN"
                            let! tickerId = getOrCreateTickerId (underlyingSymbol)

                            // Add to affected tickers for metadata
                            affectedTickerSymbols <- Set.add underlyingSymbol affectedTickerSymbols

                            match createOptionTradeFromTransaction transaction brokerAccountId currencyId tickerId with
                            | Some optionTrade ->
                                let! persistedTrade = OptionTradeExtensions.Do.saveAndReturn (optionTrade)
                                optionTrades <- persistedTrade :: optionTrades

                                if isClosingCode persistedTrade.Code then
                                    let! linkResult = OptionTradeExtensions.Do.linkClosingTrade (persistedTrade)

                                    match linkResult with
                                    | Ok _ -> ()
                                    | Error message -> errors <- message :: errors
                            | None ->
                                errors <- $"Failed to create OptionTrade from line {transaction.LineNumber}" :: errors

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
                            CoreLogger.logDebugf
                                "DatabasePersistence"
                                "Processing ACAT equity transfer for %A"
                                transaction.Symbol

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

                            CoreLogger.logDebugf
                                "DatabasePersistence"
                                "ACAT equity transfer saved: %s, quantity=%M"
                                stockSymbol
                                transaction.Quantity

                        | _ ->
                            errors <-
                                $"Unsupported transaction type on line {transaction.LineNumber}: {transaction.TransactionType}"
                                :: errors

                    with ex ->
                        do
                            CoreLogger.logErrorf
                                "DatabasePersistence"
                                "Error processing transaction line %d: %s"
                                transaction.LineNumber
                                ex.Message

                        errors <-
                            $"Error processing transaction on line {transaction.LineNumber}: {ex.Message}"
                            :: errors

                    CoreLogger.logDebugf
                        "DatabasePersistence"
                        "Completed processing transaction %d/%d"
                        (index + 1)
                        totalTransactions

                CoreLogger.logDebug "DatabasePersistence" "All transactions processed, finalizing"
                // Final progress update
                ImportState.updateStatus (SavingToDatabase("Database save completed", 1.0))

                do
                    CoreLogger.logInfof
                        "DatabasePersistence"
                        "Persistence complete. BrokerMovements=%d, OptionTrades=%d, StockTrades=%d, Errors=%d"
                        brokerMovements.Length
                        optionTrades.Length
                        stockTrades.Length
                        errors.Length

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
                        + optionTrades.Length
                        + stockTrades.Length
                        + dividends.Length }

                return
                    { BrokerMovementsCreated = brokerMovements.Length
                      OptionTradesCreated = optionTrades.Length
                      StockTradesCreated = stockTrades.Length
                      DividendsCreated = dividends.Length
                      ErrorsCount = errors.Length
                      Errors = List.rev errors
                      ImportMetadata = importMetadata }

            with
            | :? OperationCanceledException ->
                do CoreLogger.logWarning "DatabasePersistence" "Persistence cancelled by request"
                ImportState.updateStatus (SavingToDatabase("Database save cancelled", 0.0))

                return
                    { BrokerMovementsCreated = brokerMovements.Length
                      OptionTradesCreated = optionTrades.Length
                      StockTradesCreated = stockTrades.Length
                      DividendsCreated = dividends.Length
                      ErrorsCount = 1
                      Errors = [ "Operation was cancelled" ]
                      ImportMetadata = ImportMetadata.createEmpty () }
            | ex ->
                do CoreLogger.logErrorf "DatabasePersistence" "Persistence failed: %s" ex.Message
                errors <- $"Database persistence failed: {ex.Message}" :: errors

                return
                    { BrokerMovementsCreated = brokerMovements.Length
                      OptionTradesCreated = optionTrades.Length
                      StockTradesCreated = stockTrades.Length
                      DividendsCreated = dividends.Length
                      ErrorsCount = errors.Length
                      Errors = List.rev errors
                      ImportMetadata = ImportMetadata.createEmpty () }
        }
