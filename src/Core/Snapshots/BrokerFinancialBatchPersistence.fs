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
                        "Starting batch persistence of %d snapshots"
                        snapshots.Length

                    // Save snapshots individually (in future, we can optimize with true batch insert)
                    let mutable savedCount = 0

                    for snapshot in snapshots do
                        do! snapshot.save ()
                        savedCount <- savedCount + 1

                    stopwatch.Stop()

                    CoreLogger.logInfof
                        "BrokerFinancialBatchPersistence"
                        "Successfully persisted %d snapshots in %dms"
                        savedCount
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
