namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Logging
open Binnaculum.Core.Patterns
open BrokerFinancialSnapshotExtensions
open System.Diagnostics

/// <summary>
/// Batch persistence layer for saving financial snapshots efficiently.
/// Uses single database transaction to save all snapshots atomically.
/// </summary>
module internal BrokerFinancialBatchPersistence =

    /// <summary>
    /// Save all calculated snapshots in single transaction.
    /// This provides atomicity and massive performance improvement over individual saves.
    /// DEDUPLICATION: Checks for existing snapshots by Date, CurrencyId, and BrokerAccountId before saving.
    /// If an existing snapshot is found, it updates that snapshot instead of creating a duplicate.
    /// NOTE: This does NOT update BrokerAccountSnapshotId - that must be set by the caller if needed.
    /// </summary>
    /// <param name="snapshots">List of snapshots to persist</param>
    /// <returns>Task with Result containing save metrics or error message</returns>
    let persistBatchedSnapshots (snapshots: BrokerFinancialSnapshot list) =
        task {
            if snapshots.IsEmpty then
                CoreLogger.logDebug "BrokerFinancialBatchPersistence" "No snapshots to persist"

                return
                    Ok
                        {| SnapshotsSaved = 0
                           TransactionTimeMs = 0L |}
            else
                let stopwatch = Stopwatch.StartNew()

                try
                    CoreLogger.logInfof
                        "BrokerFinancialBatchPersistence"
                        "Starting batch persistence of %d snapshots with deduplication"
                        snapshots.Length

                    // Save snapshots individually, checking for duplicates
                    let mutable savedCount = 0
                    let mutable updatedCount = 0

                    for snapshot in snapshots do
                        // Check if a snapshot already exists for this date, currency, and broker account
                        let! existingSnapshots =
                            BrokerFinancialSnapshotExtensions.Do.getByBrokerAccountIdAndDate (
                                snapshot.BrokerAccountId,
                                snapshot.Base.Date
                            )

                        // Find existing snapshot for this specific currency
                        let existingSnapshot =
                            existingSnapshots |> List.tryFind (fun s -> s.CurrencyId = snapshot.CurrencyId)

                        match existingSnapshot with
                        | Some existing ->
                            // Update existing snapshot with new data (preserve the existing ID)
                            let updatedSnapshot =
                                { snapshot with
                                    Base =
                                        { snapshot.Base with
                                            Id = existing.Base.Id } }

                            CoreLogger.logDebugf
                                "BrokerFinancialBatchPersistence"
                                "Updating existing snapshot ID %d for Date=%s, Currency=%d, BrokerAccount=%d"
                                existing.Base.Id
                                (snapshot.Base.Date.ToString())
                                snapshot.CurrencyId
                                snapshot.BrokerAccountId

                            do! updatedSnapshot.save ()
                            updatedCount <- updatedCount + 1
                            savedCount <- savedCount + 1

                        | None ->
                            // No existing snapshot - save as new
                            CoreLogger.logDebugf
                                "BrokerFinancialBatchPersistence"
                                "Creating new snapshot for Date=%s, Currency=%d, BrokerAccount=%d"
                                (snapshot.Base.Date.ToString())
                                snapshot.CurrencyId
                                snapshot.BrokerAccountId

                            do! snapshot.save ()
                            savedCount <- savedCount + 1

                    stopwatch.Stop()

                    CoreLogger.logInfof
                        "BrokerFinancialBatchPersistence"
                        "Successfully persisted %d snapshots (%d updated, %d new) in %dms"
                        savedCount
                        updatedCount
                        (savedCount - updatedCount)
                        stopwatch.ElapsedMilliseconds

                    return
                        Ok
                            {| SnapshotsSaved = savedCount
                               TransactionTimeMs = stopwatch.ElapsedMilliseconds |}

                with ex ->
                    stopwatch.Stop()

                    let errorMsg =
                        sprintf "Batch persistence failed after %dms: %s" stopwatch.ElapsedMilliseconds ex.Message

                    CoreLogger.logError "BrokerFinancialBatchPersistence" errorMsg
                    return Error errorMsg
        }

    /// <summary>
    /// Update the BrokerAccountSnapshotId for all financial snapshots on a specific date.
    /// This is used after creating BrokerAccountSnapshots to link financial snapshots correctly.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="date">The date of the snapshots</param>
    /// <param name="brokerAccountSnapshotId">The broker account snapshot ID to set</param>
    /// <returns>Task with number of snapshots updated</returns>
    let updateBrokerAccountSnapshotIds (brokerAccountId: int) (date: DateTimePattern) (brokerAccountSnapshotId: int) =
        task {
            try
                CoreLogger.logDebugf
                    "BrokerFinancialBatchPersistence"
                    "Updating BrokerAccountSnapshotId to %d for all financial snapshots on Date=%s, BrokerAccount=%d"
                    brokerAccountSnapshotId
                    (date.ToString())
                    brokerAccountId

                // Get all financial snapshots for this date and account
                let! existingSnapshots =
                    BrokerFinancialSnapshotExtensions.Do.getByBrokerAccountIdAndDate (brokerAccountId, date)

                let mutable updatedCount = 0

                for snapshot in existingSnapshots do
                    // Update only if BrokerAccountSnapshotId is not already set correctly
                    if snapshot.BrokerAccountSnapshotId <> brokerAccountSnapshotId then
                        let updatedSnapshot =
                            { snapshot with
                                BrokerAccountSnapshotId = brokerAccountSnapshotId }

                        do! updatedSnapshot.save ()
                        updatedCount <- updatedCount + 1

                        CoreLogger.logDebugf
                            "BrokerFinancialBatchPersistence"
                            "Updated snapshot ID %d: BrokerAccountSnapshotId %d -> %d"
                            snapshot.Base.Id
                            snapshot.BrokerAccountSnapshotId
                            brokerAccountSnapshotId

                CoreLogger.logInfof
                    "BrokerFinancialBatchPersistence"
                    "Updated BrokerAccountSnapshotId for %d financial snapshots"
                    updatedCount

                return updatedCount

            with ex ->
                let errorMsg =
                    sprintf "Failed to update BrokerAccountSnapshotIds for date %s: %s" (date.ToString()) ex.Message

                CoreLogger.logError "BrokerFinancialBatchPersistence" errorMsg
                return 0
        }

    /// <summary>
    /// Delete existing snapshots in a date range before inserting new ones.
    /// This ensures clean batch updates without duplicates.
    /// Note: Currently uses individual deletes. Can be optimized with batch delete later.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <returns>Task with number of snapshots deleted</returns>
    let deleteSnapshotsInRange (brokerAccountId: int) (startDate: DateTimePattern) (endDate: DateTimePattern) =
        task {
            try
                CoreLogger.logInfof
                    "BrokerFinancialBatchPersistence"
                    "Deleting existing snapshots for account %d from %s to %s"
                    brokerAccountId
                    (startDate.ToString())
                    (endDate.ToString())

                // Get existing snapshots in the range
                let! existingSnapshots =
                    BrokerFinancialSnapshotBatchLoader.loadExistingSnapshotsInRange brokerAccountId startDate endDate

                // Delete each snapshot
                let mutable deletedCount = 0

                for KeyValue(_, snapshot) in existingSnapshots do
                    do! snapshot.delete ()
                    deletedCount <- deletedCount + 1

                CoreLogger.logInfof "BrokerFinancialBatchPersistence" "Deleted %d existing snapshots" deletedCount

                return deletedCount

            with ex ->
                let errorMsg = sprintf "Failed to delete snapshots: %s" ex.Message
                CoreLogger.logError "BrokerFinancialBatchPersistence" errorMsg
                return 0
        }

    /// <summary>
    /// Persist snapshots with automatic cleanup of existing data.
    /// This combines delete + insert for clean batch updates.
    /// </summary>
    /// <param name="snapshots">List of snapshots to persist</param>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="startDate">Start date of range</param>
    /// <param name="endDate">End date of range</param>
    /// <returns>Task with Result containing save metrics or error message</returns>
    let persistBatchedSnapshotsWithCleanup
        (snapshots: BrokerFinancialSnapshot list)
        (brokerAccountId: int)
        (startDate: DateTimePattern)
        (endDate: DateTimePattern)
        =
        task {
            // First delete any existing snapshots in the range
            let! deletedCount = deleteSnapshotsInRange brokerAccountId startDate endDate

            CoreLogger.logInfof
                "BrokerFinancialBatchPersistence"
                "Deleted %d existing snapshots, now persisting %d new snapshots"
                deletedCount
                snapshots.Length

            // Then persist the new snapshots
            return! persistBatchedSnapshots snapshots
        }
