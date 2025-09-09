namespace Binnaculum.Core.UI

open System
open System.Reactive
open System.Reactive.Linq
open System.Runtime.CompilerServices
open DynamicData
open Binnaculum.Core.Models
open Binnaculum.Core.Database
open Binnaculum.Core.DatabaseToModels

/// <summary>
/// Reactive movement manager that provides automatic movement updates when underlying collections change.
/// This is a simplified version that replaces the manual DataLoader.loadMovementsFor() method.
/// </summary>
module ReactiveMovementManager =
    
    /// <summary>
    /// Subscription for managing reactive updates from all base collections
    /// </summary>
    let mutable private baseCollectionsSubscription: System.IDisposable option = None
    
    /// <summary>
    /// Load movements when any base collection changes
    /// </summary>
    let private loadMovementsWhenCollectionsChange() =
        // Create observable that triggers when any base collection changes
        let baseCollectionsObservable = 
            Observable.Merge([
                Collections.Currencies.Connect().Select(fun _ -> ())
                Collections.Tickers.Connect().Select(fun _ -> ())
                Collections.Brokers.Connect().Select(fun _ -> ())
                Collections.Banks.Connect().Select(fun _ -> ())
                Collections.Accounts.Connect().Select(fun _ -> ())
            ])
            |> Observable.Throttle(TimeSpan.FromMilliseconds(100.0)) // Debounce rapid changes

        baseCollectionsObservable
        |> Observable.subscribe (fun _ ->
            // Load movements using existing synchronous methods for now
            async {
                try
                    // Use the same database loading as the original method
                    let! databaseBrokerMovements = BrokerMovementExtensions.Do.getAll() |> Async.AwaitTask
                    let! databaseBankMovements = BankAccountBalanceExtensions.Do.getAll() |> Async.AwaitTask
                    let! databaseTrades = TradeExtensions.Do.getAll() |> Async.AwaitTask
                    let! databaseDividends = DividendExtensions.Do.getAll() |> Async.AwaitTask
                    let! databaseDividendDates = DividendDateExtensions.Do.getAll() |> Async.AwaitTask
                    let! databaseDividendTaxes = DividendTaxExtensions.Do.getAll() |> Async.AwaitTask
                    let! databaseOptions = OptionTradeExtensions.Do.getAll() |> Async.AwaitTask

                    // Convert using existing synchronous methods for now
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

                    // Update the movements collection reactively
                    Collections.Movements.EditDiff movements

                    // Update account movement status like the original implementation
                    Collections.Accounts.Items
                    |> Seq.iter (fun account ->
                        if account.Bank.IsSome then
                            let hasMovements = 
                                movements 
                                |> List.filter (fun m -> m.BankAccountMovement.IsSome && m.BankAccountMovement.Value.BankAccount.Id = account.Bank.Value.Id)
                                |> List.length > 0
                            if hasMovements <> account.HasMovements then
                                let updatedAccount = { account with HasMovements = hasMovements }
                                let current = Collections.Accounts.Items |> Seq.find(fun a -> a.Bank.IsSome && a.Bank.Value.Id = account.Bank.Value.Id)
                                Collections.Accounts.Replace(current, updatedAccount)

                        if account.Broker.IsSome then
                            // Use async database query for broker accounts like original
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
                    System.Diagnostics.Debug.WriteLine($"ReactiveMovementManager error: {ex.Message}")
            } |> Async.StartImmediate
        )
    
    /// <summary>
    /// Initialize the reactive movement manager by subscribing to base collection changes
    /// </summary>
    let initialize() =
        if baseCollectionsSubscription.IsNone then
            let sub = loadMovementsWhenCollectionsChange()
            baseCollectionsSubscription <- Some sub
    
    /// <summary>
    /// Trigger a manual movement refresh (for compatibility during transition)
    /// </summary>
    let refresh() =
        // Trigger loading immediately
        async {
            try
                // Use the same loading logic as in the subscription
                let! databaseBrokerMovements = BrokerMovementExtensions.Do.getAll() |> Async.AwaitTask
                let! databaseBankMovements = BankAccountBalanceExtensions.Do.getAll() |> Async.AwaitTask
                let! databaseTrades = TradeExtensions.Do.getAll() |> Async.AwaitTask
                let! databaseDividends = DividendExtensions.Do.getAll() |> Async.AwaitTask
                let! databaseDividendDates = DividendDateExtensions.Do.getAll() |> Async.AwaitTask
                let! databaseDividendTaxes = DividendTaxExtensions.Do.getAll() |> Async.AwaitTask
                let! databaseOptions = OptionTradeExtensions.Do.getAll() |> Async.AwaitTask

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

                Collections.Movements.EditDiff movements

                Collections.Accounts.Items
                |> Seq.iter (fun account ->
                    if account.Bank.IsSome then
                        let hasMovements = 
                            movements 
                            |> List.filter (fun m -> m.BankAccountMovement.IsSome && m.BankAccountMovement.Value.BankAccount.Id = account.Bank.Value.Id)
                            |> List.length > 0
                        if hasMovements <> account.HasMovements then
                            let updatedAccount = { account with HasMovements = hasMovements }
                            let current = Collections.Accounts.Items |> Seq.find(fun a -> a.Bank.IsSome && a.Bank.Value.Id = account.Bank.Value.Id)
                            Collections.Accounts.Replace(current, updatedAccount)

                    if account.Broker.IsSome then
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
                System.Diagnostics.Debug.WriteLine($"ReactiveMovementManager.refresh error: {ex.Message}")
        } |> Async.StartImmediate
    
    /// <summary>
    /// Dispose all subscriptions (should be called at application shutdown)
    /// </summary>
    let dispose() =
        baseCollectionsSubscription |> Option.iter (fun sub -> sub.Dispose())
        baseCollectionsSubscription <- None