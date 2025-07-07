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
    let calculateBankAccountSnapshot (bankAccountId: int, date: DateTimePattern, currencyId: int) = 
        task {
            let snapshotDate = getDateOnly date
            let! allMovements = BankAccountBalanceExtensions.Do.getAll()
            let movementsUpToDate = 
                allMovements
                |> List.filter (fun m -> m.BankAccountId = bankAccountId && m.TimeStamp.Value.Date <= snapshotDate.Value.Date)
            let totalBalance = 
                movementsUpToDate
                |> List.sumBy (fun m -> 
                    match m.MovementType with
                    | BankAccountMovementType.Balance -> m.Amount.Value
                    | BankAccountMovementType.Interest -> m.Amount.Value
                    | BankAccountMovementType.Fee -> -m.Amount.Value)
                |> Money.FromAmount
            let interestEarned =
                movementsUpToDate
                |> List.filter (fun m -> m.MovementType = BankAccountMovementType.Interest)
                |> List.sumBy (fun m -> m.Amount.Value)
                |> Money.FromAmount
            let feesPaid =
                movementsUpToDate
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
        }

    /// <summary>
    /// Creates or updates a BankAccountSnapshot for the given bank account and date
    /// </summary>
    let updateBankAccountSnapshot (bankAccountId: int, date: DateTimePattern, currencyId: int) =
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
    /// Recalculates all snapshots from a given date forward for a specific bank account
    /// This is used when a retroactive movement affects existing snapshots
    /// </summary>
    let recalculateBankAccountSnapshotsFromDate (bankAccountId: int, fromDate: DateTimePattern, currencyId: int) =
        task {
            let startDate = getDateOnly fromDate
            let! futureSnapshots = BankAccountSnapshotExtensions.Do.getByDateRange(bankAccountId, startDate, DateTimePattern.FromDateTime(DateTime.MaxValue))
            for snapshot in futureSnapshots do
                do! updateBankAccountSnapshot(bankAccountId, snapshot.Base.Date, currencyId)
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
