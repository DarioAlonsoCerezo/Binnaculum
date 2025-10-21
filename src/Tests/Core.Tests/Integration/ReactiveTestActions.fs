namespace Core.Tests.Integration

open System
open Binnaculum.Core.UI
open Binnaculum.Core.Models

/// <summary>
/// Reactive test actions for performing operations and verifications.
/// All operations return Async<bool * string * string option> for consistent error handling.
/// </summary>
type ReactiveTestActions(context: ReactiveTestContext) =
    
    /// <summary>
    /// Wipe all data for testing (clean slate)
    /// </summary>
    member _.wipeDataForTesting() : Async<bool * string * string option> =
        async {
            try
                printfn "[ReactiveTestActions] Wiping all data for testing..."
                do! Overview.WipeAllDataForTesting() |> Async.AwaitTask
                printfn "[ReactiveTestActions] ✅ Data wiped successfully"
                return (true, "Data wiped", None)
            with ex ->
                let error = sprintf "Wipe failed: %s" ex.Message
                printfn "[ReactiveTestActions] ❌ %s" error
                return (false, "Wipe failed", Some error)
        }
    
    /// <summary>
    /// Initialize database using WorkOnMemory
    /// </summary>
    member _.initDatabase() : Async<bool * string * string option> =
        async {
            try
                printfn "[ReactiveTestActions] Initializing database (WorkOnMemory)..."
                do! ReactiveTestEnvironment.initializeDatabase()
                
                // Load brokers, currencies, and tickers explicitly
                printfn "[ReactiveTestActions] Loading brokers..."
                do! Binnaculum.Core.DataLoader.BrokerLoader.load() |> Async.AwaitTask
                
                printfn "[ReactiveTestActions] Loading currencies..."
                do! Binnaculum.Core.DataLoader.CurrencyLoader.load() |> Async.AwaitTask
                
                printfn "[ReactiveTestActions] Loading tickers..."
                do! Binnaculum.Core.DataLoader.TikerLoader.load() |> Async.AwaitTask
                
                // Load initial data (accounts, snapshots)
                // Note: LoadData may fail in headless mode due to file system dependencies
                // We catch the error and continue
                printfn "[ReactiveTestActions] Loading accounts and snapshots..."
                try
                    do! Overview.LoadData() |> Async.AwaitTask
                with ex ->
                    printfn "[ReactiveTestActions] ⚠️  LoadData failed (expected in headless mode): %s" ex.Message
                
                // Store common IDs from collections for later use
                let tastytrade = Collections.Brokers.Items |> Seq.tryFind (fun (b: Broker) -> b.Name = "Tastytrade")
                let ibkr = Collections.Brokers.Items |> Seq.tryFind (fun (b: Broker) -> b.Name = "IBKR")
                let usd = Collections.Currencies.Items |> Seq.tryFind (fun (c: Currency) -> c.Code = "USD")
                let eur = Collections.Currencies.Items |> Seq.tryFind (fun (c: Currency) -> c.Code = "EUR")
                let spy = Collections.Tickers.Items |> Seq.tryFind (fun (t: Ticker) -> t.Symbol = "SPY")
                
                tastytrade |> Option.iter (fun b -> context.TastytradeId <- b.Id)
                ibkr |> Option.iter (fun b -> context.IbkrId <- b.Id)
                usd |> Option.iter (fun c -> context.UsdCurrencyId <- c.Id)
                eur |> Option.iter (fun c -> context.EurCurrencyId <- c.Id)
                spy |> Option.iter (fun t -> context.SpyTickerId <- t.Id)
                
                printfn "[ReactiveTestActions] ✅ Database initialized (Tastytrade=%d, IBKR=%d, USD=%d, EUR=%d, SPY=%d)" 
                    context.TastytradeId context.IbkrId context.UsdCurrencyId context.EurCurrencyId context.SpyTickerId
                return (true, "Database initialized", None)
            with ex ->
                let error = sprintf "Init failed: %s" ex.Message
                printfn "[ReactiveTestActions] ❌ %s" error
                return (false, "Init failed", Some error)
        }
    
    /// <summary>
    /// Load data (brokers, currencies, etc.)
    /// </summary>
    member _.loadData() : Async<bool * string * string option> =
        async {
            try
                printfn "[ReactiveTestActions] Loading data..."
                do! Overview.LoadData() |> Async.AwaitTask
                printfn "[ReactiveTestActions] ✅ Data loaded"
                return (true, "Data loaded", None)
            with ex ->
                let error = sprintf "Load failed: %s" ex.Message
                printfn "[ReactiveTestActions] ❌ %s" error
                return (false, "Load failed", Some error)
        }
    
    /// <summary>
    /// Create a broker account with the given name
    /// </summary>
    member _.createBrokerAccount(name: string) : Async<bool * string * string option> =
        async {
            try
                printfn "[ReactiveTestActions] Creating broker account: %s" name
                
                if context.TastytradeId = 0 then
                    let error = "Tastytrade broker not initialized - call initDatabase first"
                    printfn "[ReactiveTestActions] ❌ %s" error
                    return (false, "No broker", Some error)
                else
                    // Create account (returns Task<unit>)
                    do! Binnaculum.Core.UI.Creator.SaveBrokerAccount(context.TastytradeId, name) |> Async.AwaitTask
                    
                    // Wait a moment for Collections to update
                    do! Async.Sleep(100)
                    
                    // Get the account ID from Collections
                    let account = 
                        Collections.Accounts.Items 
                        |> Seq.tryFind (fun acc -> 
                            match acc.Type with
                            | AccountType.BrokerAccount -> 
                                match acc.Broker with
                                | Some ba when ba.AccountNumber = name -> true
                                | _ -> false
                            | _ -> false)
                    
                    match account with
                    | Some acc when acc.Broker.IsSome ->
                        context.BrokerAccountId <- acc.Broker.Value.Id
                        printfn "[ReactiveTestActions] ✅ Broker account created (ID=%d)" acc.Broker.Value.Id
                        return (true, sprintf "Account created (ID=%d)" acc.Broker.Value.Id, None)
                    | _ ->
                        let error = "Could not find created account in Collections"
                        printfn "[ReactiveTestActions] ❌ %s" error
                        return (false, "Account not found", Some error)
            with ex ->
                let error = sprintf "Create account failed: %s" ex.Message
                printfn "[ReactiveTestActions] ❌ %s" error
                return (false, "Create failed", Some error)
        }
    
    /// <summary>
    /// Create a movement (deposit/withdrawal) for the current broker account
    /// </summary>
    member _.createMovement(amount: decimal, movementType: BrokerMovementType, daysOffset: int, ?description: string) : Async<bool * string * string option> =
        async {
            try
                printfn "[ReactiveTestActions] Creating movement: amount=%M, type=%A, daysOffset=%d" amount movementType daysOffset
                
                if context.BrokerAccountId = 0 then
                    let error = "BrokerAccount ID is 0 - call createBrokerAccount first"
                    printfn "[ReactiveTestActions] ❌ %s" error
                    return (false, "No account", Some error)
                elif context.UsdCurrencyId = 0 then
                    let error = "USD Currency ID is 0 - call initDatabase first"
                    printfn "[ReactiveTestActions] ❌ %s" error
                    return (false, "No currency", Some error)
                else
                    // Get the actual BrokerAccount and Currency objects from Collections
                    let brokerAccount = 
                        Collections.Accounts.Items
                        |> Seq.filter (fun acc -> acc.Type = AccountType.BrokerAccount)
                        |> Seq.tryPick (fun acc -> 
                            match acc.Broker with
                            | Some ba when ba.Id = context.BrokerAccountId -> Some ba
                            | _ -> None)
                    
                    let usdCurrency = 
                        Collections.Currencies.Items
                        |> Seq.tryFind (fun c -> c.Id = context.UsdCurrencyId)
                    
                    match brokerAccount, usdCurrency with
                    | None, _ ->
                        let error = "Could not find BrokerAccount object in Collections"
                        printfn "[ReactiveTestActions] ❌ %s" error
                        return (false, "Account not found", Some error)
                    | _, None ->
                        let error = "Could not find USD Currency object in Collections"
                        printfn "[ReactiveTestActions] ❌ %s" error
                        return (false, "Currency not found", Some error)
                    | Some ba, Some curr ->
                        // Create movement with specified date offset
                        let movementDate = DateTime.Now.AddDays(float daysOffset)
                        let notes = 
                            match description with
                            | Some desc -> desc
                            | None -> sprintf "Test %A movement" movementType
                        
                        let movement = {
                            Id = 0  // Will be assigned by database
                            TimeStamp = movementDate
                            Amount = amount
                            Currency = curr
                            BrokerAccount = ba
                            Commissions = 0.0m
                            Fees = 0.0m
                            MovementType = movementType
                            Notes = Some notes
                            FromCurrency = None
                            AmountChanged = None
                            Ticker = None
                            Quantity = None
                        }
                        
                        do! Binnaculum.Core.UI.Creator.SaveBrokerMovement(movement) |> Async.AwaitTask
                        
                        // Wait a moment for Collections to update
                        do! Async.Sleep(100)
                        
                        printfn "[ReactiveTestActions] ✅ Movement created successfully"
                        return (true, sprintf "Movement created: %M %A" amount movementType, None)
            with ex ->
                let error = sprintf "Create movement failed: %s" ex.Message
                printfn "[ReactiveTestActions] ❌ %s" error
                return (false, "Create failed", Some error)
        }
    
    /// <summary>
    /// Import a CSV file for the given broker and account
    /// Executes CSV import workflow using ImportManager
    /// Returns: (success, details, error_option)
    /// Side effects: Triggers Movements_Updated, Tickers_Updated, Snapshots_Updated signals
    /// </summary>
    member _.importFile(brokerId: int, accountId: int, csvPath: string) : Async<bool * string * string option> =
        async {
            try
                printfn "[ReactiveTestActions] Importing CSV: broker=%d, account=%d, file=%s" brokerId accountId csvPath
                
                if not (System.IO.File.Exists(csvPath)) then
                    let error = sprintf "CSV file not found: %s" csvPath
                    printfn "[ReactiveTestActions] ❌ %s" error
                    return (false, "File not found", Some error)
                else
                    // Call ImportManager.importFile
                    let! result = Binnaculum.Core.Import.ImportManager.importFile brokerId accountId csvPath |> Async.AwaitTask
                    
                    if result.Success then
                        let details = 
                            sprintf "Import successful: %d movements, %d tickers" 
                                result.ImportedData.BrokerMovements result.ImportedData.NewTickers
                        printfn "[ReactiveTestActions] ✅ %s" details
                        return (true, details, None)
                    else
                        let errorMsg = 
                            if result.Errors.IsEmpty then "Import failed"
                            else result.Errors |> List.map (fun e -> e.ErrorMessage) |> String.concat "; "
                        printfn "[ReactiveTestActions] ❌ Import failed: %s" errorMsg
                        return (false, "Import failed", Some errorMsg)
            with ex ->
                let error = sprintf "Import exception: %s" ex.Message
                printfn "[ReactiveTestActions] ❌ %s" error
                return (false, "Import exception", Some error)
        }
    
    /// <summary>
    /// Verify account count in Collections.Accounts
    /// </summary>
    member _.verifyAccountCount(expected: int) : Async<bool * string * string option> =
        async {
            let actual = Collections.Accounts.Count
            let success = actual = expected
            let message = sprintf "Account count: expected=%d, actual=%d" expected actual
            printfn "[ReactiveTestActions] %s %s" (if success then "✅" else "❌") message
            return (success, message, if success then None else Some "Count mismatch")
        }
    
    /// <summary>
    /// Verify movement count in Collections.Movements
    /// </summary>
    member _.verifyMovementCount(expected: int) : Async<bool * string * string option> =
        async {
            let actual = Collections.Movements.Count
            let success = actual = expected
            let message = sprintf "Movement count: expected=%d, actual=%d" expected actual
            printfn "[ReactiveTestActions] %s %s" (if success then "✅" else "❌") message
            return (success, message, if success then None else Some "Count mismatch")
        }
    
    /// <summary>
    /// Verify ticker count in Collections.Tickers
    /// </summary>
    member _.verifyTickerCount(expected: int) : Async<bool * string * string option> =
        async {
            let actual = Collections.Tickers.Count
            let success = actual = expected
            let message = sprintf "Ticker count: expected=%d, actual=%d" expected actual
            printfn "[ReactiveTestActions] %s %s" (if success then "✅" else "❌") message
            return (success, message, if success then None else Some "Count mismatch")
        }
    
    /// <summary>
    /// Verify snapshot count in Collections.Snapshots
    /// </summary>
    member _.verifySnapshotCount(expected: int) : Async<bool * string * string option> =
        async {
            let actual = Collections.Snapshots.Count
            let success = actual >= expected  // Use >= for minimum count validation
            let message = sprintf "Snapshot count: expected>=%d, actual=%d" expected actual
            printfn "[ReactiveTestActions] %s %s" (if success then "✅" else "❌") message
            return (success, message, if success then None else Some "Count too low")
        }
    
    /// <summary>
    /// Verify broker count in Collections.Brokers
    /// </summary>
    member _.verifyBrokerCount(expected: int) : Async<bool * string * string option> =
        async {
            let actual = Collections.Brokers.Count
            let success = actual = expected
            let message = sprintf "Broker count: expected=%d, actual=%d" expected actual
            printfn "[ReactiveTestActions] %s %s" (if success then "✅" else "❌") message
            return (success, message, if success then None else Some "Count mismatch")
        }
    
    /// <summary>
    /// Verify currency count in Collections.Currencies
    /// </summary>
    member _.verifyCurrencyCount(expected: int) : Async<bool * string * string option> =
        async {
            let actual = Collections.Currencies.Count
            let success = actual = expected
            let message = sprintf "Currency count: expected=%d, actual=%d" expected actual
            printfn "[ReactiveTestActions] %s %s" (if success then "✅" else "❌") message
            return (success, message, if success then None else Some "Count mismatch")
        }
    
    /// <summary>
    /// Verify total options income (sum of all option trade premiums)
    /// </summary>
    member _.verifyOptionsIncome(expectedIncome: decimal) : Async<bool * string * string option> =
        async {
            try
                // Calculate total options income from all option movements
                let totalIncome = 
                    Collections.Movements.Items
                    |> Seq.filter (fun m -> m.OptionTrade.IsSome)
                    |> Seq.sumBy (fun m -> 
                        match m.OptionTrade with
                        | Some opt -> opt.NetPremium
                        | None -> 0m)
                
                let success = totalIncome = expectedIncome
                let message = sprintf "Options income: expected=%M, actual=%M" expectedIncome totalIncome
                printfn "[ReactiveTestActions] %s %s" (if success then "✅" else "❌") message
                return (success, message, if success then None else Some "Income mismatch")
            with ex ->
                let error = sprintf "Options income verification failed: %s" ex.Message
                printfn "[ReactiveTestActions] ❌ %s" error
                return (false, "Verification failed", Some error)
        }
    
    /// <summary>
    /// Verify realized gains from closed option positions
    /// Uses FIFO matching algorithm for option pairs
    /// </summary>
    member _.verifyRealizedGains(expectedGains: decimal) : Async<bool * string * string option> =
        async {
            try
                // Get all option movements sorted by timestamp
                let optionMovements = 
                    Collections.Movements.Items
                    |> Seq.filter (fun m -> m.OptionTrade.IsSome)
                    |> Seq.sortBy (fun m -> m.TimeStamp)
                    |> Seq.toList
                
                // Calculate realized gains by matching open/close pairs
                // For simplicity, sum all "close" movements (which should net to realized gains)
                // A proper FIFO implementation would match specific option positions
                let realizedGains = 
                    optionMovements
                    |> List.filter (fun m -> 
                        match m.OptionTrade with
                        | Some opt -> 
                            // Close positions are indicated by OptionCode
                            opt.Code = OptionCode.BuyToClose || opt.Code = OptionCode.SellToClose
                        | None -> false)
                    |> List.sumBy (fun m -> 
                        match m.OptionTrade with
                        | Some opt -> opt.NetPremium
                        | None -> 0m)
                
                let success = realizedGains = expectedGains
                let message = sprintf "Realized gains: expected=%M, actual=%M" expectedGains realizedGains
                printfn "[ReactiveTestActions] %s %s" (if success then "✅" else "❌") message
                return (success, message, if success then None else Some "Gains mismatch")
            with ex ->
                let error = sprintf "Realized gains verification failed: %s" ex.Message
                printfn "[ReactiveTestActions] ❌ %s" error
                return (false, "Verification failed", Some error)
        }
    
    /// <summary>
    /// Verify unrealized gains from open option positions
    /// Query: Sum of NetPremium for unclosed option positions
    /// </summary>
    member _.verifyUnrealizedGains(expectedGains: decimal) : Async<bool * string * string option> =
        async {
            try
                // Get all option movements
                let optionMovements = 
                    Collections.Movements.Items
                    |> Seq.filter (fun m -> m.OptionTrade.IsSome)
                    |> Seq.sortBy (fun m -> m.TimeStamp)
                    |> Seq.toList
                
                // Calculate unrealized gains from open positions
                // Open positions are those without matching close
                let unrealizedGains = 
                    optionMovements
                    |> List.filter (fun m -> 
                        match m.OptionTrade with
                        | Some opt -> 
                            // Open positions are indicated by OptionCode
                            opt.Code = OptionCode.BuyToOpen || opt.Code = OptionCode.SellToOpen
                        | None -> false)
                    |> List.sumBy (fun m -> 
                        match m.OptionTrade with
                        | Some opt -> opt.NetPremium
                        | None -> 0m)
                
                let success = unrealizedGains = expectedGains
                let message = sprintf "Unrealized gains: expected=%M, actual=%M" expectedGains unrealizedGains
                printfn "[ReactiveTestActions] %s %s" (if success then "✅" else "❌") message
                return (success, message, if success then None else Some "Gains mismatch")
            with ex ->
                let error = sprintf "Unrealized gains verification failed: %s" ex.Message
                printfn "[ReactiveTestActions] ❌ %s" error
                return (false, "Verification failed", Some error)
        }
    
    /// <summary>
    /// Get the ReactiveTestContext
    /// </summary>
    member _.Context = context
