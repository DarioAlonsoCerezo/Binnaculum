namespace Binnaculum.Core.DataLoader

open Binnaculum.Core.UI
open DynamicData
open Binnaculum.Core.DatabaseToModels
open Binnaculum.Core.Models

module internal BankAccountSnapshotLoader =
    let load() = task {
        let bankAccounts = 
            Collections.Accounts.Items 
            |> Seq.filter (fun a -> a.Bank.IsSome)
            |> Seq.map (fun a -> a.Bank.Value)
            |> Seq.toList

        if bankAccounts.IsEmpty then
            return ()
        else
            let snapshots = 
                bankAccounts
                |> Seq.map (fun bankAccount ->
                    async {
                        let! latestSnapshot = BankAccountSnapshotExtensions.Do.getLatestByBankAccountId bankAccount.Id |> Async.AwaitTask
                        match latestSnapshot with
                        | Some dbSnapshot ->
                            return Some (dbSnapshot.bankAccountSnapshotToOverviewSnapshot(bankAccount))
                        | None ->
                            return None
                    })
                |> Async.Parallel
                |> Async.RunSynchronously
                |> Array.choose id
                |> Array.toList

            snapshots 
            |> List.iter (fun newSnapshot ->
                if newSnapshot.Type = OverviewSnapshotType.BankAccount && newSnapshot.BankAccount.IsSome then
                    let bankAccountId = newSnapshot.BankAccount.Value.BankAccount.Id
                    let existingSnapshot = 
                        Collections.Snapshots.Items 
                        |> Seq.tryFind (fun s -> 
                            s.Type = OverviewSnapshotType.BankAccount && 
                            s.BankAccount.IsSome && 
                            s.BankAccount.Value.BankAccount.Id = bankAccountId)
                    
                    match existingSnapshot with
                    | Some existing ->
                        if existing <> newSnapshot then Collections.Snapshots.Replace(existing, newSnapshot)
                        else () // Same snapshot, no action needed
                    | None ->
                        Collections.Snapshots.Add(newSnapshot)
                else
                    () // Ignore empty or invalid snapshots
            )
    }

