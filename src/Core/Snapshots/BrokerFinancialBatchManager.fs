namespace Binnaculum.Core.Storage

open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging
open System.Diagnostics

/// <summary>
/// Main orchestration manager for batch financial snapshot processing.
/// Coordinates loading, calculation, and persistence for optimal performance.
/// </summary>
module internal BrokerFinancialBatchManager =

    /// <summary>
    /// Request parameters for batch processing.
    /// </summary>
    type internal BatchProcessingRequest =
        { BrokerAccountId: int
          StartDate: DateTimePattern
          EndDate: DateTimePattern
          ForceRecalculation: bool }

    /// <summary>
    /// Result of batch processing with detailed metrics.
    /// </summary>
    type internal BatchProcessingResult =
        { Success: bool
          SnapshotsSaved: int
          DatesProcessed: int
          MovementsProcessed: int
          LoadTimeMs: int64
          CalculationTimeMs: int64
          PersistenceTimeMs: int64
          TotalTimeMs: int64
          Errors: string list }

    /// <summary>
    /// Main entry point for batch financial processing.
    /// This replaces the per-date processing with efficient batch operations.
    /// </summary>
    /// <param name="request">The batch processing request</param>
    /// <returns>Task containing BatchProcessingResult with all metrics</returns>
    let processBatchedFinancials
        (request: BatchProcessingRequest)
        : System.Threading.Tasks.Task<BatchProcessingResult> =
        task {
            let totalStopwatch = Stopwatch.StartNew()
            let mutable errors = []

            try
                CoreLogger.logInfof
                    "BrokerFinancialBatchManager"
                    "Starting batch processing for account %d from %s to %s"
                    request.BrokerAccountId
                    (request.StartDate.ToString())
                    (request.EndDate.ToString())

                // ========== PHASE 1: LOAD ALL REQUIRED DATA ==========
                let loadStopwatch = Stopwatch.StartNew()

                CoreLogger.logDebug "BrokerFinancialBatchManager" "Phase 1: Loading data..."

                // Load all movements in the date range
                let! movementsData =
                    BrokerMovementBatchLoader.loadMovementsForDateRange
                        request.BrokerAccountId
                        request.StartDate
                        request.EndDate

                // Load baseline snapshots (before start date)
                let! baselineSnapshots =
                    BrokerFinancialSnapshotBatchLoader.loadBaselineSnapshots request.BrokerAccountId request.StartDate

                loadStopwatch.Stop()

                CoreLogger.logInfof
                    "BrokerFinancialBatchManager"
                    "Data loading completed: %d broker movements, %d trades, %d dividends, %d baseline currencies in %dms"
                    movementsData.BrokerMovements.Length
                    movementsData.Trades.Length
                    movementsData.Dividends.Length
                    baselineSnapshots.Count
                    loadStopwatch.ElapsedMilliseconds

                // ========== PHASE 2: CALCULATE ALL SNAPSHOTS IN MEMORY ==========
                CoreLogger.logDebug "BrokerFinancialBatchManager" "Phase 2: Calculating snapshots..."

                // Generate date range for processing
                let dateRange =
                    BrokerFinancialBatchCalculator.generateDateRange request.StartDate request.EndDate

                // Group movements by date for efficient lookup
                let movementsByDate =
                    BrokerMovementBatchLoader.groupMovementsByDate request.BrokerAccountId movementsData

                // Load existing snapshots in date range (for scenarios C, D, G, H)
                // TODO: Phase 3 - implement loadExistingSnapshotsInRange
                // For now, use empty map (batch will create new snapshots)
                let existingSnapshots = Map.empty

                // Create calculation context
                let context: BrokerFinancialBatchCalculator.BatchCalculationContext =
                    { BaselineSnapshots = baselineSnapshots
                      MovementsByDate = movementsByDate
                      ExistingSnapshots = existingSnapshots
                      DateRange = dateRange
                      BrokerAccountId = request.BrokerAccountId
                      BrokerAccountSnapshotId = 0 // Will be set appropriately for each snapshot
                    }

                // Calculate all snapshots in memory
                let calculationResult =
                    BrokerFinancialBatchCalculator.calculateBatchedFinancials context

                errors <- errors @ calculationResult.Errors

                CoreLogger.logInfof
                    "BrokerFinancialBatchManager"
                    "Batch calculations completed: %d snapshots calculated from %d movements in %dms"
                    calculationResult.CalculatedSnapshots.Length
                    calculationResult.ProcessingMetrics.MovementsProcessed
                    calculationResult.ProcessingMetrics.CalculationTimeMs

                // ========== PHASE 3: PERSIST ALL RESULTS ==========
                CoreLogger.logDebug "BrokerFinancialBatchManager" "Phase 3: Persisting snapshots..."

                let! persistenceResult =
                    if request.ForceRecalculation then
                        // Delete existing snapshots and insert new ones
                        BrokerFinancialBatchPersistence.persistBatchedSnapshotsWithCleanup
                            calculationResult.CalculatedSnapshots
                            request.BrokerAccountId
                            request.StartDate
                            request.EndDate
                    else
                        // Just persist new snapshots
                        BrokerFinancialBatchPersistence.persistBatchedSnapshots calculationResult.CalculatedSnapshots

                match persistenceResult with
                | Ok metrics ->
                    totalStopwatch.Stop()

                    CoreLogger.logInfof
                        "BrokerFinancialBatchManager"
                        "Batch processing completed successfully: %d snapshots saved in %dms (total: %dms)"
                        metrics.SnapshotsSaved
                        metrics.TransactionTimeMs
                        totalStopwatch.ElapsedMilliseconds

                    CoreLogger.logInfof
                        "BrokerFinancialBatchManager"
                        "Performance breakdown: Load=%dms, Calculate=%dms, Persist=%dms, Total=%dms"
                        loadStopwatch.ElapsedMilliseconds
                        calculationResult.ProcessingMetrics.CalculationTimeMs
                        metrics.TransactionTimeMs
                        totalStopwatch.ElapsedMilliseconds

                    return
                        { Success = errors.IsEmpty
                          SnapshotsSaved = metrics.SnapshotsSaved
                          DatesProcessed = calculationResult.ProcessingMetrics.DatesProcessed
                          MovementsProcessed = calculationResult.ProcessingMetrics.MovementsProcessed
                          LoadTimeMs = loadStopwatch.ElapsedMilliseconds
                          CalculationTimeMs = calculationResult.ProcessingMetrics.CalculationTimeMs
                          PersistenceTimeMs = metrics.TransactionTimeMs
                          TotalTimeMs = totalStopwatch.ElapsedMilliseconds
                          Errors = errors }

                | Error errorMsg ->
                    totalStopwatch.Stop()
                    errors <- errorMsg :: errors

                    CoreLogger.logErrorf
                        "BrokerFinancialBatchManager"
                        "Batch processing failed during persistence: %s"
                        errorMsg

                    return
                        { Success = false
                          SnapshotsSaved = 0
                          DatesProcessed = calculationResult.ProcessingMetrics.DatesProcessed
                          MovementsProcessed = calculationResult.ProcessingMetrics.MovementsProcessed
                          LoadTimeMs = loadStopwatch.ElapsedMilliseconds
                          CalculationTimeMs = calculationResult.ProcessingMetrics.CalculationTimeMs
                          PersistenceTimeMs = 0L
                          TotalTimeMs = totalStopwatch.ElapsedMilliseconds
                          Errors = errors }

            with ex ->
                totalStopwatch.Stop()
                let errorMsg = sprintf "Batch processing failed: %s" ex.Message
                CoreLogger.logErrorf "BrokerFinancialBatchManager" "%s" errorMsg

                return
                    { Success = false
                      SnapshotsSaved = 0
                      DatesProcessed = 0
                      MovementsProcessed = 0
                      LoadTimeMs = 0L
                      CalculationTimeMs = 0L
                      PersistenceTimeMs = 0L
                      TotalTimeMs = totalStopwatch.ElapsedMilliseconds
                      Errors = [ errorMsg ] }
        }

    /// <summary>
    /// Simplified batch processing for import scenarios.
    /// Automatically determines date range from imported movements.
    /// Creates broker account snapshot after processing completes.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <returns>Task containing BatchProcessingResult</returns>
    let processBatchedFinancialsForImport (brokerAccountId: int) =
        task {
            try
                CoreLogger.logInfof
                    "BrokerFinancialBatchManager"
                    "Starting batch processing for recent imports on account %d"
                    brokerAccountId

                // First, load all movements to determine the actual date range
                let! allMovements =
                    BrokerMovementBatchLoader.loadMovementsForDateRange
                        brokerAccountId
                        (DateTimePattern.FromDateTime(System.DateTime(1900, 1, 1)))
                        (DateTimePattern.FromDateTime(System.DateTime.Now))

                // Collect all dates from movements
                let allDates =
                    [ allMovements.BrokerMovements |> List.map (fun m -> m.TimeStamp)
                      allMovements.Trades |> List.map (fun t -> t.TimeStamp)
                      allMovements.Dividends |> List.map (fun d -> d.TimeStamp)
                      allMovements.DividendTaxes |> List.map (fun dt -> dt.TimeStamp)
                      allMovements.OptionTrades |> List.map (fun ot -> ot.TimeStamp) ]
                    |> List.concat

                let (startDate, endDate) =
                    if allDates.IsEmpty then
                        // No movements found - use current date as both start and end
                        let now = DateTimePattern.FromDateTime(System.DateTime.Now)
                        CoreLogger.logWarning "BrokerFinancialBatchManager" "No movements found for batch processing"
                        (now, now)
                    else
                        // Use actual min/max movement dates
                        let minDate = allDates |> List.min
                        let maxDate = allDates |> List.max
                        (minDate, maxDate)

                CoreLogger.logInfof
                    "BrokerFinancialBatchManager"
                    "Processing movements from %s to %s (actual movement date range)"
                    (startDate.ToString())
                    (endDate.ToString())

                let request =
                    { BrokerAccountId = brokerAccountId
                      StartDate = startDate
                      EndDate = endDate
                      ForceRecalculation = true // Always recalculate on import
                    }

                let! result = processBatchedFinancials request

                // After batch processing, create BrokerAccountSnapshot for each unique date that has movements
                // This ensures BrokerAccounts.GetSnapshots() returns all snapshots (one per movement date)
                if result.Success && result.SnapshotsSaved > 0 then
                    CoreLogger.logDebug
                        "BrokerFinancialBatchManager"
                        "Creating broker account snapshots for all movement dates (OPTIMIZED - no cascade)"

                    // Get unique dates from movements
                    let uniqueMovementDates = allDates |> List.distinct |> List.sort

                    CoreLogger.logDebugf
                        "BrokerFinancialBatchManager"
                        "Creating %d broker account snapshots using OPTIMIZED batch processing"
                        uniqueMovementDates.Length

                    // PERFORMANCE OPTIMIZATION: Use optimized batch processing without cascade updates
                    // This processes dates chronologically without redundant cascade operations
                    // Expected: 95%+ performance improvement vs cascade-based approach
                    do!
                        BrokerAccountSnapshotManager.handleBrokerAccountChangesBatchOptimized (
                            brokerAccountId,
                            uniqueMovementDates
                        )

                    CoreLogger.logDebugf
                        "BrokerFinancialBatchManager"
                        "Created %d broker account snapshots successfully"
                        uniqueMovementDates.Length

                    // After processing all movement dates, ensure current date snapshot is updated
                    // This is needed when current date is after the last movement date
                    let lastMovementDate = uniqueMovementDates |> List.max
                    let currentDate = DateTimePattern.FromDateTime(System.DateTime.Now)

                    if currentDate.Value.Date > lastMovementDate.Value.Date then
                        CoreLogger.logDebugf
                            "BrokerFinancialBatchManager"
                            "Current date %s is after last movement date %s - updating current snapshot"
                            (currentDate.ToString())
                            (lastMovementDate.ToString())

                        // Create/update snapshot for current date with no new movements
                        // This will copy forward the latest financial state
                        do! BrokerAccountSnapshotManager.handleBrokerAccountChange (brokerAccountId, currentDate)

                        CoreLogger.logDebug "BrokerFinancialBatchManager" "Current date snapshot updated successfully"

                return result

            with ex ->
                let errorMsg = sprintf "Import batch processing failed: %s" ex.Message
                CoreLogger.logError "BrokerFinancialBatchManager" errorMsg

                return
                    { Success = false
                      SnapshotsSaved = 0
                      DatesProcessed = 0
                      MovementsProcessed = 0
                      LoadTimeMs = 0L
                      CalculationTimeMs = 0L
                      PersistenceTimeMs = 0L
                      TotalTimeMs = 0L
                      Errors = [ errorMsg ] }
        }
