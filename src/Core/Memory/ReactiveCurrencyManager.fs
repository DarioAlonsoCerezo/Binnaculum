namespace Binnaculum.Core.UI

open System
open System.Collections.Concurrent
open System.Reactive.Linq
open System.Runtime.CompilerServices
open DynamicData
open Binnaculum.Core.Models

/// <summary>
/// Reactive currency manager that provides O(1) currency lookups and automatic updates
/// when the underlying Currencies collection changes.
/// </summary>
module ReactiveCurrencyManager =
    
    /// <summary>
    /// Internal reactive currency cache that maintains O(1) lookups by code
    /// </summary>
    let private currencyCache = ConcurrentDictionary<string, Currency>()
    
    /// <summary>
    /// Internal reactive currency cache by ID that maintains O(1) lookups
    /// </summary>
    let private currencyCacheById = ConcurrentDictionary<int, Currency>()
    
    /// <summary>
    /// Subscription for managing reactive updates
    /// </summary>
    let mutable private subscription: System.IDisposable option = None
    
    /// <summary>
    /// Initialize the reactive currency cache by subscribing to Collections.Currencies changes
    /// </summary>
    let private initializeCache() =
        // Subscribe to currency collection changes for reactive updates
        let sub = 
            Collections.Currencies.Connect()
                .Subscribe(fun changeSet ->
                    for change in changeSet do
                        match change.Reason with
                        | ListChangeReason.Add -> 
                            currencyCache.TryAdd(change.Item.Current.Code, change.Item.Current) |> ignore
                            currencyCacheById.TryAdd(change.Item.Current.Id, change.Item.Current) |> ignore
                        | ListChangeReason.Replace ->
                            currencyCache.AddOrUpdate(change.Item.Current.Code, change.Item.Current, fun _ _ -> change.Item.Current) |> ignore
                            currencyCacheById.AddOrUpdate(change.Item.Current.Id, change.Item.Current, fun _ _ -> change.Item.Current) |> ignore
                        | ListChangeReason.Remove ->
                            currencyCache.TryRemove(change.Item.Current.Code) |> ignore
                            currencyCacheById.TryRemove(change.Item.Current.Id) |> ignore
                        | ListChangeReason.Clear ->
                            currencyCache.Clear()
                            currencyCacheById.Clear()
                        | _ -> ())
        
        subscription <- Some sub
        
        // Initialize both caches with current currency items
        Collections.Currencies.Items
        |> Seq.iter (fun currency -> 
            currencyCache.TryAdd(currency.Code, currency) |> ignore
            currencyCacheById.TryAdd(currency.Id, currency) |> ignore)
    
    /// <summary>
    /// Initialize the reactive currency manager (should be called once at application startup)
    /// </summary>
    let initialize() = 
        if subscription.IsNone then
            initializeCache()
    
    /// <summary>
    /// Get a currency by code with O(1) lookup performance.
    /// Falls back to linear search if cache is not populated.
    /// </summary>
    /// <param name="code">Currency code (e.g., "USD", "EUR")</param>
    /// <returns>Currency matching the code</returns>
    let getCurrencyFast(code: string) : Currency =
        match currencyCache.TryGetValue(code) with
        | true, currency -> currency
        | false, _ ->
            // Fallback to linear search and cache the result
            match Collections.Currencies.Items |> Seq.tryFind(fun c -> c.Code = code) with
            | Some currency ->
                currencyCache.TryAdd(code, currency) |> ignore
                currency
            | None ->
                raise (System.Collections.Generic.KeyNotFoundException($"Currency with code '{code}' not found in Collections.Currencies"))
    
    /// <summary>
    /// Get a reactive observable that emits the currency when it becomes available
    /// </summary>
    /// <param name="code">Currency code (e.g., "USD", "EUR")</param>
    /// <returns>Observable that emits the currency</returns>
    let getCurrencyReactive(code: string) : IObservable<Currency> =
        // Simplified approach: just return the current currency immediately
        // since we have the fast lookup available
        Observable.Return(getCurrencyFast(code))

    /// <summary>
    /// Get a currency by ID with O(1) lookup performance.
    /// Falls back to linear search if cache is not populated.
    /// </summary>
    /// <param name="id">Currency ID</param>
    /// <returns>Currency matching the ID</returns>
    let getCurrencyByIdFast(id: int) : Currency =
        match currencyCacheById.TryGetValue(id) with
        | true, currency -> currency
        | false, _ ->
            // Fallback to linear search and cache the result
            match Collections.Currencies.Items |> Seq.tryFind(fun c -> c.Id = id) with
            | Some currency ->
                currencyCacheById.TryAdd(id, currency) |> ignore
                currency
            | None ->
                raise (System.Collections.Generic.KeyNotFoundException($"Currency with ID {id} not found in Collections.Currencies"))
    
    /// <summary>
    /// Get a reactive observable that emits the currency when it becomes available by ID
    /// </summary>
    /// <param name="id">Currency ID</param>
    /// <returns>Observable that emits the currency</returns>
    let getCurrencyByIdReactive(id: int) : IObservable<Currency> =
        Observable.Return(getCurrencyByIdFast(id))

/// <summary>
/// Extension methods for reactive currency operations
/// </summary>
[<Extension>]
type ReactiveCurrencyExtensions() =
    
    /// <summary>
    /// Extension method to convert currency code to reactive currency observable
    /// </summary>
    /// <param name="code">Currency code</param>
    /// <returns>Observable that emits the currency</returns>
    [<Extension>]
    static member ToReactiveCurrency(code: string) : IObservable<Currency> =
        ReactiveCurrencyManager.getCurrencyReactive(code)
    
    /// <summary>
    /// Extension method to get currency with fast O(1) lookup
    /// </summary>
    /// <param name="code">Currency code</param>
    /// <returns>Currency matching the code</returns>
    [<Extension>]
    static member ToFastCurrency(code: string) : Currency =
        ReactiveCurrencyManager.getCurrencyFast(code)
    
    /// <summary>
    /// Extension method to get currency with fast O(1) lookup by ID
    /// </summary>
    /// <param name="id">Currency ID</param>
    /// <returns>Currency matching the ID</returns>
    [<Extension>]
    static member ToFastCurrencyById(id: int) : Currency =
        ReactiveCurrencyManager.getCurrencyByIdFast(id)

    /// <summary>
    /// Extension method to convert currency ID to reactive currency observable
    /// </summary>
    /// <param name="id">Currency ID</param>
    /// <returns>Observable that emits the currency</returns>
    [<Extension>]
    static member ToReactiveCurrencyById(id: int) : IObservable<Currency> =
        ReactiveCurrencyManager.getCurrencyByIdReactive(id)