namespace Binnaculum.Core.DataLoader

open Binnaculum.Core.UI
open DynamicData
open Binnaculum.Core.DatabaseToModels
open Binnaculum.Core
open Binnaculum.Core.Models

module internal BrokerAccountSnapshotLoader =
    /// <summary>
    /// Manages the addition of snapshots to Collections.Snapshots ensuring that:
    /// 1. At most one Empty OverviewSnapshot exists at any time
    /// 2. If any non-Empty OverviewSnapshot is present, no Empty snapshots exist
    /// </summary>
    let private addSnapshotWithEmptyManagement (newSnapshot: OverviewSnapshot) =
        if newSnapshot.Type = OverviewSnapshotType.Empty then
            // For empty snapshots, only add if:
            // 1. No empty snapshots already exist, AND
            // 2. No non-empty snapshots exist
            let existingEmptySnapshots = Collections.Snapshots.Items |> Seq.filter (fun s -> s.Type = OverviewSnapshotType.Empty) |> Seq.toList
            let existingNonEmptySnapshots = Collections.Snapshots.Items |> Seq.filter (fun s -> s.Type <> OverviewSnapshotType.Empty) |> Seq.toList
            if existingEmptySnapshots.IsEmpty && existingNonEmptySnapshots.IsEmpty then
                Collections.Snapshots.Add(newSnapshot)
        else
            // For non-empty snapshots, first remove any existing empty snapshots
            let emptySnapshots = Collections.Snapshots.Items |> Seq.filter (fun s -> s.Type = OverviewSnapshotType.Empty) |> Seq.toList
            emptySnapshots |> List.iter (Collections.Snapshots.Remove >> ignore)
            
            // Then add the new non-empty snapshot
            Collections.Snapshots.Add(newSnapshot)
    
    /// <summary>
    /// Ensures empty snapshots are removed when adding non-empty snapshots
    /// </summary>
    let private addNonEmptySnapshotWithEmptyCleanup (newSnapshot: OverviewSnapshot) =
        // Remove any existing empty snapshots before adding non-empty snapshot
        let emptySnapshots = Collections.Snapshots.Items |> Seq.filter (fun s -> s.Type = OverviewSnapshotType.Empty) |> Seq.toList
        emptySnapshots |> List.iter (Collections.Snapshots.Remove >> ignore)
        
        // Add the new non-empty snapshot
        Collections.Snapshots.Add(newSnapshot)

    let load() = task {
        let brokerAccounts = 
            Collections.Accounts.Items 
            |> Seq.filter (fun a -> a.Broker.IsSome)
            |> Seq.map (fun a -> a.Broker.Value)
            |> Seq.toList

        if brokerAccounts.IsEmpty then
            return ()
        else
            let snapshots = 
                brokerAccounts
                |> Seq.map (fun brokerAccount ->
                    async {
                        let! latestSnapshot = BrokerAccountSnapshotExtensions.Do.getLatestByBrokerAccountId brokerAccount.Id |> Async.AwaitTask
                        match latestSnapshot with
                        | Some dbSnapshot ->
                            return Some (dbSnapshot.brokerAccountSnapshotToOverviewSnapshot(brokerAccount))
                        | None ->
                            return Some (DatabaseToModels.Do.createEmptyOverviewSnapshot())
                    })
                |> Async.Parallel
                |> Async.RunSynchronously
                |> Array.choose id
                |> Array.toList
        
            snapshots
            |> List.iter (fun newSnapshot ->
                if newSnapshot.Type = OverviewSnapshotType.BrokerAccount && newSnapshot.BrokerAccount.IsSome then
                    let brokerAccountId = newSnapshot.BrokerAccount.Value.BrokerAccount.Id
                    let existingSnapshot = Collections.Snapshots.Items 
                                         |> Seq.tryFind (fun s -> s.Type = OverviewSnapshotType.BrokerAccount && s.BrokerAccount.IsSome && s.BrokerAccount.Value.BrokerAccount.Id = brokerAccountId)
                    match existingSnapshot with
                    | Some existing when existing <> newSnapshot ->
                        Collections.Snapshots.Replace(existing, newSnapshot)
                    | None ->
                        addNonEmptySnapshotWithEmptyCleanup(newSnapshot)
                    | Some _ -> () // Same snapshot, no action needed
                else
                    // For empty snapshots, use the helper function to manage properly
                    addSnapshotWithEmptyManagement(newSnapshot)
            )
    }

