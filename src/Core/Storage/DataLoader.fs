namespace Binnacle.Core.Storage

open ModelParser
open Binnaculum.Core
open Binnaculum.Core.UI
open Binnaculum.Core.Models
open DynamicData

/// <summary>
/// This module serves as a critical layer for managing the transformation and synchronization of data
/// between the database and the application's in-memory collections.
/// It ensures that the data is accurately represented and easily accessible for various operations.
/// </summary>
module internal DataLoader =
    let private getAllCurrencies() = task {
        let! databaseCurrencies = CurrencyExtensions.Do.getAll() |> Async.AwaitTask
        let currencies = databaseCurrencies |> List.map (fun c -> fromDatabaseCurrency c)
        Collections.Currencies.EditDiff currencies
    }

    let private getAllBrokers() = task {
        let! databaseBrokers = BrokerExtensions.Do.getAll() |> Async.AwaitTask
        let brokers = databaseBrokers |> List.map (fun b -> fromDatabaseBroker b)
        Collections.Brokers.EditDiff brokers
    }

    let private getAllBanks() = task {
        let! databaseBanks = BankExtensions.Do.getAll() |> Async.AwaitTask
        let banks = databaseBanks |> List.map (fun b -> fromDatabaseBank b)
                
        Collections.Banks.EditDiff banks            

        //As we allow users create banks, we add this default bank to recognize it in the UI
        Collections.Banks.Add({ Id = -1; Name = "AccountCreator_Create_Bank"; Image = Some "bank"; })
    }

    let private getBroker(name: string) =
        Collections.Brokers.Items |> Seq.find (fun b -> b.Name = name)

    let loadBasicData() = task {
        do! getAllCurrencies() |> Async.AwaitTask |> Async.Ignore
        do! getAllBrokers() |> Async.AwaitTask |> Async.Ignore
        do! getAllBanks() |> Async.AwaitTask |> Async.Ignore
    }

    let initialization() = task {
        let! databaseBrokerAccounts = BrokerAccountExtensions.Do.getAll() |> Async.AwaitTask
        let! databaseBankAccounts = BankAccountExtensions.Do.getAll() |> Async.AwaitTask
        if databaseBrokerAccounts.IsEmpty && databaseBankAccounts.IsEmpty then
            Collections.Accounts.Add({ Type = AccountType.EmptyAccount; Broker = None; Bank = None; })
    }

    let loadMovementsFor(account: Account) = task {
        Collections.Movements.Clear()
        return ()
    }