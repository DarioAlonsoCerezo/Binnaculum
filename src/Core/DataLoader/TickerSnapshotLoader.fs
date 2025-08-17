namespace Binnaculum.Core.DataLoader

open Binnaculum.Core.UI
open DynamicData
open Binnaculum.Core.DatabaseToModels

module internal TickerSnapshotLoader =
    let load() = task {
        let tickers = Collections.Tickers.Items
        let snapshots = 
            tickers
            |> Seq.filter (fun t -> t.Id > 0) // Exclude any default/placeholder tickers
            |> Seq.map (fun ticker ->
                async {
                    let! latestSnapshot = TickerSnapshotExtensions.Do.getLatestByTickerId ticker.Id |> Async.AwaitTask
                    match latestSnapshot with
                    | Some dbSnapshot ->
                        return Some (dbSnapshot.tickerSnapshotToModel())
                    | None ->
                        return None
                })
            |> Async.Parallel
            |> Async.RunSynchronously
            |> Array.choose id
            |> Array.toList
        
        snapshots
        |> List.iter (fun newSnapshot ->
            let tickerId = newSnapshot.Ticker.Id
            let existingSnapshot = Collections.TickerSnapshots.Items 
                                 |> Seq.tryFind (fun s -> s.Ticker.Id = tickerId)
            match existingSnapshot with
            | Some existing when existing <> newSnapshot ->
                Collections.TickerSnapshots.Replace(existing, newSnapshot)
            | None ->
                Collections.TickerSnapshots.Add(newSnapshot)
            | Some _ -> () // Same snapshot, no action needed
        )
    }

