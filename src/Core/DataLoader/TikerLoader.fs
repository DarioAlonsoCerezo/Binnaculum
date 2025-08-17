namespace Binnaculum.Core.DataLoader

open Binnaculum.Core.UI
open DynamicData
open Binnaculum.Core.DatabaseToModels

module internal TikerLoader = 
   let load() = task {
        do! TickerExtensions.Do.insertIfNotExists() |> Async.AwaitTask
        let! databaseTickers = TickerExtensions.Do.getAll() |> Async.AwaitTask
        let tickers = databaseTickers.tickersToModel()
        Collections.Tickers.EditDiff tickers
    }