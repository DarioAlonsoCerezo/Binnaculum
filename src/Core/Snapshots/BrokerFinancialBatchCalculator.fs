namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging
open System.Diagnostics

/// <summary>
/// Batch financial calculator that processes multiple dates in memory.
/// This is the core engine that reduces database I/O by 90-95%.
/// </summary>
module internal BrokerFinancialBatchCalculator =

    /// <summary>
    /// Context for batch calculation containing all required data.
    /// </summary>
    type BatchCalculationContext =
        {
            /// Map of currency ID to latest baseline snapshot before processing range
            BaselineSnapshots: Map<int, BrokerFinancialSnapshot>
            /// Map of date to movement data for that date
            MovementsByDate: Map<DateTimePattern, BrokerAccountMovementData>
            /// Map of (date, currencyId) to existing snapshot (for scenarios C, D, G, H)
            ExistingSnapshots: Map<(DateTimePattern * int), BrokerFinancialSnapshot>
            /// Map of (tickerId, currencyId, date) to market price for unrealized gains calculation
            MarketPrices: Map<(int * int * DateTimePattern), decimal>
            /// List of dates to process in chronological order
            DateRange: DateTimePattern list
            /// The broker account ID being processed
            BrokerAccountId: int
            /// The broker account snapshot ID (will be 0 for batch processing)
            BrokerAccountSnapshotId: int
        }

    /// <summary>
    /// Result of batch calculation with metrics and any errors.
    /// </summary>
    type BatchCalculationResult =
        {
            /// Successfully calculated snapshots
            CalculatedSnapshots: BrokerFinancialSnapshot list
            /// Processing metrics for monitoring
            ProcessingMetrics:
                {| DatesProcessed: int
                   MovementsProcessed: int
                   SnapshotsCreated: int
                   CalculationTimeMs: int64 |}
            /// Any errors encountered during processing
            Errors: string list
        }

    /// <summary>
    /// Calculate all financial snapshots for date range in memory.
    /// This is where the magic happens - all calculations without database I/O.
    /// </summary>
    /// <param name="context">The batch calculation context</param>
    /// <returns>BatchCalculationResult with all calculated snapshots</returns>
    let calculateBatchedFinancials (context: BatchCalculationContext) : BatchCalculationResult =
        let stopwatch = Stopwatch.StartNew()
        let mutable calculatedSnapshots = []
        let mutable errors = []
        let mutable movementsProcessed = 0
        let mutable snapshotsCreated = 0

        try
            // CoreLogger.logInfof
            //     "BrokerFinancialBatchCalculator"
            //     "Starting batch calculation for %d dates, %d baseline currencies"
            //     context.DateRange.Length
            //     context.BaselineSnapshots.Count

            // Track latest snapshot for each currency as we process
            let mutable latestSnapshotsByCurrency = context.BaselineSnapshots

            // Process each date in chronological order
            for date in context.DateRange do
                try
                    // CoreLogger.logDebugf "BrokerFinancialBatchCalculator" "Processing date %s" (date.ToString())

                    // Get movements for this date
                    let dailyMovements =
                        context.MovementsByDate.TryFind(date)
                        |> Option.defaultValue (BrokerAccountMovementData.createEmpty date context.BrokerAccountId)

                    // Track movement processing
                    if dailyMovements.HasMovements then
                        movementsProcessed <- movementsProcessed + dailyMovements.TotalMovementCount

                    // Get unique currencies with movements for this date
                    let currenciesWithMovements =
                        if dailyMovements.HasMovements then
                            dailyMovements.UniqueCurrencies
                        else
                            Set.empty

                    // Get all currencies with previous snapshots
                    let currenciesWithPreviousSnapshots =
                        latestSnapshotsByCurrency |> Map.toSeq |> Seq.map fst |> Set.ofSeq

                    // Calculate which currencies need processing (union of movements and historical)
                    let allRelevantCurrencies =
                        Set.union currenciesWithMovements currenciesWithPreviousSnapshots

                    // CoreLogger.logDebugf
                    //     "BrokerFinancialBatchCalculator"
                    //     "Date %s: %d movements, %d currencies with movements, %d with previous snapshots, %d total to process"
                    //     (date.ToString())
                    //     dailyMovements.TotalMovementCount
                    //     currenciesWithMovements.Count
                    //     currenciesWithPreviousSnapshots.Count
                    //     allRelevantCurrencies.Count

                    // Process each currency that needs attention
                    for currencyId in allRelevantCurrencies do
                        try
                            // Get movement data for this specific currency
                            let currencyMovementData = dailyMovements.MovementsByCurrency.TryFind(currencyId)

                            // Get previous snapshot for this currency
                            let previousSnapshot = latestSnapshotsByCurrency.TryFind(currencyId)

                            // Get existing snapshot for this date and currency (if reprocessing)
                            let existingSnapshot = context.ExistingSnapshots.TryFind((date, currencyId))

                            // SCENARIO DECISION TREE - All 8 scenarios handled
                            let snapshotResult =
                                match currencyMovementData, previousSnapshot, existingSnapshot with

                                // SCENARIO A: New movements, has previous snapshot, no existing snapshot
                                | Some movements, Some prev, None ->
                                    // CoreLogger.logDebugf
                                    //     "BrokerFinancialBatchCalculator"
                                    //     "SCENARIO A: currency %d date %s"
                                    //     currencyId
                                    //     (date.ToString())

                                    Some(
                                        BrokerFinancialCalculateInMemory.calculateNewSnapshot
                                            movements
                                            prev
                                            date
                                            currencyId
                                            context.BrokerAccountId
                                            context.BrokerAccountSnapshotId
                                            context.MarketPrices
                                    )

                                // SCENARIO B: New movements, no previous snapshot, no existing snapshot
                                | Some movements, None, None ->
                                    // CoreLogger.logDebugf
                                    //     "BrokerFinancialBatchCalculator"
                                    //     "SCENARIO B: currency %d date %s"
                                    //     currencyId
                                    //     (date.ToString())

                                    Some(
                                        BrokerFinancialCalculateInMemory.calculateInitialSnapshot
                                            movements
                                            date
                                            currencyId
                                            context.BrokerAccountId
                                            context.BrokerAccountSnapshotId
                                            context.MarketPrices
                                    )

                                // SCENARIO C: New movements, has previous snapshot, has existing snapshot
                                | Some movements, Some prev, Some existing ->
                                    // CoreLogger.logDebugf
                                    //     "BrokerFinancialBatchCalculator"
                                    //     "SCENARIO C: currency %d date %s"
                                    //     currencyId
                                    //     (date.ToString())

                                    Some(
                                        BrokerFinancialCalculateInMemory.updateExistingSnapshot
                                            movements
                                            prev
                                            existing
                                            date
                                            currencyId
                                            context.BrokerAccountId
                                            context.BrokerAccountSnapshotId
                                    )

                                // SCENARIO D: New movements, no previous snapshot, has existing snapshot
                                | Some movements, None, Some existing ->
                                    // CoreLogger.logDebugf
                                    //     "BrokerFinancialBatchCalculator"
                                    //     "SCENARIO D: currency %d date %s"
                                    //     currencyId
                                    //     (date.ToString())

                                    Some(
                                        BrokerFinancialCalculateInMemory.directUpdateSnapshot
                                            movements
                                            existing
                                            date
                                            currencyId
                                            context.BrokerAccountId
                                            context.BrokerAccountSnapshotId
                                    )

                                // SCENARIO E: No movements, has previous snapshot, no existing snapshot
                                | None, Some prev, None ->
                                    // CoreLogger.logDebugf
                                    //     "BrokerFinancialBatchCalculator"
                                    //     "SCENARIO E: currency %d date %s"
                                    //     currencyId
                                    //     (date.ToString())

                                    Some(
                                        BrokerFinancialCalculateInMemory.carryForwardSnapshot
                                            prev
                                            date
                                            context.BrokerAccountSnapshotId
                                    )

                                // SCENARIO F: No movements, no previous snapshot, no existing snapshot
                                | None, None, None ->
                                    // CoreLogger.logDebugf
                                    //     "BrokerFinancialBatchCalculator"
                                    //     "SCENARIO F: currency %d date %s - no action needed"
                                    //     currencyId
                                    //     (date.ToString())

                                    None

                                // SCENARIO G: No movements, has previous snapshot, has existing snapshot
                                | None, Some prev, Some existing ->
                                    // CoreLogger.logDebugf
                                    //     "BrokerFinancialBatchCalculator"
                                    //     "SCENARIO G: currency %d date %s"
                                    //     currencyId
                                    //     (date.ToString())
                                    // Returns Some if correction needed, None if snapshots match
                                    BrokerFinancialCalculateInMemory.validateAndCorrectSnapshot prev existing

                                // SCENARIO H: No movements, no previous snapshot, has existing snapshot
                                | None, None, Some existing ->
                                    // CoreLogger.logDebugf
                                    //     "BrokerFinancialBatchCalculator"
                                    //     "SCENARIO H: currency %d date %s"
                                    //     currencyId
                                    //     (date.ToString())

                                    Some(BrokerFinancialCalculateInMemory.resetSnapshot existing)

                            // Add snapshot to results if one was created/updated
                            match snapshotResult with
                            | Some newSnapshot ->
                                calculatedSnapshots <- newSnapshot :: calculatedSnapshots
                                latestSnapshotsByCurrency <- latestSnapshotsByCurrency.Add(currencyId, newSnapshot)
                                snapshotsCreated <- snapshotsCreated + 1

                            // CoreLogger.logDebugf
                            //     "BrokerFinancialBatchCalculator"
                            //     "Snapshot for currency %d on %s (Deposited: %M, Counter: %d)"
                            //     currencyId
                            //     (date.ToString())
                            //     newSnapshot.Deposited.Value
                            //     newSnapshot.MovementCounter
                            | None ->
                                CoreLogger.logDebugf
                                    "BrokerFinancialBatchCalculator"
                                    "No snapshot needed for currency %d on %s"
                                    currencyId
                                    (date.ToString())

                        with ex ->
                            let errorMsg =
                                sprintf
                                    "Error calculating snapshot for currency %d on %s: %s"
                                    currencyId
                                    (date.ToString())
                                    ex.Message

                            CoreLogger.logError "BrokerFinancialBatchCalculator" errorMsg
                            errors <- errorMsg :: errors

                with ex ->
                    let errorMsg = sprintf "Error processing date %s: %s" (date.ToString()) ex.Message
                    CoreLogger.logError "BrokerFinancialBatchCalculator" errorMsg
                    errors <- errorMsg :: errors

            stopwatch.Stop()

            // CoreLogger.logInfof
            //     "BrokerFinancialBatchCalculator"
            //     "Batch calculation completed: %d snapshots created from %d movements in %dms"
            //     snapshotsCreated
            //     movementsProcessed
            //     stopwatch.ElapsedMilliseconds

            { CalculatedSnapshots = calculatedSnapshots |> List.rev // Restore chronological order
              ProcessingMetrics =
                {| DatesProcessed = context.DateRange.Length
                   MovementsProcessed = movementsProcessed
                   SnapshotsCreated = snapshotsCreated
                   CalculationTimeMs = stopwatch.ElapsedMilliseconds |}
              Errors = errors |> List.rev }

        with ex ->
            stopwatch.Stop()
            let errorMsg = sprintf "Batch calculation failed: %s" ex.Message
            CoreLogger.logError "BrokerFinancialBatchCalculator" errorMsg

            { CalculatedSnapshots = []
              ProcessingMetrics =
                {| DatesProcessed = 0
                   MovementsProcessed = movementsProcessed
                   SnapshotsCreated = snapshotsCreated
                   CalculationTimeMs = stopwatch.ElapsedMilliseconds |}
              Errors = [ errorMsg ] }

    /// <summary>
    /// Generate list of dates between start and end for processing.
    /// Ensures all dates in the range are covered even if no movements exist.
    /// </summary>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <returns>List of dates in chronological order, normalized to start of day</returns>
    let generateDateRange (startDate: DateTimePattern) (endDate: DateTimePattern) : DateTimePattern list =
        // Normalize both dates to start of day for consistent comparison
        let normalizedStart = SnapshotManagerUtils.normalizeToStartOfDay startDate
        let normalizedEnd = SnapshotManagerUtils.normalizeToStartOfDay endDate

        let rec generateDates (acc: DateTimePattern list) (currentDate: DateTimePattern) : DateTimePattern list =
            if currentDate.Value > normalizedEnd.Value then
                acc |> List.rev
            else
                let nextDate = DateTimePattern.FromDateTime(currentDate.Value.AddDays(1.0))
                generateDates (currentDate :: acc) nextDate

        generateDates [] normalizedStart
