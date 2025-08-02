namespace Binnaculum.Core.Storage

open System
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Storage.SnapshotManagerUtils
open Microsoft.Maui.Storage
open Binnaculum.Core.Keys
open TickerSnapshotExtensions
open TickerCurrencySnapshotExtensions


module internal TickerSnapshotManager =

    /// <summary>
    /// Handles calculation, update, and recalculation of TickerSnapshots.
    /// </summary>

    /// Calculate a daily snapshot for a ticker on a given date
    let private calculateTickerSnapshot (tickerId: int) (date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            return {
                Base = createBaseSnapshot snapshotDate
                TickerId = tickerId
            }
        }

    let private createDefaultTickerCurrencySnapshot (date: DateTimePattern) (tickerId: int) (currencyId: int) (snapshotId: int) = task {
        let! priceByDate = TickerPriceExtensions.Do.getPriceByDateOrPrevious(tickerId, date.Value.ToString())
        
        let snapshotDate = getDateOnly date
        let snapshot = 
            {
                Base = createBaseSnapshot snapshotDate
                TickerId = tickerId
                CurrencyId = currencyId
                TickerSnapshotId = snapshotId
                TotalShares = 0.0M
                Weight = 0.0M
                CostBasis = Money.FromAmount(0.0m)
                RealCost = Money.FromAmount(0.0m)
                Dividends = Money.FromAmount(0.0m)
                Options = Money.FromAmount(0.0m)
                TotalIncomes = Money.FromAmount(0.0m)
                Unrealized = Money.FromAmount(0.0m)
                Realized = Money.FromAmount(0.0m)
                Performance = 0.0m
                LatestPrice = Money.FromAmount(priceByDate)
                OpenTrades = false
            }
        return snapshot
    }

    /// Create a new snapshot for a ticker and date
    let private createTickerSnapshot (tickerId: int) (date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            let newSnapshot = {
                Base = createBaseSnapshot snapshotDate
                TickerId = tickerId
            }
            do! newSnapshot.save()
            let! createdSnapshot = TickerSnapshotExtensions.Do.getByTickerIdAndDate(tickerId, date)
            match createdSnapshot with
            | None -> failwith "Failed to create ticker snapshot"
            | Some snapshot ->
                let preferenceCurrency = Preferences.Get(CurrencyKey, DefaultCurrency)
                let! currency = CurrencyExtensions.Do.getByCode(preferenceCurrency)
                match currency with
                | None -> failwithf "Currency %s not found" preferenceCurrency
                | Some currency ->
                    let! currencySnapshot = createDefaultTickerCurrencySnapshot date tickerId currency.Id snapshot.Base.Id
                    do! currencySnapshot.save()
        }

    /// Create or update a snapshot for a ticker and date
    let private updateTickerSnapshot (tickerId: int) (date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            let! existing = TickerSnapshotExtensions.Do.getByTickerIdAndDate(tickerId, snapshotDate)
            match existing with
            | Some s ->
                let updated = {
                    Base = { (createBaseSnapshot snapshotDate) with Id = s.Base.Id }
                    TickerId = tickerId
                }
                do! TickerSnapshotExtensions.Do.save(updated)
            | None ->
                let! newSnapshot = calculateTickerSnapshot tickerId snapshotDate
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

    /// Handles snapshot update when a new ticker is created
    let handleNewTicker (ticker: Ticker) =
        let today = DateTimePattern.FromDateTime(DateTime.Today)
        createTickerSnapshot ticker.Id today

    /// Handles snapshot update when a ticker movement occurs
    let handleTickerMovementSnapshot (tickerId: int, date: DateTimePattern) =
        task {
            // Update the ticker snapshot for the given date
            do! updateTickerSnapshot tickerId date
            // Recalculate all future snapshots from this date
            do! recalculateTickerSnapshotsFromDate tickerId date
        }

    let handleTickerPriceUpdate (tickerId: int, date: DateTimePattern) =
        task {
            // Update the ticker snapshot for the given date
            do! updateTickerSnapshot tickerId date
            // Recalculate all future snapshots from this date
            do! recalculateTickerSnapshotsFromDate tickerId date
        }

