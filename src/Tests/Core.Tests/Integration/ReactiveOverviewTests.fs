namespace Core.Tests.Integration

open NUnit.Framework
open System
open Binnaculum.Core.Models
open Binnaculum.Core.UI

/// <summary>
/// Reactive integration tests using signal-based approach.
/// Mirrors the MAUI tester's "Overview Reactive Validation" test.
/// Validates the core library's reactive stream emissions during Overview initialization and data loading.
///
/// Inherits from ReactiveTestFixtureBase to reuse setup/teardown logic.
/// </summary>
[<TestFixture>]
type ReactiveOverviewTests() =
    inherit ReactiveTestFixtureBase()

    /// <summary>
    /// Test: Overview Reactive Validation
    /// Mirrors Core.Platform.MauiTester's "Overview Reactive Validation" test.
    ///
    /// This is the single comprehensive test that validates:
    /// 1. Database initialization loads brokers, currencies, and tickers reactively
    /// 2. LoadData() fires Accounts_Updated and Snapshots_Updated signals
    /// 3. Collections contain expected data: Brokers, Currencies, Tickers, and Snapshots
    ///
    /// The test uses signal-based verification instead of timing-based assertions,
    /// making it robust and portable across platforms.
    /// </summary>
    [<Test>]
    [<Category("Integration")>]
    member this.``Overview reactive validation``() =
        async {
            printfn "\n=== TEST: Overview Reactive Validation ==="

            let actions = this.Actions

            // ==================== PHASE 1: DATABASE INITIALIZATION ====================
            ReactiveTestSetup.printPhaseHeader 1 "Database Initialization and Data Loading"

            // Expect signals for database init and data loading
            let expectedSignals =
                [ Brokers_Updated
                  Currencies_Updated
                  Tickers_Updated
                  Accounts_Updated
                  Snapshots_Updated ]

            // Initialize database with signal verification
            let! signalsReceived =
                ReactiveTestSetup.initializeDatabaseAndVerifySignals
                    actions
                    expectedSignals
                    (TimeSpan.FromSeconds(10.0))

            Assert.That(
                signalsReceived,
                Is.True,
                "All database initialization and data loading signals should be received"
            )

            // ==================== PHASE 2: VERIFY COLLECTIONS ====================
            ReactiveTestSetup.printPhaseHeader 2 "Verify Collections"

            // Run all standard verifications using the verification module
            let verifications = ReactiveTestVerifications.verifyFullDatabaseState ()

            // Assert all verifications passed
            for (success, message) in verifications do
                Assert.That(success, Is.True, message)
                printfn "✅ %s" message

            // Additional detailed verifications
            let (brokerSuccess, brokerMsg) = ReactiveTestVerifications.verifyBrokers 2
            Assert.That(brokerSuccess, Is.True, brokerMsg)

            let (currencySuccess, currencyMsg) = ReactiveTestVerifications.verifyCurrencies 2
            Assert.That(currencySuccess, Is.True, currencyMsg)

            let (tickerSuccess, tickerMsg) = ReactiveTestVerifications.verifyTickers 1
            Assert.That(tickerSuccess, Is.True, tickerMsg)

            let (accountSuccess, accountMsg) = ReactiveTestVerifications.verifyAccounts 0
            Assert.That(accountSuccess, Is.True, accountMsg)

            let (snapshotSuccess, snapshotMsg) = ReactiveTestVerifications.verifySnapshots 0
            Assert.That(snapshotSuccess, Is.True, snapshotMsg)

            // ==================== SUMMARY ====================
            ReactiveTestSetup.printPhaseHeader 3 "Test Summary"

            let (stateSuccess, stateSummary) =
                ReactiveTestVerifications.verifyCollectionsState ()

            printfn "%s" stateSummary

            ReactiveTestSetup.printTestCompletionSummary
                "Overview Reactive Validation"
                (sprintf "Verified all signals and collections: %s" stateSummary)
        }
