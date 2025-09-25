namespace Binnaculum.Core.Import

open System
open System.Threading
open Binnaculum.Core
open Binnaculum.Core.Database
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns
open TastytradeModels

/// <summary>
/// Database persistence module for converting parsed import data into domain objects and saving to SQLite
/// </summary>
module DatabasePersistence =

    /// <summary>
    /// Result of database persistence operations with counts
    /// </summary>
    type PersistenceResult = {
        BrokerMovementsCreated: int
        OptionTradesCreated: int
        StockTradesCreated: int
        DividendsCreated: int
        ErrorsCount: int
        Errors: string list
    }

    /// <summary>
    /// Convert TastytradeTransaction to BrokerMovement domain object
    /// </summary>
    let private createBrokerMovementFromTransaction (transaction: TastytradeTransaction) (brokerAccountId: int) (currencyId: int) : BrokerMovement option =
        match transaction.TransactionType with
        | MoneyMovement(movementSubType) ->
            let movementType = 
                match movementSubType with
                | Deposit -> BrokerMovementType.Deposit
                | Withdrawal -> BrokerMovementType.Withdrawal
                | BalanceAdjustment -> BrokerMovementType.Fee  // Regulatory fees map to Fee type
                | CreditInterest -> BrokerMovementType.InterestsGained
                | Transfer -> BrokerMovementType.Deposit  // Default transfers to deposits
            
            Some {
                Id = 0  // Will be set by database
                TimeStamp = DateTimePattern.FromDateTime(transaction.Date)
                Amount = Money.FromAmount(Math.Abs(transaction.Value))  // Store as positive amount
                CurrencyId = currencyId
                BrokerAccountId = brokerAccountId
                Commissions = Money.FromAmount(Math.Abs(transaction.Commissions))
                Fees = Money.FromAmount(Math.Abs(transaction.Fees))
                MovementType = movementType
                Notes = Some transaction.Description
                FromCurrencyId = None  // TODO: Handle currency conversions
                AmountChanged = None
                TickerId = None
                Quantity = None
                Audit = AuditableEntity.FromDateTime(DateTime.UtcNow)
            }
        | _ -> None

    /// <summary>
    /// Convert TastytradeTransaction to OptionTrade domain object
    /// </summary>
    let private createOptionTradeFromTransaction (transaction: TastytradeTransaction) (brokerAccountId: int) (currencyId: int) (tickerId: int) : OptionTrade option =
        match transaction.TransactionType, transaction.InstrumentType with
        | Trade(_, _), Some "Equity Option" ->
            let optionCode = 
                match transaction.TransactionType with
                | Trade(BuyToOpen, _) -> OptionCode.BuyToOpen
                | Trade(SellToOpen, _) -> OptionCode.SellToOpen
                | Trade(BuyToClose, _) -> OptionCode.BuyToClose
                | Trade(SellToClose, _) -> OptionCode.SellToClose
                | _ -> OptionCode.BuyToOpen  // Default fallback
            
            let optionType = 
                match transaction.CallOrPut with
                | Some "CALL" -> OptionType.Call
                | Some "PUT" -> OptionType.Put
                | _ -> OptionType.Put  // Default fallback
            
            let expirationDate = 
                match transaction.ExpirationDate with
                | Some date -> DateTimePattern.FromDateTime(date)
                | None -> DateTimePattern.FromDateTime(transaction.Date.AddDays(30))  // Default fallback
            
            let premium = Math.Abs(transaction.Value)
            let netPremium = premium - Math.Abs(transaction.Commissions) - Math.Abs(transaction.Fees)
            let multiplier = transaction.Multiplier |> Option.defaultValue 100m
            let strike = transaction.StrikePrice |> Option.defaultValue 0m
            
            Some {
                Id = 0  // Will be set by database
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
                IsOpen = true  // TODO: Implement proper open/close tracking
                ClosedWith = None
                Multiplier = multiplier
                Notes = Some transaction.Description
                Audit = AuditableEntity.FromDateTime(DateTime.UtcNow)
            }
        | _ -> None

    /// <summary>
    /// Convert TastytradeTransaction to Trade domain object
    /// </summary>
    let private createTradeFromTransaction (transaction: TastytradeTransaction) (brokerAccountId: int) (currencyId: int) (tickerId: int) : Trade option =
        match transaction.TransactionType, transaction.InstrumentType with
        | Trade(_, _), Some "Equity" ->
            let tradeCode = 
                match transaction.TransactionType with
                | Trade(BuyToOpen, _) -> TradeCode.BuyToOpen
                | Trade(SellToOpen, _) -> TradeCode.SellToOpen
                | Trade(BuyToClose, _) -> TradeCode.BuyToClose
                | Trade(SellToClose, _) -> TradeCode.SellToClose
                | _ -> TradeCode.BuyToOpen  // Default fallback
            
            let tradeType = 
                match transaction.TransactionType with
                | Trade(BuyToOpen, _) | Trade(BuyToClose, _) -> TradeType.Long
                | Trade(SellToOpen, _) | Trade(SellToClose, _) -> TradeType.Short
                | _ -> TradeType.Long  // Default fallback
            
            let price = transaction.AveragePrice |> Option.defaultValue (Math.Abs(transaction.Value / transaction.Quantity))
            
            Some {
                Id = 0  // Will be set by database
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
                Leveraged = 1.0m  // Default to no leverage
                Notes = Some transaction.Description
                Audit = AuditableEntity.FromDateTime(DateTime.UtcNow)
            }
        | _ -> None

    /// <summary>
    /// Get or create currency ID for USD (default for TastyTrade)
    /// </summary>
    let private getUSDCurrencyId() = task {
        let! currencies = CurrencyExtensions.Do.getAll() |> Async.AwaitTask
        let usdCurrency = currencies |> List.tryFind (fun c -> c.Code = "USD")
        match usdCurrency with
        | Some currency -> return currency.Id
        | None -> 
            // Create USD currency if it doesn't exist
            let newCurrency = {
                Id = 0
                Name = "US Dollar"
                Code = "USD"
                Symbol = "$"
            }
            do! CurrencyExtensions.Do.save(newCurrency) |> Async.AwaitTask
            let! allCurrencies = CurrencyExtensions.Do.getAll() |> Async.AwaitTask
            let createdCurrency = allCurrencies |> List.find (fun c -> c.Code = "USD")
            return createdCurrency.Id
    }

    /// <summary>
    /// Get or create ticker ID for a given symbol
    /// </summary>
    let private getOrCreateTickerId(symbol: string) = task {
        let! tickers = TickerExtensions.Do.getAll() |> Async.AwaitTask
        let existingTicker = tickers |> List.tryFind (fun t -> t.Symbol = symbol)
        match existingTicker with
        | Some ticker -> return ticker.Id
        | None ->
            // Create new ticker
            let newTicker = {
                Id = 0
                Symbol = symbol
                Image = None
                Name = Some symbol
                Audit = AuditableEntity.FromDateTime(DateTime.UtcNow)
            }
            do! TickerExtensions.Do.save(newTicker) |> Async.AwaitTask
            let! allTickers = TickerExtensions.Do.getAll() |> Async.AwaitTask
            let createdTicker = allTickers |> List.find (fun t -> t.Symbol = symbol)
            return createdTicker.Id
    }

    /// <summary>
    /// Convert parsed TastytradeTransactions to domain objects and persist to database
    /// </summary>
    let persistTransactionsToDatabase (transactions: TastytradeTransaction list) (brokerAccountId: int) (cancellationToken: CancellationToken) = task {
        let mutable brokerMovements = []
        let mutable optionTrades = []
        let mutable stockTrades = []
        let mutable dividends = []  // Not implemented yet for Tastytrade
        let mutable errors = []

        try
            // Get USD currency ID (Tastytrade default)
            let! currencyId = getUSDCurrencyId()

            // Process each transaction
            for (index, transaction) in transactions |> List.mapi (fun i t -> i, t) do
                cancellationToken.ThrowIfCancellationRequested()
                
                // Update progress
                let progress = float index / float transactions.Length
                ImportState.updateStatus(SavingToDatabase($"Saving transaction {index + 1} of {transactions.Length}", progress))

                try
                    match transaction.TransactionType with
                    | MoneyMovement(_) ->
                        match createBrokerMovementFromTransaction transaction brokerAccountId currencyId with
                        | Some brokerMovement ->
                            do! BrokerMovementExtensions.Do.save(brokerMovement) |> Async.AwaitTask
                            brokerMovements <- brokerMovement :: brokerMovements
                        | None ->
                            errors <- $"Failed to create BrokerMovement from line {transaction.LineNumber}" :: errors

                    | Trade(_, _) when transaction.InstrumentType = Some "Equity Option" ->
                        // Get ticker ID for the underlying symbol
                        let underlyingSymbol = transaction.UnderlyingSymbol |> Option.defaultValue "UNKNOWN"
                        let! tickerId = getOrCreateTickerId(underlyingSymbol)
                        
                        match createOptionTradeFromTransaction transaction brokerAccountId currencyId tickerId with
                        | Some optionTrade ->
                            do! OptionTradeExtensions.Do.save(optionTrade) |> Async.AwaitTask
                            optionTrades <- optionTrade :: optionTrades
                        | None ->
                            errors <- $"Failed to create OptionTrade from line {transaction.LineNumber}" :: errors

                    | Trade(_, _) when transaction.InstrumentType = Some "Equity" ->
                        // Get ticker ID for the stock symbol
                        let stockSymbol = transaction.Symbol |> Option.defaultValue "UNKNOWN"
                        let! tickerId = getOrCreateTickerId(stockSymbol)
                        
                        match createTradeFromTransaction transaction brokerAccountId currencyId tickerId with
                        | Some stockTrade ->
                            do! TradeExtensions.Do.save(stockTrade) |> Async.AwaitTask
                            stockTrades <- stockTrade :: stockTrades
                        | None ->
                            errors <- $"Failed to create Trade from line {transaction.LineNumber}" :: errors

                    | _ ->
                        errors <- $"Unsupported transaction type on line {transaction.LineNumber}: {transaction.TransactionType}" :: errors

                with
                | ex ->
                    errors <- $"Error processing transaction on line {transaction.LineNumber}: {ex.Message}" :: errors

            // Final progress update
            ImportState.updateStatus(SavingToDatabase("Database save completed", 1.0))

            return {
                BrokerMovementsCreated = brokerMovements.Length
                OptionTradesCreated = optionTrades.Length
                StockTradesCreated = stockTrades.Length
                DividendsCreated = dividends.Length
                ErrorsCount = errors.Length
                Errors = List.rev errors
            }

        with
        | :? OperationCanceledException ->
            ImportState.updateStatus(SavingToDatabase("Database save cancelled", 0.0))
            return {
                BrokerMovementsCreated = brokerMovements.Length
                OptionTradesCreated = optionTrades.Length
                StockTradesCreated = stockTrades.Length
                DividendsCreated = dividends.Length
                ErrorsCount = 1
                Errors = ["Operation was cancelled"]
            }
        | ex ->
            errors <- $"Database persistence failed: {ex.Message}" :: errors
            return {
                BrokerMovementsCreated = brokerMovements.Length
                OptionTradesCreated = optionTrades.Length
                StockTradesCreated = stockTrades.Length
                DividendsCreated = dividends.Length
                ErrorsCount = errors.Length
                Errors = List.rev errors
            }
    }