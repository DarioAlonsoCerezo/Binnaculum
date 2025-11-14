namespace Binnaculum.Core.DataLoader

open Binnaculum.Core.UI
open DynamicData
open Binnaculum.Core.DatabaseToModels
open Binnaculum.Core.Models
open Binnaculum.Core.Storage
open Binnaculum.Core.Patterns

module internal BrokerAccountSnapshotLoader =

    let private loadFinancialSnapshotsForBrokerAccountSnapshot (brokerAccountId: int) (snapshotDate: DateTimePattern) =
        task {
            // Load financial snapshots for the specific broker account snapshot
            // CoreLogger.logDebugf "BrokerAccountSnapshotLoader" "Loading financial snapshots for BrokerAccountId: %A, SnapshotDate: %A" brokerAccountId snapshotDate
            let! financialSnapshots =
                BrokerFinancialSnapshotExtensions.Do.getByBrokerAccountIdAndDate (brokerAccountId, snapshotDate)
            // CoreLogger.logDebugf "BrokerAccountSnapshotLoader" "Found %A financial snapshots" financialSnapshots.Length
            return financialSnapshots
        }

    let load () =
        task {
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
                            let! latestSnapshot =
                                BrokerAccountSnapshotExtensions.Do.getLatestByBrokerAccountId brokerAccount.Id
                                |> Async.AwaitTask

                            match latestSnapshot with
                            | Some dbSnapshot ->
                                // Load financial snapshots for this broker account snapshot
                                let! financialSnapshots =
                                    loadFinancialSnapshotsForBrokerAccountSnapshot
                                        brokerAccount.Id
                                        dbSnapshot.Base.Date
                                    |> Async.AwaitTask

                                return
                                    Some(
                                        dbSnapshot.brokerAccountSnapshotToOverviewSnapshot (
                                            financialSnapshots,
                                            brokerAccount
                                        )
                                    )
                            | None ->
                                // Create default snapshot for broker account without snapshots
                                let! databaseBrokerAccount =
                                    BrokerAccountExtensions.Do.getById brokerAccount.Id |> Async.AwaitTask

                                match databaseBrokerAccount with
                                | Some dbAccount ->
                                    do!
                                        BrokerAccountSnapshotManager.handleNewBrokerAccount (dbAccount)
                                        |> Async.AwaitTask
                                        |> Async.Ignore

                                    let! newSnapshot =
                                        BrokerAccountSnapshotExtensions.Do.getLatestByBrokerAccountId brokerAccount.Id
                                        |> Async.AwaitTask

                                    match newSnapshot with
                                    | Some dbSnapshot ->
                                        // Load financial snapshots for the newly created snapshot
                                        let! financialSnapshots =
                                            loadFinancialSnapshotsForBrokerAccountSnapshot
                                                brokerAccount.Id
                                                dbSnapshot.Base.Date
                                            |> Async.AwaitTask

                                        return
                                            Some(
                                                dbSnapshot.brokerAccountSnapshotToOverviewSnapshot (
                                                    financialSnapshots,
                                                    brokerAccount
                                                )
                                            )
                                    | None ->
                                        failwithf
                                            "Failed to create or retrieve snapshot for broker account %s (ID: %d)"
                                            brokerAccount.AccountNumber
                                            brokerAccount.Id

                                        return None
                                | None ->
                                    failwithf
                                        "Failed to retrieve database broker account %s (ID: %d)"
                                        brokerAccount.AccountNumber
                                        brokerAccount.Id

                                    return None
                        })
                    |> Async.Parallel
                    |> Async.RunSynchronously
                    |> Array.choose id
                    |> Array.toList

                // Update collections with loaded snapshots
                snapshots
                |> List.iter (fun newSnapshot ->
                    if
                        newSnapshot.Type = OverviewSnapshotType.BrokerAccount
                        && newSnapshot.BrokerAccount.IsSome
                    then
                        let brokerAccountId = newSnapshot.BrokerAccount.Value.BrokerAccount.Id

                        let existingSnapshot =
                            Collections.Snapshots.Items
                            |> Seq.tryFind (fun s ->
                                s.Type = OverviewSnapshotType.BrokerAccount
                                && s.BrokerAccount.IsSome
                                && s.BrokerAccount.Value.BrokerAccount.Id = brokerAccountId)

                        match existingSnapshot with
                        | Some existing when existing <> newSnapshot ->
                            Collections.Snapshots.Replace(existing, newSnapshot)
                        | None -> Collections.Snapshots.Add(newSnapshot)
                        | Some _ -> () // Same snapshot, no action needed
                )
        }
