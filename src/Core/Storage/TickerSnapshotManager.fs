namespace Binnaculum.Core.Storage

open System
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Storage.SnapshotManagerUtils


module internal SnapshotTickerManager =

    /// <summary>
    /// Handles calculation, update, and recalculation of TickerSnapshots.
    /// </summary>

    /// Calculate a daily snapshot for a ticker on a given date
    let private calculateTickerSnapshot (tickerId: int) (date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            // Get all trades for this ticker up to and including the date
            let! trades = TradeExtensions.Do.getAll() |> Async.AwaitTask
            let trades = trades |> List.filter (fun t -> t.TickerId = tickerId && t.TimeStamp.Value.Date <= snapshotDate.Value.Date)
            // Get all dividends for this ticker up to and including the date
            let! dividends = DividendExtensions.Do.getAll() |> Async.AwaitTask
            let dividends = dividends |> List.filter (fun d -> d.TickerId = tickerId && d.TimeStamp.Value.Date <= snapshotDate.Value.Date)
            // Get all option trades for this ticker up to and including the date
            let! options = OptionTradeExtensions.Do.getAll() |> Async.AwaitTask
            let options = options |> List.filter (fun o -> o.TickerId = tickerId && o.TimeStamp.Value.Date <= snapshotDate.Value.Date)
            // Get all splits for this ticker up to and including the date
            let! splits = TickerSplitExtensions.Do.getAll() |> Async.AwaitTask
            let splits = splits |> List.filter (fun s -> s.TickerId = tickerId && s.SplitDate.Value.Date <= snapshotDate.Value.Date)
            // Get the latest price for this ticker up to and including the date
            let! prices = TickerPriceExtensions.Do.getAll() |> Async.AwaitTask
            let prices = prices |> List.filter (fun p -> p.TickerId = tickerId && p.PriceDate.Value.Date <= snapshotDate.Value.Date)
            let latestPrice =
                prices
                |> List.sortByDescending (fun p -> p.PriceDate.Value)
                |> List.tryHead

            // Aggregate calculations
            let totalShares =
                trades
                |> List.sumBy (fun t -> match t.TradeCode with | TradeCode.BuyToOpen | TradeCode.BuyToClose -> t.Quantity | TradeCode.SellToOpen | TradeCode.SellToClose -> -t.Quantity)
                // TODO: Adjust for splits if needed
            let costBasis =
                trades
                |> List.sumBy (fun t -> t.Price.Value * t.Quantity)
                |> Money.FromAmount
            let realCost =
                trades
                |> List.sumBy (fun t -> (t.Price.Value + t.Commissions.Value + t.Fees.Value) * t.Quantity)
                |> Money.FromAmount
            let dividendsSum =
                dividends
                |> List.sumBy (fun d -> d.DividendAmount.Value)
                |> Money.FromAmount
            let optionsSum =
                options
                |> List.sumBy (fun o -> o.NetPremium.Value)
                |> Money.FromAmount
            let totalIncomes =
                dividendsSum.Value + optionsSum.Value
                |> Money.FromAmount
            let latestPriceValue =
                match latestPrice with
                | Some p -> p.Price
                | None -> Money.FromAmount 0m
            let unrealized = Money.FromAmount ((latestPriceValue.Value * totalShares) - realCost.Value)
            let realized = Money.FromAmount 0m // Placeholder: Realized P/L calculation can be added
            let performance =
                if realCost.Value <> 0m then (unrealized.Value / realCost.Value) * 100m else 0m
            let openTrades = trades |> List.exists (fun t -> t.TradeCode = TradeCode.BuyToOpen || t.TradeCode = TradeCode.SellToOpen)
            // Weight is a placeholder (requires portfolio context)
            let weight = 0m
            // Use the first trade's currency as the snapshot currency, or fallback to 1
            let currencyId = trades |> List.tryHead |> Option.map (fun t -> t.CurrencyId) |> Option.defaultValue 1

            return {
                Base = createBaseSnapshot snapshotDate
                TickerId = tickerId
                CurrencyId = currencyId
                TotalShares = totalShares
                Weight = weight
                CostBasis = costBasis
                RealCost = realCost
                Dividends = dividendsSum
                Options = optionsSum
                TotalIncomes = totalIncomes
                Unrealized = unrealized
                Realized = realized
                Performance = performance
                LatestPrice = latestPriceValue
                OpenTrades = openTrades
            }
        }

    /// Create or update a snapshot for a ticker and date
    let private updateTickerSnapshot (tickerId: int) (date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            let! existing = TickerSnapshotExtensions.Do.getByTickerIdAndDate(tickerId, snapshotDate)
            let! newSnapshot = calculateTickerSnapshot tickerId snapshotDate
            match existing with
            | Some s ->
                let updated = { newSnapshot with Base = { newSnapshot.Base with Id = s.Base.Id } }
                do! TickerSnapshotExtensions.Do.save(updated)
            | None ->
                do! TickerSnapshotExtensions.Do.save(newSnapshot)
        }

    /// Recalculate all snapshots for a ticker from a given date forward
    let private recalculateTickerSnapshotsFromDate (tickerId: int) (fromDate: DateTimePattern) =
        task {
            let startDate = getDateOnly fromDate
            let! prices = TickerPriceExtensions.Do.getAll() |> Async.AwaitTask
            let dates =
                prices
                |> List.filter (fun p -> p.TickerId = tickerId && p.PriceDate.Value.Date >= startDate.Value.Date)
                |> List.map (fun p -> p.PriceDate)
                |> List.distinct
                |> List.sort
            for d in dates do
                do! updateTickerSnapshot tickerId d
        }

    /// Handles snapshot update when a ticker movement occurs
    let handleTickerMovementSnapshot (tickerId: int, date: DateTimePattern) =
        task {
            // Update the ticker snapshot for the given date
            do! updateTickerSnapshot tickerId date
            // Recalculate all future snapshots from this date
            do! recalculateTickerSnapshotsFromDate tickerId date
        }

    /// Handles snapshot update when a new ticker is created
    let handleNewTicker (ticker: Ticker) =
        let today = DateTimePattern.FromDateTime(DateTime.Today)
        updateTickerSnapshot ticker.Id today

