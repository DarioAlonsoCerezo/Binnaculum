namespace Binnaculum.Core.Storage

open System
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Storage.SnapshotManagerUtils


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

