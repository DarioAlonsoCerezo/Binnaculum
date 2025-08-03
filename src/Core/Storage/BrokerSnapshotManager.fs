namespace Binnaculum.Core.Storage

open System
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open BrokerSnapshotExtensions
open Binnaculum.Core.Storage.SnapshotManagerUtils
open BrokerAccountExtensions // For getting BrokerAccount by id

/// <summary>
/// Handles creation, updating, and recalculation of BrokerSnapshots.
/// </summary>
module internal BrokerSnapshotManager =
    /// <summary>
    /// Calculates BrokerSnapshot for a specific broker on a specific date
    /// by aggregating all BrokerAccountSnapshots for that broker on that date
    /// </summary>
    let calculateBrokerSnapshot (brokerId: int, date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            let! allBrokerAccounts = BrokerAccountExtensions.Do.getAll()
            let brokerAccounts = allBrokerAccounts |> List.filter (fun acc -> acc.BrokerId = brokerId)
            let! accountSnapshots = 
                brokerAccounts
                |> List.map (fun account -> 
                    BrokerAccountSnapshotExtensions.Do.getByBrokerAccountIdAndDate(account.Id, snapshotDate))
                |> System.Threading.Tasks.Task.WhenAll
            let snapshots = 
                accountSnapshots
                |> Array.choose id // Only use existing snapshots
                |> Array.toList
            let accountCount = snapshots.Length
            return {
                Base = SnapshotManagerUtils.createBaseSnapshot snapshotDate
                BrokerId = brokerId
                AccountCount = accountCount
            }
        }

    /// <summary>
    /// Creates or updates a BrokerSnapshot for the given broker and date
    /// </summary>
    let updateBrokerSnapshot (brokerId: int, date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            let! existingSnapshot = BrokerSnapshotExtensions.Do.getByBrokerIdAndDate(brokerId, snapshotDate)
            let! newSnapshot = calculateBrokerSnapshot(brokerId, snapshotDate)
            match existingSnapshot with
            | Some existing ->
                let updatedSnapshot = { newSnapshot with Base = { newSnapshot.Base with Id = existing.Base.Id } }
                do! updatedSnapshot.save()
            | None ->
                do! newSnapshot.save()
        }

    /// <summary>
    /// Recalculates all broker snapshots from a given date forward for a specific broker
    /// This is used when a retroactive movement affects existing snapshots
    /// </summary>
    let recalculateBrokerSnapshotsFromDate (brokerId: int, fromDate: DateTimePattern) =
        task {
            let startDate = getDateOnly fromDate
            let! futureSnapshots = BrokerSnapshotExtensions.Do.getByDateRange(brokerId, startDate, DateTimePattern.FromDateTime(DateTime.MaxValue))
            for snapshot in futureSnapshots do
                do! updateBrokerSnapshot(brokerId, snapshot.Base.Date)
        }

    /// <summary>
    /// Handles the broker snapshot update part when a broker movement occurs
    /// </summary>
    let handleBrokerMovementSnapshot (brokerId: int, date: DateTimePattern) =
        updateBrokerSnapshot(brokerId, date)
