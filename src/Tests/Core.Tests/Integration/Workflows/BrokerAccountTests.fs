namespace Core.Tests.Integration

open NUnit.Framework
open System
open Binnaculum.Core.Models
open Binnaculum.Core.UI
open Binnaculum.Core.Logging

/// <summary>
/// BrokerAccount reactive integration tests.
/// Validates that BrokerAccount creation triggers expected reactive signals
/// and updates related collections.
///
/// Mirrors Core.Platform.MauiTester's "RunBrokerAccountReactiveTestButton" test.
/// Demonstrates the Setup/Expect/Execute/Wait/Verify pattern for BrokerAccount operations.
///
/// Inherits from TestFixtureBase - no setup/teardown boilerplate needed.
///
/// See README.md for pattern documentation and more examples.
/// See PATTERN_GUIDE.fs for detailed implementation guide.
/// </summary>
[<TestFixture>]
type BrokerAccountTests() =
    inherit TestFixtureBase()

    /// <summary>
    /// Test: BrokerAccount creation updates collections
    /// Mirrors Core.Platform.MauiTester's "RunBrokerAccountReactiveTestButton" test.
    ///
    /// This test validates:
    /// 1. Database initialization completes successfully
    /// 2. BrokerAccount creation triggers Accounts_Updated and Snapshots_Updated signals
    /// 3. Account is correctly added to Collections.Accounts
    /// 4. Snapshot is automatically calculated and added to Collections.Snapshots
    ///
    /// The test uses signal-based verification instead of timing-based assertions,
    /// making it robust and portable across platforms.
    /// </summary>
    [<Test>]
    [<Category("Integration")>]
    member this.``BrokerAccount creation updates collections``() =
        async {
            CoreLogger.logInfo "Test" "=== TEST: BrokerAccount Creation Updates Collections ==="

            let actions = this.Actions

            // ==================== PHASE 1: SETUP ====================
            TestSetup.printPhaseHeader 1 "Database Initialization"

            // Wipe all data for clean slate
            let! (ok, _, error) = actions.wipeDataForTesting ()
            Assert.That(ok, Is.True, sprintf "Wipe should succeed: %A" error)
            CoreLogger.logInfo "[TestSetup]" "✅ Data wiped successfully"

            // Initialize database (includes schema init and data loading)
            let! (ok, _, error) = actions.initDatabase ()
            Assert.That(ok, Is.True, sprintf "Database initialization should succeed: %A" error)
            CoreLogger.logInfo "[TestSetup]" "✅ Database initialized successfully"

            // ==================== PHASE 2: CREATE BROKER ACCOUNT ====================
            TestSetup.printPhaseHeader 2 "Create BrokerAccount"

            // EXPECT: Declare expected signals BEFORE operation
            StreamObserver.expectSignals (
                [ Accounts_Updated // Account added to Collections.Accounts
                  Snapshots_Updated ] // Snapshot calculated in Collections.Snapshots
            )

            CoreLogger.logDebug "StreamObserver" "🎯 Expecting signals: Accounts_Updated, Snapshots_Updated"

            // EXECUTE: Create account
            let! (ok, details, error) = actions.createBrokerAccount ("TestAccount")
            Assert.That(ok, Is.True, sprintf "Account creation should succeed: %s - %A" details error)
            CoreLogger.logInfo "TestActions" (sprintf "✅ BrokerAccount created: %s" details)

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            CoreLogger.logInfo "TestActions" "⏳ Waiting for reactive signals..."
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.That(signalsReceived, Is.True, "Expected signals should have been received")
            CoreLogger.logInfo "StreamObserver" "✅ All signals received successfully"

            // ==================== PHASE 3: VERIFY ====================
            TestSetup.printPhaseHeader 3 "Verify Account Created"

            // Verify account was created
            let! (verified, count, error) = actions.verifyAccountCount (1)
            Assert.That(verified, Is.True, sprintf "Account count verification should succeed: %s - %A" count error)

            Assert.That(
                count,
                Is.EqualTo("Account count: expected=1, actual=1"),
                sprintf "Should have exactly 1 account, but got: %s" count
            )

            CoreLogger.logInfo "Verification" "✅ Account count verified: 1"

            // ==================== SUMMARY ====================
            TestSetup.printTestCompletionSummary
                "BrokerAccount Creation"
                "Successfully created BrokerAccount, received all signals, and verified account in Collections"

            CoreLogger.logInfo "Test" "=== TEST COMPLETED SUCCESSFULLY ==="
        }
