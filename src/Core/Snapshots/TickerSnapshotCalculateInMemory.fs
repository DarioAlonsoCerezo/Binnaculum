namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging

/// <summary>
/// In-memory ticker snapshot calculation engine for batch processing.
/// Performs all ticker snapshot calculations without database I/O for maximum performance.
/// All functions are pure - they take inputs and return calculated snapshots without side effects.
/// </summary>
module internal TickerSnapshotCalculateInMemory =

    /// <summary>
    /// Movements for a specific ticker/currency/date combination.
    /// Used as input for pure calculation functions.
    /// </summary>
    type TickerCurrencyMovementData =
        {
            Trades: Trade list
            Dividends: Dividend list
            DividendTaxes: DividendTax list
            OptionTrades: OptionTrade list
            /// ALL opening option trades (BuyToOpen/SellToOpen) for this ticker/currency
            /// Includes both currently open and historically closed opening trades
            /// Used for temporal unrealized gains calculation (checking which were open at snapshot date)
            AllOpeningTrades: OptionTrade list
            /// ALL closed option trades for this ticker/currency (not just current date)
            /// Used for accurate realized gains calculation across historical round-trips
            AllClosedOptionTrades: OptionTrade list
        }

    /// <summary>
    /// Calculate new TickerCurrencySnapshot from movements and previous snapshot.
    /// Scenario A: New movements + previous snapshot → Calculate cumulative values
    /// This is the most common scenario during batch processing.
    /// </summary>
    /// <param name="movements">Movements for this ticker/currency/date</param>
    /// <param name="previousSnapshot">Previous TickerCurrencySnapshot for cumulative calculations</param>
    /// <param name="marketPrice">Market price for this date</param>
    /// <param name="date">Date for the new snapshot</param>
    /// <param name="tickerId">Ticker ID</param>
    /// <param name="currencyId">Currency ID</param>
    /// <returns>New TickerCurrencySnapshot with calculated values</returns>
    let calculateNewSnapshot
        (movements: TickerCurrencyMovementData)
        (previousSnapshot: TickerCurrencySnapshot)
        (marketPrice: decimal)
        (date: DateTimePattern)
        (tickerId: int)
        (currencyId: int)
        : TickerCurrencySnapshot =

        // CoreLogger.logDebugf
        //     "TickerSnapshotCalculateInMemory"
        //     "Calculating NEW snapshot - Ticker:%d Currency:%d Date:%s (Trades:%d Divs:%d Options:%d) Previous:%s"
        //     tickerId
        //     currencyId
        //     (date.ToString())
        //     movements.Trades.Length
        //     movements.Dividends.Length
        //     movements.OptionTrades.Length
        //     (previousSnapshot.Base.Date.Value.ToString())

        // Calculate shares delta from trades
        // CRITICAL FIX: Account for buy/sell direction using TradeCode
        // Buy trades (BuyToOpen, BuyToClose) add shares (+)
        // Sell trades (SellToOpen, SellToClose) reduce shares (-)
        let sharesDelta =
            movements.Trades
            |> List.sumBy (fun t ->
                match t.TradeCode with
                | TradeCode.BuyToOpen
                | TradeCode.BuyToClose -> t.Quantity
                | TradeCode.SellToOpen
                | TradeCode.SellToClose -> -t.Quantity)

        let totalShares = previousSnapshot.TotalShares + sharesDelta

        // Calculate cost basis delta from trades
        // Cost basis is the actual capital invested (buy side only, absolute value)
        // For BUY trades: add to cost basis
        // For SELL trades: reduce cost basis by the proceeds
        let tradeCostBasis =
            movements.Trades
            |> List.sumBy (fun t ->
                match t.TradeCode with
                | TradeCode.BuyToOpen
                | TradeCode.BuyToClose -> t.Price.Value * t.Quantity // Add to cost
                | TradeCode.SellToOpen
                | TradeCode.SellToClose -> -(t.Price.Value * t.Quantity) // Reduce cost (proceeds)
            )

        let costBasis = Money.FromAmount(previousSnapshot.CostBasis.Value + tradeCostBasis)

        // Calculate commissions and fees from trades
        let commissions = movements.Trades |> List.sumBy (fun t -> t.Commissions.Value)
        let fees = movements.Trades |> List.sumBy (fun t -> t.Fees.Value)

        // Calculate dividends (gross dividends minus taxes)
        let currentDividends =
            movements.Dividends |> List.sumBy (fun d -> d.DividendAmount.Value)

        let currentDividendTaxes =
            movements.DividendTaxes |> List.sumBy (fun dt -> dt.DividendTaxAmount.Value)

        let netDividends = currentDividends - currentDividendTaxes

        let totalDividends =
            Money.FromAmount(previousSnapshot.Dividends.Value + netDividends)

        // Calculate options income
        let currentOptions =
            movements.OptionTrades |> List.sumBy (fun o -> o.NetPremium.Value)

        let totalOptions = Money.FromAmount(previousSnapshot.Options.Value + currentOptions)

        // Calculate total incomes
        let totalIncomes = Money.FromAmount(totalDividends.Value + totalOptions.Value)

        // Calculate real cost (cost basis + commissions + fees + dividend taxes)
        let realCost =
            Money.FromAmount(costBasis.Value + commissions + fees + currentDividendTaxes)

        // Calculate unrealized gains/losses from open positions only:
        // 1. Shares: (market value - cost basis)
        // 2. Open options: net premium of positions still open
        //
        // Dividends and Realized are tracked separately in their own fields

        // Shares unrealized
        let effectivePrice = if marketPrice > 0m then marketPrice else costBasis.Value
        let sharesMarketValue = effectivePrice * totalShares
        let sharesUnrealized = sharesMarketValue - costBasis.Value

        // Options unrealized (only open positions at this snapshot date)
        // Check if the trade was open AS OF the snapshot date by comparing timestamps
        // Use AllOpeningTrades which contains ALL opening trades (BuyToOpen/SellToOpen)
        // regardless of their final IsOpen status, allowing temporal evaluation
        let normalizedSnapshotDate = SnapshotManagerUtils.normalizeToStartOfDay date

        let openOptionsUnrealized =
            let openTrades =
                movements.AllOpeningTrades
                |> List.filter (fun opt ->
                    // First check: Trade must have occurred ON OR BEFORE snapshot date
                    let normalizedTradeDate = SnapshotManagerUtils.normalizeToStartOfDay opt.TimeStamp

                    if normalizedTradeDate.Value > normalizedSnapshotDate.Value then
                        false // Trade hasn't happened yet at this snapshot date
                    else
                        // Second check: Was it still open at snapshot date?
                        match opt.ClosedWith with
                        | Some closingTradeId ->
                            // Find the closing trade in all option trades
                            let closingTrade =
                                movements.AllClosedOptionTrades |> List.tryFind (fun t -> t.Id = closingTradeId)

                            match closingTrade with
                            | Some ct ->
                                let normalizedClosingDate = SnapshotManagerUtils.normalizeToStartOfDay ct.TimeStamp
                                // BUG FIX: Trade was open if closing happened AFTER snapshot date
                                // If closing happened ON OR BEFORE snapshot date, the trade is CLOSED
                                // and should NOT be included in unrealized
                                normalizedClosingDate.Value > normalizedSnapshotDate.Value
                            | None ->
                                // Closing trade not found in our dataset - consider it open
                                true
                        | None ->
                            // Not closed yet - definitely open
                            true)

            // DEBUG: Log each open trade included for this snapshot
            // CoreLogger.logDebugf
            //     "TickerSnapshotCalculateInMemory"
            //     "[Date:%s] Open trades count: %d out of %d total opening trades"
            //     (date.ToString())
            //     openTrades.Length
            //     movements.AllOpeningTrades.Length

            openTrades
            |> List.iter (fun opt -> ()
            // CoreLogger.logDebugf
            //     "TickerSnapshotCalculateInMemory"
            //     "[Date:%s] Open trade included: Id:%d Code:%A NetPremium:%M ClosedWith:%A"
            //     (date.ToString())
            //     opt.Id
            //     opt.Code
            //     opt.NetPremium.Value
            //     opt.ClosedWith
            )
            |> ignore

            openTrades |> List.sumBy (fun opt -> opt.NetPremium.Value)

        // Total unrealized = ONLY shares unrealized (positions still in market)
        // NOTE: Unrealized is ONLY calculated for stock positions (when totalShares ≠ 0)
        // For options-only tickers (totalShares = 0), unrealized should be 0
        // Option P&L is reflected in Realized gains when positions are closed
        let unrealized = Money.FromAmount(sharesUnrealized)

        // DEBUG: Log unrealized calculation details
        // CoreLogger.logDebugf
        //     "TickerSnapshotCalculateInMemory"
        //     "[Date:%s] Unrealized calculation - SharesUnrealized:%M (Options-only ignored) = %M"
        //     (date.ToString())
        //     sharesUnrealized
        //     unrealized.Value
        //     previousSnapshot.Unrealized.Value

        // Calculate realized gains from closed option positions using proper FIFO pair matching
        // This uses OptionTradeCalculations.calculateRealizedGains which properly
        // matches opening trades with their corresponding closing trades.
        // For example:
        // - Pair 1: BuyToOpen (Trade #1, 8/25, -555.12) + SellToClose (Trade #3, 10/3, +744.88) = +189.76
        // - Pair 2: SellToOpen (Trade #2, 10/1, +49.88) + BuyToClose (Trade #4, 10/3, -64.12) = -14.24
        // - Total realized on 2025-10-03: +189.76 - 14.24 = +175.52
        //
        // We need to calculate cumulative realized gains:
        // - Realized gains are cumulative: once a trade pair is closed, its gains stay
        // - To avoid recalculating ALL pairs every snapshot, we:
        //   1. Calculate total realized from ALL trades up to this date
        //   2. Compare with previous snapshot to get only new realized gains
        //   3. Add new gains to previous cumulative realized
        let normalizedSnapshotDate = SnapshotManagerUtils.normalizeToStartOfDay date

        let normalizedPreviousDate =
            SnapshotManagerUtils.normalizeToStartOfDay previousSnapshot.Base.Date

        // All trades that closed up to (and including) this snapshot date
        let tradesUpToSnapshot =
            movements.AllClosedOptionTrades
            |> List.filter (fun opt ->
                let normalizedTradeDate = SnapshotManagerUtils.normalizeToStartOfDay opt.TimeStamp in
                normalizedTradeDate.Value <= normalizedSnapshotDate.Value)

        // All trades that closed up to (and including) the previous snapshot date
        // This prevents double-counting: we only count NEW realized gains from trades after the previous snapshot
        let tradesUpToPreviousDate =
            movements.AllClosedOptionTrades
            |> List.filter (fun opt ->
                let normalizedTradeDate = SnapshotManagerUtils.normalizeToStartOfDay opt.TimeStamp in
                normalizedTradeDate.Value <= normalizedPreviousDate.Value)

        // Calculate total realized gains from ALL trades up to this snapshot date
        let totalRealizedUpToSnapshot =
            OptionTradeExtensions.OptionTradeCalculations.calculateRealizedGains (tradesUpToSnapshot, date.Value)

        // Calculate realized gains from trades before/on previous snapshot (what we already had)
        let realizedUpToPreviousDate =
            if tradesUpToPreviousDate.IsEmpty then
                Money.FromAmount(0m)
            else
                OptionTradeExtensions.OptionTradeCalculations.calculateRealizedGains (
                    tradesUpToPreviousDate,
                    previousSnapshot.Base.Date.Value.AddDays(-1.0)
                )

        // New realized gains are the difference between total and what we already had
        let newRealizedGains =
            Money.FromAmount(totalRealizedUpToSnapshot.Value - realizedUpToPreviousDate.Value)

        // Cumulative realized gains = previous cumulative + new gains from this period
        let realized =
            Money.FromAmount(previousSnapshot.Realized.Value + newRealizedGains.Value)

        // DEBUG: Log realized gains calculation details
        // CoreLogger.logDebugf
        //     "TickerSnapshotCalculateInMemory"
        //     "[Date:%s] Realized gains - Closed trades: %d | Total up to snapshot: %M | Total up to previous: %M | New gains: %M | Cumulative: %M"
        //     (date.ToString())
        //     tradesUpToSnapshot.Length
        //     totalRealizedUpToSnapshot.Value
        //     realizedUpToPreviousDate.Value
        //     newRealizedGains.Value
        //     realized.Value

        // Calculate performance percentage
        let performance =
            if costBasis.Value <> 0.0m then
                (unrealized.Value / costBasis.Value) * 100.0m
            else
                0.0m

        // Check for open trades - include both share positions and open option trades
        // OpenTrades = true if:
        // 1. Holding shares (TotalShares > 0), OR
        // 2. Have open option contracts at this snapshot date
        let hasOpenShares = totalShares <> 0.0m
        let normalizedSnapshotDate = SnapshotManagerUtils.normalizeToStartOfDay date

        let hasOpenOptions =
            movements.AllOpeningTrades
            |> List.exists (fun opt ->
                let normalizedTradeDate = SnapshotManagerUtils.normalizeToStartOfDay opt.TimeStamp

                if normalizedTradeDate.Value > normalizedSnapshotDate.Value then
                    false
                else
                    match opt.ClosedWith with
                    | Some closingTradeId ->
                        let closingTrade =
                            movements.AllClosedOptionTrades |> List.tryFind (fun t -> t.Id = closingTradeId)

                        match closingTrade with
                        | Some ct ->
                            let normalizedClosingDate = SnapshotManagerUtils.normalizeToStartOfDay ct.TimeStamp
                            normalizedClosingDate.Value > normalizedSnapshotDate.Value
                        | None -> true
                    | None -> true)

        let openTradesFlag = hasOpenShares || hasOpenOptions

        // Weight is not calculated here - it's calculated at TickerSnapshot level
        let weight = 0.0m

        // DEBUG: Log complete snapshot calculation summary
        // CoreLogger.logDebugf
        //     "TickerSnapshotCalculateInMemory"
        //     "[Date:%s] SNAPSHOT_FINAL - Shares:%M CostBasis:%M SharesUnrealized:%M OpenOptionsUnrealized:%M TotalUnrealized:%M Dividends:%M Options:%M Realized:%M OpenTrades:%b"
        //     (date.ToString())
        //     totalShares
        //     costBasis.Value
        //     sharesUnrealized
        //     openOptionsUnrealized
        //     unrealized.Value
        //     totalDividends.Value
        //     totalOptions.Value
        //     realized.Value
        //     openTrades
        //     totalShares
        //     costBasis.Value
        //     sharesUnrealized
        //     openOptionsUnrealized
        //     unrealized.Value
        //     totalDividends.Value
        //     totalOptions.Value
        //     realized.Value
        //     openTrades
        //     hasOpenShares
        //     hasOpenOptions

        { Base = SnapshotManagerUtils.createBaseSnapshot date
          TickerId = tickerId
          CurrencyId = currencyId
          TickerSnapshotId = 0 // Will be set during persistence phase
          TotalShares = totalShares
          Weight = weight
          CostBasis = costBasis
          RealCost = realCost
          Dividends = totalDividends
          Options = totalOptions
          TotalIncomes = totalIncomes
          Unrealized = unrealized
          Realized = realized
          Performance = performance
          LatestPrice =
            if marketPrice > 0m then
                Money.FromAmount(marketPrice)
            else if totalShares > 0m && costBasis.Value > 0m then
                Money.FromAmount(abs (costBasis.Value / totalShares))
            else
                Money.FromAmount(0m)
          OpenTrades = openTradesFlag }

    /// <summary>
    /// Calculate initial TickerCurrencySnapshot from movements (no previous snapshot).
    /// Scenario B: New movements + no previous → Calculate from zero
    /// This happens for the first snapshot of a ticker or new currency.
    /// </summary>
    /// <param name="movements">Movements for this ticker/currency/date</param>
    /// <param name="marketPrice">Market price for this date</param>
    /// <param name="date">Date for the new snapshot</param>
    /// <param name="tickerId">Ticker ID</param>
    /// <param name="currencyId">Currency ID</param>
    /// <returns>Initial TickerCurrencySnapshot with calculated values</returns>
    let calculateInitialSnapshot
        (movements: TickerCurrencyMovementData)
        (marketPrice: decimal)
        (date: DateTimePattern)
        (tickerId: int)
        (currencyId: int)
        : TickerCurrencySnapshot =

        // CoreLogger.logDebugf
        //     "TickerSnapshotCalculateInMemory"
        //     "Calculating INITIAL snapshot - Ticker:%d Currency:%d Date:%s (Trades:%d Divs:%d Options:%d)"
        //     tickerId
        //     currencyId
        //     (date.ToString())
        //     movements.Trades.Length
        //     movements.Dividends.Length
        //     movements.OptionTrades.Length

        // Calculate shares from trades (no previous)
        // CRITICAL FIX: Account for buy/sell direction using TradeCode
        // Buy trades (BuyToOpen, BuyToClose) add shares (+)
        // Sell trades (SellToOpen, SellToClose) reduce shares (-)
        let totalShares =
            movements.Trades
            |> List.sumBy (fun t ->
                match t.TradeCode with
                | TradeCode.BuyToOpen
                | TradeCode.BuyToClose -> t.Quantity
                | TradeCode.SellToOpen
                | TradeCode.SellToClose -> -t.Quantity)

        // Calculate cost basis from trades (no previous)
        // Cost basis is the actual capital invested (buy side only, absolute value)
        // For BUY trades: add to cost basis
        // For SELL trades: reduce cost basis by the proceeds
        let tradeCostBasis =
            movements.Trades
            |> List.sumBy (fun t ->
                match t.TradeCode with
                | TradeCode.BuyToOpen
                | TradeCode.BuyToClose -> t.Price.Value * t.Quantity // Add to cost
                | TradeCode.SellToOpen
                | TradeCode.SellToClose -> -(t.Price.Value * t.Quantity) // Reduce cost (proceeds)
            )

        let costBasis = Money.FromAmount(tradeCostBasis)

        // Calculate commissions and fees from trades
        let commissions = movements.Trades |> List.sumBy (fun t -> t.Commissions.Value)
        let fees = movements.Trades |> List.sumBy (fun t -> t.Fees.Value)

        // Calculate dividends (gross dividends minus taxes)
        let currentDividends =
            movements.Dividends |> List.sumBy (fun d -> d.DividendAmount.Value)

        let currentDividendTaxes =
            movements.DividendTaxes |> List.sumBy (fun dt -> dt.DividendTaxAmount.Value)

        let netDividends = currentDividends - currentDividendTaxes
        let totalDividends = Money.FromAmount(netDividends)

        // Calculate options income (no previous)
        let currentOptions =
            movements.OptionTrades |> List.sumBy (fun o -> o.NetPremium.Value)

        let totalOptions = Money.FromAmount(currentOptions)

        // Calculate total incomes
        let totalIncomes = Money.FromAmount(totalDividends.Value + totalOptions.Value)

        // Calculate real cost (cost basis + commissions + fees + dividend taxes)
        let realCost =
            Money.FromAmount(costBasis.Value + commissions + fees + currentDividendTaxes)

        // Calculate unrealized gains/losses from open positions only:
        // 1. Shares: (market value - cost basis)
        // 2. Open options: net premium of positions still open
        //
        // Dividends and Realized are tracked separately in their own fields

        // Shares unrealized
        let effectivePrice = if marketPrice > 0m then marketPrice else costBasis.Value
        let sharesMarketValue = effectivePrice * totalShares
        let sharesUnrealized = sharesMarketValue - costBasis.Value

        // Options unrealized (only open positions at this snapshot date)
        // Check if the trade was open AS OF the snapshot date by comparing timestamps
        // Use AllOpeningTrades which contains ALL opening trades (BuyToOpen/SellToOpen)
        // regardless of their final IsOpen status, allowing temporal evaluation
        let normalizedSnapshotDate = SnapshotManagerUtils.normalizeToStartOfDay date

        let openOptionsUnrealized =
            movements.AllOpeningTrades
            |> List.filter (fun opt ->
                // First check: Trade must have occurred ON OR BEFORE snapshot date
                let normalizedTradeDate = SnapshotManagerUtils.normalizeToStartOfDay opt.TimeStamp

                if normalizedTradeDate.Value > normalizedSnapshotDate.Value then
                    false // Trade hasn't happened yet at this snapshot date
                else
                    // Second check: Was it still open at snapshot date?
                    match opt.ClosedWith with
                    | Some closingTradeId ->
                        // Find the closing trade in all option trades
                        let closingTrade =
                            movements.AllClosedOptionTrades |> List.tryFind (fun t -> t.Id = closingTradeId)

                        match closingTrade with
                        | Some ct ->
                            let normalizedClosingDate = SnapshotManagerUtils.normalizeToStartOfDay ct.TimeStamp
                            // Trade was open if closing happened AFTER snapshot date
                            normalizedClosingDate.Value > normalizedSnapshotDate.Value
                        | None ->
                            // Closing trade not found in our dataset - consider it open
                            true
                    | None ->
                        // Not closed yet - definitely open
                        true)
            |> List.sumBy (fun opt -> opt.NetPremium.Value)

        // Total unrealized = ONLY shares unrealized (positions still in market)
        // NOTE: Unrealized is ONLY calculated for stock positions (when totalShares ≠ 0)
        // For options-only tickers (totalShares = 0), unrealized should be 0
        // Option P&L is reflected in Realized gains when positions are closed
        let unrealized = Money.FromAmount(sharesUnrealized)

        // Calculate realized gains (zero for initial snapshot)
        let realized = Money.FromAmount(0.0m) // Calculate performance percentage

        let performance =
            if costBasis.Value <> 0.0m then
                (unrealized.Value / costBasis.Value) * 100.0m
            else
                0.0m

        // Check for open trades - include both share positions and open option trades
        // OpenTrades = true if:
        // 1. Holding shares (TotalShares > 0), OR
        // 2. Have open option contracts at this snapshot date
        let hasOpenShares = totalShares <> 0.0m
        let normalizedSnapshotDate = SnapshotManagerUtils.normalizeToStartOfDay date

        let hasOpenOptions =
            movements.AllOpeningTrades
            |> List.exists (fun opt ->
                let normalizedTradeDate = SnapshotManagerUtils.normalizeToStartOfDay opt.TimeStamp

                if normalizedTradeDate.Value > normalizedSnapshotDate.Value then
                    false
                else
                    match opt.ClosedWith with
                    | Some closingTradeId ->
                        let closingTrade =
                            movements.AllClosedOptionTrades |> List.tryFind (fun t -> t.Id = closingTradeId)

                        match closingTrade with
                        | Some ct ->
                            let normalizedClosingDate = SnapshotManagerUtils.normalizeToStartOfDay ct.TimeStamp
                            normalizedClosingDate.Value > normalizedSnapshotDate.Value
                        | None -> true
                    | None -> true)

        let openTradesFlag = hasOpenShares || hasOpenOptions

        // Weight is not calculated here - it's calculated at TickerSnapshot level
        let weight = 0.0m

        // CoreLogger.logDebugf
        //     "TickerSnapshotCalculateInMemory"
        //     "Calculated INITIAL snapshot - Shares:%M CostBasis:%M SharesUnrealized:%M OpenOptions:%M TotalUnrealized:%M Dividends:%M Options:%M OpenTrades:%b (Shares:%b Options:%b)"
        //     totalShares
        //     costBasis.Value
        //     sharesUnrealized
        //     openOptionsUnrealized
        //     unrealized.Value
        //     totalDividends.Value
        //     totalOptions.Value
        //     openTrades
        //     hasOpenShares
        //     hasOpenOptions

        { Base = SnapshotManagerUtils.createBaseSnapshot date
          TickerId = tickerId
          CurrencyId = currencyId
          TickerSnapshotId = 0 // Will be set during persistence phase
          TotalShares = totalShares
          Weight = weight
          CostBasis = costBasis
          RealCost = realCost
          Dividends = totalDividends
          Options = totalOptions
          TotalIncomes = totalIncomes
          Unrealized = unrealized
          Realized = realized
          Performance = performance
          LatestPrice =
            if marketPrice > 0m then
                Money.FromAmount(marketPrice)
            else if totalShares > 0m && costBasis.Value > 0m then
                Money.FromAmount(abs (costBasis.Value / totalShares))
            else
                Money.FromAmount(0m)
          OpenTrades = openTradesFlag }

    /// <summary>
    /// Update existing TickerCurrencySnapshot with new movements.
    /// Scenario C: New movements + previous + existing → Update existing with delta
    /// This is used when recalculating snapshots (force recalculation).
    /// </summary>
    /// <param name="movements">New movements for this ticker/currency/date</param>
    /// <param name="previousSnapshot">Previous TickerCurrencySnapshot for cumulative calculations</param>
    /// <param name="existingSnapshot">Existing snapshot to update</param>
    /// <param name="marketPrice">Market price for this date</param>
    /// <returns>Updated TickerCurrencySnapshot</returns>
    let updateExistingSnapshot
        (movements: TickerCurrencyMovementData)
        (previousSnapshot: TickerCurrencySnapshot)
        (existingSnapshot: TickerCurrencySnapshot)
        (marketPrice: decimal)
        : TickerCurrencySnapshot =

        // CoreLogger.logDebugf
        //     "TickerSnapshotCalculateInMemory"
        //     "Updating EXISTING snapshot - Ticker:%d Currency:%d Date:%s (Existing:%d movements)"
        //     existingSnapshot.TickerId
        //     existingSnapshot.CurrencyId
        //     (existingSnapshot.Base.Date.Value.ToString())
        //     (movements.Trades.Length
        //      + movements.Dividends.Length
        //      + movements.OptionTrades.Length)

        // Recalculate as if it's a new snapshot (this ensures consistency)
        let recalculated =
            calculateNewSnapshot
                movements
                previousSnapshot
                marketPrice
                existingSnapshot.Base.Date
                existingSnapshot.TickerId
                existingSnapshot.CurrencyId

        // Preserve the existing ID
        { recalculated with
            Base =
                { recalculated.Base with
                    Id = existingSnapshot.Base.Id } }

    /// <summary>
    /// Carry forward previous snapshot to new date (no movements).
    /// Scenario D: No movements + previous → Carry forward with price update
    /// This maintains snapshot continuity for dates with no activity.
    /// </summary>
    /// <param name="previousSnapshot">Previous TickerCurrencySnapshot to carry forward</param>
    /// <param name="newDate">New date for the carried-forward snapshot</param>
    /// <param name="marketPrice">Market price for the new date</param>
    /// <returns>Carried-forward TickerCurrencySnapshot</returns>
    let carryForwardSnapshot
        (previousSnapshot: TickerCurrencySnapshot)
        (newDate: DateTimePattern)
        (marketPrice: decimal)
        : TickerCurrencySnapshot =

        // CoreLogger.logDebugf
        //     "TickerSnapshotCalculateInMemory"
        //     "Carrying forward snapshot - Ticker:%d Currency:%d From:%s To:%s"
        //     previousSnapshot.TickerId
        //     previousSnapshot.CurrencyId
        //     (previousSnapshot.Base.Date.Value.ToString())
        //     (newDate.ToString())

        // Recalculate unrealized gains with new market price
        // Following "Open Positions Only" approach:
        // Unrealized = ONLY shares unrealized (positions still in market)

        // 1. Shares unrealized (recalculate with new market price)
        let effectivePrice =
            if marketPrice > 0m then
                marketPrice
            else if previousSnapshot.TotalShares > 0m && previousSnapshot.CostBasis.Value > 0m then
                // For equity positions, derive average buy price from cost basis
                abs (previousSnapshot.CostBasis.Value / previousSnapshot.TotalShares)
            else
                previousSnapshot.LatestPrice.Value // Carry forward last known price

        let sharesMarketValue = effectivePrice * previousSnapshot.TotalShares
        let sharesUnrealized = sharesMarketValue - previousSnapshot.CostBasis.Value

        // 2. Open options unrealized (carry forward - no new trades)
        // Options-only tickers have unrealized = 0, so this is not included
        // Only stock positions have unrealized
        let openOptionsUnrealized = 0m // Options don't contribute to unrealized

        // 3. Total unrealized = shares + open options (positions still in market)
        let unrealized = Money.FromAmount(sharesUnrealized + openOptionsUnrealized)

        // Recalculate performance percentage
        let performance =
            if previousSnapshot.CostBasis.Value <> 0.0m then
                (unrealized.Value / previousSnapshot.CostBasis.Value) * 100.0m
            else
                0.0m

        // CoreLogger.logDebugf
        //     "TickerSnapshotCalculateInMemory"
        //     "Carried forward - Shares:%M Price:%M->%M SharesUnrealized:%M OpenOptions:%M TotalUnrealized:%M Dividends:%M Realized:%M"
        //     previousSnapshot.TotalShares
        //     previousSnapshot.LatestPrice.Value
        //     marketPrice
        //     sharesUnrealized
        //     openOptionsUnrealized
        //     unrealized.Value
        //     previousSnapshot.Dividends.Value
        //     previousSnapshot.Realized.Value

        // Create new snapshot with same cumulative values but updated price/unrealized
        { Base = SnapshotManagerUtils.createBaseSnapshot newDate
          TickerId = previousSnapshot.TickerId
          CurrencyId = previousSnapshot.CurrencyId
          TickerSnapshotId = 0 // Will be set during persistence phase
          TotalShares = previousSnapshot.TotalShares
          Weight = 0.0m // Will be recalculated at TickerSnapshot level
          CostBasis = previousSnapshot.CostBasis
          RealCost = previousSnapshot.RealCost
          Dividends = previousSnapshot.Dividends
          Options = previousSnapshot.Options
          TotalIncomes = previousSnapshot.TotalIncomes
          Unrealized = unrealized
          Realized = previousSnapshot.Realized
          Performance = performance
          LatestPrice =
            if marketPrice > 0m then
                Money.FromAmount(marketPrice)
            else if previousSnapshot.TotalShares > 0m && previousSnapshot.CostBasis.Value > 0m then
                // For equity positions, derive average buy price from cost basis
                Money.FromAmount(abs (previousSnapshot.CostBasis.Value / previousSnapshot.TotalShares))
            else
                previousSnapshot.LatestPrice
          OpenTrades = previousSnapshot.OpenTrades }

    /// <summary>
    /// Helper to extract movements for a specific ticker/currency/date from batch data.
    /// </summary>
    /// <param name="tickerId">Ticker ID</param>
    /// <param name="currencyId">Currency ID</param>
    /// <param name="date">Date</param>
    /// <param name="allMovements">All movements loaded in batch</param>
    /// <returns>TickerCurrencyMovementData for this specific combination</returns>
    let getMovementsForTickerCurrencyDate
        (tickerId: int)
        (currencyId: int)
        (date: DateTimePattern)
        (allMovements: TickerSnapshotBatchLoader.TickerMovementData)
        : TickerCurrencyMovementData option =

        let key = (tickerId, currencyId, date)

        let trades = allMovements.Trades.TryFind(key) |> Option.defaultValue []
        let dividends = allMovements.Dividends.TryFind(key) |> Option.defaultValue []

        let dividendTaxes =
            allMovements.DividendTaxes.TryFind(key) |> Option.defaultValue []

        let optionTrades = allMovements.OptionTrades.TryFind(key) |> Option.defaultValue []

        // Get ALL closed option trades for this ticker/currency (not filtered by date)
        // This is needed for accurate realized gains calculation
        let tickerCurrencyKey = (tickerId, currencyId)

        let allClosedOptionTrades =
            allMovements.AllClosedOptionTrades.TryFind(tickerCurrencyKey)
            |> Option.defaultValue []

        // Get ALL opening option trades for this ticker/currency (not filtered by date)
        // This is needed for temporal unrealized gains calculation
        let allOpeningTrades =
            allMovements.AllOpeningTrades.TryFind(tickerCurrencyKey)
            |> Option.defaultValue []

        // Only return Some if there are any movements
        if
            trades.IsEmpty
            && dividends.IsEmpty
            && dividendTaxes.IsEmpty
            && optionTrades.IsEmpty
        then
            None
        else
            Some
                { Trades = trades
                  Dividends = dividends
                  DividendTaxes = dividendTaxes
                  OptionTrades = optionTrades
                  AllOpeningTrades = allOpeningTrades
                  AllClosedOptionTrades = allClosedOptionTrades }

    /// <summary>
    /// Get all currencies that have movements or prices for a ticker on a date.
    /// This determines which TickerCurrencySnapshots need to be created.
    /// </summary>
    /// <param name="tickerId">Ticker ID</param>
    /// <param name="date">Date</param>
    /// <param name="allMovements">All movements loaded in batch</param>
    /// <param name="marketPrices">All market prices loaded in batch</param>
    /// <returns>List of currency IDs with activity</returns>
    let getRelevantCurrenciesForTickerDate
        (tickerId: int)
        (date: DateTimePattern)
        (allMovements: TickerSnapshotBatchLoader.TickerMovementData)
        (marketPrices: Map<(int * DateTimePattern), decimal>)
        : int list =

        // Extract currencies from movements
        let currenciesFromMovements =
            [ allMovements.Trades
              |> Map.toList
              |> List.filter (fun ((tid, _, d), _) -> tid = tickerId && d = date)
              |> List.map (fun ((_, cid, _), _) -> cid)

              allMovements.Dividends
              |> Map.toList
              |> List.filter (fun ((tid, _, d), _) -> tid = tickerId && d = date)
              |> List.map (fun ((_, cid, _), _) -> cid)

              allMovements.OptionTrades
              |> Map.toList
              |> List.filter (fun ((tid, _, d), _) -> tid = tickerId && d = date)
              |> List.map (fun ((_, cid, _), _) -> cid) ]
            |> List.concat
            |> List.distinct

        // If no currencies found from movements, this means we might need to carry forward
        // For now, return empty list - the caller will handle carry-forward logic
        currenciesFromMovements
