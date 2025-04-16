namespace Binnacle.Core.Storage

open ModelParser
open Binnaculum.Core.UI
open DynamicData

/// <summary>
/// This module serves as a critical layer for managing the transformation and synchronization of data
/// between the database and the application's in-memory collections.
/// It ensures that the data is accurately represented and easily accessible for various operations.
/// </summary>
module internal DataLoader =
    let getAllCurrencies() = task {
        let! databaseCurrencies = CurrencyExtensions.Do.getAll() |> Async.AwaitTask
        let currencies = databaseCurrencies |> List.map (fun c -> fromDatabaseCurrency c)
        Collections.Currencies.EditDiff currencies
    }

    let getAllBrokers() = task {
        let! databaseBrokers = BrokerExtensions.Do.getAll() |> Async.AwaitTask
        let brokers = databaseBrokers |> List.map (fun b -> fromDatabaseBroker b)
        Collections.Brokers.EditDiff brokers
    }