namespace Core.Tests.Integration

open NUnit.Framework
open System
open Binnaculum.Core.Models
open Binnaculum.Core.Logging

/// <summary>
/// Base class for reactive integration tests using signal-based approach.
///
/// Handles all Setup/Teardown boilerplate:
/// - InMemory database initialization
/// - Reactive stream observation start/stop
/// - Test context and actions management
///
/// USAGE:
/// ------
/// 1. Inherit from this class
/// 2. Add [<Test>] methods using this.Actions
/// 3. Use StreamObserver.expectSignals() and waitForAllSignalsAsync()
/// 4. Use TestVerifications for assertions
///
/// EXAMPLE:
/// --------
///     [<TestFixture>]
///     type MyTests() =
///         inherit TestFixtureBase()
///
///         [<Test>]
///         member this.``My test``() = async {
///             let actions = this.Actions
///
///             // Setup
///             let! (ok, _, _) = actions.initDatabase()
///
///             // Expect signals BEFORE operation
///             StreamObserver.expectSignals([ Accounts_Updated ])
///
///             // Execute
///             let! (ok, _) = actions.createBrokerAccount("Test")
///
///             // Wait for signals (NOT Thread.Sleep!)
///             let! received = StreamObserver.waitForAllSignalsAsync(TimeSpan.FromSeconds(10.0))
///             Assert.That(received, Is.True)
///
///             // Verify
///             let! (verified, count, _) = actions.verifyAccountCount(1)
///             Assert.That(verified, Is.True)
///         }
///
/// PATTERN: Setup/Expect/Execute/Wait/Verify
/// See README.md for full documentation and more examples.
/// See PATTERN_GUIDE.fs for detailed implementation guide.
///
/// Provides common setup, teardown, and access to test context and actions.
///
/// Derived classes should:
/// 1. Override test methods with [<Test>] attribute
/// 2. Use Context property to access TestContext
/// 3. Use Actions property to access TestActions
/// 4. Call TestSetup utility functions for standardized operations
/// </summary>
[<AbstractClass>]
type TestFixtureBase() =

    let mutable testContext: Core.Tests.Integration.TestContext option = None
    let mutable testActions: Core.Tests.Integration.TestActions option = None

    /// <summary>
    /// Gets the test context.
    /// Available after Setup() completes.
    /// </summary>
    member _.Context =
        match testContext with
        | Some ctx -> ctx
        | None -> failwith "Test context not initialized. Ensure Setup() has completed."

    /// <summary>
    /// Gets the test actions helper.
    /// Available after Setup() completes.
    /// </summary>
    member _.Actions =
        match testActions with
        | Some actions -> actions
        | None -> failwith "Test actions not initialized. Ensure Setup() has completed."

    /// <summary>
    /// Setup before each test - prepare environment and start observing streams.
    /// This is called automatically by NUnit before each test method.
    /// </summary>
    [<SetUp>]
    member _.Setup() =
        async {
            let! (ctx, actions) = TestSetup.setupTestEnvironment ()
            testContext <- Some ctx
            testActions <- Some actions
        }

    /// <summary>
    /// Teardown after each test - stop observing streams and clean up.
    /// This is called automatically by NUnit after each test method.
    /// </summary>
    [<TearDown>]
    member _.Teardown() =
        async {
            do! TestSetup.teardownTestEnvironment ()
            testContext <- None
            testActions <- None
        }

    /// <summary>
    /// Verify and assert a list of TickerCurrencySnapshots with standardized logging.
    ///
    /// USAGE:
    /// ------
    /// let getDescription i =
    ///     let date = expectedSnapshots.[i].Date.ToString("yyyy-MM-dd")
    ///     let name = match i with | 0 -> "Opening" | 1 -> "Closing" | 2 -> "Current" | _ -> "Unknown"
    ///     sprintf "%s - %s" date name
    ///
    /// this.VerifyTickerSnapshots "SOFI" expectedSnapshots actualSnapshots getDescription
    ///
    /// This will:
    /// - Compare all expected vs actual snapshots
    /// - Log success/failure for each snapshot
    /// - Assert that all snapshots match
    /// - Show Options and Realized values in success messages
    /// </summary>
    member _.VerifyTickerSnapshots
        (snapshotName: string)
        (expected: TickerCurrencySnapshot list)
        (actual: TickerCurrencySnapshot list)
        (getDescription: int -> string)
        : unit =

        let results = TestVerifications.verifyTickerCurrencySnapshotList expected actual

        results
        |> List.iteri (fun i (allMatch, fieldResults) ->
            let description = getDescription i

            if not allMatch then
                CoreLogger.logError
                    "[Verification]"
                    (sprintf
                        "❌ %s Snapshot %d (%s) failed:\n%s"
                        snapshotName
                        (i + 1)
                        description
                        (fieldResults
                         |> List.filter (fun r -> not r.Match)
                         |> TestVerifications.formatValidationResults))
            else
                // Extract key fields for success message
                let options = fieldResults |> List.find (fun r -> r.Field = "Options")
                let realized = fieldResults |> List.find (fun r -> r.Field = "Realized")

                let message =
                    if i = 0 then
                        sprintf "✅ %s Snapshot %d verified: Options=$%s" snapshotName (i + 1) options.Actual
                    elif description.Contains("current") || description.Contains("Current") then
                        sprintf "✅ %s Snapshot %d verified: Options=$%s (current)" snapshotName (i + 1) options.Actual
                    else
                        sprintf
                            "✅ %s Snapshot %d verified: Options=$%s, Realized=$%s"
                            snapshotName
                            (i + 1)
                            options.Actual
                            realized.Actual

                CoreLogger.logInfo "[Verification]" message

            Assert.That(
                allMatch,
                Is.True,
                sprintf "%s Snapshot %d (%s) verification failed" snapshotName (i + 1) description
            ))

        CoreLogger.logInfo
            "[Verification]"
            (sprintf "✅ All %d %s ticker snapshots verified chronologically" results.Length snapshotName)

    /// <summary>
    /// Verify and assert a list of BrokerFinancialSnapshots with standardized logging.
    ///
    /// USAGE:
    /// ------
    /// let getDescription i =
    ///     let date = expectedSnapshots.[i].Date.ToString("yyyy-MM-dd")
    ///     let name = match i with | 0 -> "First deposit" | 1 -> "Second deposit" | _ -> "Unknown"
    ///     sprintf "%s - %s" date name
    ///
    /// this.VerifyBrokerSnapshots expectedSnapshots actualSnapshots getDescription
    ///
    /// This will:
    /// - Compare all expected vs actual snapshots
    /// - Log success/failure for each snapshot
    /// - Assert that all snapshots match
    /// - Show Deposited, Options, and Realized values in success messages
    /// - Log ALL fields when verification fails (for debugging)
    /// </summary>
    member _.VerifyBrokerSnapshots
        (expected: BrokerFinancialSnapshot list)
        (actual: BrokerFinancialSnapshot list)
        (getDescription: int -> string)
        : unit =

        let results = TestVerifications.verifyBrokerFinancialSnapshotList expected actual

        results
        |> List.iteri (fun i (allMatch, fieldResults) ->
            let description = getDescription i

            if not allMatch then
                CoreLogger.logError
                    "[Verification]"
                    (sprintf
                        "❌ BrokerSnapshot %d (%s) failed:\n%s"
                        (i + 1)
                        description
                        (fieldResults
                         |> List.filter (fun r -> not r.Match)
                         |> TestVerifications.formatValidationResults))

                // Also log ALL fields for debugging
                CoreLogger.logInfo
                    "[Verification]"
                    (sprintf
                        "All fields for BrokerSnapshot %d (%s):\n%s"
                        (i + 1)
                        description
                        (TestVerifications.formatValidationResults fieldResults))
            else
                let deposited = fieldResults |> List.find (fun r -> r.Field = "Deposited")
                let optionsIncome = fieldResults |> List.find (fun r -> r.Field = "OptionsIncome")
                let realizedGains = fieldResults |> List.find (fun r -> r.Field = "RealizedGains")

                let message =
                    sprintf
                        "✅ BrokerSnapshot %d (%s) verified: Deposited=$%s, Options=$%s, Realized=$%s"
                        (i + 1)
                        description
                        deposited.Actual
                        optionsIncome.Actual
                        realizedGains.Actual

                CoreLogger.logInfo "[Verification]" message

            Assert.That(allMatch, Is.True, sprintf "BrokerSnapshot %d (%s) verification failed" (i + 1) description))

        CoreLogger.logInfo
            "[Verification]"
            (sprintf "✅ All %d BrokerAccount snapshots verified chronologically" results.Length)
