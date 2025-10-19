namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging
open System.Diagnostics

/// <summary>
/// Batch ticker snapshot calculator that processes multiple tickers and dates in memory.
/// This is the core orchestration engine that reduces database I/O by 90-95%.
/// </summary>
module internal TickerSnapshotBatchCalculator =

    /// <summary>
    /// Context for batch calculation containing all required data.
    /// Pre-loaded data allows pure in-memory calculations without database access.
    /// </summary>
    type TickerSnapshotBatchContext =
        {
            /// Map of ticker ID to latest baseline TickerSnapshot before processing range
            BaselineTickerSnapshots: Map<int, TickerSnapshot>
            /// Map of (ticker ID, currency ID) to baseline TickerCurrencySnapshot
            BaselineCurrencySnapshots: Map<(int * int), TickerCurrencySnapshot>
            /// All movements grouped by ticker/currency/date
            MovementsByTickerCurrencyDate: TickerSnapshotBatchLoader.TickerMovementData
            /// Map of (ticker ID, date) to market price
            MarketPrices: Map<(int * DateTimePattern), decimal>
            /// List of dates to process in chronological order
            DateRange: DateTimePattern list
            /// List of ticker IDs being processed
            TickerIds: int list
        }

    /// <summary>
    /// Result of batch calculation with metrics and any errors.
    /// Returns separate lists for TickerSnapshot and TickerCurrencySnapshot (flat structure).
    /// </summary>
    type TickerSnapshotBatchResult =
        {
            /// Successfully calculated ticker snapshots (simple entities with no embedded children)
            TickerSnapshots: TickerSnapshot list
            /// Successfully calculated currency snapshots (separate entities, will be linked via FK during persistence)
            CurrencySnapshots: TickerCurrencySnapshot list
            /// Processing metrics for monitoring
            ProcessingMetrics:
                {| TickersProcessed: int
                   DatesProcessed: int
                   MovementsProcessed: int
                   SnapshotsCreated: int
                   CurrencySnapshotsCreated: int
                   CalculationTimeMs: int64 |}
            /// Any errors encountered during processing
            Errors: string list
        }

    /// <summary>
    /// Get all currencies that need processing for a ticker on a date.
    /// This includes currencies with movements AND currencies from previous snapshots.
    /// </summary>
    let private getRelevantCurrenciesForProcessing
        (tickerId: int)
        (date: DateTimePattern)
        (movements: TickerSnapshotBatchLoader.TickerMovementData)
        (latestCurrencySnapshots: Map<(int * int), TickerCurrencySnapshot>)
        : int list =

        // Get currencies from movements
        let currenciesFromMovements =
            TickerSnapshotCalculateInMemory.getRelevantCurrenciesForTickerDate tickerId date movements Map.empty

        // Get currencies from previous snapshots for this ticker
        let currenciesFromPrevious =
            latestCurrencySnapshots
            |> Map.toList
            |> List.filter (fun ((tid, _), _) -> tid = tickerId)
            |> List.map (fun ((_, cid), _) -> cid)

        // Combine and deduplicate
        [ currenciesFromMovements; currenciesFromPrevious ]
        |> List.concat
        |> List.distinct
        |> List.sort

    /// <summary>
    /// Calculate all ticker snapshots for date range in memory.
    /// This is where the optimization happens - all calculations without database I/O.
    /// </summary>
    /// <param name="context">The batch calculation context with pre-loaded data</param>
    /// <returns>TickerSnapshotBatchResult with all calculated snapshots and metrics</returns>
    let calculateBatchedTickerSnapshots (context: TickerSnapshotBatchContext) : TickerSnapshotBatchResult =
        let stopwatch = Stopwatch.StartNew()
        let mutable tickerSnapshots = []
        let mutable currencySnapshots = []
        let mutable errors = []
        let mutable movementsProcessed = 0
        let mutable snapshotsCreated = 0
        let mutable currencySnapshotsCreated = 0

        try
            // CoreLogger.logInfof
            //     "TickerSnapshotBatchCalculator"
            //     "Starting batch calculation for %d tickers, %d dates, %d baseline currency snapshots"
            //     context.TickerIds.Length
            //     context.DateRange.Length
            //     context.BaselineCurrencySnapshots.Count

            // Track latest TickerCurrencySnapshot for each (ticker, currency) as we process chronologically
            let mutable latestCurrencySnapshots = context.BaselineCurrencySnapshots

            // Process each date in chronological order (critical for cumulative calculations)
            for date in context.DateRange do
                try
                    // CoreLogger.logDebugf "TickerSnapshotBatchCalculator" "Processing date %s" (date.ToString())

                    // Track snapshots created for this date (will be grouped into TickerSnapshot)
                    let mutable currencySnapshotsForDate: Map<int, TickerCurrencySnapshot list> =
                        Map.empty

                    // Process each ticker
                    for tickerId in context.TickerIds do
                        try
                            // Get all relevant currencies for this ticker on this date
                            let relevantCurrencies =
                                getRelevantCurrenciesForProcessing
                                    tickerId
                                    date
                                    context.MovementsByTickerCurrencyDate
                                    latestCurrencySnapshots

                            if relevantCurrencies.IsEmpty then
                                CoreLogger.logDebugf
                                    "TickerSnapshotBatchCalculator"
                                    "No currencies to process for ticker %d on date %s"
                                    tickerId
                                    (date.ToString())
                            else
                                // CoreLogger.logDebugf
                                //     "TickerSnapshotBatchCalculator"
                                //     "Processing ticker %d on date %s (%d currencies)"
                                //     tickerId
                                //     (date.ToString())
                                //     relevantCurrencies.Length

                                let mutable tickerCurrencySnapshots = []

                                // Process each currency for this ticker
                                for currencyId in relevantCurrencies do
                                    try
                                        // Get movements for this ticker/currency/date
                                        let movements =
                                            TickerSnapshotCalculateInMemory.getMovementsForTickerCurrencyDate
                                                tickerId
                                                currencyId
                                                date
                                                context.MovementsByTickerCurrencyDate

                                        // Get previous snapshot for this ticker/currency
                                        let previousSnapshot = latestCurrencySnapshots.TryFind((tickerId, currencyId))

                                        // Get market price for this ticker/date
                                        let marketPrice =
                                            context.MarketPrices.TryFind((tickerId, date)) |> Option.defaultValue 0m

                                        // Count movements
                                        match movements with
                                        | Some mvts ->
                                            movementsProcessed <-
                                                movementsProcessed
                                                + mvts.Trades.Length
                                                + mvts.Dividends.Length
                                                + mvts.DividendTaxes.Length
                                                + mvts.OptionTrades.Length
                                        | None -> ()

                                        // SCENARIO DECISION TREE
                                        let newSnapshot =
                                            match movements, previousSnapshot with
                                            // SCENARIO A: New movements + previous snapshot → Calculate cumulative
                                            | Some mvts, Some prev ->
                                                // CoreLogger.logDebugf
                                                //     "TickerSnapshotBatchCalculator"
                                                //     "SCENARIO A: ticker %d currency %d date %s (with movements)"
                                                //     tickerId
                                                //     currencyId
                                                //     (date.ToString())

                                                Some(
                                                    TickerSnapshotCalculateInMemory.calculateNewSnapshot
                                                        mvts
                                                        prev
                                                        marketPrice
                                                        date
                                                        tickerId
                                                        currencyId
                                                )

                                            // SCENARIO B: New movements + no previous → Calculate from zero
                                            | Some mvts, None ->
                                                // CoreLogger.logDebugf
                                                //     "TickerSnapshotBatchCalculator"
                                                //     "SCENARIO B: ticker %d currency %d date %s (first snapshot)"
                                                //     tickerId
                                                //     currencyId
                                                //     (date.ToString())

                                                Some(
                                                    TickerSnapshotCalculateInMemory.calculateInitialSnapshot
                                                        mvts
                                                        marketPrice
                                                        date
                                                        tickerId
                                                        currencyId
                                                )

                                            // SCENARIO D: No movements + previous → Carry forward
                                            | None, Some prev ->
                                                // CoreLogger.logDebugf
                                                //     "TickerSnapshotBatchCalculator"
                                                //     "SCENARIO D: ticker %d currency %d date %s (carry forward)"
                                                //     tickerId
                                                //     currencyId
                                                //     (date.ToString())

                                                Some(
                                                    TickerSnapshotCalculateInMemory.carryForwardSnapshot
                                                        prev
                                                        date
                                                        marketPrice
                                                )

                                            // No snapshot needed: no movements and no previous
                                            | None, None ->
                                                // CoreLogger.logDebugf
                                                //     "TickerSnapshotBatchCalculator"
                                                //     "SCENARIO SKIP: ticker %d currency %d date %s (no data)"
                                                //     tickerId
                                                //     currencyId
                                                //     (date.ToString())

                                                None

                                        // Add snapshot to results and update tracking
                                        match newSnapshot with
                                        | Some snapshot ->
                                            // Add to currencySnapshots list
                                            currencySnapshots <- snapshot :: currencySnapshots
                                            tickerCurrencySnapshots <- snapshot :: tickerCurrencySnapshots

                                            // Update latest snapshot for this ticker/currency
                                            latestCurrencySnapshots <-
                                                latestCurrencySnapshots.Add((tickerId, currencyId), snapshot)

                                            currencySnapshotsCreated <- currencySnapshotsCreated + 1
                                        | None -> ()

                                    with ex ->
                                        let errorMsg =
                                            sprintf
                                                "Error processing ticker %d currency %d on date %s: %s"
                                                tickerId
                                                currencyId
                                                (date.ToString())
                                                ex.Message

                                        // CoreLogger.logError "TickerSnapshotBatchCalculator" errorMsg
                                        errors <- errorMsg :: errors

                                // If we created currency snapshots for this ticker, create TickerSnapshot
                                if not tickerCurrencySnapshots.IsEmpty then
                                    let tickerSnapshot =
                                        { Base = SnapshotManagerUtils.createBaseSnapshot date
                                          TickerId = tickerId }

                                    tickerSnapshots <- tickerSnapshot :: tickerSnapshots
                                    snapshotsCreated <- snapshotsCreated + 1

                        // CoreLogger.logDebugf
                        //     "TickerSnapshotBatchCalculator"
                        //     "Created TickerSnapshot for ticker %d date %s (%d currencies)"
                        //     tickerId
                        //     (date.ToString())
                        //     tickerCurrencySnapshots.Length

                        with ex ->
                            let errorMsg =
                                sprintf
                                    "Error processing ticker %d on date %s: %s"
                                    tickerId
                                    (date.ToString())
                                    ex.Message

                            // CoreLogger.logError "TickerSnapshotBatchCalculator" errorMsg
                            errors <- errorMsg :: errors

                with ex ->
                    let errorMsg = sprintf "Error processing date %s: %s" (date.ToString()) ex.Message
                    CoreLogger.logError "TickerSnapshotBatchCalculator" errorMsg
                    errors <- errorMsg :: errors

            stopwatch.Stop()

            // CoreLogger.logInfof
            //     "TickerSnapshotBatchCalculator"
            //     "Batch calculation completed: %d snapshots, %d currency snapshots, %d movements in %dms"
            //     snapshotsCreated
            //     currencySnapshotsCreated
            //     movementsProcessed
            //     stopwatch.ElapsedMilliseconds

            { TickerSnapshots = tickerSnapshots |> List.rev // Reverse to chronological order
              CurrencySnapshots = currencySnapshots |> List.rev // Reverse to chronological order
              ProcessingMetrics =
                {| TickersProcessed = context.TickerIds.Length
                   DatesProcessed = context.DateRange.Length
                   MovementsProcessed = movementsProcessed
                   SnapshotsCreated = snapshotsCreated
                   CurrencySnapshotsCreated = currencySnapshotsCreated
                   CalculationTimeMs = stopwatch.ElapsedMilliseconds |}
              Errors = errors |> List.rev }

        with ex ->
            stopwatch.Stop()
            let fatalError = sprintf "Fatal error in batch calculation: %s" ex.Message
            CoreLogger.logError "TickerSnapshotBatchCalculator" fatalError

            { TickerSnapshots = []
              CurrencySnapshots = []
              ProcessingMetrics =
                {| TickersProcessed = 0
                   DatesProcessed = 0
                   MovementsProcessed = 0
                   SnapshotsCreated = 0
                   CurrencySnapshotsCreated = 0
                   CalculationTimeMs = stopwatch.ElapsedMilliseconds |}
              Errors = [ fatalError ] }
