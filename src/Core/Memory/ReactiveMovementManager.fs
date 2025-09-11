namespace Binnaculum.Core.UI

open System
open System.Reactive.Linq
open DynamicData
open Binnaculum.Core.Models
open Binnaculum.Core.Database
open Binnaculum.Core.DatabaseToModels
open Binnaculum.Core

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
    /// Load movements from database and update Collections.Movements
    /// This is the core function that does the same work as DataLoader.loadMovementsFor()
    /// </summary>
    let private loadMovements() =
        async {
            try
                // Load all movement data from database (same as original)
                let! databaseBrokerMovements = BrokerMovementExtensions.Do.getAll() |> Async.AwaitTask
                let! databaseBankMovements = BankAccountBalanceExtensions.Do.getAll() |> Async.AwaitTask
                let! databaseTrades = TradeExtensions.Do.getAll() |> Async.AwaitTask
                let! databaseDividends = DividendExtensions.Do.getAll() |> Async.AwaitTask
                let! databaseDividendDates = DividendDateExtensions.Do.getAll() |> Async.AwaitTask
                let! databaseDividendTaxes = DividendTaxExtensions.Do.getAll() |> Async.AwaitTask
                let! databaseOptions = OptionTradeExtensions.Do.getAll() |> Async.AwaitTask

                // Convert using existing extension methods (same as original)
                let brokerMovements = databaseBrokerMovements.brokerMovementsToModel()
                let bankMovements = databaseBankMovements.bankAccountMovementsToMovements()
                let tradeMovements = databaseTrades.tradesToMovements()
                let dividendMovements = databaseDividends.dividendsReceivedToMovements()
                let dividendDates = databaseDividendDates.dividendDatesToMovements()
                let dividendTaxes = databaseDividendTaxes.dividendTaxesToMovements()
                let optionTrades = databaseOptions.optionTradesToMovements()

                let movements = 
                    brokerMovements 
                    @ bankMovements 
                    @ tradeMovements
                    @ dividendMovements
                    @ dividendDates
                    @ dividendTaxes
                    @ optionTrades

                // Update the movements collection (same as original)
                Collections.Movements.EditDiff movements

                // Update account movement status (same as original)
                Collections.Accounts.Items
                |> Seq.iter (fun account ->
                    if account.Bank.IsSome then
                        let hasMovements = 
                            movements 
                            |> List.filter (fun m -> m.BankAccountMovement.IsSome && m.BankAccountMovement.Value.BankAccount.Id = account.Bank.Value.Id)
                            |> List.length > 0
                        if hasMovements <> account.HasMovements then
                            let updatedAccount = { account with HasMovements = hasMovements }
                            match Collections.Accounts.Items |> Seq.tryFind(fun a -> a.Bank.IsSome && a.Bank.Value.Id = account.Bank.Value.Id) with
                            | Some current -> Collections.Accounts.Replace(current, updatedAccount)
                            | None -> 
                                // Log the issue but don't fail the entire operation since this is a background update
                                System.Diagnostics.Debug.WriteLine($"[ReactiveMovementManager] Bank account with ID {account.Bank.Value.Id} not found in Collections.Accounts for movement update - this may indicate a race condition")

                    if account.Broker.IsSome then
                        // Use async database query for broker accounts (same as original)
                        async {
                            let! hasMovements = 
                                BrokerAccountExtensions.Do.hasMovements account.Broker.Value.Id
                                |> Async.AwaitTask
                            
                            if hasMovements <> account.HasMovements then
                                let updatedAccount = { account with HasMovements = hasMovements }
                                Collections.updateBrokerAccount updatedAccount
                        } |> Async.StartImmediate
                )
            with
            | ex ->
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"ReactiveMovementManager.loadMovements error: {ex.Message}")
        }
    
    /// <summary>
    /// Create observable that triggers when any base collection changes
    /// </summary>
    let private createBaseCollectionsObservable() =
        [
            Collections.Currencies.Connect().Select(fun _ -> ())
            Collections.Tickers.Connect().Select(fun _ -> ())
            Collections.Brokers.Connect().Select(fun _ -> ())
            Collections.Banks.Connect().Select(fun _ -> ())
            Collections.Accounts.Connect().Select(fun _ -> ())
        ]
        |> Observable.Merge
    
    /// <summary>
    /// Initialize the reactive movement manager by subscribing to base collection changes
    /// </summary>
    let initialize() =
        if baseCollectionsSubscription.IsNone then
            let observable = createBaseCollectionsObservable()
            let sub = 
                observable.Subscribe(fun _ ->
                    // Trigger movement loading when any base collection changes
                    loadMovements() |> Async.StartImmediate
                )
            baseCollectionsSubscription <- Some sub
    
    /// <summary>
    /// Trigger a manual movement refresh (for compatibility during transition)
    /// This provides the same interface as the original DataLoader.loadMovementsFor()
    /// </summary>
    let refresh() =
        loadMovements() |> Async.StartImmediate
    
    /// <summary>
    /// Dispose all subscriptions (should be called at application shutdown)
    /// </summary>
    let dispose() =
        baseCollectionsSubscription |> Option.iter (fun sub -> sub.Dispose())
        baseCollectionsSubscription <- None