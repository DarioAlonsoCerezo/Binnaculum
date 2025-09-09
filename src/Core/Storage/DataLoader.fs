namespace Binnaculum.Core.Storage

open Binnaculum.Core.UI
open Binnaculum.Core.Models
open DynamicData
open Microsoft.Maui.Storage
open System.IO
open System
open Binnaculum.Core.DatabaseToModels
open Binnaculum.Core
open Binnaculum.Core.Patterns

/// <summary>
/// This module serves as a critical layer for managing the transformation and synchronization of data
/// between the database and the application's in-memory collections.
/// It ensures that the data is accurately represented and easily accessible for various operations.
/// </summary>
module internal DataLoader =
    
    /// <summary>
    /// Retrieves the latest saved bank from the database and adds it to the Banks collection.
    /// This is used when adding new banks to ensure the UI reflects the newly created bank.
    /// </summary>
    let internal loadAddedBank() = task {
        // Get the latest bank (the one with the highest ID, which was just saved)
        let! databaseBank = BankExtensions.Do.getLatest() |> Async.AwaitTask
        
        match databaseBank with
        | Some bank -> 
            let modelBank = bank.bankToModel()
            Collections.Banks.Add(modelBank)
        | None -> ()

        //As we allow users create banks, we add this default bank to recognize it in the UI (if not already present)
        let hasDefaultBank = Collections.Banks.Items |> Seq.exists (fun b -> b.Id = -1)
        if not hasDefaultBank then
            Collections.Banks.Add({ Id = -1; Name = "AccountCreator_Create_Bank"; Image = Some "bank"; CreatedAt = DateTime.Now; })
    }
    
    /// <summary>
    /// Retrieves the latest saved broker from the database and adds it to the Brokers collection.
    /// This is used when adding new brokers to ensure the UI reflects the newly created broker.
    /// </summary>
    let internal loadAddedBroker() = task {
        // Get the latest broker (the one with the highest ID, which was just saved)
        let! databaseBroker = BrokerExtensions.Do.getLatest() |> Async.AwaitTask
        
        match databaseBroker with
        | Some broker -> 
            let modelBroker = broker.brokerToModel()
            Collections.Brokers.Add(modelBroker)
        | None -> ()

        //As we allow users create brokers, we add this default broker to recognize it in the UI (if not already present)
        let hasDefaultBroker = Collections.Brokers.Items |> Seq.exists (fun b -> b.Id = -1)
        if not hasDefaultBroker then
            Collections.Brokers.Add({ Id = -1; Name = "AccountCreator_Create_Broker"; Image = "broker"; SupportedBroker = "Unknown"; })
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

    let private loadBrokerAccounts() = task {
        let! databaseBrokerAccounts = BrokerAccountExtensions.Do.getAll() |> Async.AwaitTask
        let brokerAccounts = 
            databaseBrokerAccounts 
            |> fun b -> b.brokerAccountsToModel()
            |> List.map (fun account -> 
                async {
                    // Use the database query to check if the account has any movements, including option trades
                    let! hasMovements = 
                        BrokerAccountExtensions.Do.hasMovements account.Id 
                        |> Async.AwaitTask
                    return { 
                        Type = AccountType.BrokerAccount; 
                        Broker = Some account; 
                        Bank = None;
                        HasMovements = hasMovements
                    }
                }) 
        let accounts = brokerAccounts |> Async.Parallel |> Async.RunSynchronously

        return accounts |> Array.toList
    }

    let private loadBankAccounts() = task {
        let! databaseBankAccounts = BankAccountExtensions.Do.getAll() |> Async.AwaitTask
        let bankAccounts = 
            databaseBankAccounts.bankAccountsToModel() 
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

    let getOrRefreshAllAccounts() = task {
        let! brokerAccounts = loadBrokerAccounts() |> Async.AwaitTask
        let! bankAccounts = loadBankAccounts() |> Async.AwaitTask
        let allAccounts = brokerAccounts @ bankAccounts

        if allAccounts.IsEmpty then
            Collections.Accounts.Add({ Type = AccountType.EmptyAccount; Broker = None; Bank = None; HasMovements = false; })
        else
            Collections.Accounts.Clear()
            Collections.Accounts.EditDiff allAccounts
    }

    /// <summary>
    /// [DEPRECATED] Manual snapshot loading method - use ReactiveSnapshotManager instead.
    /// This method will be removed in a future version. Use ReactiveApplicationManager.initializeReactiveApplication() 
    /// or ReactiveSnapshotManager.initialize() + refresh() for automatic reactive snapshot updates.
    /// 
    /// TRANSITION STATUS: 4 usages remaining to convert:
    /// - DataLoader.fs initialization() 
    /// - Saver.fs (line 77)
    /// - Overview.fs (line 24)
    /// - Creator.fs (line 71)
    /// </summary>
    let loadOverviewSnapshots() = task {
        do! DataLoader.BrokerSnapshotLoader.load() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.BankSnapshotLoader.load() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.BrokerAccountSnapshotLoader.load() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.BankAccountSnapshotLoader.load() |> Async.AwaitTask |> Async.Ignore

        if Collections.Snapshots.Items.Count = 0 then
            // If no snapshots are available, add an empty snapshot to the collection
            Collections.Snapshots.Add(DatabaseToModels.Do.createEmptyOverviewSnapshot())
    }

    let loadBasicData() = task {
        do! DataLoader.CurrencyLoader.load() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.BrokerLoader.load() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.BankLoader.load() |> Async.AwaitTask |> Async.Ignore
        do! getOrRefresAvailableImages() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.TikerLoader.load() |> Async.AwaitTask |> Async.Ignore
    }

    let initialization() = task {
        do! getOrRefreshAllAccounts() |> Async.AwaitTask |> Async.Ignore
        // Use reactive snapshot manager instead of manual loading
        ReactiveSnapshotManager.refresh()
        do! DataLoader.TickerSnapshotLoader.load() |> Async.AwaitTask |> Async.Ignore
    }

    let private updateBankAccount(account: Account, movements: Movement list) = 
        let hasMovements = 
            movements 
            |> List.filter (fun m -> m.BankAccountMovement.IsSome && m.BankAccountMovement.Value.BankAccount.Id = account.Bank.Value.Id)
            |> List.length > 0
        if hasMovements <> account.HasMovements then
            let updatedAccount = { account with HasMovements = hasMovements }
            // Update the account directly in the collection instead of using updateBankAccount
            let current = Collections.Accounts.Items |> Seq.find(fun a -> a.Bank.IsSome && a.Bank.Value.Id = account.Bank.Value.Id)
            Collections.Accounts.Replace(current, updatedAccount)

    let private updateBrokerAccount(account: Account, movements: Movement list) = 
        // Use the database query to directly check if the account has any movements
        async {
            let! hasMovements = 
                BrokerAccountExtensions.Do.hasMovements account.Broker.Value.Id
                |> Async.AwaitTask
            
            if hasMovements <> account.HasMovements then
                let updatedAccount = { account with HasMovements = hasMovements }
                Collections.updateBrokerAccount updatedAccount
        } |> Async.StartImmediate

    let loadMovementsFor() = task {

        // PERFORMANCE NOTE: This method is now primarily used by ReactiveMovementManager
        // for reactive updates when base collections change, which is much more efficient
        // than the previous approach of manual calls after every save operation.
        // 
        // PREVIOUS APPROACH: Manual calls after every save (8+ locations in Saver.fs)
        // CURRENT APPROACH: Reactive loading only when collections actually change
        //
        // For account-specific movement queries, use database extensions with filtering:
        // - BrokerMovementExtensions.Do.getByBrokerAccountIdFromDate(accountId, date)
        // - TradeExtensions.Do.getByBrokerAccountAndCurrency(accountId, currencyId)
        // - DividendExtensions.Do.getByBrokerAccountIdFromDate(accountId, date)
        // These provide O(1) database-level filtering for large datasets.
        let! databaseBrokerMovements = BrokerMovementExtensions.Do.getAll()
        let! databaseBankMovements = BankAccountBalanceExtensions.Do.getAll()
        let! databaseTrades = TradeExtensions.Do.getAll() |> Async.AwaitTask
        let! databaseDividends = DividendExtensions.Do.getAll() |> Async.AwaitTask
        let! databaseDividendDates = DividendDateExtensions.Do.getAll() |> Async.AwaitTask
        let! databaseDividendTaxes = DividendTaxExtensions.Do.getAll() |> Async.AwaitTask
        let! databaseOptions = OptionTradeExtensions.Do.getAll() |> Async.AwaitTask

        let brokerMovements = databaseBrokerMovements.brokerMovementsToModel()
        let bankMovements = databaseBankMovements.bankAccountMovementsToMovements()
        let tradeMovements =  databaseTrades.tradesToMovements()
        let dividendMovements = databaseDividends.dividendsReceivedToMovements()
        let dividendDates = databaseDividendDates.dividendDatesToMovements()
        let dividendTaxes = databaseDividendTaxes.dividendTaxesToMovements()       
        let optionTrades = databaseOptions.optionTradesToMovements()
        
        let movements = 
            brokerMovements 
            @ bankMovements 
            @ tradeMovements
            @ dividendMovements
            @ dividendDates
            @ dividendTaxes
            @ optionTrades
        
        Collections.Movements.EditDiff movements

        //Here we check if accounts have movements
        Collections.Accounts.Items
            |> Seq.iter (fun account ->
                if account.Bank.IsSome then
                    (account, movements) 
                    |> updateBankAccount

                if account.Broker.IsSome then
                    (account, movements) 
                    |> updateBrokerAccount
                )
    }

    /// <summary>
    /// Optimized method for loading movements for a specific broker account using database-level filtering.
    /// This addresses the performance concern in the original loadMovementsFor TODO by providing
    /// efficient account-specific queries instead of loading all movements.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID to filter movements for</param>
    /// <param name="startDate">Optional start date for filtering (if None, loads all historical movements)</param>
    let loadMovementsForBrokerAccount(brokerAccountId: int, startDate: DateTimePattern option) = task {
        let dateFilter = startDate |> Option.defaultValue (DateTimePattern.FromDateTime(DateTime.MinValue))
        
        // Use database-level filtering for optimal performance
        let! databaseBrokerMovements = BrokerMovementExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, dateFilter)
        let! databaseTrades = TradeExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, dateFilter)
        let! databaseDividends = DividendExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, dateFilter)
        // Note: DividendDateExtensions doesn't have filtering by account, so we get all and filter
        let! allDividendDates = DividendDateExtensions.Do.getAll() |> Async.AwaitTask
        let! databaseDividendTaxes = DividendTaxExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, dateFilter)
        let! databaseOptions = OptionTradeExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, dateFilter)

        // Convert using existing extension methods
        let brokerMovements = databaseBrokerMovements.brokerMovementsToModel()
        let tradeMovements = databaseTrades.tradesToMovements()
        let dividendMovements = databaseDividends.dividendsReceivedToMovements()
        let allDividendDatesConverted = allDividendDates.dividendDatesToMovements()
        // Filter dividend dates by broker account after conversion
        let dividendDates = allDividendDatesConverted |> List.filter (fun m -> 
            m.DividendDate.IsSome && m.DividendDate.Value.BrokerAccount.Id = brokerAccountId)
        let dividendTaxes = databaseDividendTaxes.dividendTaxesToMovements()
        let optionTrades = databaseOptions.optionTradesToMovements()
        
        let accountMovements = 
            brokerMovements 
            @ tradeMovements
            @ dividendMovements
            @ dividendDates
            @ dividendTaxes
            @ optionTrades

        return accountMovements
    }

    let internal changeOptionsGrouped() = task {
        // Remove all option trades from current movements
        let currentMovements = Collections.Movements.Items |> Seq.toList
        let movementsWithoutOptions = 
            currentMovements 
            |> List.filter (fun movement -> movement.OptionTrade.IsNone)
        
        // Get the latest options from the database
        let! databaseOptions = OptionTradeExtensions.Do.getAll() |> Async.AwaitTask
        
        // Convert to movements based on current GroupOptions setting
        let optionTrades = databaseOptions.optionTradesToMovements()
        
        // Combine non-option movements with the new option movements
        let updatedMovements = movementsWithoutOptions @ optionTrades
        
        // Update the movements collection
        Collections.Movements.EditDiff updatedMovements
        
        // Update accounts movement status
        Collections.Accounts.Items
            |> Seq.iter (fun account ->
                if account.Broker.IsSome then
                    (account, updatedMovements) 
                    |> updateBrokerAccount
            )
    }