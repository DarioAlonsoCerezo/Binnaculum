namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.DatabaseModel
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

        CoreLogger.logDebugf
            "TickerSnapshotCalculateInMemory"
            "Calculating NEW snapshot - Ticker:%d Currency:%d Date:%s (Trades:%d Divs:%d Options:%d) Previous:%s"
            tickerId
            currencyId
            (date.ToString())
            movements.Trades.Length
            movements.Dividends.Length
            movements.OptionTrades.Length
            (previousSnapshot.Date.Value.ToString())

        // Calculate shares delta from trades
        let sharesDelta = movements.Trades |> List.sumBy (fun t -> t.Quantity)
        let totalShares = previousSnapshot.TotalShares + sharesDelta

        // Calculate cost basis delta from trades
        let tradeCostBasis = movements.Trades |> List.sumBy (fun t -> t.Price.Value * t.Quantity)
        let costBasis = Money.FromAmount(previousSnapshot.CostBasis.Value + tradeCostBasis)

        // Calculate commissions and fees from trades
        let commissions = movements.Trades |> List.sumBy (fun t -> t.Commissions.Value)
        let fees = movements.Trades |> List.sumBy (fun t -> t.Fees.Value)

        // Calculate dividends (gross dividends minus taxes)
        let currentDividends = movements.Dividends |> List.sumBy (fun d -> d.DividendAmount.Value)
        let currentDividendTaxes = movements.DividendTaxes |> List.sumBy (fun dt -> dt.DividendTaxAmount.Value)
        let netDividends = currentDividends - currentDividendTaxes
        let totalDividends = Money.FromAmount(previousSnapshot.Dividends.Value + netDividends)

        // Calculate options income
        let currentOptions = movements.OptionTrades |> List.sumBy (fun o -> o.NetPremium.Value)
        let totalOptions = Money.FromAmount(previousSnapshot.Options.Value + currentOptions)

        // Calculate total incomes
        let totalIncomes = Money.FromAmount(totalDividends.Value + totalOptions.Value)

        // Calculate real cost (cost basis + commissions + fees + dividend taxes)
        let realCost = Money.FromAmount(costBasis.Value + commissions + fees + currentDividendTaxes)

        // Calculate unrealized gains/losses
        let marketValue = marketPrice * totalShares
        let unrealized = Money.FromAmount(marketValue - costBasis.Value)

        // Calculate realized gains (carry forward from previous snapshot for now)
        // TODO: Track closed positions for accurate realized gains calculation
        let realized = previousSnapshot.Realized

        // Calculate performance percentage
        let performance =
            if costBasis.Value <> 0.0m then
                (unrealized.Value / costBasis.Value) * 100.0m
            else
                0.0m

        // Check for open trades - include both share positions and open option trades
        let hasOpenShares = totalShares <> 0.0m
        let hasOpenOptions = movements.OptionTrades |> List.exists (fun opt -> opt.IsOpen)
        let openTrades = hasOpenShares || hasOpenOptions

        // Weight is not calculated here - it's calculated at TickerSnapshot level
        let weight = 0.0m

        CoreLogger.logDebugf
            "TickerSnapshotCalculateInMemory"
            "Calculated NEW snapshot - Shares:%M CostBasis:%M Unrealized:%M Options:%M Realized:%M"
            totalShares
            costBasis.Value
            unrealized.Value
            totalOptions.Value
            realized.Value

        { Base = SnapshotManagerUtils.createBaseSnapshot date
          TickerId = tickerId
          CurrencyId = currencyId
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
          LatestPrice = marketPrice
          OpenTrades = openTrades }

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

        CoreLogger.logDebugf
            "TickerSnapshotCalculateInMemory"
            "Calculating INITIAL snapshot - Ticker:%d Currency:%d Date:%s (Trades:%d Divs:%d Options:%d)"
            tickerId
            currencyId
            (date.ToString())
            movements.Trades.Length
            movements.Dividends.Length
            movements.OptionTrades.Length

        // Calculate shares from trades (no previous)
        let totalShares = movements.Trades |> List.sumBy (fun t -> t.Quantity)

        // Calculate cost basis from trades (no previous)
        let tradeCostBasis = movements.Trades |> List.sumBy (fun t -> t.Price.Value * t.Quantity)
        let costBasis = Money.FromAmount(tradeCostBasis)

        // Calculate commissions and fees from trades
        let commissions = movements.Trades |> List.sumBy (fun t -> t.Commissions.Value)
        let fees = movements.Trades |> List.sumBy (fun t -> t.Fees.Value)

        // Calculate dividends (gross dividends minus taxes)
        let currentDividends = movements.Dividends |> List.sumBy (fun d -> d.DividendAmount.Value)
        let currentDividendTaxes = movements.DividendTaxes |> List.sumBy (fun dt -> dt.DividendTaxAmount.Value)
        let netDividends = currentDividends - currentDividendTaxes
        let totalDividends = Money.FromAmount(netDividends)

        // Calculate options income (no previous)
        let currentOptions = movements.OptionTrades |> List.sumBy (fun o -> o.NetPremium.Value)
        let totalOptions = Money.FromAmount(currentOptions)

        // Calculate total incomes
        let totalIncomes = Money.FromAmount(totalDividends.Value + totalOptions.Value)

        // Calculate real cost (cost basis + commissions + fees + dividend taxes)
        let realCost = Money.FromAmount(costBasis.Value + commissions + fees + currentDividendTaxes)

        // Calculate unrealized gains/losses
        let marketValue = marketPrice * totalShares
        let unrealized = Money.FromAmount(marketValue - costBasis.Value)

        // Calculate realized gains (zero for initial snapshot)
        let realized = Money.FromAmount(0.0m)

        // Calculate performance percentage
        let performance =
            if costBasis.Value <> 0.0m then
                (unrealized.Value / costBasis.Value) * 100.0m
            else
                0.0m

        // Check for open trades - include both share positions and open option trades
        let hasOpenShares = totalShares <> 0.0m
        let hasOpenOptions = movements.OptionTrades |> List.exists (fun opt -> opt.IsOpen)
        let openTrades = hasOpenShares || hasOpenOptions

        // Weight is not calculated here - it's calculated at TickerSnapshot level
        let weight = 0.0m

        CoreLogger.logDebugf
            "TickerSnapshotCalculateInMemory"
            "Calculated INITIAL snapshot - Shares:%M CostBasis:%M Unrealized:%M Options:%M"
            totalShares
            costBasis.Value
            unrealized.Value
            totalOptions.Value

        { Base = SnapshotManagerUtils.createBaseSnapshot date
          TickerId = tickerId
          CurrencyId = currencyId
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
          LatestPrice = marketPrice
          OpenTrades = openTrades }

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

        CoreLogger.logDebugf
            "TickerSnapshotCalculateInMemory"
            "Updating EXISTING snapshot - Ticker:%d Currency:%d Date:%s (Existing:%d movements)"
            existingSnapshot.TickerId
            existingSnapshot.CurrencyId
            (existingSnapshot.Date.Value.ToString())
            (movements.Trades.Length + movements.Dividends.Length + movements.OptionTrades.Length)

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
        { recalculated with Base = { recalculated.Base with Id = existingSnapshot.Base.Id } }

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

        CoreLogger.logDebugf
            "TickerSnapshotCalculateInMemory"
            "Carrying forward snapshot - Ticker:%d Currency:%d From:%s To:%s"
            previousSnapshot.TickerId
            previousSnapshot.CurrencyId
            (previousSnapshot.Date.Value.ToString())
            (newDate.ToString())

        // Recalculate unrealized gains with new market price
        let marketValue = marketPrice * previousSnapshot.TotalShares
        let unrealized = Money.FromAmount(marketValue - previousSnapshot.CostBasis.Value)

        // Recalculate performance percentage
        let performance =
            if previousSnapshot.CostBasis.Value <> 0.0m then
                (unrealized.Value / previousSnapshot.CostBasis.Value) * 100.0m
            else
                0.0m

        CoreLogger.logDebugf
            "TickerSnapshotCalculateInMemory"
            "Carried forward - Shares:%M Price:%M->%M Unrealized:%M->%M"
            previousSnapshot.TotalShares
            previousSnapshot.LatestPrice
            marketPrice
            previousSnapshot.Unrealized.Value
            unrealized.Value

        // Create new snapshot with same cumulative values but updated price/unrealized
        { Base = SnapshotManagerUtils.createBaseSnapshot newDate
          TickerId = previousSnapshot.TickerId
          CurrencyId = previousSnapshot.CurrencyId
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
          LatestPrice = marketPrice
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
        let dividendTaxes = allMovements.DividendTaxes.TryFind(key) |> Option.defaultValue []
        let optionTrades = allMovements.OptionTrades.TryFind(key) |> Option.defaultValue []

        // Only return Some if there are any movements
        if trades.IsEmpty && dividends.IsEmpty && dividendTaxes.IsEmpty && optionTrades.IsEmpty then
            None
        else
            Some
                { Trades = trades
                  Dividends = dividends
                  DividendTaxes = dividendTaxes
                  OptionTrades = optionTrades }

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
