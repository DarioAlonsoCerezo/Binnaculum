namespace Binnacle.Core.Storage

open ModelParser
open Binnaculum.Core.UI
open Binnaculum.Core.Models
open DynamicData
open Microsoft.Maui.Storage
open System.IO

/// <summary>
/// This module serves as a critical layer for managing the transformation and synchronization of data
/// between the database and the application's in-memory collections.
/// It ensures that the data is accurately represented and easily accessible for various operations.
/// </summary>
module internal DataLoader =
    let private getAllCurrencies() = task {
        do! CurrencyExtensions.Do.insertDefaultValues() |> Async.AwaitTask 
        let! databaseCurrencies = CurrencyExtensions.Do.getAll() |> Async.AwaitTask
        let currencies = databaseCurrencies |> List.map (fun c -> fromDatabaseCurrency c)
        Collections.Currencies.EditDiff currencies
    }

    let getOrRefreshAllBrokers() = task {
        do! BrokerExtensions.Do.insertIfNotExists() |> Async.AwaitTask
        let! databaseBrokers = BrokerExtensions.Do.getAll() |> Async.AwaitTask
        let brokers = databaseBrokers |> List.map (fun b -> fromDatabaseBroker b)
        Collections.Brokers.EditDiff brokers

        //As we allow users create brokers, we add this default broker to recognize it in the UI
        Collections.Brokers.Add({ Id = -1; Name = "AccountCreator_Create_Broker"; Image = "broker"; SupportedBroker = "Unknown"; })
    }

    let getOrRefreshBanks() = task {
        let! databaseBanks = BankExtensions.Do.getAll() |> Async.AwaitTask
        let banks = databaseBanks |> List.map (fun b -> fromDatabaseBank b)
                
        Collections.Banks.EditDiff banks            

        //As we allow users create banks, we add this default bank to recognize it in the UI
        Collections.Banks.Add({ Id = -1; Name = "AccountCreator_Create_Bank"; Image = Some "bank"; })
    }

    let getOrRefresAvailableImages() = task {
        let directory = FileSystem.AppDataDirectory
    
        // Check if the directory exists
        if Directory.Exists(directory) then
            // Get all image files (common image extensions)
            let imageExtensions = [|".jpg"; ".jpeg"; ".png"|]
            let imagePaths = 
                Directory.GetFiles(directory)
                |> Array.filter (fun file -> 
                    let extension = Path.GetExtension(file).ToLowerInvariant()
                    Array.contains extension imageExtensions)
                |> Array.map (fun path ->
                    // Use cross-platform path format for consistency
                    path.Replace("\\", "/"))
                |> Array.toList
        
            // Update the AvailableImages collection with full paths
            Collections.AvailableImages.EditDiff imagePaths
        else
            // Create the directory if it doesn't exist
            Directory.CreateDirectory(directory) |> ignore
            Collections.AvailableImages.Clear()
    }

    let private getOrRefreshAllBrokerAccounts() = task {
        let! databaseBrokerAccounts = BrokerAccountExtensions.Do.getAll() |> Async.AwaitTask
        let brokerAccounts = 
            databaseBrokerAccounts 
            |> List.map (fun b -> fromDatabaseBrokerAccount b)
            |> List.map (fun account -> 
                { 
                    Type = AccountType.BrokerAccount; 
                    Broker = Some account; 
                    Bank = None;
                    HasMovements = Collections.Movements.Items
                        |> Seq.filter (fun m -> m.BrokerMovement.IsSome)
                        |> Seq.exists (fun m -> m.BrokerMovement.Value.BrokerAccount.Id = account.Id)
                })

        return brokerAccounts
    }

    let private getOrRefreshAllBankAccounts() = task {
        let! databaseBankAccounts = BankAccountExtensions.Do.getAll() |> Async.AwaitTask
        let bankAccounts = 
            databaseBankAccounts 
            |> List.map (fun b -> fromDatabaseBankAccount b)
            |> List.map (fun account -> 
                { 
                    Type = AccountType.BankAccount; 
                    Broker = None; 
                    Bank = Some account;
                    HasMovements = Collections.Movements.Items
                        |> Seq.filter (fun m -> m.BankAccountMovement.IsSome)
                        |> Seq.exists (fun m -> m.BankAccountMovement.Value.BankAccount.Id = account.Id)
                })

        return bankAccounts       
    }

    let private getAccountMovements(account: Account) = task {
        return []
    }

    let private getEmptyMovement() = 
        { 
            Type = AccountMovementType.EmptyMovement
            Trade = None
            Dividend = None
            DividendTax = None
            DividendDate = None
            OptionTrade = None
            BrokerMovement = None
            BankAccountMovement = None
            TickerSplit = None
        }

    let getOrRefreshAllAccounts() = task {
        let! brokerAccounts = getOrRefreshAllBrokerAccounts() |> Async.AwaitTask
        let! bankAccounts = getOrRefreshAllBankAccounts() |> Async.AwaitTask
        let allAccounts = brokerAccounts @ bankAccounts

        if allAccounts.IsEmpty then
            Collections.Accounts.Add({ Type = AccountType.EmptyAccount; Broker = None; Bank = None; HasMovements = false; })
        else
            Collections.Accounts.Clear()
            Collections.Accounts.EditDiff allAccounts
    }

    let loadBasicData() = task {
        do! getAllCurrencies() |> Async.AwaitTask |> Async.Ignore
        do! getOrRefreshAllBrokers() |> Async.AwaitTask |> Async.Ignore
        do! getOrRefreshBanks() |> Async.AwaitTask |> Async.Ignore
        do! getOrRefresAvailableImages() |> Async.AwaitTask |> Async.Ignore
    }

    let initialization() = task {
        do! getOrRefreshAllAccounts() |> Async.AwaitTask |> Async.Ignore
    }

    let loadMovementsFor(account: Account option) = task {
        // Clear the movements collection before loading new data
        Collections.Movements.Clear()
        // Check if the account is None, and if so, return early
        if account.IsNone then
            Collections.Movements.EditDiff [getEmptyMovement()]
        else 
            // Load movements for the specified account
            let! movements = getAccountMovements account.Value |> Async.AwaitTask
            if movements.IsEmpty then
                Collections.Movements.EditDiff [getEmptyMovement()]
            else
                Collections.Movements.EditDiff movements        
    }