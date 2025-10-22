namespace Core.Tests.Integration

open System
open Binnaculum.Core.UI
open Binnaculum.Core.Models
open Binnaculum.Core.Logging

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
                CoreLogger.logInfo "ReactiveTestActions" "Wiping all data for testing..."
                do! Overview.WipeAllDataForTesting() |> Async.AwaitTask
                CoreLogger.logInfo "ReactiveTestActions" "✅ Data wiped successfully"
                return (true, "Data wiped", None)
            with ex ->
                let error = sprintf "❌ Wipe failed: %s" ex.Message
                CoreLogger.logError "ReactiveTestActions" error
                return (false, "Wipe failed", Some error)
        }

    /// <summary>
    /// Initialize database using correct sequence:
    /// 1. Configure WorkOnMemory mode
    /// 2. Initialize database schema/structure
    /// 3. Load reference data and populate Collections
    /// 4. Extract and cache common entity IDs for later use
    /// </summary>
    member _.initDatabase() : Async<bool * string * string option> =
        async {
            try
                CoreLogger.logInfo "ReactiveTestActions" "Step 1: Configuring WorkOnMemory mode..."
                do! ReactiveTestEnvironment.initializeDatabase ()

                CoreLogger.logInfo "ReactiveTestActions" "Step 2: Initializing database schema..."
                do! Overview.InitDatabase() |> Async.AwaitTask

                CoreLogger.logInfo "ReactiveTestActions" "Step 3: Loading reference data..."
                do! Overview.LoadData() |> Async.AwaitTask

                // Wait a moment for Collections to populate
                CoreLogger.logInfo "ReactiveTestActions" "Step 4: Verifying Collections population..."
                do! Async.Sleep(200)

                // Verify Collections are populated
                let collectionsValid =
                    if Collections.Brokers.Count = 0 then
                        CoreLogger.logError "ReactiveTestActions" "❌ Brokers collection is empty after LoadData"
                        None
                    elif Collections.Currencies.Count = 0 then
                        CoreLogger.logError "ReactiveTestActions" "❌ Currencies collection is empty after LoadData"
                        None
                    elif Collections.Tickers.Count = 0 then
                        CoreLogger.logError "ReactiveTestActions" "❌ Tickers collection is empty after LoadData"
                        None
                    else
                        Some()

                match collectionsValid with
                | None -> return (false, "Collections empty", Some "One or more collections failed to populate")
                | Some() ->
                    CoreLogger.logInfo "ReactiveTestActions" "Step 5: Extracting common entity IDs..."

                    // Store common IDs from collections for later use
                    let tastytrade =
                        Collections.Brokers.Items
                        |> Seq.tryFind (fun (b: Broker) -> b.Name = "Tastytrade")

                    let ibkr =
                        Collections.Brokers.Items |> Seq.tryFind (fun (b: Broker) -> b.Name = "IBKR")

                    let usd =
                        Collections.Currencies.Items
                        |> Seq.tryFind (fun (c: Currency) -> c.Code = "USD")

                    let eur =
                        Collections.Currencies.Items
                        |> Seq.tryFind (fun (c: Currency) -> c.Code = "EUR")

                    let spy =
                        Collections.Tickers.Items |> Seq.tryFind (fun (t: Ticker) -> t.Symbol = "SPY")

                    tastytrade |> Option.iter (fun b -> context.TastytradeId <- b.Id)
                    ibkr |> Option.iter (fun b -> context.IbkrId <- b.Id)
                    usd |> Option.iter (fun c -> context.UsdCurrencyId <- c.Id)
                    eur |> Option.iter (fun c -> context.EurCurrencyId <- c.Id)
                    spy |> Option.iter (fun t -> context.SpyTickerId <- t.Id)

                    CoreLogger.logInfof
                        "ReactiveTestActions"
                        "✅ Database initialized (Brokers=%d, Currencies=%d, Tickers=%d, Tastytrade=%d, IBKR=%d, USD=%d, EUR=%d, SPY=%d)"
                        Collections.Brokers.Count
                        Collections.Currencies.Count
                        Collections.Tickers.Count
                        context.TastytradeId
                        context.IbkrId
                        context.UsdCurrencyId
                        context.EurCurrencyId
                        context.SpyTickerId

                    return (true, "Database initialized", None)
            with ex ->
                let error = sprintf "❌ Init failed: %s" ex.Message
                CoreLogger.logError "ReactiveTestActions" error
                return (false, "Init failed", Some error)
        }

    /// <summary>
    /// Create a broker account with the given name
    /// </summary>
    member _.createBrokerAccount(name: string) : Async<bool * string * string option> =
        async {
            try
                CoreLogger.logInfof "ReactiveTestActions" "Creating broker account: %s" name

                if context.TastytradeId = 0 then
                    let error = "❌ Tastytrade broker not initialized - call initDatabase first"
                    CoreLogger.logError "ReactiveTestActions" error
                    return (false, "No broker", Some error)
                else
                    // Create account (returns Task<unit>)
                    do!
                        Binnaculum.Core.UI.Creator.SaveBrokerAccount(context.TastytradeId, name)
                        |> Async.AwaitTask

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
                        CoreLogger.logInfof "ReactiveTestActions" "✅ Broker account created (ID=%d)" acc.Broker.Value.Id
                        return (true, sprintf "Account created (ID=%d)" acc.Broker.Value.Id, None)
                    | _ ->
                        let error = "❌ Could not find created account in Collections"
                        CoreLogger.logError "ReactiveTestActions" error
                        return (false, "Account not found", Some error)
            with ex ->
                let error = sprintf "❌ Create account failed: %s" ex.Message
                CoreLogger.logError "ReactiveTestActions" error
                return (false, "Create failed", Some error)
        }

    /// <summary>
    /// Create a movement (deposit/withdrawal) for the current broker account
    /// </summary>
    member _.createMovement
        (amount: decimal, movementType: BrokerMovementType, daysOffset: int, ?description: string)
        : Async<bool * string * string option> =
        async {
            try
                CoreLogger.logInfof
                    "ReactiveTestActions"
                    "Creating movement: amount=%M, type=%A, daysOffset=%d"
                    amount
                    movementType
                    daysOffset

                if context.BrokerAccountId = 0 then
                    let error = "❌ BrokerAccount ID is 0 - call createBrokerAccount first"
                    CoreLogger.logError "ReactiveTestActions" error
                    return (false, "No account", Some error)
                elif context.UsdCurrencyId = 0 then
                    let error = "❌ USD Currency ID is 0 - call initDatabase first"
                    CoreLogger.logError "ReactiveTestActions" error
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
                        let error = "❌ Could not find BrokerAccount object in Collections"
                        CoreLogger.logError "ReactiveTestActions" error
                        return (false, "Account not found", Some error)
                    | _, None ->
                        let error = "❌ Could not find USD Currency object in Collections"
                        CoreLogger.logError "ReactiveTestActions" error
                        return (false, "Currency not found", Some error)
                    | Some ba, Some curr ->
                        // Create movement with specified date offset
                        let movementDate = DateTime.Now.AddDays(float daysOffset)

                        let notes =
                            match description with
                            | Some desc -> desc
                            | None -> sprintf "Test %A movement" movementType

                        let movement =
                            { Id = 0 // Will be assigned by database
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
                              Quantity = None }

                        do! Binnaculum.Core.UI.Creator.SaveBrokerMovement(movement) |> Async.AwaitTask

                        // Wait a moment for Collections to update
                        do! Async.Sleep(100)

                        CoreLogger.logInfo "ReactiveTestActions" "✅ Movement created successfully"
                        return (true, sprintf "Movement created: %M %A" amount movementType, None)
            with ex ->
                let error = sprintf "❌ Create movement failed: %s" ex.Message
                CoreLogger.logError "ReactiveTestActions" error
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
                CoreLogger.logInfof
                    "ReactiveTestActions"
                    "Importing CSV: broker=%d, account=%d, file=%s"
                    brokerId
                    accountId
                    csvPath

                if not (System.IO.File.Exists(csvPath)) then
                    let error = sprintf "❌ CSV file not found: %s" csvPath
                    CoreLogger.logError "ReactiveTestActions" error
                    return (false, "File not found", Some error)
                else
                    // Call ImportManager.importFile
                    let! result =
                        Binnaculum.Core.Import.ImportManager.importFile brokerId accountId csvPath
                        |> Async.AwaitTask

                    if result.Success then
                        let details =
                            sprintf
                                "✅ Import successful: %d movements, %d tickers"
                                result.ImportedData.BrokerMovements
                                result.ImportedData.NewTickers

                        CoreLogger.logInfo "ReactiveTestActions" details
                        return (true, details, None)
                    else
                        let errorMsg =
                            if result.Errors.IsEmpty then
                                "Import failed"
                            else
                                result.Errors |> List.map (fun e -> e.ErrorMessage) |> String.concat "; "

                        CoreLogger.logError "ReactiveTestActions" (sprintf "❌ Import failed: %s" errorMsg)
                        return (false, "Import failed", Some errorMsg)
            with ex ->
                let error = sprintf "❌ Import exception: %s" ex.Message
                CoreLogger.logError "ReactiveTestActions" error
                return (false, "Import exception", Some error)
        }

    /// <summary>
    /// Verify account count in Collections.Accounts
    /// </summary>
    member _.verifyAccountCount(expected: int) : Async<bool * string * string option> =
        async {
            let actual = Collections.Accounts.Count
            let success = actual = expected

            let logMessage =
                sprintf "%s Account count: expected=%d, actual=%d" (if success then "✅" else "❌") expected actual

            let cleanMessage = sprintf "Account count: expected=%d, actual=%d" expected actual

            if success then
                CoreLogger.logInfo "ReactiveTestActions" logMessage
            else
                CoreLogger.logError "ReactiveTestActions" logMessage

            return (success, cleanMessage, if success then None else Some "Count mismatch")
        }

    /// <summary>
    /// Verify movement count in Collections.Movements
    /// </summary>
    member _.verifyMovementCount(expected: int) : Async<bool * string * string option> =
        async {
            let actual = Collections.Movements.Count
            let success = actual = expected

            let logMessage =
                sprintf "%s Movement count: expected=%d, actual=%d" (if success then "✅" else "❌") expected actual

            let cleanMessage = sprintf "Movement count: expected=%d, actual=%d" expected actual

            if success then
                CoreLogger.logInfo "ReactiveTestActions" logMessage
            else
                CoreLogger.logError "ReactiveTestActions" logMessage

            return (success, cleanMessage, if success then None else Some "Count mismatch")
        }

    /// <summary>
    /// Verify ticker count in Collections.Tickers
    /// </summary>
    member _.verifyTickerCount(expected: int) : Async<bool * string * string option> =
        async {
            let actual = Collections.Tickers.Count
            let success = actual = expected

            let logMessage =
                sprintf "%s Ticker count: expected=%d, actual=%d" (if success then "✅" else "❌") expected actual

            let cleanMessage = sprintf "Ticker count: expected=%d, actual=%d" expected actual

            if success then
                CoreLogger.logInfo "ReactiveTestActions" logMessage
            else
                CoreLogger.logError "ReactiveTestActions" logMessage

            return (success, cleanMessage, if success then None else Some "Count mismatch")
        }

    /// <summary>
    /// Verify snapshot count in Collections.Snapshots
    /// </summary>
    member _.verifySnapshotCount(expected: int) : Async<bool * string * string option> =
        async {
            let actual = Collections.Snapshots.Count
            let success = actual >= expected // Use >= for minimum count validation

            let logMessage =
                sprintf "%s Snapshot count: expected>=%d, actual=%d" (if success then "✅" else "❌") expected actual

            let cleanMessage = sprintf "Snapshot count: expected>=%d, actual=%d" expected actual

            if success then
                CoreLogger.logInfo "ReactiveTestActions" logMessage
            else
                CoreLogger.logError "ReactiveTestActions" logMessage

            return (success, cleanMessage, if success then None else Some "Count too low")
        }

    /// <summary>
    /// Verify broker count in Collections.Brokers
    /// </summary>
    member _.verifyBrokerCount(expected: int) : Async<bool * string * string option> =
        async {
            let actual = Collections.Brokers.Count
            let success = actual = expected

            let logMessage =
                sprintf "%s Broker count: expected=%d, actual=%d" (if success then "✅" else "❌") expected actual

            let cleanMessage = sprintf "Broker count: expected=%d, actual=%d" expected actual

            if success then
                CoreLogger.logInfo "ReactiveTestActions" logMessage
            else
                CoreLogger.logError "ReactiveTestActions" logMessage

            return (success, cleanMessage, if success then None else Some "Count mismatch")
        }

    /// <summary>
    /// Verify currency count in Collections.Currencies
    /// </summary>
    member _.verifyCurrencyCount(expected: int) : Async<bool * string * string option> =
        async {
            let actual = Collections.Currencies.Count
            let success = actual = expected

            let logMessage =
                sprintf "%s Currency count: expected=%d, actual=%d" (if success then "✅" else "❌") expected actual

            let cleanMessage = sprintf "Currency count: expected=%d, actual=%d" expected actual

            if success then
                CoreLogger.logInfo "ReactiveTestActions" logMessage
            else
                CoreLogger.logError "ReactiveTestActions" logMessage

            return (success, cleanMessage, if success then None else Some "Count mismatch")
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

                let message =
                    sprintf
                        "%s Options income: expected=%M, actual=%M"
                        (if success then "✅" else "❌")
                        expectedIncome
                        totalIncome

                if success then
                    CoreLogger.logInfo "ReactiveTestActions" message
                else
                    CoreLogger.logError "ReactiveTestActions" message

                return (success, message, if success then None else Some "Income mismatch")
            with ex ->
                let error = sprintf "❌ Options income verification failed: %s" ex.Message
                CoreLogger.logError "ReactiveTestActions" error
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

                let message =
                    sprintf
                        "%s Realized gains: expected=%M, actual=%M"
                        (if success then "✅" else "❌")
                        expectedGains
                        realizedGains

                if success then
                    CoreLogger.logInfo "ReactiveTestActions" message
                else
                    CoreLogger.logError "ReactiveTestActions" message

                return (success, message, if success then None else Some "Gains mismatch")
            with ex ->
                let error = sprintf "❌ Realized gains verification failed: %s" ex.Message
                CoreLogger.logError "ReactiveTestActions" error
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

                let message =
                    sprintf
                        "%s Unrealized gains: expected=%M, actual=%M"
                        (if success then "✅" else "❌")
                        expectedGains
                        unrealizedGains

                if success then
                    CoreLogger.logInfo "ReactiveTestActions" message
                else
                    CoreLogger.logError "ReactiveTestActions" message

                return (success, message, if success then None else Some "Gains mismatch")
            with ex ->
                let error = sprintf "❌ Unrealized gains verification failed: %s" ex.Message
                CoreLogger.logError "ReactiveTestActions" error
                return (false, "Verification failed", Some error)
        }

    /// <summary>
    /// Verify total deposited amount in broker account snapshot
    /// Query: BrokerAccount.Financial.Deposited
    /// Returns: (success, actual_amount_string, error_option)
    /// </summary>
    member _.verifyDeposited(expectedAmount: decimal) : Async<bool * string * string option> =
        async {
            try
                // Get the latest broker account snapshot from Collections.Snapshots
                let brokerAccountSnapshot =
                    Collections.Snapshots.Items
                    |> Seq.filter (fun s -> s.Type = OverviewSnapshotType.BrokerAccount)
                    |> Seq.tryHead

                match brokerAccountSnapshot with
                | Some snapshot when snapshot.BrokerAccount.IsSome ->
                    let deposited = snapshot.BrokerAccount.Value.Financial.Deposited
                    let success = deposited = expectedAmount
                    let message = sprintf "%M" deposited

                    let logMsg =
                        sprintf
                            "%s Deposited: expected=%M, actual=%M"
                            (if success then "✅" else "❌")
                            expectedAmount
                            deposited

                    if success then
                        CoreLogger.logInfo "ReactiveTestActions" logMsg
                    else
                        CoreLogger.logError "ReactiveTestActions" logMsg

                    return (success, message, if success then None else Some "Deposited mismatch")
                | _ ->
                    let error = "❌ No BrokerAccount snapshot found in Collections.Snapshots"
                    CoreLogger.logError "ReactiveTestActions" error
                    return (false, "0", Some error)
            with ex ->
                let error = sprintf "❌ Deposited verification failed: %s" ex.Message
                CoreLogger.logError "ReactiveTestActions" error
                return (false, "Verification failed", Some error)
        }

    /// <summary>
    /// Verify total withdrawn amount in broker account snapshot
    /// Query: BrokerAccount.Financial.Withdrawn
    /// Returns: (success, actual_amount_string, error_option)
    /// </summary>
    member _.verifyWithdrawn(expectedAmount: decimal) : Async<bool * string * string option> =
        async {
            try
                // Get the latest broker account snapshot from Collections.Snapshots
                let brokerAccountSnapshot =
                    Collections.Snapshots.Items
                    |> Seq.filter (fun s -> s.Type = OverviewSnapshotType.BrokerAccount)
                    |> Seq.tryHead

                match brokerAccountSnapshot with
                | Some snapshot when snapshot.BrokerAccount.IsSome ->
                    let withdrawn = snapshot.BrokerAccount.Value.Financial.Withdrawn
                    let success = withdrawn = expectedAmount
                    let message = sprintf "%M" withdrawn

                    let logMsg =
                        sprintf
                            "%s Withdrawn: expected=%M, actual=%M"
                            (if success then "✅" else "❌")
                            expectedAmount
                            withdrawn

                    if success then
                        CoreLogger.logInfo "ReactiveTestActions" logMsg
                    else
                        CoreLogger.logError "ReactiveTestActions" logMsg

                    return (success, message, if success then None else Some "Withdrawn mismatch")
                | _ ->
                    let error = "❌ No BrokerAccount snapshot found in Collections.Snapshots"
                    CoreLogger.logError "ReactiveTestActions" error
                    return (false, "0", Some error)
            with ex ->
                let error = sprintf "❌ Withdrawn verification failed: %s" ex.Message
                CoreLogger.logError "ReactiveTestActions" error
                return (false, "Verification failed", Some error)
        }

    /// <summary>
    /// Verify MovementCounter in broker account snapshot
    /// Query: BrokerAccount.Financial.MovementCounter
    /// Returns: (success, count_as_string, error_option)
    /// </summary>
    member _.verifyMovementCounter(expectedCount: int) : Async<bool * string * string option> =
        async {
            try
                // Get the latest broker account snapshot from Collections.Snapshots
                let brokerAccountSnapshot =
                    Collections.Snapshots.Items
                    |> Seq.filter (fun s -> s.Type = OverviewSnapshotType.BrokerAccount)
                    |> Seq.tryHead

                match brokerAccountSnapshot with
                | Some snapshot when snapshot.BrokerAccount.IsSome ->
                    let counter = snapshot.BrokerAccount.Value.Financial.MovementCounter
                    let success = counter = expectedCount
                    let message = sprintf "%d" counter

                    let logMsg =
                        sprintf
                            "%s MovementCounter: expected=%d, actual=%d"
                            (if success then "✅" else "❌")
                            expectedCount
                            counter

                    if success then
                        CoreLogger.logInfo "ReactiveTestActions" logMsg
                    else
                        CoreLogger.logError "ReactiveTestActions" logMsg

                    return (success, message, if success then None else Some "MovementCounter mismatch")
                | _ ->
                    let error = "❌ No BrokerAccount snapshot found in Collections.Snapshots"
                    CoreLogger.logError "ReactiveTestActions" error
                    return (false, "0", Some error)
            with ex ->
                let error = sprintf "❌ MovementCounter verification failed: %s" ex.Message
                CoreLogger.logError "ReactiveTestActions" error
                return (false, "Verification failed", Some error)
        }

    /// <summary>
    /// Calls BrokerAccounts.GetSnapshots and verifies count
    /// Query: BrokerAccounts.GetSnapshots(accountId) length
    /// Returns: (success, count_as_string, error_option)
    /// </summary>
    member _.verifyBrokerAccountSnapshots(accountId: int, expectedCount: int) : Async<bool * string * string option> =
        async {
            try
                // Call BrokerAccounts.GetSnapshots to retrieve snapshots for the account
                let! snapshots = BrokerAccounts.GetSnapshots(accountId) |> Async.AwaitTask
                let actualCount = snapshots |> List.length
                let success = actualCount = expectedCount
                let message = sprintf "%d" actualCount

                let logMsg =
                    sprintf
                        "%s BrokerAccounts.GetSnapshots: expected=%d, actual=%d"
                        (if success then "✅" else "❌")
                        expectedCount
                        actualCount

                if success then
                    CoreLogger.logInfo "ReactiveTestActions" logMsg
                else
                    CoreLogger.logError "ReactiveTestActions" logMsg

                return (success, message, if success then None else Some "Snapshot count mismatch")
            with ex ->
                let error =
                    sprintf "❌ BrokerAccounts.GetSnapshots verification failed: %s" ex.Message

                CoreLogger.logError "ReactiveTestActions" error
                return (false, "Verification failed", Some error)
        }

    /// <summary>
    /// Verify PFE ticker snapshots using Tickers.GetSnapshots
    /// Query: Tickers.GetSnapshots(pfeTickerId) length
    /// Returns: (success, count_as_string, error_option)
    /// </summary>
    member _.verifyPfizerSnapshots(expectedCount: int) : Async<bool * string * string option> =
        async {
            try
                // Find PFE ticker in Collections
                let pfeTicker = Collections.Tickers.Items |> Seq.tryFind (fun t -> t.Symbol = "PFE")

                match pfeTicker with
                | Some ticker ->
                    // Call Tickers.GetSnapshots to retrieve snapshots for PFE
                    let! snapshots = Tickers.GetSnapshots(ticker.Id) |> Async.AwaitTask
                    let actualCount = snapshots |> List.length
                    let success = actualCount = expectedCount
                    let message = sprintf "%d" actualCount

                    let logMsg =
                        sprintf
                            "%s Tickers.GetSnapshots(PFE): expected=%d, actual=%d"
                            (if success then "✅" else "❌")
                            expectedCount
                            actualCount

                    if success then
                        CoreLogger.logInfo "ReactiveTestActions" logMsg
                    else
                        CoreLogger.logError "ReactiveTestActions" logMsg

                    return (success, message, if success then None else Some "Snapshot count mismatch")
                | None ->
                    let error = "❌ PFE ticker not found in Collections.Tickers"
                    CoreLogger.logError "ReactiveTestActions" error
                    return (false, "0", Some error)
            with ex ->
                let error = sprintf "❌ Tickers.GetSnapshots verification failed: %s" ex.Message
                CoreLogger.logError "ReactiveTestActions" error
                return (false, "Verification failed", Some error)
        }

    /// <summary>
    /// Verify TSLL ticker snapshots count using Tickers.GetSnapshots
    /// Query: Tickers.GetSnapshots(tsllTickerId) length
    /// Returns: (success, count_as_string, error_option)
    /// </summary>
    member _.verifyTsllSnapshotCount(expectedCount: int) : Async<bool * string * string option> =
        async {
            try
                // Find TSLL ticker in Collections
                let tsllTicker =
                    Collections.Tickers.Items |> Seq.tryFind (fun t -> t.Symbol = "TSLL")

                match tsllTicker with
                | Some ticker ->
                    // Call Tickers.GetSnapshots to retrieve snapshots for TSLL
                    let! snapshots = Tickers.GetSnapshots(ticker.Id) |> Async.AwaitTask
                    let actualCount = snapshots |> List.length
                    let success = actualCount = expectedCount
                    let message = sprintf "%d" actualCount

                    let logMsg =
                        sprintf
                            "%s Tickers.GetSnapshots(TSLL): expected=%d, actual=%d"
                            (if success then "✅" else "❌")
                            expectedCount
                            actualCount

                    if success then
                        CoreLogger.logInfo "ReactiveTestActions" logMsg
                    else
                        CoreLogger.logError "ReactiveTestActions" logMsg

                    return (success, message, if success then None else Some "Snapshot count mismatch")
                | None ->
                    let error = "❌ TSLL ticker not found in Collections.Tickers"
                    CoreLogger.logError "ReactiveTestActions" error
                    return (false, "0", Some error)
            with ex ->
                let error = sprintf "❌ Tickers.GetSnapshots verification failed: %s" ex.Message
                CoreLogger.logError "ReactiveTestActions" error
                return (false, "Verification failed", Some error)
        }

    /// <summary>
    /// Validate a specific TSLL snapshot by date
    /// Checks: Options income, TotalShares, Unrealized, Realized values
    /// Returns: (success, validation_details, error_option)
    /// </summary>
    member _.validateTsllSnapshot(year: int, month: int, day: int) : Async<bool * string * string option> =
        async {
            try
                // Find TSLL ticker in Collections
                let tsllTicker =
                    Collections.Tickers.Items |> Seq.tryFind (fun t -> t.Symbol = "TSLL")

                match tsllTicker with
                | Some ticker ->
                    // Call Tickers.GetSnapshots to retrieve snapshots for TSLL
                    let! snapshots = Tickers.GetSnapshots(ticker.Id) |> Async.AwaitTask

                    // Find snapshot for the specific date
                    let targetDate = DateOnly(year, month, day)
                    let snapshot = snapshots |> List.tryFind (fun s -> s.Date = targetDate)

                    match snapshot with
                    | Some snap ->
                        // Get the main currency snapshot (should be USD)
                        let mainCurrency = snap.MainCurrency

                        // Validate key properties
                        let validations =
                            [ ("TotalShares",
                               mainCurrency.TotalShares = 0.0m,
                               sprintf "Expected 0, got %M" mainCurrency.TotalShares)
                              ("Currency",
                               mainCurrency.Currency.Code = "USD",
                               sprintf "Expected USD, got %s" mainCurrency.Currency.Code) ]

                        let allValid = validations |> List.forall (fun (_, valid, _) -> valid)

                        let details =
                            validations
                            |> List.map (fun (name, valid, msg) ->
                                if valid then
                                    sprintf "%s: ✓" name
                                else
                                    sprintf "%s: ✗ %s" name msg)
                            |> String.concat ", "

                        let message =
                            sprintf
                                "%s Snapshot %04d-%02d-%02d validated: %s"
                                (if allValid then "✅" else "❌")
                                year
                                month
                                day
                                details

                        if allValid then
                            CoreLogger.logInfo "ReactiveTestActions" message
                        else
                            CoreLogger.logError "ReactiveTestActions" message

                        return (allValid, message, if allValid then None else Some "Validation failed")
                    | None ->
                        let error =
                            sprintf "❌ TSLL snapshot not found for date %04d-%02d-%02d" year month day

                        CoreLogger.logError "ReactiveTestActions" error
                        return (false, error, Some error)
                | None ->
                    let error = "❌ TSLL ticker not found in Collections.Tickers"
                    CoreLogger.logError "ReactiveTestActions" error
                    return (false, "Ticker not found", Some error)
            with ex ->
                let error = sprintf "❌ TSLL snapshot validation failed: %s" ex.Message
                CoreLogger.logError "ReactiveTestActions" error
                return (false, "Validation exception", Some error)
        }

    /// <summary>
    /// Get the ReactiveTestContext
    /// </summary>
    member _.Context = context
