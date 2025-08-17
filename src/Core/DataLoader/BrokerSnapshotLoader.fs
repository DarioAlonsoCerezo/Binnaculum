namespace Binnaculum.Core.DataLoader

open Binnaculum.Core.UI
open DynamicData
open Binnaculum.Core.DatabaseToModels
open Binnaculum.Core.Models
open Binnaculum.Core

module internal BrokerSnapshotLoader =
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
        let brokers = Collections.Brokers.Items
        
        // Get brokers with valid IDs (exclude default "-1" broker)
        let brokersWithSnapshots = 
            brokers
            |> Seq.filter (fun b -> b.Id > 0) // Exclude the default "-1" broker
            |> Seq.toList

        if brokersWithSnapshots.IsEmpty then
            return []
        else
            let snapshots = 
                brokersWithSnapshots
                |> Seq.map (fun broker ->
                    async {
                        try
                            let! latestSnapshot = BrokerSnapshotExtensions.Do.getLatestByBrokerId broker.Id |> Async.AwaitTask
                            match latestSnapshot with
                            | Some dbSnapshot ->
                                // Get financial snapshots for this specific broker using the latest date
                                let! brokerFinancialSnapshots = 
                                    BrokerFinancialSnapshotExtensions.Do.getLatestByBrokerIdGroupedByDate broker.Id 
                                    |> Async.AwaitTask
                                return Some (dbSnapshot.brokerSnapshotToOverviewSnapshot(broker, brokerFinancialSnapshots))
                            | None ->
                                return Some (DatabaseToModels.Do.createEmptyOverviewSnapshot())
                        with
                        | _ ->
                            return Some (DatabaseToModels.Do.createEmptyOverviewSnapshot())
                    })
                |> Async.Parallel
                |> Async.RunSynchronously
                |> Array.choose id
                |> Array.toList
            
            snapshots
            |> List.iter (fun newSnapshot ->
                if newSnapshot.Type = OverviewSnapshotType.Broker && newSnapshot.Broker.IsSome then
                    let brokerId = newSnapshot.Broker.Value.Broker.Id
                    let existingSnapshot = Collections.Snapshots.Items 
                                         |> Seq.tryFind (fun s -> s.Type = OverviewSnapshotType.Broker && s.Broker.IsSome && s.Broker.Value.Broker.Id = brokerId)
                    match existingSnapshot with
                    | Some existing when existing <> newSnapshot ->
                        Collections.Snapshots.Replace(existing, newSnapshot)
                    | None ->
                        addNonEmptySnapshotWithEmptyCleanup newSnapshot
                    | _ -> () // No change needed
            )
            
            return snapshots
    }

