namespace Binnaculum.Core.UI

open System
open System.Collections.Concurrent
open System.Reactive.Linq
open System.Runtime.CompilerServices
open System.Threading.Tasks
open DynamicData
open Binnaculum.Core.Models
open Binnaculum.Core.Logging

/// <summary>
/// Reactive ticker manager that provides O(1) ticker lookups and automatic updates
/// when the underlying Tickers collection changes.
/// </summary>
module ReactiveTickerManager =

    /// <summary>
    /// Internal reactive ticker cache that maintains O(1) lookups by symbol
    /// </summary>
    let private tickerCache = ConcurrentDictionary<string, Ticker>()

    /// <summary>
    /// Internal reactive ticker cache by ID that maintains O(1) lookups
    /// </summary>
    let private tickerCacheById = ConcurrentDictionary<int, Ticker>()

    /// <summary>
    /// Subscription for managing reactive updates
    /// </summary>
    let mutable private subscription: System.IDisposable option = None

    /// <summary>
    /// Initialize the reactive ticker cache by subscribing to Collections.Tickers changes
    /// </summary>
    let private initializeCache () =
        // Subscribe to ticker collection changes for reactive updates
        let sub =
            Collections.Tickers
                .Connect()
                .Subscribe(fun changeSet ->
                    for change in changeSet do
                        match change.Reason with
                        | ListChangeReason.Add ->
                            tickerCache.TryAdd(change.Item.Current.Symbol, change.Item.Current) |> ignore
                            tickerCacheById.TryAdd(change.Item.Current.Id, change.Item.Current) |> ignore
                        | ListChangeReason.Replace ->
                            tickerCache.AddOrUpdate(
                                change.Item.Current.Symbol,
                                change.Item.Current,
                                fun _ _ -> change.Item.Current
                            )
                            |> ignore

                            tickerCacheById.AddOrUpdate(
                                change.Item.Current.Id,
                                change.Item.Current,
                                fun _ _ -> change.Item.Current
                            )
                            |> ignore
                        | ListChangeReason.Remove ->
                            tickerCache.TryRemove(change.Item.Current.Symbol) |> ignore
                            tickerCacheById.TryRemove(change.Item.Current.Id) |> ignore
                        | ListChangeReason.Clear ->
                            tickerCache.Clear()
                            tickerCacheById.Clear()
                        | _ -> ())

        subscription <- Some sub

        // Initialize both caches with current ticker items
        Collections.Tickers.Items
        |> Seq.iter (fun ticker ->
            tickerCache.TryAdd(ticker.Symbol, ticker) |> ignore
            tickerCacheById.TryAdd(ticker.Id, ticker) |> ignore)

    /// <summary>
    /// Initialize the reactive ticker manager (should be called once at application startup)
    /// </summary>
    let initialize () =
        if subscription.IsNone then
            initializeCache ()

    /// <summary>
    /// Refresh ticker data from database and update Collections.Tickers
    /// This properly fixes cache consistency by using the authoritative data source
    /// </summary>
    let refresh () =
        task {
            // CoreLogger.logDebug "ReactiveTickerManager" "Starting loadTickers"

            try
                // ✅ Load fresh data from DATABASE (authoritative source)
                let! freshDatabaseTickers = TickerExtensions.Do.getAll () |> Async.AwaitTask

                // ✅ Convert database models to UI models (inline conversion to avoid dependency issues)
                let freshTickers =
                    freshDatabaseTickers
                    |> List.map (fun dbTicker ->
                        { Id = dbTicker.Id
                          Symbol = dbTicker.Symbol
                          Image = dbTicker.Image
                          Name = dbTicker.Name
                          OptionsEnabled = dbTicker.OptionsEnabled
                          OptionContractMultiplier = dbTicker.OptionContractMultiplier })

                // ✅ Update Collections.Tickers using EditDiff (triggers reactive updates)
                Collections.Tickers.EditDiff(freshTickers)

                // ✅ Give reactive updates time to propagate through DynamicData
                do! Task.Delay(50) |> Async.AwaitTask

                // CoreLogger.logDebug "ReactiveTickerManager" "Completed loadTickers"
            with ex ->
                CoreLogger.logErrorf "ReactiveTickerManager" "loadTickers error: %s" ex.Message

        // ✅ NO manual cache manipulation needed!
        // The reactive subscription automatically updates both caches efficiently
        }

    /// <summary>
    /// Get a ticker by symbol with O(1) lookup performance.
    /// Falls back to linear search if cache is not populated.
    /// </summary>
    /// <param name="symbol">Ticker symbol (e.g., "AAPL", "MSFT")</param>
    /// <returns>Ticker matching the symbol</returns>
    let getTickerFast (symbol: string) : Ticker =
        match tickerCache.TryGetValue(symbol) with
        | true, ticker -> ticker
        | false, _ ->
            // Fallback to linear search and cache the result
            match Collections.Tickers.Items |> Seq.tryFind (fun t -> t.Symbol = symbol) with
            | Some ticker ->
                tickerCache.TryAdd(symbol, ticker) |> ignore
                ticker
            | None ->
                raise (
                    System.Collections.Generic.KeyNotFoundException(
                        $"Ticker with symbol '{symbol}' not found in Collections.Tickers"
                    )
                )

    /// <summary>
    /// Get a reactive observable that emits the ticker when it becomes available
    /// </summary>
    /// <param name="symbol">Ticker symbol (e.g., "AAPL", "MSFT")</param>
    /// <returns>Observable that emits the ticker</returns>
    let getTickerReactive (symbol: string) : IObservable<Ticker> =
        // Simplified approach: just return the current ticker immediately
        // since we have the fast lookup available
        Observable.Return(getTickerFast (symbol))

    /// <summary>
    /// Get a ticker by ID with O(1) lookup performance.
    /// Falls back to linear search if cache is not populated.
    /// </summary>
    /// <param name="id">Ticker ID</param>
    /// <returns>Ticker matching the ID</returns>
    let getTickerByIdFast (id: int) : Ticker =
        match tickerCacheById.TryGetValue(id) with
        | true, ticker -> ticker
        | false, _ ->
            // Fallback to linear search and cache the result
            match Collections.Tickers.Items |> Seq.tryFind (fun t -> t.Id = id) with
            | Some ticker ->
                tickerCacheById.TryAdd(id, ticker) |> ignore
                ticker
            | None ->
                raise (
                    System.Collections.Generic.KeyNotFoundException(
                        $"Ticker with ID {id} not found in Collections.Tickers"
                    )
                )

    /// <summary>
    /// Get a reactive observable that emits the ticker when it becomes available by ID
    /// </summary>
    /// <param name="id">Ticker ID</param>
    /// <returns>Observable that emits the ticker</returns>
    let getTickerByIdReactive (id: int) : IObservable<Ticker> =
        Observable.Return(getTickerByIdFast (id))

    /// <summary>
    /// Async version for proper awaiting in ImportManager
    /// </summary>
    let refreshAsync () = refresh () |> Async.AwaitTask



