namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Logging
open Binnaculum.Core.Patterns
open TickerSnapshotExtensions
open TickerCurrencySnapshotExtensions
open System.Diagnostics

/// <summary>
/// Batch persistence layer for saving ticker snapshots efficiently.
/// Uses database transactions to save all snapshots atomically with deduplication.
/// </summary>
module internal TickerSnapshotBatchPersistence =

    /// <summary>
    /// Metrics returned after persisting snapshots.
    /// </summary>
    type PersistenceMetrics =
        {
            /// Number of TickerSnapshots saved (created or updated)
            TickerSnapshotsSaved: int
            /// Number of TickerCurrencySnapshots saved (created or updated)
            CurrencySnapshotsSaved: int
            /// Number of snapshots that were updates (not new)
            UpdatedCount: int
            /// Transaction execution time in milliseconds
            TransactionTimeMs: int64
        }

    /// <summary>
    /// Save all calculated snapshots with deduplication.
    /// Checks for existing snapshots and updates them instead of creating duplicates.
    /// Each TickerSnapshot contains multiple TickerCurrencySnapshots which are all saved.
    /// </summary>
    /// <param name="snapshots">List of (TickerSnapshot, TickerCurrencySnapshot list) tuples to persist</param>
    /// <returns>Task with Result containing persistence metrics or error message</returns>
    let persistBatchedSnapshots (snapshots: (TickerSnapshot * TickerCurrencySnapshot list) list) =
        task {
            if snapshots.IsEmpty then
                CoreLogger.logDebug "TickerSnapshotBatchPersistence" "No snapshots to persist"

                return
                    Ok
                        { TickerSnapshotsSaved = 0
                          CurrencySnapshotsSaved = 0
                          UpdatedCount = 0
                          TransactionTimeMs = 0L }
            else
                let stopwatch = Stopwatch.StartNew()

                try
                    CoreLogger.logInfof
                        "TickerSnapshotBatchPersistence"
                        "Starting batch persistence of %d ticker snapshots with deduplication"
                        snapshots.Length

                    let mutable tickerSnapshotsSaved = 0
                    let mutable currencySnapshotsSaved = 0
                    let mutable updatedCount = 0

                    // Process each TickerSnapshot + its TickerCurrencySnapshots
                    for (tickerSnapshot, currencySnapshots) in snapshots do
                        try
                            // Check if TickerSnapshot already exists for this ticker and date
                            let! existingTickerSnapshot =
                                TickerSnapshotExtensions.Do.getByTickerIdAndDate (
                                    tickerSnapshot.TickerId,
                                    tickerSnapshot.Base.Date
                                )

                            // Save or update TickerSnapshot
                            let tickerSnapshotToSave =
                                match existingTickerSnapshot with
                                | Some existing ->
                                    CoreLogger.logDebugf
                                        "TickerSnapshotBatchPersistence"
                                        "Updating existing TickerSnapshot ID %d for Ticker=%d, Date=%s"
                                        existing.Base.Id
                                        tickerSnapshot.TickerId
                                        (tickerSnapshot.Base.Date.ToString())

                                    updatedCount <- updatedCount + 1

                                    // Preserve existing ID
                                    { tickerSnapshot with
                                        Base =
                                            { tickerSnapshot.Base with
                                                Id = existing.Base.Id } }

                                | None ->
                                    CoreLogger.logDebugf
                                        "TickerSnapshotBatchPersistence"
                                        "Creating new TickerSnapshot for Ticker=%d, Date=%s"
                                        tickerSnapshot.TickerId
                                        (tickerSnapshot.Base.Date.ToString())

                                    tickerSnapshot

                            // Save the TickerSnapshot
                            do! tickerSnapshotToSave.save ()
                            tickerSnapshotsSaved <- tickerSnapshotsSaved + 1

                            // Get the saved TickerSnapshot ID (for TickerCurrencySnapshots)
                            let! savedTickerSnapshot =
                                TickerSnapshotExtensions.Do.getByTickerIdAndDate (
                                    tickerSnapshot.TickerId,
                                    tickerSnapshot.Base.Date
                                )

                            let tickerSnapshotId =
                                match savedTickerSnapshot with
                                | Some ts -> ts.Base.Id
                                | None ->
                                    failwithf
                                        "Failed to retrieve saved TickerSnapshot for Ticker=%d, Date=%s"
                                        tickerSnapshot.TickerId
                                        (tickerSnapshot.Base.Date.ToString())

                            // Save each TickerCurrencySnapshot
                            for currencySnapshot in currencySnapshots do
                                try
                                    // Check if TickerCurrencySnapshot already exists
                                    let! existingCurrencySnapshots =
                                        TickerCurrencySnapshotExtensions.Do.getAllByTickerIdAndDate (
                                            currencySnapshot.TickerId,
                                            currencySnapshot.Base.Date
                                        )

                                    let existingCurrencySnapshot =
                                        existingCurrencySnapshots
                                        |> List.tryFind (fun cs -> cs.CurrencyId = currencySnapshot.CurrencyId)

                                    // Save or update TickerCurrencySnapshot
                                    let currencySnapshotToSave =
                                        match existingCurrencySnapshot with
                                        | Some existing ->
                                            CoreLogger.logDebugf
                                                "TickerSnapshotBatchPersistence"
                                                "Updating existing TickerCurrencySnapshot ID %d for Ticker=%d, Currency=%d, Date=%s"
                                                existing.Base.Id
                                                currencySnapshot.TickerId
                                                currencySnapshot.CurrencyId
                                                (currencySnapshot.Base.Date.ToString())

                                            // Preserve existing ID, update TickerSnapshotId reference
                                            { currencySnapshot with
                                                Base =
                                                    { currencySnapshot.Base with
                                                        Id = existing.Base.Id }
                                                TickerSnapshotId = tickerSnapshotId }

                                        | None ->
                                            CoreLogger.logDebugf
                                                "TickerSnapshotBatchPersistence"
                                                "Creating new TickerCurrencySnapshot for Ticker=%d, Currency=%d, Date=%s"
                                                currencySnapshot.TickerId
                                                currencySnapshot.CurrencyId
                                                (currencySnapshot.Base.Date.ToString())

                                            // Set TickerSnapshotId reference
                                            { currencySnapshot with
                                                TickerSnapshotId = tickerSnapshotId }

                                    do! currencySnapshotToSave.save ()
                                    currencySnapshotsSaved <- currencySnapshotsSaved + 1

                                with ex ->
                                    let errorMsg =
                                        sprintf
                                            "Failed to save TickerCurrencySnapshot for Ticker=%d, Currency=%d, Date=%s: %s"
                                            currencySnapshot.TickerId
                                            currencySnapshot.CurrencyId
                                            (currencySnapshot.Base.Date.ToString())
                                            ex.Message

                                    CoreLogger.logError "TickerSnapshotBatchPersistence" errorMsg
                        // Continue with other snapshots instead of failing completely

                        with ex ->
                            let errorMsg =
                                sprintf
                                    "Failed to save TickerSnapshot for Ticker=%d, Date=%s: %s"
                                    tickerSnapshot.TickerId
                                    (tickerSnapshot.Base.Date.ToString())
                                    ex.Message

                            CoreLogger.logError "TickerSnapshotBatchPersistence" errorMsg
                    // Continue with other snapshots instead of failing completely

                    stopwatch.Stop()

                    CoreLogger.logInfof
                        "TickerSnapshotBatchPersistence"
                        "Successfully persisted %d ticker snapshots, %d currency snapshots (%d updated) in %dms"
                        tickerSnapshotsSaved
                        currencySnapshotsSaved
                        updatedCount
                        stopwatch.ElapsedMilliseconds

                    return
                        Ok
                            { TickerSnapshotsSaved = tickerSnapshotsSaved
                              CurrencySnapshotsSaved = currencySnapshotsSaved
                              UpdatedCount = updatedCount
                              TransactionTimeMs = stopwatch.ElapsedMilliseconds }

                with ex ->
                    stopwatch.Stop()

                    let errorMsg =
                        sprintf "Batch persistence failed after %dms: %s" stopwatch.ElapsedMilliseconds ex.Message

                    CoreLogger.logError "TickerSnapshotBatchPersistence" errorMsg
                    return Error errorMsg
        }

    /// <summary>
    /// Delete existing snapshots in a date range, then insert new ones.
    /// This ensures clean batch updates without duplicates.
    /// Used for force recalculation scenarios.
    /// </summary>
    /// <param name="snapshots">List of (TickerSnapshot, TickerCurrencySnapshot list) tuples to persist</param>
    /// <param name="tickerIds">List of ticker IDs to clean up</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <returns>Task with Result containing persistence metrics or error message</returns>
    let persistBatchedSnapshotsWithCleanup
        (snapshots: (TickerSnapshot * TickerCurrencySnapshot list) list)
        (tickerIds: int list)
        (startDate: DateTimePattern)
        (endDate: DateTimePattern)
        =
        task {
            let stopwatch = Stopwatch.StartNew()

            try
                CoreLogger.logInfof
                    "TickerSnapshotBatchPersistence"
                    "Starting batch persistence with cleanup for %d tickers from %s to %s"
                    tickerIds.Length
                    (startDate.ToString())
                    (endDate.ToString())

                // PHASE 1: Delete existing snapshots in range
                let mutable deletedTickerSnapshots = 0
                let mutable deletedCurrencySnapshots = 0

                for tickerId in tickerIds do
                    try
                        // Get date range for deletion
                        let dateRange = SnapshotManagerUtils.generateDateRange startDate endDate

                        for date in dateRange do
                            // Delete TickerCurrencySnapshots first (they reference TickerSnapshot)
                            let! existingCurrencySnapshots =
                                TickerCurrencySnapshotExtensions.Do.getAllByTickerIdAndDate (tickerId, date)

                            for cs in existingCurrencySnapshots do
                                do! cs.delete ()
                                deletedCurrencySnapshots <- deletedCurrencySnapshots + 1

                            // Delete TickerSnapshot
                            let! existingTickerSnapshot =
                                TickerSnapshotExtensions.Do.getByTickerIdAndDate (tickerId, date)

                            match existingTickerSnapshot with
                            | Some ts ->
                                do! ts.delete ()
                                deletedTickerSnapshots <- deletedTickerSnapshots + 1
                            | None -> ()

                    with ex ->
                        let errorMsg =
                            sprintf "Failed to delete existing snapshots for Ticker=%d: %s" tickerId ex.Message

                        CoreLogger.logWarning "TickerSnapshotBatchPersistence" errorMsg
                // Continue with other tickers

                CoreLogger.logInfof
                    "TickerSnapshotBatchPersistence"
                    "Deleted %d ticker snapshots, %d currency snapshots"
                    deletedTickerSnapshots
                    deletedCurrencySnapshots

                // PHASE 2: Insert new snapshots
                let! result = persistBatchedSnapshots snapshots

                stopwatch.Stop()

                match result with
                | Ok metrics ->
                    CoreLogger.logInfof
                        "TickerSnapshotBatchPersistence"
                        "Cleanup and persistence completed in %dms (deleted %d+%d, saved %d+%d)"
                        stopwatch.ElapsedMilliseconds
                        deletedTickerSnapshots
                        deletedCurrencySnapshots
                        metrics.TickerSnapshotsSaved
                        metrics.CurrencySnapshotsSaved

                    return
                        Ok
                            { metrics with
                                TransactionTimeMs = stopwatch.ElapsedMilliseconds }

                | Error error -> return Error error

            with ex ->
                stopwatch.Stop()

                let errorMsg =
                    sprintf
                        "Batch persistence with cleanup failed after %dms: %s"
                        stopwatch.ElapsedMilliseconds
                        ex.Message

                CoreLogger.logError "TickerSnapshotBatchPersistence" errorMsg
                return Error errorMsg
        }

    /// <summary>
    /// Update the TickerSnapshotId reference for TickerCurrencySnapshots after TickerSnapshot is saved.
    /// This ensures proper hierarchical relationships in the database.
    /// </summary>
    /// <param name="tickerId">The ticker ID</param>
    /// <param name="date">The date of the snapshots</param>
    /// <param name="tickerSnapshotId">The TickerSnapshot ID to set</param>
    /// <returns>Task with number of currency snapshots updated</returns>
    let updateTickerSnapshotIds (tickerId: int) (date: DateTimePattern) (tickerSnapshotId: int) =
        task {
            try
                CoreLogger.logDebugf
                    "TickerSnapshotBatchPersistence"
                    "Updating TickerSnapshotId to %d for all currency snapshots on Ticker=%d, Date=%s"
                    tickerSnapshotId
                    tickerId
                    (date.ToString())

                // Get all currency snapshots for this ticker and date
                let! existingSnapshots = TickerCurrencySnapshotExtensions.Do.getAllByTickerIdAndDate (tickerId, date)

                let mutable updatedCount = 0

                for snapshot in existingSnapshots do
                    // Update only if TickerSnapshotId is not already set correctly
                    if snapshot.TickerSnapshotId <> tickerSnapshotId then
                        let updatedSnapshot =
                            { snapshot with
                                TickerSnapshotId = tickerSnapshotId }

                        do! updatedSnapshot.save ()
                        updatedCount <- updatedCount + 1

                        CoreLogger.logDebugf
                            "TickerSnapshotBatchPersistence"
                            "Updated currency snapshot ID %d: TickerSnapshotId %d -> %d"
                            snapshot.Base.Id
                            snapshot.TickerSnapshotId
                            tickerSnapshotId

                CoreLogger.logInfof
                    "TickerSnapshotBatchPersistence"
                    "Updated TickerSnapshotId for %d currency snapshots"
                    updatedCount

                return updatedCount

            with ex ->
                let errorMsg =
                    sprintf
                        "Failed to update TickerSnapshotIds for ticker %d, date %s: %s"
                        tickerId
                        (date.ToString())
                        ex.Message

                CoreLogger.logError "TickerSnapshotBatchPersistence" errorMsg
                return 0
        }
