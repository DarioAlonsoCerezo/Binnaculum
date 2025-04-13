namespace Binnaculum.Core.UI

open Binnaculum.Core
open Binnaculum.Core.Models
open DynamicData

module internal ModelUI =
    let toModelSupportedBroker(databaseSupportedBroker: Database.DatabaseModel.SupportedBroker) =
        match databaseSupportedBroker with
        | Database.DatabaseModel.SupportedBroker.IBKR -> SupportedBroker.IBKR
        | Database.DatabaseModel.SupportedBroker.Tastytrade -> SupportedBroker.Tastytrade

    let private loadCurrencies() = task {
        let! databaseCurrencies = CurrencyExtensions.Do.getAll() |> Async.AwaitTask
        let currencies =
            databaseCurrencies 
            |> List.map (fun c -> 
            { 
                Id = c.Id; 
                Name = c.Name; 
                Code = c.Code;
                Symbol = c.Symbol 
            })
        Collections.Currencies.EditDiff currencies
    }

    let private loadBrokers() = task {
        let! databaseBrokers = BrokerExtensions.Do.getAll() |> Async.AwaitTask
        let brokers =
            databaseBrokers 
            |> List.map (fun b -> 
            { 
                Id = b.Id; 
                Name = b.Name; 
                Image = b.Image; 
                SupportedBroker = toModelSupportedBroker b.SupportedBroker
            })

        Collections.Brokers.EditDiff brokers
    }

    let initialize() = task {
        do! Database.Do.init() |> Async.AwaitTask |> Async.Ignore
        do! BrokerExtensions.Do.insertIfNotExists() |> Async.AwaitTask |> Async.Ignore
        do! CurrencyExtensions.Do.insertDefaultValues() |> Async.AwaitTask |> Async.Ignore
        do! loadCurrencies() |> Async.AwaitTask |> Async.Ignore
        do! loadBrokers() |> Async.AwaitTask |> Async.Ignore
    }