/// <summary>
/// Extension methods for reactive ticker operations
/// </summary>
[<Extension>]
type ReactiveTickerExtensions() =

    /// <summary>
    /// Extension method to convert ticker symbol to reactive ticker observable
    /// </summary>
    /// <param name="symbol">Ticker symbol</param>
    /// <returns>Observable that emits the ticker</returns>
    [<Extension>]
    static member ToReactiveTicker(symbol: string) : IObservable<Ticker> =
        ReactiveTickerManager.getTickerReactive (symbol)

    /// <summary>
    /// Extension method to get ticker with fast O(1) lookup
    /// </summary>
    /// <param name="symbol">Ticker symbol</param>
    /// <returns>Ticker matching the symbol</returns>
    [<Extension>]
    static member ToFastTicker(symbol: string) : Ticker =
        ReactiveTickerManager.getTickerFast (symbol)

    /// <summary>
    /// Extension method to get ticker with fast O(1) lookup by ID
    /// </summary>
    /// <param name="id">Ticker ID</param>
    /// <returns>Ticker matching the ID</returns>
    [<Extension>]
    static member ToFastTickerById(id: int) : Ticker =
        ReactiveTickerManager.getTickerByIdFast (id)

    /// <summary>
    /// Extension method to convert ticker ID to reactive ticker observable
    /// </summary>
    /// <param name="id">Ticker ID</param>
    /// <returns>Observable that emits the ticker</returns>
    [<Extension>]
    static member ToReactiveTickerById(id: int) : IObservable<Ticker> =
        ReactiveTickerManager.getTickerByIdReactive (id)
