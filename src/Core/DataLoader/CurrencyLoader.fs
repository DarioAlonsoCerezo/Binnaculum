namespace Binnaculum.Core.DataLoader

open Binnaculum.Core.UI
open DynamicData
open Binnaculum.Core.DatabaseToModels

module internal CurrencyLoader =
    let load() = task {
        do! CurrencyExtensions.Do.insertDefaultValues() |> Async.AwaitTask 
        let! databaseCurrencies = CurrencyExtensions.Do.getAll() |> Async.AwaitTask
        Collections.Currencies.EditDiff(databaseCurrencies.currenciesToModel())
    }