namespace Binnaculum.Core.UI

open System
open System.Reactive.Linq
open DynamicData
open Binnaculum.Core.Models
open Binnaculum.Core.DatabaseToModels
open Binnaculum.Core.Logging

/// <summary>
/// Reactive movement manager that provides automatic movement updates when underlying collections change.
/// This replaces the manual DataLoader.loadMovementsFor() method with reactive patterns.
/// </summary>
module ReactiveMovementManager =

    /// <summary>
    /// Subscription for managing reactive updates from all base collections
    /// </summary>
    let mutable private baseCollectionsSubscription: System.IDisposable option = None

    /// <summary>
    /// Reentrancy protection flag to prevent concurrent executions of loadMovements
    /// </summary>
    let mutable private isLoadingMovements = false

    /// <summary>
    /// Load movements from database and update Collections.Movements
    /// This is the core function that does the same work as DataLoader.loadMovementsFor()
    /// </summary>
    let private loadMovements () =
        async {
            // Prevent reentrancy to avoid infinite loops during movement processing
            if isLoadingMovements then
                // CoreLogger.logDebug "ReactiveMovementManager" "Skipping loadMovements - already in progress"
                return ()

            // Defer reactive updates during import to prevent database connection conflicts
            if Binnaculum.Core.Import.ImportState.isImportInProgress () then
                // CoreLogger.logDebug "ReactiveMovementManager" "Skipping loadMovements - import in progress, will update after completion"

                return ()

            isLoadingMovements <- true
            // CoreLogger.logDebug "ReactiveMovementManager" "Starting loadMovements"

            try
                // Get all account IDs from Collections.Accounts
                let brokerAccounts =
                    Collections.Accounts.Items
                    |> Seq.filter (fun acc -> acc.Broker.IsSome)
                    |> Seq.map (fun acc -> acc.Broker.Value.Id)
                    |> Seq.toList
                
                let bankAccounts =
                    Collections.Accounts.Items
                    |> Seq.filter (fun acc -> acc.Bank.IsSome)
                    |> Seq.map (fun acc -> acc.Bank.Value.Id)
                    |> Seq.toList
                
                // Build async tasks (one per account per movement type)
                let movementTasks = [
                    // Broker movements per account
                    for brokerId in brokerAccounts do
                        async {
                            let! movements =
                                BrokerMovementExtensions.Do.loadMovementsPaged(brokerId, 0, 50)
                                |> Async.AwaitTask
                            return (brokerId, movements.brokerMovementsToModel())
                        }
                    
                    // Trades per account
                    for brokerId in brokerAccounts do
                        async {
                            let! trades =
                                TradeExtensions.Do.loadTradesPaged(brokerId, 0, 50)
                                |> Async.AwaitTask
                            return (brokerId, trades.tradesToMovements())
                        }
                    
                    // Dividends per account
                    for brokerId in brokerAccounts do
                        async {
                            let! dividends =
                                DividendExtensions.Do.loadDividendsPaged(brokerId, 0, 50)
                                |> Async.AwaitTask
                            return (brokerId, dividends.dividendsReceivedToMovements())
                        }
                    
                    // Dividend dates per account
                    for brokerId in brokerAccounts do
                        async {
                            let! dividendDates =
                                DividendDateExtensions.Do.loadDividendDatesPaged(brokerId, 0, 50)
                                |> Async.AwaitTask
                            return (brokerId, dividendDates.dividendDatesToMovements())
                        }
                    
                    // Dividend taxes per account
                    for brokerId in brokerAccounts do
                        async {
                            let! dividendTaxes =
                                DividendTaxExtensions.Do.loadDividendTaxesPaged(brokerId, 0, 50)
                                |> Async.AwaitTask
                            return (brokerId, dividendTaxes.dividendTaxesToMovements())
                        }
                    
                    // Option trades per account
                    for brokerId in brokerAccounts do
                        async {
                            let! optionTrades =
                                OptionTradeExtensions.Do.loadOptionTradesPaged(brokerId, 0, 50)
                                |> Async.AwaitTask
                            return (brokerId, optionTrades.optionTradesToMovements())
                        }
                    
                    // Bank movements per account
                    for bankId in bankAccounts do
                        async {
                            let! bankMovements =
                                BankAccountBalanceExtensions.Do.loadBankMovementsPaged(bankId, 0, 50)
                                |> Async.AwaitTask
                            return (bankId, bankMovements.bankAccountMovementsToMovements())
                        }
                ]
                
                // Execute all tasks in parallel
                let! allMovementsByAccount = Async.Parallel movementTasks
                
                // Group by account, combine types, sort, and truncate to 50 per account
                let movements =
                    allMovementsByAccount
                    |> Array.toList
                    |> List.groupBy fst  // Group by accountId
                    |> List.collect (fun (accountId, accountMovements) ->
                        accountMovements
                        |> List.collect snd  // Get all movements
                        |> List.sortByDescending (fun m -> m.TimeStamp)  // Newest first
                        |> List.truncate 50)  // Top 50 per account
                
                // Update the movements collection with bounded dataset
                Collections.Movements.EditDiff movements

                // Update account movement status
                Collections.Accounts.Items
                |> Seq.iter (fun account ->
                    if account.Bank.IsSome then
                        let hasMovements =
                            movements
                            |> List.filter (fun m ->
                                m.BankAccountMovement.IsSome
                                && m.BankAccountMovement.Value.BankAccount.Id = account.Bank.Value.Id)
                            |> List.length > 0

                        if hasMovements <> account.HasMovements then
                            let updatedAccount =
                                { account with
                                    HasMovements = hasMovements }

                            match
                                Collections.Accounts.Items
                                |> Seq.tryFind (fun a -> a.Bank.IsSome && a.Bank.Value.Id = account.Bank.Value.Id)
                            with
                            | Some current -> Collections.Accounts.Replace(current, updatedAccount)
                            | None ->
                                // CoreLogger.logDebug "ReactiveMovementManager" $"Bank account with ID {account.Bank.Value.Id} not found in Collections.Accounts for movement update"
                                ()

                    if account.Broker.IsSome then
                        async {
                            let! hasMovements =
                                BrokerAccountExtensions.Do.hasMovements account.Broker.Value.Id
                                |> Async.AwaitTask

                            if hasMovements <> account.HasMovements then
                                let updatedAccount =
                                    { account with
                                        HasMovements = hasMovements }

                                Collections.updateBrokerAccount updatedAccount
                        }
                        |> Async.StartImmediate)

                // CoreLogger.logDebug "ReactiveMovementManager" "Completed loadMovements"
                isLoadingMovements <- false
            with ex ->
                CoreLogger.logError "ReactiveMovementManager" $"loadMovements error: {ex.Message}"
                isLoadingMovements <- false
        }

    /// <summary>
    /// Create observable that triggers when any base collection changes
    /// </summary>
    let private createBaseCollectionsObservable () =
        [ Collections.Currencies.Connect().Select(fun _ -> ())
          Collections.Tickers.Connect().Select(fun _ -> ())
          Collections.Brokers.Connect().Select(fun _ -> ())
          Collections.Banks.Connect().Select(fun _ -> ())
          Collections.Accounts.Connect().Select(fun _ -> ()) ]
        |> Observable.Merge

    /// <summary>
    /// Initialize the reactive movement manager by subscribing to base collection changes
    /// </summary>
    let initialize () =
        if baseCollectionsSubscription.IsNone then
            let observable = createBaseCollectionsObservable ()

            let sub =
                observable.Subscribe(fun _ ->
                    // Trigger movement loading when any base collection changes
                    loadMovements () |> Async.StartImmediate)

            baseCollectionsSubscription <- Some sub

    /// <summary>
    /// Trigger a manual movement refresh and wait for completion
    /// This fixes the async timing issue where imported data doesn't appear in UI immediately
    /// </summary>
    let refreshAsync () = loadMovements ()

    /// <summary>
    /// Trigger a manual movement refresh (for compatibility during transition)
    /// This provides the same interface as the original DataLoader.loadMovementsFor()
    /// </summary>
    let refresh () =
        loadMovements () |> Async.StartImmediate

    /// <summary>
    /// Dispose all subscriptions (should be called at application shutdown)
    /// </summary>
    let dispose () =
        baseCollectionsSubscription |> Option.iter (fun sub -> sub.Dispose())
        baseCollectionsSubscription <- None
