namespace Binnaculum.Core.Storage

open System
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open BrokerAccountSnapshotExtensions
open Binnaculum.Core.Storage.SnapshotManagerUtils

/// <summary>
/// Handles creation, updating, and recalculation of BrokerAccountSnapshots.
/// </summary>
module internal BrokerAccountSnapshotManager =
    /// <summary>
    /// Calculates BrokerAccountSnapshot for a specific broker account on a specific date
    /// by aggregating all movements up to and including that date
    /// </summary>
    let calculateBrokerAccountSnapshot (brokerAccountId: int, date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            // Aggregate all movements up to and including the date
            let! movements = BrokerMovementExtensions.Do.getByBrokerAccountIdAndDateRange(brokerAccountId, snapshotDate)
            let netCash = 
                movements
                |> List.sumBy (fun m -> 
                    match m.MovementType with
                    | BrokerMovementType.Deposit -> m.Amount.Value
                    | BrokerMovementType.Withdrawal -> -m.Amount.Value
                    | BrokerMovementType.Fee -> -m.Amount.Value
                    | BrokerMovementType.InterestsGained -> m.Amount.Value
                    | BrokerMovementType.InterestsPaid -> -m.Amount.Value
                    | _ -> 0m)
                |> Money.FromAmount
            return {
                Base = SnapshotManagerUtils.createBaseSnapshot snapshotDate
                BrokerAccountId = brokerAccountId
                PortfolioValue = netCash
            }
        }

    /// <summary>
    /// Creates or updates a BrokerAccountSnapshot for the given broker account and date
    /// </summary>
    let updateBrokerAccountSnapshot (brokerAccountId: int, date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            let! existingSnapshot = BrokerAccountSnapshotExtensions.Do.getByBrokerAccountIdAndDate(brokerAccountId, snapshotDate)
            let! newSnapshot = calculateBrokerAccountSnapshot(brokerAccountId, snapshotDate)
            match existingSnapshot with
            | Some existing ->
                let updatedSnapshot = { newSnapshot with Base = { newSnapshot.Base with Id = existing.Base.Id } }
                do! updatedSnapshot.save()
            | None ->
                do! newSnapshot.save()
            // After saving, update the corresponding broker snapshot
            let! brokerAccountOpt = BrokerAccountExtensions.Do.getById(brokerAccountId)
            match brokerAccountOpt with
            | Some brokerAccount ->
                do! Binnaculum.Core.Storage.BrokerSnapshotManager.updateBrokerSnapshot(brokerAccount.BrokerId, snapshotDate)
            | None ->
                ()
        }

    /// <summary>
    /// Recalculates all snapshots from a given date forward for a specific broker account
    /// This is used when a retroactive movement affects existing snapshots
    /// </summary>
    let recalculateBrokerAccountSnapshotsFromDate (brokerAccountId: int, fromDate: DateTimePattern) =
        task {
            let startDate = getDateOnly fromDate
            let! futureSnapshots = BrokerAccountSnapshotExtensions.Do.getByDateRange(brokerAccountId, startDate, DateTimePattern.FromDateTime(DateTime.MaxValue))
            for snapshot in futureSnapshots do
                do! updateBrokerAccountSnapshot(brokerAccountId, snapshot.Base.Date)
        }

    /// <summary>
    /// Handles snapshot updates when a new BrokerAccount is created
    /// Creates snapshots for the current day
    /// </summary>
    let handleNewBrokerAccount (brokerAccount: BrokerAccount) =
        task {
            let today = DateTimePattern.FromDateTime(DateTime.Today)
            do! updateBrokerAccountSnapshot(brokerAccount.Id, today)
            // After creating a new broker account snapshot, update the broker snapshot
            do! Binnaculum.Core.Storage.BrokerSnapshotManager.updateBrokerSnapshot(brokerAccount.BrokerId, today)
        }

    /// <summary>
    /// Handles the broker account snapshot update part when a broker movement occurs
    /// </summary>
    let handleBrokerMovementSnapshot (brokerAccountId: int, date: DateTimePattern) =
        updateBrokerAccountSnapshot(brokerAccountId, date)
