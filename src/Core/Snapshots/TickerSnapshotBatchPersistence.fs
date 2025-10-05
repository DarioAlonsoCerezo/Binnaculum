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
    /// Save all calculated snapshots with deduplication and proper FK linking.
    /// PHASE 1: Save parent TickerSnapshots and get their database IDs
    /// PHASE 2: Update TickerCurrencySnapshots with correct TickerSnapshotId FKs
    /// PHASE 3: Save child TickerCurrencySnapshots with proper FK references
    /// </summary>
    /// <param name="batchResult">TickerSnapshotBatchResult containing separate lists of snapshots</param>
    /// <returns>Task with Result containing persistence metrics or error message</returns>
    let persistBatchedSnapshots (batchResult: TickerSnapshotBatchCalculator.TickerSnapshotBatchResult) =
        task {
            if batchResult.TickerSnapshots.IsEmpty && batchResult.CurrencySnapshots.IsEmpty then
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
                        "Starting batch persistence of %d ticker snapshots and %d currency snapshots with deduplication"
                        batchResult.TickerSnapshots.Length
                        batchResult.CurrencySnapshots.Length

                    let mutable tickerSnapshotsSaved = 0
                    let mutable currencySnapshotsSaved = 0
                    let mutable updatedCount = 0

                    // PHASE 1: Save parent TickerSnapshots and build ID lookup
                    let mutable tickerSnapshotIdLookup: Map<(int * DateTimePattern), int> = Map.empty

                    for tickerSnapshot in batchResult.TickerSnapshots do
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

                            // Get the saved TickerSnapshot ID and add to lookup
                            let! savedTickerSnapshot =
                                TickerSnapshotExtensions.Do.getByTickerIdAndDate (
                                    tickerSnapshot.TickerId,
                                    tickerSnapshot.Base.Date
                                )

                            match savedTickerSnapshot with
                            | Some ts ->
                                let key = (tickerSnapshot.TickerId, tickerSnapshot.Base.Date)
                                tickerSnapshotIdLookup <- tickerSnapshotIdLookup.Add(key, ts.Base.Id)

                                CoreLogger.logDebugf
                                    "TickerSnapshotBatchPersistence"
                                    "Saved TickerSnapshot ID %d for Ticker=%d, Date=%s"
                                    ts.Base.Id
                                    tickerSnapshot.TickerId
                                    (tickerSnapshot.Base.Date.ToString())

                            | None ->
                                failwithf
                                    "Failed to retrieve saved TickerSnapshot for Ticker=%d, Date=%s"
                                    tickerSnapshot.TickerId
                                    (tickerSnapshot.Base.Date.ToString())

                        with ex ->
                            let errorMsg =
                                sprintf
                                    "Failed to save TickerSnapshot for Ticker=%d, Date=%s: %s"
                                    tickerSnapshot.TickerId
                                    (tickerSnapshot.Base.Date.ToString())
                                    ex.Message

                            CoreLogger.logError "TickerSnapshotBatchPersistence" errorMsg
                    // Continue with other snapshots

                    // PHASE 2 & 3: Update and save TickerCurrencySnapshots with correct FKs
                    for currencySnapshot in batchResult.CurrencySnapshots do
                        try
                            // Look up the TickerSnapshotId from our saved snapshots
                            let key = (currencySnapshot.TickerId, currencySnapshot.Base.Date)

                            match tickerSnapshotIdLookup.TryFind(key) with
                            | Some tickerSnapshotId ->
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

                            | None ->
                                CoreLogger.logWarningf
                                    "TickerSnapshotBatchPersistence"
                                    "Skipping TickerCurrencySnapshot - no parent TickerSnapshot found for Ticker=%d, Date=%s"
                                    currencySnapshot.TickerId
                                    (currencySnapshot.Base.Date.ToString())

                        with ex ->
                            let errorMsg =
                                sprintf
                                    "Failed to save TickerCurrencySnapshot for Ticker=%d, Currency=%d, Date=%s: %s"
                                    currencySnapshot.TickerId
                                    currencySnapshot.CurrencyId
                                    (currencySnapshot.Base.Date.ToString())
                                    ex.Message

                            CoreLogger.logError "TickerSnapshotBatchPersistence" errorMsg
                    // Continue with other snapshots

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
