namespace Binnaculum.Core.UI

open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.ModelsToDatabase
open System
open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging
open Binnaculum.Core.Storage
open Microsoft.FSharp.Core
open Binnaculum.Core.DataLoader

/// <summary>
/// This module handles user-initiated save operations from the UI layer.
/// It orchestrates model validation, database persistence via Saver, and snapshot updates.
/// This ensures snapshots are only updated for user actions, not during batch imports.
/// </summary>
module Creator =

    let SaveBank (bank: Binnaculum.Core.Models.Bank) =
        task {
            let! databaseBank = bank.bankToDatabase () |> Async.AwaitTask
            do! Saver.saveBank (databaseBank) |> Async.AwaitTask |> Async.Ignore
        }

    let SaveBroker (broker: Binnaculum.Core.Models.Broker) =
        task {
            let! databaseBroker = broker.brokerToDatabase () |> Async.AwaitTask
            do! Saver.saveBroker (databaseBroker) |> Async.AwaitTask |> Async.Ignore
        }

    let SaveBankAccount (bankAccount: Binnaculum.Core.Models.BankAccount) =
        task {
            let! databaseBankAccount = bankAccount.bankAccountToDatabase () |> Async.AwaitTask
            let isNewAccount = databaseBankAccount.Id = 0
            do! Saver.saveBankAccount (databaseBankAccount) |> Async.AwaitTask |> Async.Ignore

            // If it's a new account, create initial snapshots
            if isNewAccount then
                do!
                    SnapshotManager.handleNewBankAccount (databaseBankAccount)
                    |> Async.AwaitTask
                    |> Async.Ignore
        }

    let SaveBrokerAccount (brokerId: int, accountNumber: string) =
        task {
            let audit =
                { CreatedAt = Some(DateTimePattern.FromDateTime(DateTime.Now))
                  UpdatedAt = None }

            let account =
                { Id = 0
                  BrokerId = brokerId
                  AccountNumber = accountNumber
                  Audit = audit }

            let isNewAccount = account.Id = 0
            do! Saver.saveBrokerAccount (account) |> Async.AwaitTask |> Async.Ignore

            // If it's a new account, create initial snapshots
            if isNewAccount then
                // Get the saved account from database to get the assigned ID
                let! savedAccounts = BrokerAccountExtensions.Do.getAll () |> Async.AwaitTask

                match
                    savedAccounts
                    |> List.tryFind (fun a -> a.BrokerId = brokerId && a.AccountNumber = accountNumber)
                with
                | Some databaseAccount ->
                    do!
                        SnapshotManager.handleNewBrokerAccount (databaseAccount)
                        |> Async.AwaitTask
                        |> Async.Ignore
                | None ->
                    failwithf
                        "Failed to retrieve saved broker account for broker %d with account number %s"
                        brokerId
                        accountNumber
        }

    let SaveBrokerMovement (movement: Binnaculum.Core.Models.BrokerMovement) =
        task {
            try
                // CoreLogger.logDebugf "Creator" "SaveBrokerMovement ENTRY - Amount: %A, Type: %A, BrokerAccountId: %A, Date: %A" movement.Amount movement.MovementType movement.BrokerAccount.Id movement.TimeStamp

                // Validate FromCurrency based on MovementType
                // CoreLogger.logDebug "Creator" "Validating movement type and FromCurrency..."

                match movement.MovementType, movement.FromCurrency with
                | Binnaculum.Core.Models.BrokerMovementType.Conversion, None ->
                    failwith "FromCurrency is required when MovementType is Conversion"
                | Binnaculum.Core.Models.BrokerMovementType.Conversion, Some _ -> () // Valid: Conversion with FromCurrency
                | _, Some _ -> failwith "FromCurrency should only be set when MovementType is Conversion"
                | _, None -> () // Valid: Non-conversion without FromCurrency

                // CoreLogger.logDebug "Creator" "Movement validation passed"

                // Set default AmountChanged for Conversion movements if not provided
                let movementWithDefaults =
                    match movement.MovementType, movement.AmountChanged with
                    | Binnaculum.Core.Models.BrokerMovementType.Conversion, None ->
                        // Set default value for AmountChanged (using the same amount as the main Amount for now)
                        { movement with
                            AmountChanged = Some movement.Amount }
                    | _ -> movement

                // CoreLogger.logDebug "Creator" "Converting movement to database model..."
                let databaseModel = movementWithDefaults.brokerMovementToDatabase ()
                // CoreLogger.logDebug "Creator" "Database model created, saving movement..."
                do! Saver.saveBrokerMovement (databaseModel) |> Async.AwaitTask
                // CoreLogger.logDebug "Creator" "Movement saved to database successfully"

                // Update snapshots for this movement using coordinator (batch mode if enabled)
                let movementDatePattern = DateTimePattern.FromDateTime(movement.TimeStamp)

                // CoreLogger.logDebugf "Creator" "SaveBrokerMovement - About to update snapshots for movement date: %A, Amount: %A, Type: %A" movementDatePattern movement.Amount movement.MovementType

                do!
                    SnapshotProcessingCoordinator.handleBrokerAccountChange (
                        movement.BrokerAccount.Id,
                        movementDatePattern
                    )
                    |> Async.AwaitTask

                // CoreLogger.logDebug "Creator" "SaveBrokerMovement - Historical movement date snapshot update completed"

                // If this is a historical movement (not today), also update today's snapshot to reflect the new data
                let today = DateTime.Now.Date
                let movementDate = movement.TimeStamp.Date

                // CoreLogger.logDebugf "Creator" "Checking if historical movement - Movement date: %A, Today: %A" movementDate today

                if movementDate < today then
                    // CoreLogger.logDebugf "Creator" "*** HISTORICAL MOVEMENT DETECTED *** - Movement date: %A, Today: %A" movementDate today

                    // CoreLogger.logDebugf "Creator" "About to update today's snapshot to reflect historical deposit of %A" movement.Amount

                    let todayPattern = DateTimePattern.FromDateTime(today.AddDays(1).AddTicks(-1)) // End of today
                    // CoreLogger.logDebugf "Creator" "Today pattern calculated: %A" todayPattern

                    do!
                        SnapshotProcessingCoordinator.handleBrokerAccountChange (
                            movement.BrokerAccount.Id,
                            todayPattern
                        )
                        |> Async.AwaitTask

                    // CoreLogger.logDebug "Creator" "*** TODAY'S SNAPSHOT UPDATE COMPLETED AFTER HISTORICAL MOVEMENT ***"
                else
                    // CoreLogger.logDebug "Creator" "Movement is for today - no additional snapshot update needed"
                    ()

                // CoreLogger.logDebug "Creator" "Refreshing reactive snapshot manager..."
                ReactiveSnapshotManager.refresh ()
                // CoreLogger.logDebug "Creator" "SaveBrokerMovement COMPLETED SUCCESSFULLY"
            with ex ->
                CoreLogger.logErrorf "Creator" "*** ERROR IN SaveBrokerMovement *** - Exception: %A" ex.Message
                CoreLogger.logErrorf "Creator" "*** STACK TRACE *** - %A" ex.StackTrace
                raise ex
        }

    let SaveBankMovement (movement: Binnaculum.Core.Models.BankAccountMovement) =
        task {
            let databaseModel = movement.bankAccountMovementToDatabase ()
            do! Saver.saveBankMovement (databaseModel) |> Async.AwaitTask

            // Update snapshots for this movement
            do! SnapshotManager.handleBankMovementSnapshot (databaseModel) |> Async.AwaitTask
        }

    /// <summary>
    /// Save a new or updated ticker and create initial snapshot for new tickers.
    /// </summary>
    let SaveTicker (ticker: Binnaculum.Core.Models.Ticker) =
        task {
            let! databaseTicker = ticker.tickerToDatabase () |> Async.AwaitTask
            let isNewTicker = databaseTicker.Id = 0
            do! Saver.saveTicker (databaseTicker) |> Async.AwaitTask |> Async.Ignore

            // If it's a new ticker, create initial snapshot
            if isNewTicker then
                // Get the saved ticker from database by symbol to get the assigned ID
                let! savedTicker = TickerExtensions.Do.getBySymbol (databaseTicker.Symbol)

                do!
                    TickerSnapshotManager.handleNewTicker (savedTicker)
                    |> Async.AwaitTask
                    |> Async.Ignore

                do! TickerSnapshotLoader.load () |> Async.AwaitTask |> Async.Ignore
        }

    /// <summary>
    /// Save a new or updated trade and refresh the trades collection.
    /// </summary>
    let SaveTrade (trade: Binnaculum.Core.Models.Trade) =
        task {
            let databaseTrade = trade.tradeToDatabase ()
            do! Saver.saveTrade (databaseTrade) |> Async.AwaitTask

            // Update snapshots for this trade using coordinator (batch mode if enabled)
            let datePattern = DateTimePattern.FromDateTime(trade.TimeStamp)

            do!
                SnapshotProcessingCoordinator.handleBrokerAccountChange (trade.BrokerAccount.Id, datePattern)
                |> Async.AwaitTask

            ReactiveSnapshotManager.refresh ()
        }

    /// <summary>
    /// Save a new or updated dividend and refresh the dividends collection.
    /// </summary>
    let SaveDividend (dividend: Binnaculum.Core.Models.Dividend) =
        task {
            let databaseDividend = dividend.dividendReceivedToDatabase ()
            do! Saver.saveDividend (databaseDividend) |> Async.AwaitTask

            // Update snapshots for this dividend using coordinator (batch mode if enabled)
            let datePattern = DateTimePattern.FromDateTime(dividend.TimeStamp)

            do!
                SnapshotProcessingCoordinator.handleBrokerAccountChange (dividend.BrokerAccount.Id, datePattern)
                |> Async.AwaitTask

            ReactiveSnapshotManager.refresh ()
        }

    /// <summary>
    /// Save a new or updated dividend date and refresh the dividend dates collection.
    /// </summary>
    let SaveDividendDate (dividendDate: Binnaculum.Core.Models.DividendDate) =
        task {
            let databaseModel = dividendDate.dividendDateToDatabase ()
            do! Saver.saveDividendDate (databaseModel) |> Async.AwaitTask

            // Update snapshots for this dividend date using coordinator (batch mode if enabled)
            let datePattern = DateTimePattern.FromDateTime(dividendDate.TimeStamp)

            do!
                SnapshotProcessingCoordinator.handleBrokerAccountChange (dividendDate.BrokerAccount.Id, datePattern)
                |> Async.AwaitTask

            ReactiveSnapshotManager.refresh ()
        }

    /// <summary>
    /// Save a new or updated dividend tax and refresh the dividend taxes collection.
    /// </summary>
    let SaveDividendTax (dividendTax: Binnaculum.Core.Models.DividendTax) =
        task {
            let databaseModel = dividendTax.dividendTaxToDatabase ()
            do! Saver.saveDividendTax (databaseModel) |> Async.AwaitTask

            // Update snapshots for this dividend tax using coordinator (batch mode if enabled)
            let datePattern = DateTimePattern.FromDateTime(dividendTax.TimeStamp)

            do!
                SnapshotProcessingCoordinator.handleBrokerAccountChange (dividendTax.BrokerAccount.Id, datePattern)
                |> Async.AwaitTask

            ReactiveSnapshotManager.refresh ()
        }

    /// <summary>
    /// Save option trades and refresh the movements collection.
    /// </summary>
    let SaveOptionsTrade (optionTrades: Binnaculum.Core.Models.OptionTrade list) =
        task {
            // Expand trades with quantity > 1 into multiple trades with quantity = 1
            let expandedTrades =
                optionTrades
                |> List.collect (fun trade ->
                    if trade.Quantity > 1 then
                        let netPremium = trade.NetPremium / decimal trade.Quantity

                        [ for _ in 1 .. trade.Quantity ->
                              { trade with
                                  Quantity = 1
                                  NetPremium = netPremium } ]
                    else
                        [ trade ])

            let databaseModels = expandedTrades.optionTradesToDatabase ()
            do! Saver.saveOptionsTrade (databaseModels) |> Async.AwaitTask

            // Update snapshots for all affected broker accounts using coordinator (batch mode if enabled)
            let uniqueBrokerAccountDates =
                expandedTrades
                |> List.map (fun trade -> (trade.BrokerAccount.Id, trade.TimeStamp))
                |> List.distinct

            for (brokerAccountId, timeStamp) in uniqueBrokerAccountDates do
                let datePattern = DateTimePattern.FromDateTime(timeStamp)

                do!
                    SnapshotProcessingCoordinator.handleBrokerAccountChange (brokerAccountId, datePattern)
                    |> Async.AwaitTask

            ReactiveSnapshotManager.refresh ()
        }

    let UpdateOptionsTimestampNotesAndMultiplier
        (timestamp: DateTime, notes: string option, multiplier: decimal, trade: Binnaculum.Core.Models.OptionTrade)
        =
        { trade with
            TimeStamp = timestamp
            Multiplier = multiplier
            Notes = notes }

    let GetBrokerMovementType (uiSelectedType: Binnaculum.Core.Models.MovementType option) =
        match uiSelectedType with
        | None -> None
        | Some selected ->
            match selected with
            | Binnaculum.Core.Models.MovementType.Deposit -> Some Binnaculum.Core.Models.BrokerMovementType.Deposit
            | Binnaculum.Core.Models.MovementType.Withdrawal ->
                Some Binnaculum.Core.Models.BrokerMovementType.Withdrawal
            | Binnaculum.Core.Models.MovementType.Fee -> Some Binnaculum.Core.Models.BrokerMovementType.Fee
            | Binnaculum.Core.Models.MovementType.InterestsGained ->
                Some Binnaculum.Core.Models.BrokerMovementType.InterestsGained
            | Binnaculum.Core.Models.MovementType.Lending -> Some Binnaculum.Core.Models.BrokerMovementType.Lending
            | Binnaculum.Core.Models.MovementType.ACATMoneyTransferSent ->
                Some Binnaculum.Core.Models.BrokerMovementType.ACATMoneyTransferSent
            | Binnaculum.Core.Models.MovementType.ACATMoneyTransferReceived ->
                Some Binnaculum.Core.Models.BrokerMovementType.ACATMoneyTransferReceived
            | Binnaculum.Core.Models.MovementType.ACATSecuritiesTransferSent ->
                Some Binnaculum.Core.Models.BrokerMovementType.ACATSecuritiesTransferSent
            | Binnaculum.Core.Models.MovementType.ACATSecuritiesTransferReceived ->
                Some Binnaculum.Core.Models.BrokerMovementType.ACATSecuritiesTransferReceived
            | Binnaculum.Core.Models.MovementType.InterestsPaid ->
                Some Binnaculum.Core.Models.BrokerMovementType.InterestsPaid
            | Binnaculum.Core.Models.MovementType.Conversion ->
                Some Binnaculum.Core.Models.BrokerMovementType.Conversion
            | _ -> None
