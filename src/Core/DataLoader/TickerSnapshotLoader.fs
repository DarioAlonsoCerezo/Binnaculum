namespace Binnaculum.Core.DataLoader

open Binnaculum.Core.UI
open DynamicData
open Binnaculum.Core.DatabaseToModels
open Binnaculum.Core.Storage
open Binnaculum.Core.ModelsToDatabase

module internal TickerSnapshotLoader =
    let load() = task {
        let tickers = Collections.Tickers.Items
        
        // Check if there are any tickers and stop if there are none
        if Seq.isEmpty tickers then
            return ()
        else
            let snapshots = 
                tickers
                |> Seq.filter (fun t -> t.Id > 0) // Exclude any default/placeholder tickers
                |> Seq.map (fun ticker -> async {
                    let! latestSnapshot = TickerSnapshotExtensions.Do.getLatestByTickerId ticker.Id |> Async.AwaitTask
                    match latestSnapshot with
                    | Some dbSnapshot ->
                        return Some (dbSnapshot.tickerSnapshotToModel())
                    | None ->
                        // Create default snapshot for ticker without snapshots
                        let! databaseTicker = ticker.tickerToDatabase() |> Async.AwaitTask
                        do! TickerSnapshotManager.handleNewTicker(databaseTicker) |> Async.AwaitTask |> Async.Ignore
                        // Try to get the newly created snapshot
                        let! newSnapshot = TickerSnapshotExtensions.Do.getLatestByTickerId ticker.Id |> Async.AwaitTask
                        match newSnapshot with
                        | Some dbSnapshot ->
                            return Some (dbSnapshot.tickerSnapshotToModel())
                        | None ->
                            // If we still can't get a snapshot, something is wrong
                            failwithf "Failed to create or retrieve snapshot for ticker %s (ID: %d)" ticker.Symbol ticker.Id
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

