namespace Binnaculum.Core.UI

open System
open System.Collections.Concurrent
open System.Reactive
open System.Reactive.Linq
open System.Runtime.CompilerServices
open DynamicData
open Binnaculum.Core.Models

/// <summary>
/// Reactive broker account manager that provides O(1) broker account lookups and automatic updates
/// when the underlying Accounts or Brokers collections change. Handles cross-collection dependencies.
/// </summary>
module ReactiveBrokerAccountManager =
    
    /// <summary>
    /// Internal reactive broker account cache by ID that maintains O(1) lookups
    /// </summary>
    let private brokerAccountCacheById = ConcurrentDictionary<int, BrokerAccount>()
    
    /// <summary>
    /// Internal reactive broker account cache by AccountNumber that maintains O(1) lookups
    /// </summary>
    let private brokerAccountCacheByAccountNumber = ConcurrentDictionary<string, BrokerAccount>()
    
    /// <summary>
    /// Subscription for managing reactive updates from Accounts collection
    /// </summary>
    let mutable private accountsSubscription: System.IDisposable option = None
    
    /// <summary>
    /// Subscription for managing reactive updates from Brokers collection
    /// </summary>
    let mutable private brokersSubscription: System.IDisposable option = None
    
    /// <summary>
    /// Initialize the reactive broker account cache by subscribing to Collections.Accounts and Collections.Brokers changes
    /// </summary>
    let private initializeCache() =
        // Subscribe to accounts collection changes for broker accounts
        let accountsSub = 
            Collections.Accounts.Connect()
                .Filter(fun account -> account.Type = AccountType.BrokerAccount && account.Broker.IsSome)
                .Subscribe(fun changeSet ->
                    for change in changeSet do
                        match change.Reason with
                        | ListChangeReason.Add -> 
                            let brokerAccount = change.Item.Current.Broker.Value
                            brokerAccountCacheById.TryAdd(brokerAccount.Id, brokerAccount) |> ignore
                            brokerAccountCacheByAccountNumber.TryAdd(brokerAccount.AccountNumber, brokerAccount) |> ignore
                        | ListChangeReason.Replace ->
                            let brokerAccount = change.Item.Current.Broker.Value
                            brokerAccountCacheById.AddOrUpdate(brokerAccount.Id, brokerAccount, fun _ _ -> brokerAccount) |> ignore
                            brokerAccountCacheByAccountNumber.AddOrUpdate(brokerAccount.AccountNumber, brokerAccount, fun _ _ -> brokerAccount) |> ignore
                        | ListChangeReason.Remove ->
                            let brokerAccount = change.Item.Current.Broker.Value
                            brokerAccountCacheById.TryRemove(brokerAccount.Id) |> ignore
                            brokerAccountCacheByAccountNumber.TryRemove(brokerAccount.AccountNumber) |> ignore
                        | ListChangeReason.Clear ->
                            brokerAccountCacheById.Clear()
                            brokerAccountCacheByAccountNumber.Clear()
                        | _ -> ())
        
        // Subscribe to brokers collection changes to update broker account references
        let brokersSub = 
            Collections.Brokers.Connect()
                .Subscribe(fun changeSet ->
                    for change in changeSet do
                        match change.Reason with
                        | ListChangeReason.Replace ->
                            let updatedBroker = change.Item.Current
                            // Update any broker accounts that reference this broker
                            brokerAccountCacheById.Values
                            |> Seq.filter (fun ba -> ba.Broker.Id = updatedBroker.Id)
                            |> Seq.iter (fun ba ->
                                let updatedBrokerAccount = { ba with Broker = updatedBroker }
                                brokerAccountCacheById.AddOrUpdate(ba.Id, updatedBrokerAccount, fun _ _ -> updatedBrokerAccount) |> ignore
                                brokerAccountCacheByAccountNumber.AddOrUpdate(ba.AccountNumber, updatedBrokerAccount, fun _ _ -> updatedBrokerAccount) |> ignore)
                        | _ -> ())
        
        accountsSubscription <- Some accountsSub
        brokersSubscription <- Some brokersSub
        
        // Initialize cache with current broker account items
        Collections.Accounts.Items
        |> Seq.filter (fun account -> account.Type = AccountType.BrokerAccount && account.Broker.IsSome)
        |> Seq.iter (fun account -> 
            let brokerAccount = account.Broker.Value
            brokerAccountCacheById.TryAdd(brokerAccount.Id, brokerAccount) |> ignore
            brokerAccountCacheByAccountNumber.TryAdd(brokerAccount.AccountNumber, brokerAccount) |> ignore)
    
    /// <summary>
    /// Initialize the reactive broker account manager (should be called once at application startup)
    /// </summary>
    let initialize() = 
        if accountsSubscription.IsNone then
            initializeCache()
    
    /// <summary>
    /// Get a broker account by ID with O(1) lookup performance.
    /// Falls back to linear search if cache is not populated.
    /// </summary>
    /// <param name="id">Broker account ID</param>
    /// <returns>Broker account matching the ID</returns>
    let getBrokerAccountByIdFast(id: int) : BrokerAccount =
        match brokerAccountCacheById.TryGetValue(id) with
        | true, brokerAccount -> brokerAccount
        | false, _ ->
            // Fallback to linear search and cache the result
            match Collections.Accounts.Items |> Seq.tryFind(fun b -> b.Broker.IsSome && b.Broker.Value.Id = id) with
            | Some account ->
                let brokerAccount = account.Broker.Value
                brokerAccountCacheById.TryAdd(id, brokerAccount) |> ignore
                brokerAccountCacheByAccountNumber.TryAdd(brokerAccount.AccountNumber, brokerAccount) |> ignore
                brokerAccount
            | None ->
                raise (System.Collections.Generic.KeyNotFoundException($"Broker account with ID {id} not found in Collections.Accounts"))
    
    /// <summary>
    /// Get a broker account by account number with O(1) lookup performance.
    /// Falls back to linear search if cache is not populated.
    /// </summary>
    /// <param name="accountNumber">Broker account number</param>
    /// <returns>Option of broker account matching the account number</returns>
    let getBrokerAccountByAccountNumberFast(accountNumber: string) : BrokerAccount option =
        match brokerAccountCacheByAccountNumber.TryGetValue(accountNumber) with
        | true, brokerAccount -> Some brokerAccount
        | false, _ ->
            // Fallback to linear search and cache the result
            let accountOpt = Collections.Accounts.Items 
                           |> Seq.tryFind(fun b -> b.Broker.IsSome && b.Broker.Value.AccountNumber = accountNumber)
            match accountOpt with
            | Some account ->
                let brokerAccount = account.Broker.Value
                brokerAccountCacheById.TryAdd(brokerAccount.Id, brokerAccount) |> ignore
                brokerAccountCacheByAccountNumber.TryAdd(brokerAccount.AccountNumber, brokerAccount) |> ignore
                Some brokerAccount
            | None -> None
    
    /// <summary>
    /// Get a reactive observable that emits the broker account when it becomes available by ID
    /// </summary>
    /// <param name="id">Broker account ID</param>
    /// <returns>Observable that emits the broker account</returns>
    let getBrokerAccountByIdReactive(id: int) : IObservable<BrokerAccount> =
        Observable.Return(getBrokerAccountByIdFast(id))
    
    /// <summary>
    /// Dispose all subscriptions and clear caches
    /// </summary>
    let dispose() =
        accountsSubscription |> Option.iter (fun sub -> sub.Dispose())
        brokersSubscription |> Option.iter (fun sub -> sub.Dispose())
        accountsSubscription <- None
        brokersSubscription <- None
        brokerAccountCacheById.Clear()
        brokerAccountCacheByAccountNumber.Clear()

