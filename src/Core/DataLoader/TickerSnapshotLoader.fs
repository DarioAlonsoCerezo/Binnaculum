namespace Binnaculum.Core.DataLoader

open System
open Binnaculum.Core.UI
open DynamicData
open Binnaculum.Core.DatabaseToModels
open Binnaculum.Core.Storage
open Binnaculum.Core.ModelsToDatabase
open Binnaculum.Core.Database
open Binnaculum.Core.Models

module internal TickerSnapshotLoader =
    let load () =
        task {
            let tickers = Collections.Tickers.Items

            // Check if there are any tickers and stop if there are none
            if Seq.isEmpty tickers then
                return ()
            else
                let snapshots =
                    tickers
                    |> Seq.filter (fun t -> t.Id > 0) // Exclude any default/placeholder tickers
                    |> Seq.map (fun ticker ->
                        async {
                            let! latestSnapshot =
                                TickerSnapshotExtensions.Do.getLatestByTickerId ticker.Id |> Async.AwaitTask

                            match latestSnapshot with
                            | Some dbSnapshot ->
                                // Load the currency snapshots for this ticker snapshot
                                let! currencySnapshots =
                                    TickerCurrencySnapshotExtensions.Do.getAllByTickerSnapshotId dbSnapshot.Base.Id
                                    |> Async.AwaitTask

                                // Convert to model with actual currency data
                                let mainCurrency =
                                    match currencySnapshots |> List.tryFind (fun cs -> cs.CurrencyId = 141) with // USD = 141
                                    | Some cs ->
                                        { Id = cs.Base.Id
                                          Date = DateOnly.FromDateTime(cs.Base.Date.Value)
                                          Ticker = cs.TickerId.ToFastTickerById()
                                          Currency = cs.CurrencyId.ToFastCurrencyById()
                                          TotalShares = cs.TotalShares
                                          Weight = cs.Weight
                                          CostBasis = cs.CostBasis.Value
                                          RealCost = cs.RealCost.Value
                                          Dividends = cs.Dividends.Value
                                          Options = cs.Options.Value
                                          TotalIncomes = cs.TotalIncomes.Value
                                          Unrealized = cs.Unrealized.Value
                                          Realized = cs.Realized.Value
                                          Performance = cs.Performance
                                          LatestPrice = cs.LatestPrice.Value
                                          OpenTrades = cs.OpenTrades
                                          Commissions = cs.Commissions.Value
                                          Fees = cs.Fees.Value }
                                    | None ->
                                        // Fallback to default if no USD currency snapshot found
                                        { Id = 0
                                          Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                                          Ticker = dbSnapshot.TickerId.ToFastTickerById()
                                          Currency = "USD".ToFastCurrency()
                                          TotalShares = 0m
                                          Weight = 0.0m
                                          CostBasis = 0.0m
                                          RealCost = 0.0m
                                          Dividends = 0.0m
                                          Options = 0.0m
                                          TotalIncomes = 0.0m
                                          Unrealized = 0.0m
                                          Realized = 0.0m
                                          Performance = 0.0m
                                          LatestPrice = 0.0m
                                          OpenTrades = false
                                          Commissions = 0.0m
                                          Fees = 0.0m }

                                let tickerSnapshot =
                                    { Id = dbSnapshot.Base.Id
                                      Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                                      Ticker = dbSnapshot.TickerId.ToFastTickerById()
                                      MainCurrency = mainCurrency
                                      OtherCurrencies = [] }

                                return Some tickerSnapshot
                            | None ->
                                // Create default snapshot for ticker without snapshots
                                let! databaseTicker = ticker.tickerToDatabase () |> Async.AwaitTask

                                do!
                                    TickerSnapshotManager.handleNewTicker (databaseTicker)
                                    |> Async.AwaitTask
                                    |> Async.Ignore
                                // Try to get the newly created snapshot
                                let! newSnapshot =
                                    TickerSnapshotExtensions.Do.getLatestByTickerId ticker.Id |> Async.AwaitTask

                                match newSnapshot with
                                | Some dbSnapshot ->
                                    // Load currency snapshots for the new snapshot
                                    let! currencySnapshots =
                                        TickerCurrencySnapshotExtensions.Do.getAllByTickerSnapshotId dbSnapshot.Base.Id
                                        |> Async.AwaitTask

                                    let mainCurrency =
                                        match currencySnapshots |> List.tryFind (fun cs -> cs.CurrencyId = 141) with
                                        | Some cs ->
                                            { Id = cs.Base.Id
                                              Date = DateOnly.FromDateTime(cs.Base.Date.Value)
                                              Ticker = cs.TickerId.ToFastTickerById()
                                              Currency = cs.CurrencyId.ToFastCurrencyById()
                                              TotalShares = cs.TotalShares
                                              Weight = cs.Weight
                                              CostBasis = cs.CostBasis.Value
                                              RealCost = cs.RealCost.Value
                                              Dividends = cs.Dividends.Value
                                              Options = cs.Options.Value
                                              TotalIncomes = cs.TotalIncomes.Value
                                              Unrealized = cs.Unrealized.Value
                                              Realized = cs.Realized.Value
                                              Performance = cs.Performance
                                              LatestPrice = cs.LatestPrice.Value
                                              OpenTrades = cs.OpenTrades
                                              Commissions = cs.Commissions.Value
                                              Fees = cs.Fees.Value }
                                        | None ->
                                            { Id = 0
                                              Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                                              Ticker = dbSnapshot.TickerId.ToFastTickerById()
                                              Currency = "USD".ToFastCurrency()
                                              TotalShares = 0m
                                              Weight = 0.0m
                                              CostBasis = 0.0m
                                              RealCost = 0.0m
                                              Dividends = 0.0m
                                              Options = 0.0m
                                              TotalIncomes = 0.0m
                                              Unrealized = 0.0m
                                              Realized = 0.0m
                                              Performance = 0.0m
                                              LatestPrice = 0.0m
                                              OpenTrades = false
                                              Commissions = 0.0m
                                              Fees = 0.0m }

                                    let tickerSnapshot =
                                        { Id = dbSnapshot.Base.Id
                                          Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                                          Ticker = dbSnapshot.TickerId.ToFastTickerById()
                                          MainCurrency = mainCurrency
                                          OtherCurrencies = [] }

                                    return Some tickerSnapshot
                                | None ->
                                    // If we still can't get a snapshot, something is wrong
                                    failwithf
                                        "Failed to create or retrieve snapshot for ticker %s (ID: %d)"
                                        ticker.Symbol
                                        ticker.Id

                                    return None
                        })
                    |> Async.Parallel
                    |> Async.RunSynchronously
                    |> Array.choose id
                    |> Array.toList

                snapshots
                |> List.iter (fun newSnapshot ->
                    let tickerId = newSnapshot.Ticker.Id

                    let existingSnapshot =
                        Collections.TickerSnapshots.Items
                        |> Seq.tryFind (fun s -> s.Ticker.Id = tickerId)

                    match existingSnapshot with
                    | Some existing when existing <> newSnapshot ->
                        Collections.TickerSnapshots.Replace(existing, newSnapshot)
                    | None -> Collections.TickerSnapshots.Add(newSnapshot)
                    | Some _ -> () // Same snapshot, no action needed
                )
        }
