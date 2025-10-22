namespace Core.Tests.Integration

open System
open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks
open System.Reactive.Linq
open DynamicData
open Binnaculum.Core.UI

/// <summary>
/// Signal types representing reactive stream emissions
/// </summary>
type Signal =
    | Accounts_Updated
    | Movements_Updated
    | Snapshots_Updated
    | Tickers_Updated
    | Currencies_Updated
    | Brokers_Updated
    | Banks_Updated

/// <summary>
/// Reactive stream observer for signal-based testing.
/// Monitors Collections streams and emits signals when changes occur.
/// Provides deterministic waiting for expected signals instead of arbitrary delays.
/// </summary>
module StreamObserver =
    
    let private subscriptions = ResizeArray<IDisposable>()
    let private expectedSignals = ConcurrentBag<Signal>()
    let private receivedSignals = ConcurrentBag<Signal>()
    let private completionSourceRef = ref None
    let private lockObj = obj()
    
    /// <summary>
    /// Record that a signal was received
    /// </summary>
    let signalReceived (signal: Signal) : unit =
        lock lockObj (fun () ->
            receivedSignals.Add(signal)
            printfn "[StreamObserver] Signal received: %A (Total: %d/%d)" 
                signal receivedSignals.Count expectedSignals.Count
            
            // Check if all expected signals have been received
            match !completionSourceRef with
            | Some (tcs: TaskCompletionSource<bool>) when not tcs.Task.IsCompleted ->
                let expected = expectedSignals |> Seq.toList
                let received = receivedSignals |> Seq.toList
                
                // Check if all expected signals are in received
                let allReceived = 
                    expected 
                    |> List.forall (fun exp -> 
                        received |> List.filter ((=) exp) |> List.length >= 
                        (expected |> List.filter ((=) exp) |> List.length))
                
                if allReceived then
                    printfn "[StreamObserver] ✅ All expected signals received!"
                    (tcs: TaskCompletionSource<bool>).SetResult(true)
            | _ -> ()
        )
    
    /// <summary>
    /// Start observing all reactive streams
    /// </summary>
    let startObserving() : unit =
        lock lockObj (fun () ->
            printfn "[StreamObserver] 📡 Starting observation of reactive collections..."
            
            // Clear previous observations
            subscriptions |> Seq.iter (fun d -> d.Dispose())
            subscriptions.Clear()
            expectedSignals.Clear()
            receivedSignals.Clear()
            completionSourceRef := None
            
            // Observe Collections.Accounts stream
            let accountsSub = 
                Collections.Accounts.Connect()
                    .Subscribe(fun changes ->
                        if changes.Count > 0 then
                            signalReceived Accounts_Updated)
            subscriptions.Add(accountsSub)
            printfn "[StreamObserver] ✓ Subscribed to Collections.Accounts"
            
            // Observe Collections.Movements stream
            let movementsSub = 
                Collections.Movements.Connect()
                    .Subscribe(fun changes ->
                        if changes.Count > 0 then
                            signalReceived Movements_Updated)
            subscriptions.Add(movementsSub)
            printfn "[StreamObserver] ✓ Subscribed to Collections.Movements"
            
            // Observe Collections.Snapshots stream
            let snapshotsSub = 
                Collections.Snapshots.Connect()
                    .Subscribe(fun changes ->
                        if changes.Count > 0 then
                            signalReceived Snapshots_Updated)
            subscriptions.Add(snapshotsSub)
            printfn "[StreamObserver] ✓ Subscribed to Collections.Snapshots"
            
            // Observe Collections.Tickers stream
            let tickersSub = 
                Collections.Tickers.Connect()
                    .Subscribe(fun changes ->
                        if changes.Count > 0 then
                            signalReceived Tickers_Updated)
            subscriptions.Add(tickersSub)
            printfn "[StreamObserver] ✓ Subscribed to Collections.Tickers"
            
            // Observe Collections.Currencies stream
            let currenciesSub = 
                Collections.Currencies.Connect()
                    .Subscribe(fun changes ->
                        if changes.Count > 0 then
                            signalReceived Currencies_Updated)
            subscriptions.Add(currenciesSub)
            printfn "[StreamObserver] ✓ Subscribed to Collections.Currencies"
            
            // Observe Collections.Brokers stream
            let brokersSub = 
                Collections.Brokers.Connect()
                    .Subscribe(fun changes ->
                        if changes.Count > 0 then
                            signalReceived Brokers_Updated)
            subscriptions.Add(brokersSub)
            printfn "[StreamObserver] ✓ Subscribed to Collections.Brokers"
            
            // Observe Collections.Banks stream
            let banksSub = 
                Collections.Banks.Connect()
                    .Subscribe(fun changes ->
                        if changes.Count > 0 then
                            signalReceived Banks_Updated)
            subscriptions.Add(banksSub)
            printfn "[StreamObserver] ✓ Subscribed to Collections.Banks"
            
            printfn "[StreamObserver] ✅ Observation started for all collections"
        )
    
    /// <summary>
    /// Stop observing reactive streams
    /// </summary>
    let stopObserving() : unit =
        lock lockObj (fun () ->
            printfn "[StreamObserver] Stopping observation..."
            subscriptions |> Seq.iter (fun d -> d.Dispose())
            subscriptions.Clear()
            expectedSignals.Clear()
            receivedSignals.Clear()
            completionSourceRef := None
            printfn "[StreamObserver] ✅ Observation stopped"
        )
    
    /// <summary>
    /// Declare expected signals before performing an operation
    /// </summary>
    let expectSignals (signals: Signal list) : unit =
        lock lockObj (fun () ->
            expectedSignals.Clear()
            receivedSignals.Clear()
            signals |> List.iter expectedSignals.Add
            completionSourceRef := Some (new TaskCompletionSource<bool>())
            printfn "[StreamObserver] 🎯 Expecting %d signals: %A" signals.Length signals
        )
    
    /// <summary>
    /// Get the current status of signal reception
    /// Returns (expected signals, received signals, missing signals)
    /// </summary>
    let getSignalStatus() : (Signal array * Signal array * Signal array) =
        lock lockObj (fun () ->
            let expected = expectedSignals |> Seq.toArray
            let received = receivedSignals |> Seq.toArray
            let missing = 
                expected 
                |> Array.filter (fun exp ->
                    let expectedCount = expected |> Array.filter ((=) exp) |> Array.length
                    let receivedCount = received |> Array.filter ((=) exp) |> Array.length
                    receivedCount < expectedCount)
            
            (expected, received, missing)
        )
    
    /// <summary>
    /// Wait for all expected signals to be received (with timeout)
    /// Returns true if all signals were received, false if timeout occurred
    /// </summary>
    let waitForAllSignalsAsync (timeout: TimeSpan) : Async<bool> =
        async {
            match !completionSourceRef with
            | None ->
                printfn "[StreamObserver] ⚠️  No signals expected - call expectSignals first"
                return false
            | Some tcs ->
                printfn "[StreamObserver] ⏳ Waiting for signals (timeout: %A)..." timeout
                
                use cts = new CancellationTokenSource(timeout)
                use _ = cts.Token.Register(fun () ->
                    if not tcs.Task.IsCompleted then
                        printfn "[StreamObserver] ⏰ Timeout reached"
                        tcs.TrySetResult(false) |> ignore
                )
                
                let! result = tcs.Task |> Async.AwaitTask
                
                if result then
                    printfn "[StreamObserver] ✅ All signals received successfully"
                else
                    let (expected, received, missing) = getSignalStatus()
                    printfn "[StreamObserver] ❌ Timeout - Expected: %A, Received: %A, Missing: %A" 
                        expected received missing
                
                return result
        }
