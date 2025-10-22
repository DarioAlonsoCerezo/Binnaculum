namespace Core.Tests.Integration

open System
open Binnaculum.Core.UI

/// <summary>
/// Reusable test environment setup and teardown utilities.
///
/// Provides:
/// - setupTestEnvironment() - Initialize InMemory DB and signal observation
/// - teardownTestEnvironment() - Cleanup streams
/// - initializeDatabaseAndVerifySignals() - DB init with signal wait
/// - printPhaseHeader() - Standardized phase reporting
///
/// See README.md for usage examples.
/// </summary>
module TestSetup =

    /// <summary>
    /// Sets up the test environment for reactive testing.
    /// - Enables InMemory mode to avoid platform dependencies
    /// - Wipes all existing data
    /// - Starts observing reactive streams
    ///
    /// Returns a tuple of (testContext, testActions) for use in tests.
    /// </summary>
    let setupTestEnvironment () : Async<TestContext * TestActions> =
        async {
            printfn "\n=== Test Setup ==="

            // Ensure we're in memory mode to avoid platform dependencies
            Overview.WorkOnMemory()
            printfn "✅ Overview configured for InMemory mode"

            // Wipe all data
            try
                do! Overview.WipeAllDataForTesting() |> Async.AwaitTask
                printfn "✅ Data wiped successfully"
            with ex ->
                printfn "⚠️  Wipe failed: %s (may be expected if DB not initialized)" ex.Message

            // Start observing reactive streams
            StreamObserver.startObserving ()
            printfn "✅ Reactive stream observation started"

            // Create test context and actions
            let ctx = TestContext.create ()
            let actions = TestActions(ctx)

            printfn "=== Setup Complete ===\n"

            return (ctx, actions)
        }

    /// <summary>
    /// Tears down the test environment after reactive testing.
    /// - Stops observing reactive streams
    /// - Cleans up resources
    /// </summary>
    let teardownTestEnvironment () : Async<unit> =
        async {
            printfn "\n=== Test Teardown ==="
            StreamObserver.stopObserving ()
            printfn "✅ Reactive stream observation stopped"
            printfn "=== Teardown Complete ===\n"
        }

    /// <summary>
    /// Initializes database and verifies all expected signals were received.
    /// Provides a reusable pattern for database initialization tests.
    /// </summary>
    let initializeDatabaseAndVerifySignals
        (actions: TestActions)
        (expectedSignals: Signal list)
        (timeout: TimeSpan)
        : Async<bool> =
        async {
            // Expect the signals
            StreamObserver.expectSignals expectedSignals

            let signalNames =
                expectedSignals |> List.map (fun s -> s.ToString()) |> String.concat ", "

            printfn "Expecting signals: %s" signalNames

            // Initialize database
            let! (ok, _, error) = actions.initDatabase ()

            if not ok then
                printfn "❌ Database initialization failed: %s" (error |> Option.defaultValue "Unknown error")
            else
                printfn "✅ Database initialized"

            // Wait for signals
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync timeout

            if signalsReceived then
                printfn "✅ All expected signals received"
            else
                printfn "❌ Not all signals received (timeout)"

            return signalsReceived
        }

    /// <summary>
    /// Prints a formatted test phase header.
    /// Standardizes phase reporting across tests.
    /// </summary>
    let printPhaseHeader (phaseNumber: int) (phaseName: string) : unit =
        printfn "\n--- Phase %d: %s ---" phaseNumber phaseName

    /// <summary>
    /// Prints a formatted test completion summary.
    /// Standardizes test completion reporting.
    /// </summary>
    let printTestCompletionSummary (testName: string) (details: string) : unit =
        printfn "\n=== Test Summary ==="
        printfn "✅ %s" testName
        printfn "%s" details
        printfn "=== %s Complete ✅ ===" testName

    /// <summary>
    /// Verifies all standard verifications and prints results.
    /// </summary>
    let verifyAndPrintState (description: string) : Async<bool> =
        async {
            let (success, stateSummary) = TestVerifications.verifyCollectionsState ()
            printfn "%s: %s" description stateSummary

            if not success then
                printfn "⚠️  Some collections may not be populated as expected"

            return success
        }
