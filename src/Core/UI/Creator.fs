namespace Binnaculum.Core.UI
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.ModelsToDatabase
open System
open Binnaculum.Core.Patterns
open Binnaculum.Core.Storage
open Microsoft.FSharp.Core
open TickerExtensions
open TickerSnapshotExtensions
open BrokerAccountExtensions
open Binnaculum.Core.DataLoader

/// <summary>
/// This module handles user-initiated save operations from the UI layer.
/// It orchestrates model validation, database persistence via Saver, and snapshot updates.
/// This ensures snapshots are only updated for user actions, not during batch imports.
/// </summary>
module Creator =
    
    let SaveBank(bank: Binnaculum.Core.Models.Bank) = task {
        let! databaseBank = bank.bankToDatabase() |> Async.AwaitTask
        do! Saver.saveBank(databaseBank) |> Async.AwaitTask |> Async.Ignore
    }

    let SaveBroker(broker: Binnaculum.Core.Models.Broker) = task {
        let! databaseBroker = broker.brokerToDatabase() |> Async.AwaitTask
        do! Saver.saveBroker(databaseBroker) |> Async.AwaitTask |> Async.Ignore
    }

    let SaveBankAccount(bankAccount: Binnaculum.Core.Models.BankAccount) = task {
        let! databaseBankAccount = bankAccount.bankAccountToDatabase() |> Async.AwaitTask
        let isNewAccount = databaseBankAccount.Id = 0
        do! Saver.saveBankAccount(databaseBankAccount) |> Async.AwaitTask |> Async.Ignore
        
        // If it's a new account, create initial snapshots
        if isNewAccount then
            do! SnapshotManager.handleNewBankAccount(databaseBankAccount) |> Async.AwaitTask |> Async.Ignore
    }

    let SaveBrokerAccount(brokerId: int, accountNumber: string) = task {
        let audit = { CreatedAt = Some(DateTimePattern.FromDateTime(DateTime.Now)); UpdatedAt = None }
        let account = { Id = 0; BrokerId = brokerId; AccountNumber = accountNumber; Audit = audit }
        let isNewAccount = account.Id = 0
        do! Saver.saveBrokerAccount(account) |> Async.AwaitTask |> Async.Ignore
        
        // If it's a new account, create initial snapshots
        if isNewAccount then
            // Get the saved account from database to get the assigned ID
            let! savedAccounts = BrokerAccountExtensions.Do.getAll() |> Async.AwaitTask
            match savedAccounts |> List.tryFind (fun a -> a.BrokerId = brokerId && a.AccountNumber = accountNumber) with
            | Some databaseAccount -> 
                do! SnapshotManager.handleNewBrokerAccount(databaseAccount) |> Async.AwaitTask |> Async.Ignore
            | None ->
                failwithf "Failed to retrieve saved broker account for broker %d with account number %s" brokerId accountNumber
    }

    let SaveBrokerMovement(movement: Binnaculum.Core.Models.BrokerMovement) = task {
        // Validate FromCurrency based on MovementType
        match movement.MovementType, movement.FromCurrency with
        | Binnaculum.Core.Models.BrokerMovementType.Conversion, None ->
            failwith "FromCurrency is required when MovementType is Conversion"
        | Binnaculum.Core.Models.BrokerMovementType.Conversion, Some _ ->
            () // Valid: Conversion with FromCurrency
        | _, Some _ ->
            failwith "FromCurrency should only be set when MovementType is Conversion"
        | _, None ->
            () // Valid: Non-conversion without FromCurrency
        
        // Set default AmountChanged for Conversion movements if not provided
        let movementWithDefaults = 
            match movement.MovementType, movement.AmountChanged with
            | Binnaculum.Core.Models.BrokerMovementType.Conversion, None ->
                // Set default value for AmountChanged (using the same amount as the main Amount for now)
                { movement with AmountChanged = Some movement.Amount }
            | _ -> movement
        
        let databaseModel = movementWithDefaults.brokerMovementToDatabase()
        do! Saver.saveBrokerMovement(databaseModel) |> Async.AwaitTask |> Async.Ignore
        
        // Update snapshots for this movement using reactive manager
        let datePattern = DateTimePattern.FromDateTime(movement.TimeStamp)
        do! BrokerAccountSnapshotManager.handleBrokerAccountChange(movement.BrokerAccount.Id, datePattern) |> Async.AwaitTask |> Async.Ignore
        ReactiveSnapshotManager.refresh()
    }

    let SaveBankMovement(movement: Binnaculum.Core.Models.BankAccountMovement) = task {
        let databaseModel = movement.bankAccountMovementToDatabase()
        do! Saver.saveBankMovement(databaseModel) |> Async.AwaitTask |> Async.Ignore
        
        // Update snapshots for this movement
        do! SnapshotManager.handleBankMovementSnapshot(databaseModel) |> Async.AwaitTask |> Async.Ignore
    }

    /// <summary>
    /// Save a new or updated ticker and create initial snapshot for new tickers.
    /// </summary>
    let SaveTicker(ticker: Binnaculum.Core.Models.Ticker) = task {
        let! databaseTicker = ticker.tickerToDatabase() |> Async.AwaitTask
        let isNewTicker = databaseTicker.Id = 0
        do! Saver.saveTicker(databaseTicker) |> Async.AwaitTask |> Async.Ignore
        
        // If it's a new ticker, create initial snapshot
        if isNewTicker then
            // Get the saved ticker from database by symbol to get the assigned ID
            let! savedTicker = TickerExtensions.Do.getBySymbol(databaseTicker.Symbol)
            match savedTicker with
            | Some ticker -> 
                do! TickerSnapshotManager.handleNewTicker(ticker) |> Async.AwaitTask |> Async.Ignore
                do! TickerSnapshotLoader.load() |> Async.AwaitTask |> Async.Ignore
            | None ->
                failwithf "Failed to retrieve saved ticker with symbol %s" databaseTicker.Symbol
    }

    /// <summary>
    /// Save a new or updated trade and refresh the trades collection.
    /// </summary>
    let SaveTrade(trade: Binnaculum.Core.Models.Trade) = task {
        let databaseTrade = trade.tradeToDatabase()
        do! Saver.saveTrade(databaseTrade) |> Async.AwaitTask |> Async.Ignore
        
        // Update snapshots for this trade using reactive manager
        let datePattern = DateTimePattern.FromDateTime(trade.TimeStamp)
        do! BrokerAccountSnapshotManager.handleBrokerAccountChange(trade.BrokerAccount.Id, datePattern) |> Async.AwaitTask |> Async.Ignore
        ReactiveSnapshotManager.refresh()
    }

    /// <summary>
    /// Save a new or updated dividend and refresh the dividends collection.
    /// </summary>
    let SaveDividend(dividend: Binnaculum.Core.Models.Dividend) = task {
        let databaseDividend = dividend.dividendReceivedToDatabase()
        do! Saver.saveDividend(databaseDividend) |> Async.AwaitTask |> Async.Ignore
        
        // Update snapshots for this dividend using reactive manager
        let datePattern = DateTimePattern.FromDateTime(dividend.TimeStamp)
        do! BrokerAccountSnapshotManager.handleBrokerAccountChange(dividend.BrokerAccount.Id, datePattern) |> Async.AwaitTask |> Async.Ignore
        ReactiveSnapshotManager.refresh()
    }

    /// <summary>
    /// Save a new or updated dividend date and refresh the dividend dates collection.
    /// </summary>
    let SaveDividendDate(dividendDate: Binnaculum.Core.Models.DividendDate) = task {
        let databaseModel = dividendDate.dividendDateToDatabase()
        do! Saver.saveDividendDate(databaseModel) |> Async.AwaitTask |> Async.Ignore
        
        // Update snapshots for this dividend date using reactive manager
        let datePattern = DateTimePattern.FromDateTime(dividendDate.TimeStamp)
        do! BrokerAccountSnapshotManager.handleBrokerAccountChange(dividendDate.BrokerAccount.Id, datePattern) |> Async.AwaitTask |> Async.Ignore
        ReactiveSnapshotManager.refresh()
    }

    /// <summary>
    /// Save a new or updated dividend tax and refresh the dividend taxes collection.
    /// </summary>
    let SaveDividendTax(dividendTax: Binnaculum.Core.Models.DividendTax) = task {
        let databaseModel = dividendTax.dividendTaxToDatabase()
        do! Saver.saveDividendTax(databaseModel) |> Async.AwaitTask |> Async.Ignore
        
        // Update snapshots for this dividend tax using reactive manager
        let datePattern = DateTimePattern.FromDateTime(dividendTax.TimeStamp)
        do! BrokerAccountSnapshotManager.handleBrokerAccountChange(dividendTax.BrokerAccount.Id, datePattern) |> Async.AwaitTask |> Async.Ignore
        ReactiveSnapshotManager.refresh()
    }

    /// <summary>
    /// Save option trades and refresh the movements collection.
    /// </summary>
    let SaveOptionsTrade(optionTrades: Binnaculum.Core.Models.OptionTrade list) = task {
        // Expand trades with quantity > 1 into multiple trades with quantity = 1
        let expandedTrades = 
            optionTrades 
            |> List.collect (fun trade ->
                if trade.Quantity > 1 then
                    let netPremium = trade.NetPremium / decimal trade.Quantity
                    [ for _ in 1 .. trade.Quantity -> { trade with Quantity = 1; NetPremium = netPremium;  } ]
                else
                    [trade]
            )
        
        let databaseModels = expandedTrades.optionTradesToDatabase()
        do! Saver.saveOptionsTrade(databaseModels) |> Async.AwaitTask |> Async.Ignore
        
        // Update snapshots for all affected broker accounts using reactive manager
        let uniqueBrokerAccountDates = 
            expandedTrades
            |> List.map (fun trade -> (trade.BrokerAccount.Id, trade.TimeStamp))
            |> List.distinct
        
        for (brokerAccountId, timeStamp) in uniqueBrokerAccountDates do
            let datePattern = DateTimePattern.FromDateTime(timeStamp)
            do! BrokerAccountSnapshotManager.handleBrokerAccountChange(brokerAccountId, datePattern) |> Async.AwaitTask |> Async.Ignore
            
        ReactiveSnapshotManager.refresh()
    }

    let UpdateOptionsTimestampNotesAndMultiplier(timestamp: DateTime, notes: string option, multiplier: decimal, trade: Binnaculum.Core.Models.OptionTrade) =
        {   trade with TimeStamp = timestamp; Multiplier = multiplier; Notes = notes }

    let GetBrokerMovementType(uiSelectedType: Binnaculum.Core.Models.MovementType option) =
        match uiSelectedType with
        | None -> None
        | Some selected ->
            match selected with
            | Binnaculum.Core.Models.MovementType.Deposit 
                -> Some Binnaculum.Core.Models.BrokerMovementType.Deposit
            | Binnaculum.Core.Models.MovementType.Withdrawal 
                -> Some Binnaculum.Core.Models.BrokerMovementType.Withdrawal
            | Binnaculum.Core.Models.MovementType.Fee 
                -> Some Binnaculum.Core.Models.BrokerMovementType.Fee
            | Binnaculum.Core.Models.MovementType.InterestsGained 
                -> Some Binnaculum.Core.Models.BrokerMovementType.InterestsGained
            | Binnaculum.Core.Models.MovementType.Lending 
                -> Some Binnaculum.Core.Models.BrokerMovementType.Lending
            | Binnaculum.Core.Models.MovementType.ACATMoneyTransferSent 
                -> Some Binnaculum.Core.Models.BrokerMovementType.ACATMoneyTransferSent
            | Binnaculum.Core.Models.MovementType.ACATMoneyTransferReceived 
                -> Some Binnaculum.Core.Models.BrokerMovementType.ACATMoneyTransferReceived
            | Binnaculum.Core.Models.MovementType.ACATSecuritiesTransferSent 
                -> Some Binnaculum.Core.Models.BrokerMovementType.ACATSecuritiesTransferSent
            | Binnaculum.Core.Models.MovementType.ACATSecuritiesTransferReceived 
                -> Some Binnaculum.Core.Models.BrokerMovementType.ACATSecuritiesTransferReceived
            | Binnaculum.Core.Models.MovementType.InterestsPaid 
                -> Some Binnaculum.Core.Models.BrokerMovementType.InterestsPaid
            | Binnaculum.Core.Models.MovementType.Conversion 
                -> Some Binnaculum.Core.Models.BrokerMovementType.Conversion
            | _ -> None
