namespace Binnaculum.Core.UI

open System
open System.Collections.Concurrent
open System.Reactive
open System.Reactive.Linq
open System.Runtime.CompilerServices
open DynamicData
open Binnaculum.Core.Models

/// <summary>
/// Reactive broker manager that provides O(1) broker lookups and automatic updates
/// when the underlying Brokers collection changes.
/// </summary>
module ReactiveBrokerManager =
    
    /// <summary>
    /// Internal reactive broker cache by ID that maintains O(1) lookups
    /// </summary>
    let private brokerCacheById = ConcurrentDictionary<int, Broker>()
    
    /// <summary>
    /// Subscription for managing reactive updates
    /// </summary>
    let mutable private subscription: System.IDisposable option = None
    
    /// <summary>
    /// Initialize the reactive broker cache by subscribing to Collections.Brokers changes
    /// </summary>
    let private initializeCache() =
        // Subscribe to broker collection changes for reactive updates
        let sub = 
            Collections.Brokers.Connect()
                .Subscribe(fun changeSet ->
                    for change in changeSet do
                        match change.Reason with
                        | ListChangeReason.Add -> 
                            brokerCacheById.TryAdd(change.Item.Current.Id, change.Item.Current) |> ignore
                        | ListChangeReason.Replace ->
                            brokerCacheById.AddOrUpdate(change.Item.Current.Id, change.Item.Current, fun _ _ -> change.Item.Current) |> ignore
                        | ListChangeReason.Remove ->
                            brokerCacheById.TryRemove(change.Item.Current.Id) |> ignore
                        | ListChangeReason.Clear ->
                            brokerCacheById.Clear()
                        | _ -> ())
        
        subscription <- Some sub
        
        // Initialize cache with current broker items
        Collections.Brokers.Items
        |> Seq.iter (fun broker -> 
            brokerCacheById.TryAdd(broker.Id, broker) |> ignore)
    
    /// <summary>
    /// Initialize the reactive broker manager (should be called once at application startup)
    /// </summary>
    let initialize() = 
        if subscription.IsNone then
            initializeCache()
    
    /// <summary>
    /// Get a broker by ID with O(1) lookup performance.
    /// Falls back to linear search if cache is not populated.
    /// </summary>
    /// <param name="id">Broker ID</param>
    /// <returns>Broker matching the ID</returns>
    let getBrokerByIdFast(id: int) : Broker =
        match brokerCacheById.TryGetValue(id) with
        | true, broker -> broker
        | false, _ ->
            // Fallback to linear search and cache the result
            match Collections.Brokers.Items |> Seq.tryFind(fun b -> b.Id = id) with
            | Some broker ->
                brokerCacheById.TryAdd(id, broker) |> ignore
                broker
            | None ->
                raise (System.Collections.Generic.KeyNotFoundException($"Broker with ID {id} not found in Collections.Brokers"))
    
    /// <summary>
    /// Get a reactive observable that emits the broker when it becomes available by ID
    /// </summary>
    /// <param name="id">Broker ID</param>
    /// <returns>Observable that emits the broker</returns>
    let getBrokerByIdReactive(id: int) : IObservable<Broker> =
        Observable.Return(getBrokerByIdFast(id))

/// <summary>
/// Extension methods for reactive broker operations
/// </summary>
[<Extension>]
type ReactiveBrokerExtensions() =
    
    /// <summary>
    /// Extension method to get broker with fast O(1) lookup by ID
    /// </summary>
    /// <param name="id">Broker ID</param>
    /// <returns>Broker matching the ID</returns>
    [<Extension>]
    static member ToFastBrokerById(id: int) : Broker =
        ReactiveBrokerManager.getBrokerByIdFast(id)

    /// <summary>
    /// Extension method to convert broker ID to reactive broker observable
    /// </summary>
    /// <param name="id">Broker ID</param>
    /// <returns>Observable that emits the broker</returns>
    [<Extension>]
    static member ToReactiveBrokerById(id: int) : IObservable<Broker> =
        ReactiveBrokerManager.getBrokerByIdReactive(id)