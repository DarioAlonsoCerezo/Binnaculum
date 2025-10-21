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
                
                // Load initial data (brokers, currencies, etc.)
                printfn "[ReactiveTestActions] Loading initial data..."
                do! Overview.LoadData() |> Async.AwaitTask
                
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
                    let! accountId = Binnaculum.Core.UI.Creator.SaveBrokerAccount(context.TastytradeId, name) |> Async.AwaitTask
                    context.BrokerAccountId <- accountId
                    printfn "[ReactiveTestActions] ✅ Broker account created (ID=%d)" accountId
                    return (true, sprintf "Account created (ID=%d)" accountId, None)
            with ex ->
                let error = sprintf "Create account failed: %s" ex.Message
                printfn "[ReactiveTestActions] ❌ %s" error
                return (false, "Create failed", Some error)
        }
    
    /// <summary>
    /// Create a movement (deposit/withdrawal) for the current broker account
    /// </summary>
    member _.createMovement(amount: decimal, movementType: BrokerMovementType, daysOffset: int) : Async<bool * string * string option> =
        async {
            try
                printfn "[ReactiveTestActions] Creating movement: %M (%A) with offset %d days" amount movementType daysOffset
                
                if context.BrokerAccountId = 0 then
                    let error = "Broker account not created - call createBrokerAccount first"
                    printfn "[ReactiveTestActions] ❌ %s" error
                    return (false, "No account", Some error)
                else
                    let date = DateTime.Today.AddDays(float daysOffset)
                    let movement: BrokerMovement = {
                        Id = 0
                        TimeStamp = date
                        BrokerAccountId = context.BrokerAccountId
                        Type = movementType
                        Amount = amount
                        Description = ""
                    }
                    do! Binnaculum.Core.UI.Creator.SaveBrokerMovement(movement) |> Async.AwaitTask
                    printfn "[ReactiveTestActions] ✅ Movement created"
                    return (true, "Movement created", None)
            with ex ->
                let error = sprintf "Create movement failed: %s" ex.Message
                printfn "[ReactiveTestActions] ❌ %s" error
                return (false, "Create failed", Some error)
        }
    
    /// <summary>
    /// Import a CSV file for the given broker and account
    /// </summary>
    member _.importFile(brokerId: int, accountId: int, csvPath: string) : Async<bool * string * string option> =
        async {
            try
                printfn "[ReactiveTestActions] Importing file: %s (Broker=%d, Account=%d)" csvPath brokerId accountId
                
                if not (System.IO.File.Exists(csvPath)) then
                    let error = sprintf "CSV file not found: %s" csvPath
                    printfn "[ReactiveTestActions] ❌ %s" error
                    return (false, "File not found", Some error)
                else
                    do! Binnaculum.Core.Import.ImportManager.importFile(brokerId, accountId, csvPath) |> Async.AwaitTask
                    printfn "[ReactiveTestActions] ✅ File imported successfully"
                    return (true, "File imported", None)
            with ex ->
                let error = sprintf "Import failed: %s" ex.Message
                printfn "[ReactiveTestActions] ❌ %s" error
                return (false, "Import failed", Some error)
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
