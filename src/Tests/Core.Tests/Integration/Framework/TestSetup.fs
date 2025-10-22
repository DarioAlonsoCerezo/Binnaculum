namespace Core.Tests.Integration

open System
open Binnaculum.Core.UI
open Binnaculum.Core.Logging

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
            CoreLogger.logInfo "[TestSetup]" "=== Test Setup ==="

            // Ensure we're in memory mode to avoid platform dependencies
            Overview.WorkOnMemory()
            CoreLogger.logInfo "[TestSetup]" "✅ Overview configured for InMemory mode"

            // Wipe all data
            try
                do! Overview.WipeAllDataForTesting() |> Async.AwaitTask
                CoreLogger.logInfo "[TestSetup]" "✅ Data wiped successfully"
            with ex ->
                CoreLogger.logWarning "[TestSetup]" (sprintf "⚠️  Wipe failed: %s (may be expected if DB not initialized)" ex.Message)

            // Start observing reactive streams
            StreamObserver.startObserving ()
            CoreLogger.logInfo "[TestSetup]" "✅ Reactive stream observation started"

            // Create test context and actions
            let ctx = TestContext.create ()
            let actions = TestActions(ctx)

            CoreLogger.logInfo "[TestSetup]" "=== Setup Complete ==="

            return (ctx, actions)
        }

    /// <summary>
    /// Tears down the test environment after reactive testing.
    /// - Stops observing reactive streams
    /// - Cleans up resources
    /// </summary>
    let teardownTestEnvironment () : Async<unit> =
        async {
            CoreLogger.logInfo "[TestSetup]" "=== Test Teardown ==="
            StreamObserver.stopObserving ()
            CoreLogger.logInfo "[TestSetup]" "✅ Reactive stream observation stopped"
            CoreLogger.logInfo "[TestSetup]" "=== Teardown Complete ==="
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

            CoreLogger.logDebug "[TestSetup]" (sprintf "Expecting signals: %s" signalNames)

            // Initialize database
            let! (ok, _, error) = actions.initDatabase ()

            if not ok then
                CoreLogger.logError "[TestSetup]" (sprintf "❌ Database initialization failed: %s" (error |> Option.defaultValue "Unknown error"))
            else
                CoreLogger.logInfo "[TestSetup]" "✅ Database initialized"

            // Wait for signals
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync timeout

            if signalsReceived then
                CoreLogger.logInfo "[TestSetup]" "✅ All expected signals received"
            else
                CoreLogger.logError "[TestSetup]" "❌ Not all signals received (timeout)"

            return signalsReceived
        }

    /// <summary>
    /// Prints a formatted test phase header.
    /// Standardizes phase reporting across tests.
    /// </summary>
    let printPhaseHeader (phaseNumber: int) (phaseName: string) : unit =
        CoreLogger.logInfo "[Test]" (sprintf "--- Phase %d: %s ---" phaseNumber phaseName)

    /// <summary>
    /// Prints a formatted test completion summary.
    /// Standardizes test completion reporting.
    /// </summary>
    let printTestCompletionSummary (testName: string) (details: string) : unit =
        CoreLogger.logInfo "[Test]" "=== Test Summary ==="
        CoreLogger.logInfo "[Test]" (sprintf "✅ %s" testName)
        CoreLogger.logInfo "[Test]" details
        CoreLogger.logInfo "[Test]" (sprintf "=== %s Complete ✅ ===" testName)

    /// <summary>
    /// Verifies all standard verifications and prints results.
    /// </summary>
    let verifyAndPrintState (description: string) : Async<bool> =
        async {
            let (success, stateSummary) = TestVerifications.verifyCollectionsState ()
            CoreLogger.logInfo "[Verification]" (sprintf "%s: %s" description stateSummary)

            if not success then
                CoreLogger.logWarning "[Verification]" "⚠️  Some collections may not be populated as expected"

            return success
        }
