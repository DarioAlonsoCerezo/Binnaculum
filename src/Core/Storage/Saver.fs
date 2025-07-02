namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.DatabaseModel
open BankExtensions

/// <summary>
/// This module serves as the central handler for saving entities to the database
/// and managing their corresponding in-memory collection updates.
/// It ensures consistency between database operations and UI state management.
/// </summary>
module internal Saver =
    
    /// <summary>
    /// Saves a Bank entity to the database and updates the corresponding collections.
    /// - If the Bank is new (Id = 0), after saving, it loads the newly added bank from the database
    /// - If the Bank is being updated, it refreshes the specific bank and its associated accounts
    /// </summary>
    let saveBank(bank: Binnaculum.Core.Database.DatabaseModel.Bank) = task {
        do! bank.save() |> Async.AwaitTask |> Async.Ignore
        if bank.Id = 0 then
            do! DataLoader.loadAddedBank() |> Async.AwaitTask |> Async.Ignore
        else
            do! DataLoader.refreshSpecificBank(bank.Id) |> Async.AwaitTask |> Async.Ignore        
    }