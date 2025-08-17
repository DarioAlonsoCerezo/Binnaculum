namespace Binnaculum.Core.DataLoader

open Binnaculum.Core.UI
open DynamicData
open Binnaculum.Core.DatabaseToModels
open Binnaculum.Core.Models

module internal BrokerAccountSnapshotLoader =
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
                            return None
                    })
                |> Async.Parallel
                |> Async.RunSynchronously
                |> Array.choose id
                |> Array.toList

            snapshots
            |> List.iter (fun newSnapshot ->
                if newSnapshot.Type = OverviewSnapshotType.BrokerAccount && newSnapshot.BrokerAccount.IsSome then
                    let brokerAccountId = newSnapshot.BrokerAccount.Value.BrokerAccount.Id
                    let existingSnapshot = 
                        Collections.Snapshots.Items 
                        |> Seq.tryFind (fun s -> s.Type = OverviewSnapshotType.BrokerAccount && s.BrokerAccount.IsSome && s.BrokerAccount.Value.BrokerAccount.Id = brokerAccountId)
                    match existingSnapshot with
                    | Some existing when existing <> newSnapshot ->
                        Collections.Snapshots.Replace(existing, newSnapshot)
                    | None ->
                        Collections.Snapshots.Add(newSnapshot)
                    | Some _ -> () // Same snapshot, no action needed
                else
                    () // Ignore empty or invalid snapshots
            )
    }

