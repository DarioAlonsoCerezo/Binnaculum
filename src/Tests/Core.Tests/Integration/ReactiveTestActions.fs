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
                let usd = Collections.Currencies.Items |> Seq.tryFind (fun (c: Currency) -> c.Symbol = "USD")
                let eur = Collections.Currencies.Items |> Seq.tryFind (fun (c: Currency) -> c.Symbol = "EUR")
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
    /// NOTE: Temporarily disabled due to complex BrokerMovement model requirements
    /// </summary>
    member _.createMovement(amount: decimal, movementType: BrokerMovementType, daysOffset: int) : Async<bool * string * string option> =
        async {
            let error = "createMovement temporarily disabled - needs proper BrokerMovement construction"
            printfn "[ReactiveTestActions] ⚠️  %s" error
            return (false, "Not implemented", Some error)
        }
    
    /// <summary>
    /// Import a CSV file for the given broker and account
    /// NOTE: Temporarily disabled for headless CI compatibility
    /// </summary>
    member _.importFile(brokerId: int, accountId: int, csvPath: string) : Async<bool * string * string option> =
        async {
            let error = "importFile temporarily disabled - requires file I/O"
            printfn "[ReactiveTestActions] ⚠️  %s" error
            return (false, "Not implemented", Some error)
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
