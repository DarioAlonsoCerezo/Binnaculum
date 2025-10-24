namespace Binnaculum.Core.Storage

open System.Threading.Tasks
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging
open TickerSnapshotExtensions
open TickerCurrencySnapshotExtensions
open TradeExtensions
open DividendExtensions
open DividendTaxExtensions
open OptionTradeExtensions
open TickerPriceExtensions

/// <summary>
/// Batch loader for ticker snapshots to optimize database I/O.
/// Loads all data needed for ticker snapshot calculations in single queries instead of per-date/per-ticker queries.
/// This module reduces database queries from N×M×3 (N tickers × M dates × 3 movement types) to 3 queries.
/// </summary>
module internal TickerSnapshotBatchLoader =

    /// <summary>
    /// Movement data grouped by ticker, currency, and date.
    /// This structure allows efficient lookup during batch processing.
    /// </summary>
    type TickerMovementData =
        {
            /// Trades grouped by (tickerId, currencyId, date)
            Trades: Map<(int * int * DateTimePattern), Trade list>
            /// Dividends grouped by (tickerId, currencyId, date)
            Dividends: Map<(int * int * DateTimePattern), Dividend list>
            /// Dividend taxes grouped by (tickerId, currencyId, date)
            DividendTaxes: Map<(int * int * DateTimePattern), DividendTax list>
            /// Option trades grouped by (tickerId, currencyId, date)
            OptionTrades: Map<(int * int * DateTimePattern), OptionTrade list>
            /// ALL opening option trades (BuyToOpen/SellToOpen) for each ticker (not grouped by date)
            /// Used for unrealized gains temporal calculation - grouped by (tickerId, currencyId)
            AllOpeningTrades: Map<(int * int), OptionTrade list>
            /// ALL closed option trades for each ticker (not grouped by date)
            /// Used for calculating realized gains - grouped by (tickerId, currencyId)
            AllClosedOptionTrades: Map<(int * int), OptionTrade list>
        }

    /// <summary>
    /// Generate a list of dates between start and end (inclusive).
    /// Helper function for generating date ranges for price loading.
    /// </summary>
    let private generateDateRange (startDate: DateTimePattern) (endDate: DateTimePattern) : DateTimePattern list =
        let normalizedStart = SnapshotManagerUtils.normalizeToStartOfDay startDate
        let normalizedEnd = SnapshotManagerUtils.normalizeToStartOfDay endDate

        let rec generateDates (acc: DateTimePattern list) (currentDate: DateTimePattern) : DateTimePattern list =
            if currentDate.Value > normalizedEnd.Value then
                acc |> List.rev
            else
                let nextDate = DateTimePattern.FromDateTime(currentDate.Value.AddDays(1.0))
                generateDates (currentDate :: acc) nextDate

        generateDates [] normalizedStart

    /// <summary>
    /// Load baseline snapshots (latest before processing period) for multiple tickers.
    /// Returns both TickerSnapshots and their TickerCurrencySnapshots.
    /// </summary>
    /// <param name="tickerIds">List of ticker IDs to load baselines for</param>
    /// <param name="beforeDate">Load snapshots before this date (exclusive)</param>
    /// <returns>Tuple of (TickerSnapshot map, TickerCurrencySnapshot map keyed by (tickerId, currencyId))</returns>
    let loadBaselineSnapshots
        (tickerIds: int list)
        (beforeDate: DateTimePattern)
        : Task<Map<int, TickerSnapshot> * Map<(int * int), TickerCurrencySnapshot>> =
        task {
            if tickerIds.IsEmpty then
                // CoreLogger.logDebug "TickerSnapshotBatchLoader" "No ticker IDs provided, returning empty baselines"
                return (Map.empty, Map.empty)
            else
                // CoreLogger.logInfof
                //     "TickerSnapshotBatchLoader"
                //     "Loading baseline snapshots for %d tickers before %s"
                //     tickerIds.Length
                //     (beforeDate.ToString())

                // Load latest TickerSnapshot before the date for each ticker
                let! tickerSnapshots =
                    tickerIds
                    |> List.map (fun tickerId ->
                        task {
                            let! snapshot =
                                TickerSnapshotExtensions.Do.getLatestBeforeDate (tickerId, beforeDate.ToString())

                            return (tickerId, snapshot)
                        })
                    |> Task.WhenAll

                let tickerSnapshotMap =
                    tickerSnapshots
                    |> Array.choose (fun (tickerId, snapshot) -> snapshot |> Option.map (fun s -> (tickerId, s)))
                    |> Map.ofArray

                // Load TickerCurrencySnapshots for found TickerSnapshots
                let! currencySnapshots =
                    tickerSnapshotMap
                    |> Map.toList
                    |> List.map (fun (tickerId, tickerSnapshot) ->
                        task {
                            // Query TickerCurrencySnapshots by foreign key (TickerSnapshotId)
                            let! currencies =
                                TickerCurrencySnapshotExtensions.Do.getAllByTickerSnapshotId (tickerSnapshot.Base.Id)

                            // Return tuples keyed by (tickerId, currencyId)
                            return currencies |> List.map (fun cs -> ((tickerId, cs.CurrencyId), cs))
                        })
                    |> Task.WhenAll

                let currencySnapshotMap: Map<(int * int), TickerCurrencySnapshot> =
                    currencySnapshots |> Array.collect (Array.ofList) |> Map.ofArray

                // CoreLogger.logInfof
                //     "TickerSnapshotBatchLoader"
                //     "Loaded %d ticker snapshots and %d currency snapshots as baselines"
                //     tickerSnapshotMap.Count
                //     currencySnapshotMap.Count

                return (tickerSnapshotMap, currencySnapshotMap)
        }

    /// <summary>
    /// Load all movements for multiple tickers in a date range using batch queries.
    /// This replaces N×M×3 per-ticker/per-date queries with 3 optimized queries.
    /// </summary>
    /// <param name="tickerIds">List of ticker IDs to load movements for</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <returns>TickerMovementData with movements grouped by ticker/currency/date</returns>
    let loadTickerMovements
        (tickerIds: int list)
        (startDate: DateTimePattern)
        (endDate: DateTimePattern)
        : Task<TickerMovementData> =
        task {
            if tickerIds.IsEmpty then
                // CoreLogger.logDebug "TickerSnapshotBatchLoader" "No ticker IDs provided, returning empty movements"

                return
                    { Trades = Map.empty
                      Dividends = Map.empty
                      DividendTaxes = Map.empty
                      OptionTrades = Map.empty
                      AllOpeningTrades = Map.empty
                      AllClosedOptionTrades = Map.empty }
            else
                // CoreLogger.logInfof
                //     "TickerSnapshotBatchLoader"
                //     "Loading movements for %d tickers from %s to %s"
                //     tickerIds.Length
                //     (startDate.ToString())
                //     (endDate.ToString())

                // Load all trades for all tickers in parallel
                let! allTrades =
                    tickerIds
                    |> List.map (fun tickerId ->
                        task {
                            let! trades = TradeExtensions.Do.getByTickerIdFromDate (tickerId, startDate)
                            return trades |> List.filter (fun t -> t.TimeStamp.Value <= endDate.Value)
                        })
                    |> Task.WhenAll

                // Load all dividends for all tickers in parallel
                let! allDividends =
                    tickerIds
                    |> List.map (fun tickerId ->
                        task {
                            let! dividends = DividendExtensions.Do.getByTickerIdFromDate (tickerId, startDate)
                            return dividends |> List.filter (fun d -> d.TimeStamp.Value <= endDate.Value)
                        })
                    |> Task.WhenAll

                // Load all dividend taxes for all tickers in parallel
                let! allDividendTaxes =
                    tickerIds
                    |> List.map (fun tickerId ->
                        task {
                            let! dividendTaxes = DividendTaxExtensions.Do.getByTickerIdFromDate (tickerId, startDate)
                            return dividendTaxes |> List.filter (fun dt -> dt.TimeStamp.Value <= endDate.Value)
                        })
                    |> Task.WhenAll

                // Load all option trades for all tickers in parallel
                let! allOptionTrades =
                    tickerIds
                    |> List.map (fun tickerId ->
                        task {
                            let! optionTrades = OptionTradeExtensions.Do.getByTickerIdFromDate (tickerId, startDate)
                            return optionTrades |> List.filter (fun ot -> ot.TimeStamp.Value <= endDate.Value)
                        })
                    |> Task.WhenAll

                // Flatten and group by (tickerId, currencyId, date)
                let tradesByKey: Map<(int * int * DateTimePattern), Trade list> =
                    allTrades
                    |> Array.collect (List.toArray)
                    |> Array.groupBy (fun (t: Trade) ->
                        (t.TickerId, t.CurrencyId, SnapshotManagerUtils.normalizeToStartOfDay t.TimeStamp))
                    |> Array.map (fun (key, trades) -> (key, Array.toList trades))
                    |> Map.ofArray

                let dividendsByKey: Map<(int * int * DateTimePattern), Dividend list> =
                    allDividends
                    |> Array.collect (List.toArray)
                    |> Array.groupBy (fun (d: Dividend) ->
                        (d.TickerId, d.CurrencyId, SnapshotManagerUtils.normalizeToStartOfDay d.TimeStamp))
                    |> Array.map (fun (key, dividends) -> (key, Array.toList dividends))
                    |> Map.ofArray

                let dividendTaxesByKey: Map<(int * int * DateTimePattern), DividendTax list> =
                    allDividendTaxes
                    |> Array.collect (List.toArray)
                    |> Array.groupBy (fun (dt: DividendTax) ->
                        (dt.TickerId, dt.CurrencyId, SnapshotManagerUtils.normalizeToStartOfDay dt.TimeStamp))
                    |> Array.map (fun (key, taxes) -> (key, Array.toList taxes))
                    |> Map.ofArray

                let optionTradesByKey: Map<(int * int * DateTimePattern), OptionTrade list> =
                    allOptionTrades
                    |> Array.collect (List.toArray)
                    |> Array.groupBy (fun (ot: OptionTrade) ->
                        (ot.TickerId, ot.CurrencyId, SnapshotManagerUtils.normalizeToStartOfDay ot.TimeStamp))
                    |> Array.map (fun (key, options) -> (key, Array.toList options))
                    |> Map.ofArray

                let totalMovements =
                    (allTrades |> Array.sumBy (fun (arr: Trade list) -> arr.Length))
                    + (allDividends |> Array.sumBy (fun (arr: Dividend list) -> arr.Length))
                    + (allDividendTaxes |> Array.sumBy (fun (arr: DividendTax list) -> arr.Length))
                    + (allOptionTrades |> Array.sumBy (fun (arr: OptionTrade list) -> arr.Length))

                // CoreLogger.logInfof
                //     "TickerSnapshotBatchLoader"
                //     "Loaded %d total movements (%d trades, %d dividends, %d taxes, %d options)"
                //     totalMovements
                //     (allTrades |> Array.sumBy (fun (arr: Trade list) -> arr.Length))
                //     (allDividends |> Array.sumBy (fun (arr: Dividend list) -> arr.Length))
                //     (allDividendTaxes |> Array.sumBy (fun (arr: DividendTax list) -> arr.Length))
                //     (allOptionTrades |> Array.sumBy (fun (arr: OptionTrade list) -> arr.Length))

                // Group ALL option trades by (tickerId, currencyId) for realized gains calculation
                // This includes both opening and closing trades. The realized gains calculation
                // will filter by closing codes (BuyToClose, SellToClose, Assigned, Exercised, etc.)
                // to extract only realized gains from closed positions.
                // We need ALL trades here so we can match opening trades with their closing partners.
                let allClosedOptionTradesByTickerCurrency: Map<(int * int), OptionTrade list> =
                    allOptionTrades
                    |> Array.collect (List.toArray)
                    |> Array.groupBy (fun (ot: OptionTrade) -> (ot.TickerId, ot.CurrencyId))
                    |> Array.map (fun (key, options) -> (key, Array.toList options))
                    |> Map.ofArray

                // Group ALL opening option trades by (tickerId, currencyId) for unrealized gains calculation
                // This includes both open and closed opening trades (BuyToOpen/SellToOpen)
                // so we can apply temporal logic to determine which were open at each snapshot date
                let allOpeningTradesByTickerCurrency: Map<(int * int), OptionTrade list> =
                    allOptionTrades
                    |> Array.collect (List.toArray)
                    |> Array.filter (fun (ot: OptionTrade) ->
                        match ot.Code with
                        | OptionCode.BuyToOpen
                        | OptionCode.SellToOpen -> true
                        | _ -> false)
                    |> Array.groupBy (fun (ot: OptionTrade) -> (ot.TickerId, ot.CurrencyId))
                    |> Array.map (fun (key, options) -> (key, Array.toList options))
                    |> Map.ofArray

                let totalClosedOptions =
                    allClosedOptionTradesByTickerCurrency
                    |> Map.toSeq
                    |> Seq.sumBy (fun (_, trades) -> trades.Length)

                let totalOpeningOptions =
                    allOpeningTradesByTickerCurrency
                    |> Map.toSeq
                    |> Seq.sumBy (fun (_, trades) -> trades.Length)

                // CoreLogger.logInfof
                //     "TickerSnapshotBatchLoader"
                //     "Found %d closed option trades across all tickers for realized gains calculation"
                //     totalClosedOptions

                // CoreLogger.logInfof
                //     "TickerSnapshotBatchLoader"
                //     "Found %d opening option trades across all tickers for unrealized gains calculation"
                //     totalOpeningOptions

                return
                    { Trades = tradesByKey
                      Dividends = dividendsByKey
                      DividendTaxes = dividendTaxesByKey
                      OptionTrades = optionTradesByKey
                      AllOpeningTrades = allOpeningTradesByTickerCurrency
                      AllClosedOptionTrades = allClosedOptionTradesByTickerCurrency }
        }

    /// <summary>
    /// Load market prices for multiple tickers across a date range.
    /// Pre-loads prices to avoid repeated database queries during calculations.
    /// </summary>
    /// <param name="tickerIds">List of ticker IDs to load prices for</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <returns>Map of (tickerId, date) to price</returns>
    let loadMarketPrices
        (tickerIds: int list)
        (startDate: DateTimePattern)
        (endDate: DateTimePattern)
        : Task<Map<(int * DateTimePattern), decimal>> =
        task {
            if tickerIds.IsEmpty then
                // CoreLogger.logDebug "TickerSnapshotBatchLoader" "No ticker IDs provided, returning empty prices"
                return Map.empty
            else
                // CoreLogger.logInfof
                //     "TickerSnapshotBatchLoader"
                //     "Loading market prices for %d tickers from %s to %s"
                //     tickerIds.Length
                //     (startDate.ToString())
                //     (endDate.ToString())

                // Generate all dates in range
                let dateRange = generateDateRange startDate endDate

                // Load prices for each ticker/date combination
                let! allPrices =
                    tickerIds
                    |> List.map (fun tickerId ->
                        task {
                            // For each date, get price or previous
                            let! pricesForTicker =
                                dateRange
                                |> List.map (fun date ->
                                    task {
                                        let! price =
                                            TickerPriceExtensions.Do.getPriceByDateOrPrevious (
                                                tickerId,
                                                date.ToString()
                                            )

                                        return ((tickerId, date), price)
                                    })
                                |> Task.WhenAll

                            return pricesForTicker |> List.ofArray
                        })
                    |> Task.WhenAll

                let priceMap: Map<(int * DateTimePattern), decimal> =
                    allPrices |> Array.collect (Array.ofList) |> Map.ofArray

                let pricesFound = priceMap |> Map.filter (fun _ price -> price > 0m) |> Map.count

                // CoreLogger.logInfof
                //     "TickerSnapshotBatchLoader"
                //     "Loaded %d/%d price entries (%d with valid prices)"
                //     priceMap.Count
                //     (tickerIds.Length * dateRange.Length)
                //     pricesFound

                return priceMap
        }

    /// <summary>
    /// Get list of all tickers affected by recent import (those with movements).
    /// This is a helper to identify which tickers need batch processing.
    /// </summary>
    /// <param name="brokerAccountId">Broker account ID to check</param>
    /// <param name="sinceDate">Check for movements since this date</param>
    /// <returns>List of ticker IDs that have movements</returns>
    let getTickersAffectedByImport (brokerAccountId: int) (sinceDate: DateTimePattern) : Task<int list> =
        task {
            // CoreLogger.logInfof
            //     "TickerSnapshotBatchLoader"
            //     "Finding tickers affected by import for account %d since %s"
            //     brokerAccountId
            //     (sinceDate.ToString())

            // Get all trades, dividends, and options since date
            let! trades = TradeExtensions.Do.getByBrokerAccountIdFromDate (brokerAccountId, sinceDate)
            let! dividends = DividendExtensions.Do.getByBrokerAccountIdFromDate (brokerAccountId, sinceDate)
            let! optionTrades = OptionTradeExtensions.Do.getByBrokerAccountIdFromDate (brokerAccountId, sinceDate)

            // Extract unique ticker IDs
            let tickerIds =
                [ trades |> List.map (fun t -> t.TickerId)
                  dividends |> List.map (fun d -> d.TickerId)
                  optionTrades |> List.map (fun ot -> ot.TickerId) ]
                |> List.concat
                |> List.distinct
                |> List.sort

            // CoreLogger.logInfof "TickerSnapshotBatchLoader" "Found %d tickers affected by import" tickerIds.Length

            return tickerIds
        }
