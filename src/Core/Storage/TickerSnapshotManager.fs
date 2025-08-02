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
            return()
        }

    /// Handles snapshot update when a new ticker is created
    let handleNewTicker (ticker: Ticker) =
        let today = DateTimePattern.FromDateTime(DateTime.Today)
        createTickerSnapshot ticker.Id today

    let handleTickerChange (tickerId: int, date: DateTimePattern) =
        updateTickerSnapshot tickerId date
        

