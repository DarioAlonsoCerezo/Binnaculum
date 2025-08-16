namespace Binnaculum.Core.Storage

open Binnaculum.Core.UI
open Binnaculum.Core.Models
open DynamicData
open Microsoft.Maui.Storage
open System.IO
open System
open Binnaculum.Core.DatabaseToModels
open Binnaculum.Core

/// <summary>
/// This module serves as a critical layer for managing the transformation and synchronization of data
/// between the database and the application's in-memory collections.
/// It ensures that the data is accurately represented and easily accessible for various operations.
/// </summary>
module internal DataLoader =
    
    /// <summary>
    /// Manages the addition of snapshots to Collections.Snapshots ensuring that:
    /// 1. At most one Empty OverviewSnapshot exists at any time
    /// 2. If any non-Empty OverviewSnapshot is present, no Empty snapshots exist
    /// </summary>
    let private addSnapshotWithEmptyManagement (newSnapshot: OverviewSnapshot) =
        if newSnapshot.Type = OverviewSnapshotType.Empty then
            // For empty snapshots, only add if:
            // 1. No empty snapshots already exist, AND
            // 2. No non-empty snapshots exist
            let existingEmptySnapshots = Collections.Snapshots.Items |> Seq.filter (fun s -> s.Type = OverviewSnapshotType.Empty) |> Seq.toList
            let existingNonEmptySnapshots = Collections.Snapshots.Items |> Seq.filter (fun s -> s.Type <> OverviewSnapshotType.Empty) |> Seq.toList
            if existingEmptySnapshots.IsEmpty && existingNonEmptySnapshots.IsEmpty then
                Collections.Snapshots.Add(newSnapshot)
        else
            // For non-empty snapshots, first remove any existing empty snapshots
            let emptySnapshots = Collections.Snapshots.Items |> Seq.filter (fun s -> s.Type = OverviewSnapshotType.Empty) |> Seq.toList
            emptySnapshots |> List.iter (Collections.Snapshots.Remove >> ignore)
            
            // Then add the new non-empty snapshot
            Collections.Snapshots.Add(newSnapshot)
    
    /// <summary>
    /// Ensures empty snapshots are removed when adding non-empty snapshots
    /// </summary>
    let private addNonEmptySnapshotWithEmptyCleanup (newSnapshot: OverviewSnapshot) =
        // Remove any existing empty snapshots before adding non-empty snapshot
        let emptySnapshots = Collections.Snapshots.Items |> Seq.filter (fun s -> s.Type = OverviewSnapshotType.Empty) |> Seq.toList
        emptySnapshots |> List.iter (Collections.Snapshots.Remove >> ignore)
        
        // Add the new non-empty snapshot
        Collections.Snapshots.Add(newSnapshot)

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
    
    /// <summary>
    /// Refreshes a specific broker in the collections.
    /// This is used when updating existing brokers to ensure the UI reflects the changes.
    /// </summary>
    let internal refreshSpecificBroker(brokerId) = task {
        let! databaseBroker = BrokerExtensions.Do.getById brokerId |> Async.AwaitTask
        match databaseBroker with
        | None -> return()
        | Some b -> Collections.updateBroker(b.brokerToModel())
    }
    
    /// <summary>
    /// Refreshes a specific bank and its associated bank accounts in the collections.
    /// This is used when updating existing banks to ensure the UI reflects the changes.
    /// </summary>
    let internal refreshSpecificBank(bankId) = task {
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

    let loadTickers() = task {
        do! TickerExtensions.Do.insertIfNotExists() |> Async.AwaitTask
        let! databaseTickers = TickerExtensions.Do.getAll() |> Async.AwaitTask
        let tickers = databaseTickers.tickersToModel()
        Collections.Tickers.EditDiff tickers
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

    let private loadLatestBrokerSnapshots() = task {
        let brokers = Collections.Brokers.Items
        
        // Get brokers with valid IDs (exclude default "-1" broker)
        let brokersWithSnapshots = 
            brokers
            |> Seq.filter (fun b -> b.Id > 0) // Exclude the default "-1" broker
            |> Seq.toList

        if brokersWithSnapshots.IsEmpty then
            return []
        else
            let snapshots = 
                brokersWithSnapshots
                |> Seq.map (fun broker ->
                    async {
                        try
                            let! latestSnapshot = BrokerSnapshotExtensions.Do.getLatestByBrokerId broker.Id |> Async.AwaitTask
                            match latestSnapshot with
                            | Some dbSnapshot ->
                                // Get financial snapshots for this specific broker using the latest date
                                let! brokerFinancialSnapshots = 
                                    BrokerFinancialSnapshotExtensions.Do.getLatestByBrokerIdGroupedByDate broker.Id 
                                    |> Async.AwaitTask
                                return Some (dbSnapshot.brokerSnapshotToOverviewSnapshot(broker, brokerFinancialSnapshots))
                            | None ->
                                return Some (DatabaseToModels.Do.createEmptyOverviewSnapshot())
                        with
                        | _ ->
                            return Some (DatabaseToModels.Do.createEmptyOverviewSnapshot())
                    })
                |> Async.Parallel
                |> Async.RunSynchronously
                |> Array.choose id
                |> Array.toList
            
            snapshots
            |> List.iter (fun newSnapshot ->
                if newSnapshot.Type = OverviewSnapshotType.Broker && newSnapshot.Broker.IsSome then
                    let brokerId = newSnapshot.Broker.Value.Broker.Id
                    let existingSnapshot = Collections.Snapshots.Items 
                                         |> Seq.tryFind (fun s -> s.Type = OverviewSnapshotType.Broker && s.Broker.IsSome && s.Broker.Value.Broker.Id = brokerId)
                    match existingSnapshot with
                    | Some existing when existing <> newSnapshot ->
                        Collections.Snapshots.Replace(existing, newSnapshot)
                    | None ->
                        addNonEmptySnapshotWithEmptyCleanup newSnapshot
                    | _ -> () // No change needed
            )
            
            return snapshots
    }

    let private loadLatestBankSnapshots() = task {
        let banks = Collections.Banks.Items
        let snapshots = 
            banks
            |> Seq.filter (fun b -> b.Id > 0) // Exclude the default "-1" bank
            |> Seq.map (fun bank ->
                async {
                    let! latestSnapshot = BankSnapshotExtensions.Do.getLatestByBankId bank.Id |> Async.AwaitTask
                    match latestSnapshot with
                    | Some dbSnapshot ->
                        return Some (dbSnapshot.bankSnapshotToOverviewSnapshot(bank))
                    | None ->
                        return Some (DatabaseToModels.Do.createEmptyOverviewSnapshot())
                })
            |> Async.Parallel
            |> Async.RunSynchronously
            |> Array.choose id
            |> Array.toList
        
        snapshots
        |> List.iter (fun newSnapshot ->
            if newSnapshot.Type = OverviewSnapshotType.Bank && newSnapshot.Bank.IsSome then
                let bankId = newSnapshot.Bank.Value.Bank.Id
                let existingSnapshot = Collections.Snapshots.Items 
                                     |> Seq.tryFind (fun s -> s.Type = OverviewSnapshotType.Bank && s.Bank.IsSome && s.Bank.Value.Bank.Id = bankId)
                match existingSnapshot with
                | Some existing when existing <> newSnapshot ->
                    Collections.Snapshots.Replace(existing, newSnapshot)
                | None ->
                    addNonEmptySnapshotWithEmptyCleanup(newSnapshot)
                | Some _ -> () // Same snapshot, no action needed
            else
                // For empty snapshots, use the helper function to manage properly
                addSnapshotWithEmptyManagement(newSnapshot)
        )
    }

    let private loadLatestBrokerAccountSnapshots() = task {
        let brokerAccounts = 
            Collections.Accounts.Items 
            |> Seq.filter (fun a -> a.Broker.IsSome)
            |> Seq.map (fun a -> a.Broker.Value)
            |> Seq.toList

        if brokerAccounts.IsEmpty then
            return ()
        else
            let snapshots = 
                brokerAccounts
                |> Seq.map (fun brokerAccount ->
                    async {
                        let! latestSnapshot = BrokerAccountSnapshotExtensions.Do.getLatestByBrokerAccountId brokerAccount.Id |> Async.AwaitTask
                        match latestSnapshot with
                        | Some dbSnapshot ->
                            return Some (dbSnapshot.brokerAccountSnapshotToOverviewSnapshot(brokerAccount))
                        | None ->
                            return Some (DatabaseToModels.Do.createEmptyOverviewSnapshot())
                    })
                |> Async.Parallel
                |> Async.RunSynchronously
                |> Array.choose id
                |> Array.toList
        
            snapshots
            |> List.iter (fun newSnapshot ->
                if newSnapshot.Type = OverviewSnapshotType.BrokerAccount && newSnapshot.BrokerAccount.IsSome then
                    let brokerAccountId = newSnapshot.BrokerAccount.Value.BrokerAccount.Id
                    let existingSnapshot = Collections.Snapshots.Items 
                                         |> Seq.tryFind (fun s -> s.Type = OverviewSnapshotType.BrokerAccount && s.BrokerAccount.IsSome && s.BrokerAccount.Value.BrokerAccount.Id = brokerAccountId)
                    match existingSnapshot with
                    | Some existing when existing <> newSnapshot ->
                        Collections.Snapshots.Replace(existing, newSnapshot)
                    | None ->
                        addNonEmptySnapshotWithEmptyCleanup(newSnapshot)
                    | Some _ -> () // Same snapshot, no action needed
                else
                    // For empty snapshots, use the helper function to manage properly
                    addSnapshotWithEmptyManagement(newSnapshot)
            )
    }

    let private loadLatestBankAccountSnapshots() = task {
        let bankAccounts = 
            Collections.Accounts.Items 
            |> Seq.filter (fun a -> a.Bank.IsSome)
            |> Seq.map (fun a -> a.Bank.Value)
        
        let snapshots = 
            bankAccounts
            |> Seq.map (fun bankAccount ->
                async {
                    let! latestSnapshot = BankAccountSnapshotExtensions.Do.getLatestByBankAccountId bankAccount.Id |> Async.AwaitTask
                    match latestSnapshot with
                    | Some dbSnapshot ->
                        return Some (dbSnapshot.bankAccountSnapshotToOverviewSnapshot(bankAccount))
                    | None ->
                        return Some (DatabaseToModels.Do.createEmptyOverviewSnapshot())
                })
            |> Async.Parallel
            |> Async.RunSynchronously
            |> Array.choose id
            |> Array.toList
        
        snapshots
        |> List.iter (fun newSnapshot ->
            if newSnapshot.Type = OverviewSnapshotType.BankAccount && newSnapshot.BankAccount.IsSome then
                let bankAccountId = newSnapshot.BankAccount.Value.BankAccount.Id
                let existingSnapshot = Collections.Snapshots.Items 
                                     |> Seq.tryFind (fun s -> s.Type = OverviewSnapshotType.BankAccount && s.BankAccount.IsSome && s.BankAccount.Value.BankAccount.Id = bankAccountId)
                match existingSnapshot with
                | Some existing when existing <> newSnapshot ->
                    Collections.Snapshots.Replace(existing, newSnapshot)
                | None ->
                    addNonEmptySnapshotWithEmptyCleanup(newSnapshot)
                | Some _ -> () // Same snapshot, no action needed
            else
                // For empty snapshots, use the helper function to manage properly
                addSnapshotWithEmptyManagement(newSnapshot)
        )
    }

    let private loadLatestTickerSnapshots() = task {
        let tickers = Collections.Tickers.Items
        let snapshots = 
            tickers
            |> Seq.filter (fun t -> t.Id > 0) // Exclude any default/placeholder tickers
            |> Seq.map (fun ticker ->
                async {
                    let! latestSnapshot = TickerSnapshotExtensions.Do.getLatestByTickerId ticker.Id |> Async.AwaitTask
                    match latestSnapshot with
                    | Some dbSnapshot ->
                        return Some (dbSnapshot.tickerSnapshotToModel())
                    | None ->
                        return None
                })
            |> Async.Parallel
            |> Async.RunSynchronously
            |> Array.choose id
            |> Array.toList
        
        snapshots
        |> List.iter (fun newSnapshot ->
            let tickerId = newSnapshot.Ticker.Id
            let existingSnapshot = Collections.TickerSnapshots.Items 
                                 |> Seq.tryFind (fun s -> s.Ticker.Id = tickerId)
            match existingSnapshot with
            | Some existing when existing <> newSnapshot ->
                Collections.TickerSnapshots.Replace(existing, newSnapshot)
            | None ->
                Collections.TickerSnapshots.Add(newSnapshot)
            | Some _ -> () // Same snapshot, no action needed
        )
    }

    let loadLatestSnapshots() = task {
        do! loadLatestBrokerSnapshots() |> Async.AwaitTask |> Async.Ignore
        do! loadLatestBankSnapshots() |> Async.AwaitTask |> Async.Ignore
        do! loadLatestBrokerAccountSnapshots() |> Async.AwaitTask |> Async.Ignore
        do! loadLatestBankAccountSnapshots() |> Async.AwaitTask |> Async.Ignore
        do! loadLatestTickerSnapshots() |> Async.AwaitTask |> Async.Ignore
    }

    let loadBasicData() = task {
        do! DataLoader.CurrencyLoader.load() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.BrokerLoader.load() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.BankLoader.load() |> Async.AwaitTask |> Async.Ignore
        do! getOrRefresAvailableImages() |> Async.AwaitTask |> Async.Ignore
        do! loadTickers() |> Async.AwaitTask |> Async.Ignore
    }

    let initialization() = task {
        do! getOrRefreshAllAccounts() |> Async.AwaitTask |> Async.Ignore
        do! loadLatestSnapshots() |> Async.AwaitTask |> Async.Ignore
    }

    let private updateBankAccount(account: Account, movements: Movement list) = 
        let hasMovements = 
            movements 
            |> List.filter (fun m -> m.BankAccountMovement.IsSome && m.BankAccountMovement.Value.BankAccount.Id = account.Bank.Value.Id)
            |> List.length > 0
        if hasMovements <> account.HasMovements then
            let updatedAccount = { account with HasMovements = hasMovements }
            Collections.updateBankAccount updatedAccount

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

    let loadMovementsFor(account: Account option) = task {

        // TODO: Performance Optimization Needed
        // This method currently loads ALL movements from the database regardless of the account parameter.
        // For large datasets, this can cause significant performance issues.
        // Consider implementing filtering at the database level based on the account parameter:
        // 1. If account is Some, only load movements for that specific account
        // 2. Use parameterized queries in each extension method to filter data before loading
        // 3. Consider pagination for very large movement histories
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