namespace Binnaculum.Core.DataLoader

open Binnaculum.Core.UI
open DynamicData
open Binnaculum.Core.DatabaseToModels
open Binnaculum.Core.Models

module internal BankSnapshotLoader = 
    let load() = task {
        let banks = Collections.Banks.Items
        let snapshots = 
            banks
            |> Seq.filter (fun b -> b.Id > 0) // Exclude the default "-1" bank
            |> Seq.map (fun bank ->
                async {
                    let! latestSnapshot = BankSnapshotExtensions.Do.getLatestByBankId bank.Id |> Async.AwaitTask
                    match latestSnapshot with
                    | Some dbSnapshot ->
                        return Some (dbSnapshot.bankSnapshotToOverviewSnapshot(bank))
                    | None ->
                        return None
                })
            |> Async.Parallel
            |> Async.RunSynchronously
            |> Array.choose id
            |> Array.toList
        
        snapshots
        |> List.iter (fun newSnapshot ->
            if newSnapshot.Type = OverviewSnapshotType.Bank && newSnapshot.Bank.IsSome then
                let bankId = newSnapshot.Bank.Value.Bank.Id
                let existingSnapshot = Collections.Snapshots.Items 
                                     |> Seq.tryFind (fun s -> s.Type = OverviewSnapshotType.Bank && s.Bank.IsSome && s.Bank.Value.Bank.Id = bankId)
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

