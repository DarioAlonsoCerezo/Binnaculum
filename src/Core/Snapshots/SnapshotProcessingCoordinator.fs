namespace Binnaculum.Core.Storage

open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging

/// <summary>
/// Coordinates snapshot processing strategy (batch vs per-date).
/// This module sits above both BrokerAccountSnapshotManager and BrokerFinancialBatchManager
/// in the compilation order, allowing it to choose between strategies.
///
/// PHASE 4: In-Memory Financial Calculations Migration
/// This module enables gradual rollout of batch processing with fallback to per-date mode.
/// </summary>
module internal SnapshotProcessingCoordinator =

    /// <summary>
    /// Feature flag to enable batch processing mode.
    /// Default: false (safe rollout - no behavior change until explicitly enabled)
    /// </summary>
    let mutable private useBatchMode = false

    /// <summary>
    /// Enable or disable batch processing mode.
    /// This allows gradual rollout and easy rollback if issues are discovered.
    /// </summary>
    /// <param name="enabled">Whether to enable batch mode</param>
    let enableBatchMode (enabled: bool) = useBatchMode <- enabled
    // CoreLogger.logInfof "SnapshotProcessingCoordinator" "Batch mode %s" (if enabled then "ENABLED" else "DISABLED")

    /// <summary>
    /// Check if batch mode is currently enabled.
    /// </summary>
    let isBatchModeEnabled () = useBatchMode

    /// <summary>
    /// Coordinated entry point for handling broker account changes.
    /// Decides whether to use batch or per-date processing based on the feature flag.
    /// Falls back to per-date mode if batch processing fails.
    ///
    /// This is the recommended entry point for all broker account snapshot updates
    /// when batch mode is desired. Direct calls to BrokerAccountSnapshotManager.handleBrokerAccountChange
    /// will always use per-date mode.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID that changed</param>
    /// <param name="date">The date of the change</param>
    /// <returns>Task that completes when processing finishes</returns>
    let handleBrokerAccountChange (brokerAccountId: int, date: DateTimePattern) =
        task {
            // CoreLogger.logDebugf
            //     "SnapshotProcessingCoordinator"
            //     "handleBrokerAccountChange - AccountId: %d, Date: %s, BatchMode: %b"
            //     brokerAccountId
            //     (date.ToString())
            //     useBatchMode

            // ========== BATCH MODE PATH ==========
            if useBatchMode then
                // CoreLogger.logDebug "SnapshotProcessingCoordinator" "Attempting batch processing mode..."

                // Determine date range for batch processing
                // Strategy: Process from this date through today to catch all historical snapshots
                // No need to process future dates - they will be created when movements occur
                let startDate = date
                let endDate = DateTimePattern.FromDateTime(System.DateTime.Now) // Only process up to today

                // CoreLogger.logInfof
                //     "SnapshotProcessingCoordinator"
                //     "Batch mode: Processing date range %s to %s"
                //     (startDate.ToString())
                //     (endDate.ToString())

                // Create batch request
                let batchRequest =
                    { BrokerFinancialBatchManager.BatchProcessingRequest.BrokerAccountId = brokerAccountId
                      BrokerFinancialBatchManager.BatchProcessingRequest.StartDate = startDate
                      BrokerFinancialBatchManager.BatchProcessingRequest.EndDate = endDate
                      BrokerFinancialBatchManager.BatchProcessingRequest.ForceRecalculation = false }

                // Execute batch processing (no operations or ticker snapshots needed in non-import context)
                let! batchResult = BrokerFinancialBatchManager.processBatchedFinancials batchRequest [] [] None

                if batchResult.Success then
                    // CoreLogger.logInfof
                    //     "SnapshotProcessingCoordinator"
                    //     "Batch mode SUCCESS: %d snapshots saved, %d dates processed in %dms (Load: %dms, Calc: %dms, Persist: %dms)"
                    //     batchResult.SnapshotsSaved
                    //     batchResult.DatesProcessed
                    //     batchResult.TotalTimeMs
                    //     batchResult.LoadTimeMs
                    //     batchResult.CalculationTimeMs
                    //     batchResult.PersistenceTimeMs

                    return () // Success - exit early
                else
                    // Batch mode failed - fall through to per-date processing
                    CoreLogger.logErrorf
                        "SnapshotProcessingCoordinator"
                        "Batch mode FAILED: %s - Falling back to per-date processing"
                        (String.concat "; " batchResult.Errors)

            // ========== PER-DATE MODE PATH (FALLBACK) ==========
            // CoreLogger.logDebug "SnapshotProcessingCoordinator" "Using per-date processing mode..."

            // Delegate to existing per-date logic
            do! BrokerAccountSnapshotManager.handleBrokerAccountChange (brokerAccountId, date)

        // CoreLogger.logDebug "SnapshotProcessingCoordinator" "Per-date processing completed successfully"
        }