/// <summary>
/// Extension methods for reactive broker account operations
/// </summary>
[<Extension>]
type ReactiveBrokerAccountExtensions() =
    
    /// <summary>
    /// Extension method to get broker account with fast O(1) lookup by ID
    /// </summary>
    /// <param name="id">Broker account ID</param>
    /// <returns>Broker account matching the ID</returns>
    [<Extension>]
    static member ToFastBrokerAccountById(id: int) : BrokerAccount =
        ReactiveBrokerAccountManager.getBrokerAccountByIdFast(id)
    
    /// <summary>
    /// Extension method to get broker account with fast O(1) lookup by account number
    /// </summary>
    /// <param name="accountNumber">Broker account number</param>
    /// <returns>Option of broker account matching the account number</returns>
    [<Extension>]
    static member ToFastBrokerAccountByAccountNumber(accountNumber: string) : BrokerAccount option =
        ReactiveBrokerAccountManager.getBrokerAccountByAccountNumberFast(accountNumber)

    /// <summary>
    /// Extension method to convert broker account ID to reactive broker account observable
    /// </summary>
    /// <param name="id">Broker account ID</param>
    /// <returns>Observable that emits the broker account</returns>
    [<Extension>]
    static member ToReactiveBrokerAccountById(id: int) : IObservable<BrokerAccount> =
        ReactiveBrokerAccountManager.getBrokerAccountByIdReactive(id)