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
            CoreLogger.logInfof
                "BrokerFinancialBatchCalculator"
                "Starting batch calculation for %d dates, %d baseline currencies"
                context.DateRange.Length
                context.BaselineSnapshots.Count

            // Track latest snapshot for each currency as we process
            let mutable latestSnapshotsByCurrency = context.BaselineSnapshots

            // Process each date in chronological order
            for date in context.DateRange do
                try
                    CoreLogger.logDebugf "BrokerFinancialBatchCalculator" "Processing date %s" (date.ToString())

                    // Get movements for this date
                    let dailyMovements =
                        context.MovementsByDate.TryFind(date)
                        |> Option.defaultValue (BrokerAccountMovementData.createEmpty date context.BrokerAccountId)

                    if dailyMovements.HasMovements then
                        movementsProcessed <- movementsProcessed + dailyMovements.TotalMovementCount

                        // Get unique currencies with movements for this date
                        let currenciesWithMovements = dailyMovements.UniqueCurrencies

                        CoreLogger.logDebugf
                            "BrokerFinancialBatchCalculator"
                            "Date %s has %d movements across %d currencies"
                            (date.ToString())
                            dailyMovements.TotalMovementCount
                            currenciesWithMovements.Count

                        // Process each currency that has movements
                        for currencyId in currenciesWithMovements do
                            try
                                // Get movement data for this specific currency
                                let currencyMovementData = dailyMovements.MovementsByCurrency.TryFind(currencyId)

                                match currencyMovementData with
                                | Some currencyMovements ->
                                    // Get previous snapshot for this currency
                                    let previousSnapshot = latestSnapshotsByCurrency.TryFind(currencyId)

                                    // Calculate new snapshot based on whether we have previous data
                                    let newSnapshot =
                                        match previousSnapshot with
                                        | Some prev ->
                                            // SCENARIO A: New movements with previous snapshot
                                            BrokerFinancialCalculateInMemory.calculateNewSnapshot
                                                currencyMovements
                                                prev
                                                date
                                                currencyId
                                                context.BrokerAccountId
                                                context.BrokerAccountSnapshotId
                                        | None ->
                                            // SCENARIO B: Initial snapshot for this currency
                                            BrokerFinancialCalculateInMemory.calculateInitialSnapshot
                                                currencyMovements
                                                date
                                                currencyId
                                                context.BrokerAccountId
                                                context.BrokerAccountSnapshotId

                                    // Add to calculated snapshots and update latest for this currency
                                    calculatedSnapshots <- newSnapshot :: calculatedSnapshots
                                    latestSnapshotsByCurrency <- latestSnapshotsByCurrency.Add(currencyId, newSnapshot)
                                    snapshotsCreated <- snapshotsCreated + 1

                                    CoreLogger.logDebugf
                                        "BrokerFinancialBatchCalculator"
                                        "Created snapshot for currency %d on %s (Deposited: %M, Counter: %d)"
                                        currencyId
                                        (date.ToString())
                                        newSnapshot.Deposited.Value
                                        newSnapshot.MovementCounter

                                | None ->
                                    CoreLogger.logWarningf
                                        "BrokerFinancialBatchCalculator"
                                        "Currency %d marked as having movements but no data found"
                                        currencyId

                            with ex ->
                                let errorMsg =
                                    sprintf
                                        "Error calculating snapshot for currency %d on %s: %s"
                                        currencyId
                                        (date.ToString())
                                        ex.Message

                                CoreLogger.logError "BrokerFinancialBatchCalculator" errorMsg
                                errors <- errorMsg :: errors
                    else
                        CoreLogger.logDebugf
                            "BrokerFinancialBatchCalculator"
                            "Date %s has no movements, skipping"
                            (date.ToString())

                with ex ->
                    let errorMsg = sprintf "Error processing date %s: %s" (date.ToString()) ex.Message
                    CoreLogger.logError "BrokerFinancialBatchCalculator" errorMsg
                    errors <- errorMsg :: errors

            stopwatch.Stop()

            CoreLogger.logInfof
                "BrokerFinancialBatchCalculator"
                "Batch calculation completed: %d snapshots created from %d movements in %dms"
                snapshotsCreated
                movementsProcessed
                stopwatch.ElapsedMilliseconds

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
