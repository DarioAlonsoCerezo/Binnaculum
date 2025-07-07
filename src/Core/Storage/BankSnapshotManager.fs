namespace Binnaculum.Core.Storage

open System
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open BankSnapshotExtensions
open Binnaculum.Core.Storage.SnapshotManagerUtils

/// <summary>
/// Handles creation, updating, and recalculation of BankSnapshots.
/// </summary>
module internal BankSnapshotManager =
    /// <summary>
    /// Calculates BankSnapshot for a specific bank on a specific date
    /// by aggregating all BankAccountSnapshots for that bank on that date
    /// </summary>
    let calculateBankSnapshot (bankId: int, date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            let! allBankAccounts = BankAccountExtensions.Do.getAll()
            let bankAccounts = allBankAccounts |> List.filter (fun acc -> acc.BankId = bankId)
            let! accountSnapshots = 
                bankAccounts
                |> List.map (fun account -> 
                    task {
                        let! existing = BankAccountSnapshotExtensions.Do.getByBankAccountIdAndDate(account.Id, snapshotDate)
                        match existing with
                        | Some snapshot -> return snapshot
                        | None -> 
                            return! BankAccountSnapshotManager.calculateBankAccountSnapshot(account.Id, snapshotDate, account.CurrencyId)
                    })
                |> System.Threading.Tasks.Task.WhenAll
            let snapshots = accountSnapshots |> Array.toList
            let totalBalance = 
                snapshots
                |> List.sumBy (fun s -> s.Balance.Value)
                |> Money.FromAmount
            let totalInterestEarned =
                snapshots
                |> List.sumBy (fun s -> s.InterestEarned.Value)
                |> Money.FromAmount
            let totalFeesPaid =
                snapshots
                |> List.sumBy (fun s -> s.FeesPaid.Value)
                |> Money.FromAmount
            let accountCount = snapshots.Length
            return {
                Base = SnapshotManagerUtils.createBaseSnapshot snapshotDate
                BankId = bankId
                TotalBalance = totalBalance
                InterestEarned = totalInterestEarned
                FeesPaid = totalFeesPaid
                AccountCount = accountCount
            }
        }

    /// <summary>
    /// Creates or updates a BankSnapshot for the given bank and date
    /// </summary>
    let updateBankSnapshot (bankId: int, date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            let! existingSnapshot = BankSnapshotExtensions.Do.getByBankIdAndDate(bankId, snapshotDate)
            let! newSnapshot = calculateBankSnapshot(bankId, snapshotDate)
            match existingSnapshot with
            | Some existing ->
                let updatedSnapshot = { newSnapshot with Base = { newSnapshot.Base with Id = existing.Base.Id } }
                do! updatedSnapshot.save()
            | None ->
                do! newSnapshot.save()
        }

    /// <summary>
    /// Recalculates all bank snapshots from a given date forward for a specific bank
    /// This is used when a retroactive movement affects existing snapshots
    /// </summary>
    let recalculateBankSnapshotsFromDate (bankId: int, fromDate: DateTimePattern) =
        task {
            let startDate = getDateOnly fromDate
            let! futureSnapshots = BankSnapshotExtensions.Do.getByDateRange(bankId, startDate, DateTimePattern.FromDateTime(DateTime.MaxValue))
            for snapshot in futureSnapshots do
                do! updateBankSnapshot(bankId, snapshot.Base.Date)
        }

    /// <summary>
    /// Handles the bank snapshot update part when a bank movement occurs
    /// </summary>
    let handleBankMovementSnapshot (bankId: int, date: DateTimePattern) =
        updateBankSnapshot(bankId, date)
