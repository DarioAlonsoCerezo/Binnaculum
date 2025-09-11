namespace Binnaculum.Core.UI

open System
open System.Collections.Concurrent
open System.Reactive
open System.Reactive.Linq
open System.Runtime.CompilerServices
open DynamicData
open Binnaculum.Core.Models

/// <summary>
/// Reactive bank manager that provides O(1) bank lookups and automatic updates
/// when the underlying Banks collection changes.
/// </summary>
module ReactiveBankManager =
    
    /// <summary>
    /// Internal reactive bank cache by ID that maintains O(1) lookups
    /// </summary>
    let private bankCacheById = ConcurrentDictionary<int, Bank>()
    
    /// <summary>
    /// Subscription for managing reactive updates
    /// </summary>
    let mutable private subscription: System.IDisposable option = None
    
    /// <summary>
    /// Initialize the reactive bank cache by subscribing to Collections.Banks changes
    /// </summary>
    let private initializeCache() =
        // Subscribe to bank collection changes for reactive updates
        let sub = 
            Collections.Banks.Connect()
                .Subscribe(fun changeSet ->
                    for change in changeSet do
                        match change.Reason with
                        | ListChangeReason.Add -> 
                            bankCacheById.TryAdd(change.Item.Current.Id, change.Item.Current) |> ignore
                        | ListChangeReason.Replace ->
                            bankCacheById.AddOrUpdate(change.Item.Current.Id, change.Item.Current, fun _ _ -> change.Item.Current) |> ignore
                        | ListChangeReason.Remove ->
                            bankCacheById.TryRemove(change.Item.Current.Id) |> ignore
                        | ListChangeReason.Clear ->
                            bankCacheById.Clear()
                        | _ -> ())
        
        subscription <- Some sub
        
        // Initialize cache with current bank items
        Collections.Banks.Items
        |> Seq.iter (fun bank -> 
            bankCacheById.TryAdd(bank.Id, bank) |> ignore)
    
    /// <summary>
    /// Initialize the reactive bank manager (should be called once at application startup)
    /// </summary>
    let initialize() = 
        if subscription.IsNone then
            initializeCache()
    
    /// <summary>
    /// Get a bank by ID with O(1) lookup performance.
    /// Falls back to linear search if cache is not populated.
    /// </summary>
    /// <param name="id">Bank ID</param>
    /// <returns>Bank matching the ID</returns>
    let getBankByIdFast(id: int) : Bank =
        match bankCacheById.TryGetValue(id) with
        | true, bank -> bank
        | false, _ ->
            // Fallback to linear search and cache the result
            match Collections.Banks.Items |> Seq.tryFind(fun b -> b.Id = id) with
            | Some bank ->
                bankCacheById.TryAdd(id, bank) |> ignore
                bank
            | None ->
                raise (System.Collections.Generic.KeyNotFoundException($"Bank with ID {id} not found in Collections.Banks"))
    
    /// <summary>
    /// Get a reactive observable that emits the bank when it becomes available by ID
    /// </summary>
    /// <param name="id">Bank ID</param>
    /// <returns>Observable that emits the bank</returns>
    let getBankByIdReactive(id: int) : IObservable<Bank> =
        Observable.Return(getBankByIdFast(id))

/// <summary>
/// Extension methods for reactive bank operations
/// </summary>
[<Extension>]
type ReactiveBankExtensions() =
    
    /// <summary>
    /// Extension method to get bank with fast O(1) lookup by ID
    /// </summary>
    /// <param name="id">Bank ID</param>
    /// <returns>Bank matching the ID</returns>
    [<Extension>]
    static member ToFastBankById(id: int) : Bank =
        ReactiveBankManager.getBankByIdFast(id)

    /// <summary>
    /// Extension method to convert bank ID to reactive bank observable
    /// </summary>
    /// <param name="id">Bank ID</param>
    /// <returns>Observable that emits the bank</returns>
    [<Extension>]
    static member ToReactiveBankById(id: int) : IObservable<Bank> =
        ReactiveBankManager.getBankByIdReactive(id)