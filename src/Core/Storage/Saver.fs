namespace Binnaculum.Core.Storage

open Binnaculum.Core.Storage.ModelsToDatabase
open Binnaculum.Core.Database.DatabaseModel
open BankExtensions
open System

/// <summary>
/// This module serves as the central handler for saving entities to the database
/// and managing their corresponding in-memory collection updates.
/// It ensures consistency between database operations and UI state management.
/// </summary>
module internal Saver =
    
    /// <summary>
    /// Saves a Bank entity to the database and updates the corresponding collections.
    /// - If the Bank is new (Id = 0), after saving, it refreshes the entire Banks collection
    /// - If the Bank is being updated, it performs the same logic as refreshBankAccount
    /// </summary>
    let saveBank(bank: Binnaculum.Core.Models.Bank) = task {
        let! databaseBank = bank.bankToDatabase() |> Async.AwaitTask
        do! databaseBank.save() |> Async.AwaitTask |> Async.Ignore
        if bank.Id = 0 then
            do! DataLoader.getOrRefreshBanks() |> Async.AwaitTask |> Async.Ignore
        else
            do! DataLoader.refreshBankAccount(bank.Id) |> Async.AwaitTask |> Async.Ignore        
    }