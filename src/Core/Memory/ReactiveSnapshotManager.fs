namespace Binnaculum.Core.UI

open System
open System.Reactive.Linq
open DynamicData
open Binnaculum.Core.Models
open Binnaculum.Core.DataLoader
open Binnaculum.Core

/// <summary>
/// Reactive snapshot manager that provides automatic snapshot updates when underlying collections change.
/// This replaces the manual DataLoader.loadOverviewSnapshots() method with reactive patterns.
/// </summary>
module ReactiveSnapshotManager =

    /// <summary>
    /// Subscription for managing reactive updates from all base collections
    /// </summary>
    let mutable private baseCollectionsSubscription: System.IDisposable option = None

    /// <summary>
    /// Reentrancy protection flag to prevent concurrent executions of loadSnapshots
    /// </summary>
    let mutable private isLoadingSnapshots = false

    /// <summary>
    /// Load snapshots from database and update Collections.Snapshots
    /// This is the core function that does the same work as DataLoader.loadOverviewSnapshots()
    /// </summary>
    let private loadSnapshots () =
        async {
            // Prevent reentrancy to avoid infinite loops during snapshot creation
            if isLoadingSnapshots then
                // CoreLogger.logDebug "ReactiveSnapshotManager" "Skipping loadSnapshots - already in progress"
                return ()

            // Defer reactive updates during import to prevent database connection conflicts
            if Binnaculum.Core.Import.ImportState.isImportInProgress () then
                // CoreLogger.logDebug "ReactiveSnapshotManager" "Skipping loadSnapshots - import in progress, will update after completion"

                return ()

            try
                isLoadingSnapshots <- true
                // CoreLogger.logDebug "ReactiveSnapshotManager" "Starting loadSnapshots"

                // Load all snapshot types (same as original loadOverviewSnapshots)
                do! BrokerSnapshotLoader.load () |> Async.AwaitTask |> Async.Ignore
                do! BankSnapshotLoader.load () |> Async.AwaitTask |> Async.Ignore
                do! BrokerAccountSnapshotLoader.load () |> Async.AwaitTask |> Async.Ignore
                do! BankAccountSnapshotLoader.load () |> Async.AwaitTask |> Async.Ignore

                // Check for empty snapshots and real snapshots
                let emptySnapshots =
                    Collections.Snapshots.Items
                    |> Seq.filter (fun s -> s.Type = Models.OverviewSnapshotType.Empty)
                    |> Seq.toList

                let realSnapshots =
                    Collections.Snapshots.Items
                    |> Seq.filter (fun s -> s.Type <> Models.OverviewSnapshotType.Empty)
                    |> Seq.toList

                // If we have real snapshots, remove any empty snapshots
                if not (List.isEmpty realSnapshots) then
                    emptySnapshots |> List.iter (Collections.Snapshots.Remove >> ignore)
                // If we have no snapshots at all, add empty snapshot
                elif Collections.Snapshots.Items.Count = 0 then
                    Collections.Snapshots.Add(DatabaseToModels.Do.createEmptyOverviewSnapshot ())

                // CoreLogger.logDebug "ReactiveSnapshotManager" "Completed loadSnapshots"
            finally
                isLoadingSnapshots <- false
        }

    /// <summary>
    /// Create observable that triggers when any base collection changes
    /// Snapshots depend on all collections because they aggregate financial data from all entities
    /// </summary>
    let private createBaseCollectionsObservable () =
        [ Collections.Movements.Connect().Select(fun _ -> ()) // Snapshots depend on movements
          Collections.Currencies.Connect().Select(fun _ -> ()) // Financial snapshots need currencies
          Collections.Brokers.Connect().Select(fun _ -> ()) // Broker snapshots need brokers
          Collections.Banks.Connect().Select(fun _ -> ()) // Bank snapshots need banks
          Collections.Accounts.Connect().Select(fun _ -> ()) ] // All snapshots reference accounts
        |> Observable.Merge

    /// <summary>
    /// Initialize the reactive snapshot manager by subscribing to base collection changes
    /// </summary>
    let initialize () =
        if baseCollectionsSubscription.IsNone then
            let observable = createBaseCollectionsObservable ()

            let sub =
                observable.Subscribe(fun _ ->
                    // Trigger snapshot loading when any base collection changes
                    loadSnapshots () |> Async.StartImmediate)

            baseCollectionsSubscription <- Some sub

    /// <summary>
    /// Trigger a manual snapshot refresh and wait for completion
    /// This fixes the async timing issue where imported data doesn't appear in UI immediately
    /// </summary>
    let refreshAsync () = loadSnapshots ()

    /// <summary>
    /// Trigger a manual snapshot refresh (for compatibility during transition)
    /// This provides the same interface as the original DataLoader.loadOverviewSnapshots()
    /// </summary>
    let refresh () =
        loadSnapshots () |> Async.StartImmediate

    /// <summary>
    /// Dispose all subscriptions (should be called at application shutdown)
    /// </summary>
    let dispose () =
        baseCollectionsSubscription |> Option.iter (fun sub -> sub.Dispose())
        baseCollectionsSubscription <- None
