namespace Binnaculum.Core.DataLoader

open Binnaculum.Core.UI
open DynamicData
open Binnaculum.Core.DatabaseToModels
open Binnaculum.Core.Models

module internal BrokerSnapshotLoader =
    let load() = task {
        let brokers = Collections.Brokers.Items
        
        // Get brokers with valid IDs (exclude default "-1" broker)
        let brokersWithSnapshots = 
            brokers
            |> Seq.filter (fun b -> b.Id > 0) // Exclude the default "-1" broker
            |> Seq.toList

        if brokersWithSnapshots.IsEmpty then
            ()
        else
            let snapshots = 
                brokersWithSnapshots
                |> Seq.map (fun broker ->
                    async {
                        let! latestSnapshot = BrokerSnapshotExtensions.Do.getLatestByBrokerId broker.Id |> Async.AwaitTask
                        match latestSnapshot with
                        | Some dbSnapshot ->
                            // Get financial snapshots for this specific broker using the latest date
                            let! brokerFinancialSnapshots = 
                                BrokerFinancialSnapshotExtensions.Do.getLatestByBrokerIdGroupedByDate broker.Id 
                                |> Async.AwaitTask
                            if not brokerFinancialSnapshots.IsEmpty then
                                return Some (dbSnapshot.brokerSnapshotToOverviewSnapshot(broker, brokerFinancialSnapshots))
                            else
                                return None
                        | None ->
                            return None
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
                    | _ -> () // No change needed
            )
            ()
    }

