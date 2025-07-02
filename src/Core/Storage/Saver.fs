namespace Binnaculum.Core.Storage

open Binnaculum.Core.Storage.ModelsToDatabase
open Binnaculum.Core.Storage.DatabaseToModels
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.UI
open Binnaculum.Core.Models
open BankExtensions
open BankAccountExtensions
open DynamicData
open System

/// <summary>
/// This module serves as the central handler for saving entities to the database
/// and managing their corresponding in-memory collection updates.
/// It ensures consistency between database operations and UI state management.
/// </summary>
module internal Saver =
    
    /// <summary>
    /// Refreshes the entire Banks collection by loading all banks from the database.
    /// This is used when adding new banks to ensure the UI reflects the latest state.
    /// </summary>
    let private refreshAllBanks() = task {
        let! databaseBanks = BankExtensions.Do.getAll() |> Async.AwaitTask
        let banks = databaseBanks.banksToModel()
                
        Collections.Banks.EditDiff banks            

        //As we allow users create banks, we add this default bank to recognize it in the UI
        Collections.Banks.Add({ Id = -1; Name = "AccountCreator_Create_Bank"; Image = Some "bank"; CreatedAt = DateTime.Now; })
    }
    
    /// <summary>
    /// Refreshes a specific bank and its associated bank accounts in the collections.
    /// This is used when updating existing banks to ensure the UI reflects the changes.
    /// </summary>
    let private refreshSpecificBank(bankId) = task {
        let! databaseBank = BankExtensions.Do.getById bankId |> Async.AwaitTask
        match databaseBank with
        | None -> return()
        | Some b -> Collections.updateBank(b.bankToModel())
            
        let! databaseBankAccounts = BankAccountExtensions.Do.getAll() |> Async.AwaitTask
        
        let accounts = 
            databaseBankAccounts.bankAccountsToModel() 
            |> List.filter (fun b -> b.Bank.Id = bankId)
            |> List.map (fun account -> 
                { 
                    Type = AccountType.BankAccount; 
                    Broker = None; 
                    Bank = Some account;
                    HasMovements = Collections.Movements.Items
                        |> Seq.filter (fun m -> m.BankAccountMovement.IsSome)
                        |> Seq.exists (fun m -> m.BankAccountMovement.Value.BankAccount.Id = account.Id)
                })
        accounts
        |> List.iter (fun account -> Collections.updateBankAccount account)
        return()
    }
    
    /// <summary>
    /// Saves a Bank entity to the database and updates the corresponding collections.
    /// - If the Bank is new (Id = 0), after saving, it refreshes the entire Banks collection
    /// - If the Bank is being updated, it refreshes the specific bank and its associated accounts
    /// </summary>
    let saveBank(bank: Binnaculum.Core.Models.Bank) = task {
        let! databaseBank = bank.bankToDatabase() |> Async.AwaitTask
        do! databaseBank.save() |> Async.AwaitTask |> Async.Ignore
        if bank.Id = 0 then
            do! refreshAllBanks() |> Async.AwaitTask |> Async.Ignore
        else
            do! refreshSpecificBank(bank.Id) |> Async.AwaitTask |> Async.Ignore        
    }