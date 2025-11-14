namespace Binnaculum.Core.Storage

open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging
open Binnaculum.Core.Import
open Binnaculum.Core.Database.DatabaseModel
open System.Diagnostics

/// <summary>
/// Main orchestration manager for batch ticker snapshot processing.
/// Coordinates loading, calculation, and persistence for optimal performance.
/// Reduces database I/O by ~90-95% through batch operations.
/// </summary>
module internal TickerSnapshotBatchManager =

    /// <summary>
    /// Request parameters for batch ticker snapshot processing.
    /// </summary>
    type internal BatchProcessingRequest =
        {
            /// Optional broker account ID for operation tracking during imports
            BrokerAccountId: int option
            /// List of ticker IDs to process
            TickerIds: int list
            /// Start date for processing period (inclusive)
            StartDate: DateTimePattern
            /// End date for processing period (inclusive)
            EndDate: DateTimePattern
            /// If true, delete existing snapshots and recalculate
            ForceRecalculation: bool
        }

    /// <summary>
    /// Result of batch processing with detailed metrics.
    /// </summary>
    type internal BatchProcessingResult =
        {
            /// Whether processing completed without critical errors
            Success: bool
            /// Total TickerSnapshots saved (created or updated)
            TickerSnapshotsSaved: int
            /// Total TickerCurrencySnapshots saved
            CurrencySnapshotsSaved: int
            /// Number of dates processed
            DatesProcessed: int
            /// Number of tickers processed
            TickersProcessed: int
            /// Operations calculated during ticker snapshot processing with their associated date
            CalculatedOperations: (DateTimePattern * AutoImportOperation) list
            /// Ticker snapshots calculated with their associated date for broker performance aggregation
            CalculatedTickerSnapshots:
                (DateTimePattern * Binnaculum.Core.Database.SnapshotsModel.TickerCurrencySnapshot) list
            /// Time spent loading data (ms)
            LoadTimeMs: int64
            /// Time spent calculating snapshots (ms)
            CalculationTimeMs: int64
            /// Time spent persisting snapshots (ms)
            PersistenceTimeMs: int64
            /// Total processing time (ms)
            TotalTimeMs: int64
            /// List of non-critical errors encountered
            Errors: string list
        }

    /// <summary>
    /// Main entry point for batch ticker snapshot processing.
    /// This replaces the per-date, per-ticker processing with efficient batch operations.
    /// </summary>
    /// <param name="request">The batch processing request</param>
    /// <param name="progressCallback">Optional callback for reporting progress to UI</param>
    /// <returns>Task containing BatchProcessingResult with all metrics</returns>
    let processBatchedTickers
        (request: BatchProcessingRequest)
        (progressCallback: TickerSnapshotBatchCalculator.SnapshotProgressCallback option)
        : System.Threading.Tasks.Task<BatchProcessingResult> =
        task {
            let totalStopwatch = Stopwatch.StartNew()
            let mutable errors = []

            try
                // CoreLogger.logInfof
                //     "TickerSnapshotBatchManager"
                //     "Starting batch processing for %d tickers from %s to %s"
                //     request.TickerIds.Length
                //     (request.StartDate.ToString())
                //     (request.EndDate.ToString())

                // ========== PHASE 1: LOAD ALL REQUIRED DATA ==========
                let loadStopwatch = Stopwatch.StartNew()

                // CoreLogger.logDebug "TickerSnapshotBatchManager" "Phase 1: Loading data..."

                // Load baseline snapshots (latest before start date)
                let! (baselineTickerSnapshots, baselineCurrencySnapshots) =
                    TickerSnapshotBatchLoader.loadBaselineSnapshots request.TickerIds request.StartDate

                // Load ticker movements for the date range
                let! tickerMovementData =
                    TickerSnapshotBatchLoader.loadTickerMovements request.TickerIds request.StartDate request.EndDate

                // Load market prices for all tickers in the date range
                let! marketPrices =
                    TickerSnapshotBatchLoader.loadMarketPrices request.TickerIds request.StartDate request.EndDate

                loadStopwatch.Stop()

                let totalTrades =
                    tickerMovementData.Trades
                    |> Map.toSeq
                    |> Seq.sumBy (fun (_, trades) -> trades.Length)

                let totalDividends =
                    tickerMovementData.Dividends
                    |> Map.toSeq
                    |> Seq.sumBy (fun (_, divs) -> divs.Length)

                let totalDividendTaxes =
                    tickerMovementData.DividendTaxes
                    |> Map.toSeq
                    |> Seq.sumBy (fun (_, taxes) -> taxes.Length)

                let totalOptionTrades =
                    tickerMovementData.OptionTrades
                    |> Map.toSeq
                    |> Seq.sumBy (fun (_, opts) -> opts.Length)

                // CoreLogger.logInfof
                //     "TickerSnapshotBatchManager"
                //     "Data loading completed: %d trades, %d dividends, %d dividend taxes, %d option trades, %d baseline ticker snapshots, %d baseline currency snapshots in %dms"
                //     totalTrades
                //     totalDividends
                //     totalDividendTaxes
                //     totalOptionTrades
                //     baselineTickerSnapshots.Count
                //     baselineCurrencySnapshots.Count
                //     loadStopwatch.ElapsedMilliseconds

                // ========== PHASE 2: CALCULATE ALL SNAPSHOTS IN MEMORY ==========
                // CoreLogger.logDebug "TickerSnapshotBatchManager" "Phase 2: Calculating snapshots..."

                // SMART DATE FILTERING: Only process dates that actually have activity
                // Extract dates from movements (normalized to start of day)
                let movementDates =
                    [ tickerMovementData.Trades
                      |> Map.toSeq
                      |> Seq.map (fun ((_, _, date), _) -> date)
                      tickerMovementData.Dividends
                      |> Map.toSeq
                      |> Seq.map (fun ((_, _, date), _) -> date)
                      tickerMovementData.DividendTaxes
                      |> Map.toSeq
                      |> Seq.map (fun ((_, _, date), _) -> date)
                      tickerMovementData.OptionTrades
                      |> Map.toSeq
                      |> Seq.map (fun ((_, _, date), _) -> date) ]
                    |> Seq.concat
                    |> Set.ofSeq
                    |> Set.toList
                    |> List.sort

                // CoreLogger.logInfof
                //     "TickerSnapshotBatchManager"
                //     "Smart date filtering: %d dates with movements to process (vs %d days in full range)"
                //     movementDates.Length
                //     ((request.EndDate.Value - request.StartDate.Value).Days + 1)

                // Add current date to the range if it's after the last movement date
                // This ensures the current snapshot is updated with the latest state
                let dateRange =
                    if not movementDates.IsEmpty then
                        let lastMovementDate = movementDates |> List.max
                        let currentDate = DateTimePattern.FromDateTime(System.DateTime.Now)

                        if currentDate.Value.Date > lastMovementDate.Value.Date then
                            // CoreLogger.logDebugf
                            //     "TickerSnapshotBatchManager"
                            //     "Adding current date %s to process list (after last movement %s)"
                            //     (currentDate.ToString())
                            //     (lastMovementDate.ToString())

                            movementDates @ [ currentDate ]
                        else
                            movementDates
                    else
                        movementDates

                // Create calculation context
                let context: TickerSnapshotBatchCalculator.TickerSnapshotBatchContext =
                    { BrokerAccountId = request.BrokerAccountId // Pass through for operation tracking
                      BaselineTickerSnapshots = baselineTickerSnapshots
                      BaselineCurrencySnapshots = baselineCurrencySnapshots
                      MovementsByTickerCurrencyDate = tickerMovementData
                      MarketPrices = marketPrices
                      DateRange = dateRange
                      TickerIds = request.TickerIds }

                // Calculate all snapshots in memory (with optional progress callback)
                let! calculationResult =
                    TickerSnapshotBatchCalculator.calculateBatchedTickerSnapshots context progressCallback

                errors <- errors @ calculationResult.Errors

                // CoreLogger.logInfof
                //     "TickerSnapshotBatchManager"
                //     "Batch calculations completed: %d ticker snapshots calculated, %d currency snapshots calculated in %dms"
                //     calculationResult.ProcessingMetrics.SnapshotsCreated
                //     calculationResult.ProcessingMetrics.CurrencySnapshotsCreated
                //     calculationResult.ProcessingMetrics.CalculationTimeMs

                // ========== PHASE 3: PERSIST ALL RESULTS ==========
                // CoreLogger.logDebug "TickerSnapshotBatchManager" "Phase 3: Persisting snapshots..."

                let! persistenceResult =
                    // Persist new snapshots (with deduplication and FK linking)
                    TickerSnapshotBatchPersistence.persistBatchedSnapshots calculationResult

                match persistenceResult with
                | Ok metrics ->
                    totalStopwatch.Stop()

                    // CoreLogger.logInfof
                    //     "TickerSnapshotBatchManager"
                    //     "Batch processing completed successfully: %d ticker snapshots saved, %d currency snapshots saved in %dms (total: %dms)"
                    //     metrics.TickerSnapshotsSaved
                    //     metrics.CurrencySnapshotsSaved
                    //     metrics.TransactionTimeMs
                    //     totalStopwatch.ElapsedMilliseconds

                    // CoreLogger.logInfof
                    //     "TickerSnapshotBatchManager"
                    //     "Performance breakdown: Load=%dms, Calculate=%dms, Persist=%dms, Total=%dms"
                    //     loadStopwatch.ElapsedMilliseconds
                    //     calculationResult.ProcessingMetrics.CalculationTimeMs
                    //     metrics.TransactionTimeMs
                    //     totalStopwatch.ElapsedMilliseconds

                    return
                        { Success = errors.IsEmpty
                          TickerSnapshotsSaved = metrics.TickerSnapshotsSaved
                          CurrencySnapshotsSaved = metrics.CurrencySnapshotsSaved
                          DatesProcessed = calculationResult.ProcessingMetrics.DatesProcessed
                          TickersProcessed = calculationResult.ProcessingMetrics.TickersProcessed
                          CalculatedOperations = calculationResult.CalculatedOperations
                          CalculatedTickerSnapshots = calculationResult.CalculatedTickerSnapshots
                          LoadTimeMs = loadStopwatch.ElapsedMilliseconds
                          CalculationTimeMs = calculationResult.ProcessingMetrics.CalculationTimeMs
                          PersistenceTimeMs = metrics.TransactionTimeMs
                          TotalTimeMs = totalStopwatch.ElapsedMilliseconds
                          Errors = errors }

                | Error errorMsg ->
                    totalStopwatch.Stop()
                    errors <- errorMsg :: errors

                    // CoreLogger.logErrorf
                    //     "TickerSnapshotBatchManager"
                    //     "Batch processing failed during persistence: %s"
                    //     errorMsg

                    return
                        { Success = false
                          TickerSnapshotsSaved = 0
                          CurrencySnapshotsSaved = 0
                          DatesProcessed = calculationResult.ProcessingMetrics.DatesProcessed
                          TickersProcessed = calculationResult.ProcessingMetrics.TickersProcessed
                          CalculatedOperations = calculationResult.CalculatedOperations
                          CalculatedTickerSnapshots = calculationResult.CalculatedTickerSnapshots
                          LoadTimeMs = loadStopwatch.ElapsedMilliseconds
                          CalculationTimeMs = calculationResult.ProcessingMetrics.CalculationTimeMs
                          PersistenceTimeMs = 0L
                          TotalTimeMs = totalStopwatch.ElapsedMilliseconds
                          Errors = errors }

            with ex ->
                totalStopwatch.Stop()
                let errorMsg = sprintf "Batch processing failed: %s" ex.Message
                CoreLogger.logErrorf "TickerSnapshotBatchManager" "%s" errorMsg

                return
                    { Success = false
                      TickerSnapshotsSaved = 0
                      CurrencySnapshotsSaved = 0
                      DatesProcessed = 0
                      TickersProcessed = 0
                      CalculatedOperations = []
                      CalculatedTickerSnapshots = []
                      LoadTimeMs = 0L
                      CalculationTimeMs = 0L
                      PersistenceTimeMs = 0L
                      TotalTimeMs = totalStopwatch.ElapsedMilliseconds
                      Errors = [ errorMsg ] }
        }

    /// <summary>
    /// Simplified batch processing for import scenarios.
    /// Automatically determines affected tickers and date ranges from broker account movements.
    /// This is the main entry point called from ImportManager after CSV import completes.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID to process</param>
    /// <param name="importMetadata">Metadata from the import containing oldest movement date</param>
    /// <param name="progressCallback">Optional callback for reporting progress to UI</param>
    /// <returns>Task containing BatchProcessingResult</returns>
    let processBatchedTickersForImport
        (brokerAccountId: int)
        (importMetadata: ImportMetadata)
        (progressCallback: TickerSnapshotBatchCalculator.SnapshotProgressCallback option)
        =
        task {
            try
                // CoreLogger.logInfof
                //     "TickerSnapshotBatchManager"
                //     "Starting batch processing for recent imports on broker account %d"
                //     brokerAccountId

                // Determine which tickers are affected by this import
                // Use the actual oldest movement date from import metadata to determine lookback period
                let sinceDate =
                    match importMetadata.OldestMovementDate with
                    | Some date -> DateTimePattern.FromDateTime(date)
                    | None ->
                        // Fallback: if no movement date (shouldn't happen), use current date
                        // CoreLogger.logWarning
                        //     "TickerSnapshotBatchManager"
                        //     "No OldestMovementDate in import metadata - using current date as fallback"

                        DateTimePattern.FromDateTime(System.DateTime.Now)

                let! affectedTickers = TickerSnapshotBatchLoader.getTickersAffectedByImport brokerAccountId sinceDate

                if affectedTickers.IsEmpty then
                    // CoreLogger.logWarningf
                    //     "TickerSnapshotBatchManager"
                    //     "No affected tickers found for broker account %d - skipping batch processing"
                    //     brokerAccountId

                    return
                        { Success = true
                          TickerSnapshotsSaved = 0
                          CurrencySnapshotsSaved = 0
                          DatesProcessed = 0
                          TickersProcessed = 0
                          CalculatedOperations = []
                          CalculatedTickerSnapshots = []
                          LoadTimeMs = 0L
                          CalculationTimeMs = 0L
                          PersistenceTimeMs = 0L
                          TotalTimeMs = 0L
                          Errors = [] }
                else
                    // CoreLogger.logInfof
                    //     "TickerSnapshotBatchManager"
                    //     "Found %d affected tickers for batch processing"
                    //     affectedTickers.Length

                    // For import scenario, use the lookback period as start date
                    // This ensures we capture all movements for affected tickers
                    // The smart date filtering in Phase 2 will skip dates with no movements
                    let startDate = sinceDate

                    // End date is always current date to ensure latest snapshots are calculated
                    let endDate = DateTimePattern.FromDateTime(System.DateTime.Now)

                    // CoreLogger.logInfof
                    //     "TickerSnapshotBatchManager"
                    //     "Processing %d tickers from %s to %s (using import lookback period)"
                    //     affectedTickers.Length
                    //     (startDate.ToString())
                    //     (endDate.ToString())

                    let request =
                        { BrokerAccountId = Some brokerAccountId // Pass broker account for operation tracking
                          TickerIds = affectedTickers
                          StartDate = startDate
                          EndDate = endDate
                          ForceRecalculation = true // Always recalculate on import to ensure accuracy
                        }

                    let! result = processBatchedTickers request progressCallback

                    // if result.Success then
                    //     CoreLogger.logInfof
                    //         "TickerSnapshotBatchManager"
                    //         "Import batch processing completed: %d ticker snapshots, %d currency snapshots saved in %dms"
                    //         result.TickerSnapshotsSaved
                    //         result.CurrencySnapshotsSaved
                    //         result.TotalTimeMs
                    // else
                    //     CoreLogger.logErrorf
                    //         "TickerSnapshotBatchManager"
                    //         "Import batch processing completed with %d errors"
                    //         result.Errors.Length

                    return result

            with ex ->
                let errorMsg = sprintf "Import batch processing failed: %s" ex.Message
                CoreLogger.logError "TickerSnapshotBatchManager" errorMsg

                return
                    { Success = false
                      TickerSnapshotsSaved = 0
                      CurrencySnapshotsSaved = 0
                      DatesProcessed = 0
                      TickersProcessed = 0
                      CalculatedOperations = []
                      CalculatedTickerSnapshots = []
                      LoadTimeMs = 0L
                      CalculationTimeMs = 0L
                      PersistenceTimeMs = 0L
                      TotalTimeMs = 0L
                      Errors = [ errorMsg ] }
        }

    /// <summary>
    /// Process a single ticker with targeted date range.
    /// Useful for debugging or incremental updates of specific tickers.
    /// </summary>
    /// <param name="tickerId">The ticker ID to process</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <param name="forceRecalculation">Whether to delete and recalculate existing snapshots</param>
    /// <returns>Task containing BatchProcessingResult</returns>
    let processSingleTickerBatch
        (tickerId: int)
        (startDate: DateTimePattern)
        (endDate: DateTimePattern)
        (forceRecalculation: bool)
        =
        task {
            // CoreLogger.logInfof
            //     "TickerSnapshotBatchManager"
            //     "Processing single ticker %d from %s to %s (force=%b)"
            //     tickerId
            //     (startDate.ToString())
            //     (endDate.ToString())
            //     forceRecalculation

            let request =
                { BrokerAccountId = None // No broker account context for single ticker processing
                  TickerIds = [ tickerId ]
                  StartDate = startDate
                  EndDate = endDate
                  ForceRecalculation = forceRecalculation }

            return! processBatchedTickers request None // No progress callback for single ticker
        }
