namespace Binnaculum.Core.Storage

open System
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open BankAccountSnapshotExtensions
open Binnaculum.Core.Storage.SnapshotManagerUtils

/// <summary>
/// Handles creation, updating, and recalculation of BankAccountSnapshots.
/// </summary>
module internal BankAccountSnapshotManager =
    /// <summary>
    /// Calculates BankAccountSnapshot for a specific bank account on a specific date
    /// by aggregating all movements up to and including that date
    /// </summary>
    let private calculateBankAccountSnapshot (bankAccountId: int, date: DateTimePattern, currencyId: int) = 
        task {
            let snapshotDate = getDateOnly date
            // Try to get the latest snapshot before the given date
            let! previousSnapshotOpt = BankAccountSnapshotExtensions.Do.getLatestBeforeDateByBankAccountId(bankAccountId, snapshotDate)
            match previousSnapshotOpt with
            | None ->
                // No previous snapshot, calculate from scratch using all movements up to and including the date
                let! movements = BankAccountBalanceExtensions.Do.getByBankAccountIdAndDateTo(bankAccountId, snapshotDate)
                let totalBalance = 
                    movements
                    |> List.sumBy (fun m -> 
                        match m.MovementType with
                        | BankAccountMovementType.Balance -> m.Amount.Value
                        | BankAccountMovementType.Interest -> m.Amount.Value
                        | BankAccountMovementType.Fee -> -m.Amount.Value)
                    |> Money.FromAmount
                let interestEarned =
                    movements
                    |> List.filter (fun m -> m.MovementType = BankAccountMovementType.Interest)
                    |> List.sumBy (fun m -> m.Amount.Value)
                    |> Money.FromAmount
                let feesPaid =
                    movements
                    |> List.filter (fun m -> m.MovementType = BankAccountMovementType.Fee)
                    |> List.sumBy (fun m -> m.Amount.Value)
                    |> Money.FromAmount
                return {
                    Base = SnapshotManagerUtils.createBaseSnapshot snapshotDate
                    BankAccountId = bankAccountId
                    Balance = totalBalance
                    CurrencyId = currencyId
                    InterestEarned = interestEarned
                    FeesPaid = feesPaid
                }
            | Some prevSnapshot ->
                // Previous snapshot exists, only aggregate movements after previous snapshot date up to and including the target date
                let prevDate = prevSnapshot.Base.Date
                let! movements = BankAccountBalanceExtensions.Do.getByBankAccountIdAndDateRange(bankAccountId, prevDate, snapshotDate)
                let balanceDelta =
                    movements
                    |> List.sumBy (fun m -> 
                        match m.MovementType with
                        | BankAccountMovementType.Balance -> m.Amount.Value
                        | BankAccountMovementType.Interest -> m.Amount.Value
                        | BankAccountMovementType.Fee -> -m.Amount.Value)
                let interestDelta =
                    movements
                    |> List.filter (fun m -> m.MovementType = BankAccountMovementType.Interest)
                    |> List.sumBy (fun m -> m.Amount.Value)
                let feesDelta =
                    movements
                    |> List.filter (fun m -> m.MovementType = BankAccountMovementType.Fee)
                    |> List.sumBy (fun m -> m.Amount.Value)
                return {
                    Base = SnapshotManagerUtils.createBaseSnapshot snapshotDate
                    BankAccountId = bankAccountId
                    Balance = Money.FromAmount (prevSnapshot.Balance.Value + balanceDelta)
                    CurrencyId = currencyId
                    InterestEarned = Money.FromAmount (prevSnapshot.InterestEarned.Value + interestDelta)
                    FeesPaid = Money.FromAmount (prevSnapshot.FeesPaid.Value + feesDelta)
                }
        }

    /// <summary>
    /// Creates or updates a BankAccountSnapshot for the given bank account and date
    /// </summary>
    let private updateBankAccountSnapshot (bankAccountId: int, date: DateTimePattern, currencyId: int) =
        task {
            let snapshotDate = getDateOnly date
            let! existingSnapshot = BankAccountSnapshotExtensions.Do.getByBankAccountIdAndDate(bankAccountId, snapshotDate)
            let! newSnapshot = calculateBankAccountSnapshot(bankAccountId, snapshotDate, currencyId)
            match existingSnapshot with
            | Some existing ->
                let updatedSnapshot = { newSnapshot with Base = { newSnapshot.Base with Id = existing.Base.Id } }
                do! updatedSnapshot.save()
            | None ->
                do! newSnapshot.save()
        }

    /// <summary>
    /// Handles snapshot updates when a new BankAccount is created
    /// Creates snapshots for the current day
    /// </summary>
    let handleNewBankAccount (bankAccount: BankAccount) =
        task {
            let today = DateTimePattern.FromDateTime(DateTime.Today)
            do! updateBankAccountSnapshot(bankAccount.Id, today, bankAccount.CurrencyId)
        }

    /// <summary>
    /// Handles bank account-related changes for a specific date, automatically cascading updates if future snapshots exist.
    /// </summary>
    let handleBankAccountChange (bankAccountId: int, date: DateTimePattern) =
        task {
            let! bankAccountOpt = BankAccountExtensions.Do.getById(bankAccountId)
            match bankAccountOpt with
            | Some bankAccount ->
                let currencyId = bankAccount.CurrencyId
                let! subsequentSnapshots =
                    BankAccountSnapshotExtensions.Do.getByDateRange(
                        bankAccountId,
                        date,
                        DateTimePattern.FromDateTime(DateTime.MaxValue)
                    )
                if subsequentSnapshots.IsEmpty then
                    do! updateBankAccountSnapshot(bankAccountId, date, currencyId)
                else
                    do! updateBankAccountSnapshot(bankAccountId, date, currencyId)
                    for snapshot in subsequentSnapshots do
                        do! updateBankAccountSnapshot(bankAccountId, snapshot.Base.Date, currencyId)
            | None -> ()
        }
