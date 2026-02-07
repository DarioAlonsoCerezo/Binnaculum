namespace Core.Tests.Integration

open Microsoft.VisualStudio.TestTools.UnitTesting
open System
open Binnaculum.Core.Models
open Binnaculum.Core.UI
open Binnaculum.Core.Logging

/// <summary>
/// Example reactive integration test - Overview validation.
///
/// Mirrors Core.Platform.MauiTester's "Overview Reactive Validation" test.
/// Demonstrates the Setup/Expect/Execute/Wait/Verify pattern.
///
/// Inherits from TestFixtureBase - no setup/teardown boilerplate needed.
///
/// See README.md for pattern documentation and more examples.
/// See PATTERN_GUIDE.fs for detailed implementation guide.
/// </summary>
[<TestClass>]
type OverviewTests() =
    inherit TestFixtureBase()

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
    [<TestMethod>]
    [<TestCategory("Integration")>]
    member this.``Overview reactive validation``() =
        async {
            CoreLogger.logInfo "Test" "=== TEST: Overview Reactive Validation ==="

            let actions = this.Actions

            // ==================== PHASE 1: DATABASE INITIALIZATION ====================
            TestSetup.printPhaseHeader 1 "Database Initialization and Data Loading"

            // Expect signals for database init and data loading
            let expectedSignals =
                [ Brokers_Updated
                  Currencies_Updated
                  Tickers_Updated
                  Accounts_Updated
                  Snapshots_Updated ]

            // Initialize database with signal verification
            let! signalsReceived =
                TestSetup.initializeDatabaseAndVerifySignals actions expectedSignals (TimeSpan.FromSeconds(10.0))

            Assert.IsTrue(signalsReceived, "All database initialization and data loading signals should be received")

            // ==================== PHASE 2: VERIFY COLLECTIONS ====================
            TestSetup.printPhaseHeader 2 "Verify Collections"

            // Run all standard verifications using the verification module
            let verifications = TestVerifications.verifyFullDatabaseState ()

            // Assert all verifications passed
            for (success, message) in verifications do
                Assert.IsTrue(success, message)
                CoreLogger.logInfo "Verification" (sprintf "✅ %s" message)

            // Additional detailed verifications
            let (brokerSuccess, brokerMsg) = TestVerifications.verifyBrokers 2
            Assert.IsTrue(brokerSuccess, brokerMsg)

            let (currencySuccess, currencyMsg) = TestVerifications.verifyCurrencies 2
            Assert.IsTrue(currencySuccess, currencyMsg)

            let (tickerSuccess, tickerMsg) = TestVerifications.verifyTickers 1
            Assert.IsTrue(tickerSuccess, tickerMsg)

            let (accountSuccess, accountMsg) = TestVerifications.verifyAccounts 0
            Assert.IsTrue(accountSuccess, accountMsg)

            let (snapshotSuccess, snapshotMsg) = TestVerifications.verifySnapshots 0
            Assert.IsTrue(snapshotSuccess, snapshotMsg)

            // ==================== SUMMARY ====================
            TestSetup.printPhaseHeader 3 "Test Summary"

            let (stateSuccess, stateSummary) = TestVerifications.verifyCollectionsState ()

            CoreLogger.logInfo "Test" stateSummary

            TestSetup.printTestCompletionSummary
                "Overview Reactive Validation"
                (sprintf "Verified all signals and collections: %s" stateSummary)
        } |> Async.StartAsTask :> System.Threading.Tasks.Task
