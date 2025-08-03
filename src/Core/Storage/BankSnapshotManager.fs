namespace Binnaculum.Core.Storage

open System
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Storage.SnapshotManagerUtils

/// <summary>
/// Handles creation, updating, and recalculation of BankSnapshots and BankAccountSnapshots in a standardized way.
/// </summary>
module internal BankSnapshotManager =
    /// Helper type for bank snapshot calculation
    type private BankSnapshotCalculationData = {
        BankId: int
        Date: DateTimePattern
        AccountSnapshots: BankAccountSnapshot list
    }

    /// Create a default BankSnapshot with zeroed metrics
    let private createDefaultBankSnapshot (bankId: int) (date: DateTimePattern) =
        let snapshotDate = getDateOnly date
        {
            Base = createBaseSnapshot snapshotDate
            BankId = bankId
            TotalBalance = Money.FromAmount(0.0m)
            InterestEarned = Money.FromAmount(0.0m)
            FeesPaid = Money.FromAmount(0.0m)
            AccountCount = 0
        }

    /// Calculate a BankSnapshot from calculation data
    let private calculateBankSnapshot (data: BankSnapshotCalculationData) =
        let totalBalance = data.AccountSnapshots |> List.sumBy (fun s -> s.Balance.Value) |> Money.FromAmount
        let totalInterestEarned = data.AccountSnapshots |> List.sumBy (fun s -> s.InterestEarned.Value) |> Money.FromAmount
        let totalFeesPaid = data.AccountSnapshots |> List.sumBy (fun s -> s.FeesPaid.Value) |> Money.FromAmount
        let accountCount = data.AccountSnapshots.Length
        {
            Base = createBaseSnapshot (getDateOnly data.Date)
            BankId = data.BankId
            TotalBalance = totalBalance
            InterestEarned = totalInterestEarned
            FeesPaid = totalFeesPaid
            AccountCount = accountCount
        }

    /// <summary>
    /// Handles snapshot initialization when a new bank is created.
    /// Creates initial BankSnapshot and BankAccountSnapshots for today's date.
    /// </summary>
    let handleNewBank (bank: Bank) =
        task {
            let today = DateTimePattern.FromDateTime(DateTime.Today)
            let defaultBankSnapshot = createDefaultBankSnapshot bank.Id today
            do! BankSnapshotExtensions.Do.save(defaultBankSnapshot)
        }

    /// <summary>
    /// Creates or updates a BankSnapshot for the given bank and date
    /// </summary>
    let updateBankSnapshot (bankId: int, date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            let! existingSnapshot = BankSnapshotExtensions.Do.getByBankIdAndDate(bankId, snapshotDate)
            let! bankAccounts = BankAccountExtensions.Do.getByBankId(bankId)
            let! accountSnapshots =
                bankAccounts
                |> List.map (fun account -> BankAccountSnapshotExtensions.Do.getByBankAccountIdAndDate(account.Id, snapshotDate))
                |> System.Threading.Tasks.Task.WhenAll
            let snapshots = accountSnapshots |> Array.choose id |> Array.toList
            let calcData = { BankId = bankId; Date = date; AccountSnapshots = snapshots }
            let newSnapshot = calculateBankSnapshot calcData
            let newSnapshotWithId =
                match existingSnapshot with
                | Some s -> { newSnapshot with Base = { newSnapshot.Base with Id = s.Base.Id } }
                | None -> newSnapshot
            do! BankSnapshotExtensions.Do.save(newSnapshotWithId)
        }

    /// <summary>
    /// Handles bank-related changes for a specific date, cascading updates if future snapshots exist.
    /// </summary>
    let handleBankChange (bankId: int, date: DateTimePattern) =
        task {
            let! futureSnapshots = BankSnapshotExtensions.Do.getBankSnapshotsAfterDate(bankId, date)
            if futureSnapshots |> List.isEmpty then
                do! updateBankSnapshot(bankId, date)
            else
                do! updateBankSnapshot(bankId, date)
                for snapshot in futureSnapshots do
                    do! updateBankSnapshot(bankId, snapshot.Base.Date)
        }

    /// <summary>
    /// Handles bank account-related changes for a specific date, updating parent bank snapshot as needed.
    /// </summary>
    let handleBankAccountChange (bankAccountId: int, date: DateTimePattern) =
        task {
            let! bankAccount = BankAccountExtensions.Do.getById(bankAccountId)
            match bankAccount with
            | None -> ()
            | Some bankAccount ->
                // Only trigger BankSnapshot recalculation, do not modify BankAccountSnapshot
                do! handleBankChange(bankAccount.BankId, date)
        }
