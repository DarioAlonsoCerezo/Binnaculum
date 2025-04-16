namespace Binnaculum.Core.UI

open Binnaculum.Core
open Binnaculum.Core.Models
open DynamicData

/// <summary>
/// This module serves as a critical layer for managing the transformation and synchronization of data 
/// between the database models and the UI models. It is responsible for loading, creating, and modifying 
/// UI models, acting as a proxy to ensure the UI remains decoupled from the database structure. 
/// As the application evolves, this module will expand to support additional features, providing a 
/// centralized and scalable approach to handling UI data.
/// </summary>
module internal ModelUI =
    let toModelSupportedBroker(databaseSupportedBroker: Database.DatabaseModel.SupportedBroker) =
        match databaseSupportedBroker with
        | Database.DatabaseModel.SupportedBroker.IBKR -> Keys.Broker_IBKR
        | Database.DatabaseModel.SupportedBroker.Tastytrade -> Keys.Broker_Tastytrade

    let private loadCurrencies() = task {
        let! databaseCurrencies = CurrencyExtensions.Do.getAll() |> Async.AwaitTask
        let currencies =
            databaseCurrencies 
            |> List.map (fun c -> 
            { 
                Id = c.Id; 
                Title = c.Name; 
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

    let private loadBanks() = task {
        let! databaseBanks = BankExtensions.Do.getAll() |> Async.AwaitTask
        if databaseBanks.IsEmpty then
            return ()
        let banks =
            databaseBanks 
            |> List.map (fun b -> 
            { 
                Id = b.Id; 
                Name = b.Name; 
                Image = b.Image; 
            })

        Collections.Banks.EditDiff banks
    }

    let initialize(overview: OverviewUI) = task {
        do! Database.Do.init() |> Async.AwaitTask |> Async.Ignore
        do! BrokerExtensions.Do.insertIfNotExists() |> Async.AwaitTask |> Async.Ignore
        do! CurrencyExtensions.Do.insertDefaultValues() |> Async.AwaitTask |> Async.Ignore
        do! loadCurrencies() |> Async.AwaitTask |> Async.Ignore
        do! loadBrokers() |> Async.AwaitTask |> Async.Ignore
        do! loadBanks() |> Async.AwaitTask |> Async.Ignore
        return { overview with IsDatabaseInitialized = true; }
    }

    let loadData(overview: OverviewUI) = task {
        let! databaseBrokerAccounts = BrokerAccountExtensions.Do.getAll() |> Async.AwaitTask
        let! databaseBankAccounts = BankAccountExtensions.Do.getAll() |> Async.AwaitTask

        if databaseBrokerAccounts.IsEmpty && databaseBankAccounts.IsEmpty then
            Collections.OverviewAccounts.Add(Account.EmptyAccount "")
            Collections.OverviewMovements.Add(Movement.EmptyMovement "")

        return { overview with TransactionsLoaded = true; }
    }