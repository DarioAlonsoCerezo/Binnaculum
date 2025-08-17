namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.DatabaseModel
open BankExtensions
open BankAccountExtensions
open BrokerExtensions
open BrokerAccountExtensions
open BrokerMovementExtensions
open BankAccountBalanceExtensions
open TickerExtensions
open TradeExtensions
open DividendExtensions
open DividendDateExtensions
open DividendTaxExtensions
open OptionTradeExtensions

/// <summary>
/// This module serves as the central handler for saving entities to the database
/// and managing their corresponding in-memory collection updates.
/// It ensures consistency between database operations and UI state management.
/// Note: Snapshot updates are handled by the Creator module for user-initiated actions.
/// </summary>
module internal Saver =
    
    /// <summary>
    /// Saves a Bank entity to the database and updates the corresponding collections.
    /// - If the Bank is new (Id = 0), after saving, it loads the newly added bank from the database
    /// - If the Bank is being updated, it refreshes the specific bank and its associated accounts
    /// </summary>
    let saveBank(bank: Bank) = task {
        do! bank.save() |> Async.AwaitTask |> Async.Ignore
        if bank.Id = 0 then
            do! DataLoader.loadAddedBank() |> Async.AwaitTask |> Async.Ignore
        else
            do! DataLoader.refreshSpecificBank(bank.Id) |> Async.AwaitTask |> Async.Ignore        
    }
    
    /// <summary>
    /// Saves a Broker entity to the database and updates the corresponding collections.
    /// - If the Broker is new (Id = 0), after saving, it loads the newly added broker from the database
    /// - If the Broker is being updated, it refreshes the specific broker
    /// </summary>
    let saveBroker(broker: Broker) = task {
        do! broker.save() |> Async.AwaitTask |> Async.Ignore
        if broker.Id = 0 then
            do! DataLoader.loadAddedBroker() |> Async.AwaitTask |> Async.Ignore
        else
            do! DataLoader.refreshSpecificBroker(broker.Id) |> Async.AwaitTask |> Async.Ignore        
    }
    
    /// <summary>
    /// Saves a BankAccount entity to the database and updates the corresponding collections.
    /// - After saving, it refreshes all accounts to ensure UI consistency
    /// </summary>
    let saveBankAccount(bankAccount: BankAccount) = task {
        do! bankAccount.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.getOrRefreshAllAccounts() |> Async.AwaitTask |> Async.Ignore
    }

    /// <summary>
    /// Saves a BrokerAccount entity to the database and updates the corresponding collections.
    /// - After saving, it refreshes all accounts to ensure UI consistency
    /// </summary>
    let saveBrokerAccount(brokerAccount: BrokerAccount) = task {
        do! brokerAccount.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.getOrRefreshAllAccounts() |> Async.AwaitTask |> Async.Ignore
    }

    /// <summary>
    /// Saves a BrokerMovement entity to the database and updates the corresponding collections.
    /// - After saving, it refreshes all movements to ensure UI consistency
    /// </summary>
    let saveBrokerMovement(brokerMovement: BrokerMovement) = task {
        do! brokerMovement.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.loadMovementsFor() |> Async.AwaitTask |> Async.Ignore
    }

    /// <summary>
    /// Saves a BankAccountMovement entity to the database and updates the corresponding collections.
    /// - After saving, it refreshes all movements to ensure UI consistency
    /// </summary>
    let saveBankMovement(bankMovement: BankAccountMovement) = task {
        do! bankMovement.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.loadMovementsFor() |> Async.AwaitTask |> Async.Ignore
    }

    /// <summary>
    /// Saves a Ticker entity to the database and updates the corresponding collections.
    /// - After saving, it refreshes the tickers collection to ensure UI consistency
    /// </summary>
    let saveTicker(ticker: Ticker) = task {
        do! ticker.save() |> Async.AwaitTask |> Async.Ignore
        do! Binnaculum.Core.DataLoader.TikerLoader.load() |> Async.AwaitTask |> Async.Ignore
    }

    /// <summary>
    /// Saves a Trade entity to the database and updates the corresponding collections.
    /// - After saving, it refreshes all movements to ensure UI consistency
    /// </summary>
    let saveTrade(trade: Trade) = task {
        do! trade.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.loadMovementsFor() |> Async.AwaitTask |> Async.Ignore
    }

    /// <summary>
    /// Saves a Dividend entity to the database and updates the corresponding collections.
    /// - After saving, it refreshes all movements to ensure UI consistency
    /// </summary>
    let saveDividend(dividend: Dividend) = task {
        do! dividend.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.loadMovementsFor() |> Async.AwaitTask |> Async.Ignore
    }

    /// <summary>
    /// Saves a DividendDate entity to the database and updates the corresponding collections.
    /// - After saving, it refreshes all movements to ensure UI consistency
    /// </summary>
    let saveDividendDate(dividendDate: DividendDate) = task {
        do! dividendDate.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.loadMovementsFor() |> Async.AwaitTask |> Async.Ignore
    }

    /// <summary>
    /// Saves a DividendTax entity to the database and updates the corresponding collections.
    /// - After saving, it refreshes all movements to ensure UI consistency
    /// </summary>
    let saveDividendTax(dividendTax: DividendTax) = task {
        do! dividendTax.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.loadMovementsFor() |> Async.AwaitTask |> Async.Ignore
    }

    /// <summary>
    /// Saves a list of OptionTrade entities to the database and updates the corresponding collections.
    /// - Saves all option trades in parallel for efficiency
    /// - After saving, it refreshes all movements to ensure UI consistency
    /// </summary>
    let saveOptionsTrade(optionTrades: OptionTrade list) = task {
        do! optionTrades
            |> List.map (fun model -> 
                let endOfDay = model.ExpirationDate.WithEndOfDay()
                let updated = { model with ExpirationDate = endOfDay }
                // Save each option trade asynchronously
                updated.save() |> Async.AwaitTask |> Async.Ignore)
            |> Async.Parallel
            |> Async.Ignore
        
        // Refresh the movements collection to reflect the new option trades
        do! DataLoader.loadMovementsFor() |> Async.AwaitTask |> Async.Ignore
    }