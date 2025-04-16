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
    }

    let loadBasicData() = task {
        do! getAllCurrencies() |> Async.AwaitTask |> Async.Ignore
        do! getAllBrokers() |> Async.AwaitTask |> Async.Ignore
        do! getAllBanks() |> Async.AwaitTask |> Async.Ignore
    }

    let initialization() = task {
        let! databaseBrokerAccounts = BrokerAccountExtensions.Do.getAll() |> Async.AwaitTask
        let! databaseBankAccounts = BankAccountExtensions.Do.getAll() |> Async.AwaitTask
        if databaseBrokerAccounts.IsEmpty && databaseBankAccounts.IsEmpty then
            let ibkr =
                {
                    Id = 1
                    Name = Keys.Broker_IBKR
                    Image = Keys.Broker_Image_IBKR
                    SupportedBroker = Keys.Broker_IBKR
                }
            let brokerAccount =
                {
                    Id = 1
                    Broker = ibkr
                    AccountNumber = "0123"
                }

            let tasty =
                {
                    Id = 2
                    Name = Keys.Broker_Tastytrade
                    Image = Keys.Broker_Image_Tastytrade
                    SupportedBroker = Keys.Broker_Tastytrade
                }

            let accountBroker = 
                {
                    Type = AccountType.BrokerAccount
                    Broker = Some brokerAccount
                    Bank = None
                }

            let tastyAccount =
                {
                    Id = 2
                    Broker = tasty
                    AccountNumber = "4567"
                }
            let accountTasty = 
                {
                    Type = AccountType.BrokerAccount
                    Broker = Some tastyAccount
                    Bank = None
                }

            let bank = 
                {
                    Id = 1
                    Name = "Wise"
                    Image = None
                }
            let bankAccount =
                {
                    Id = 1
                    Bank = bank
                    Name = "Wise"
                    Description = None
                    Currency = Collections.Currencies.Items |> Seq.head
                }

            let accountBank = 
                {
                    Type = AccountType.BankAccount
                    Broker = None
                    Bank = Some bankAccount
                }
            Collections.Accounts.Add(accountBroker)
            Collections.Accounts.Add(accountTasty)
            Collections.Accounts.Add({ Type = AccountType.EmptyAccount; Broker = None; Bank = None; })
    }

    let loadMovementsFor(account: Account) = task {
        Collections.Movements.Clear()
        return ()
    }