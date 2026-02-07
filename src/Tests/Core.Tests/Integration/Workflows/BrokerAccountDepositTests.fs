namespace Core.Tests.Integration

open Microsoft.VisualStudio.TestTools.UnitTesting
open System
open Binnaculum.Core.Models
open Binnaculum.Core.UI
open Binnaculum.Core.Logging

/// <summary>
/// BrokerAccount + Deposit reactive integration tests.
/// Validates that BrokerAccount creation followed by deposit movement
/// triggers expected reactive signals and updates related collections.
///
/// Mirrors Core.Platform.MauiTester's "RunBrokerAccountDepositReactiveTestButton" test.
/// Demonstrates the Setup/Expect/Execute/Wait/Verify pattern for multi-phase operations.
///
/// Inherits from TestFixtureBase - no setup/teardown boilerplate needed.
///
/// See README.md for pattern documentation and more examples.
/// See PATTERN_GUIDE.fs for detailed implementation guide.
/// </summary>
[<TestClass>]
type BrokerAccountDepositTests() =
    inherit TestFixtureBase()

    /// <summary>
    /// Test: BrokerAccount creation with deposit updates collections
    /// Mirrors Core.Platform.MauiTester's "RunBrokerAccountDepositReactiveTestButton" test.
    ///
    /// This test validates:
    /// 1. Database initialization completes successfully
    /// 2. BrokerAccount creation triggers Accounts_Updated and Snapshots_Updated signals
    /// 3. Deposit movement creation triggers Movements_Updated and Snapshots_Updated signals
    /// 4. Account is correctly added to Collections.Accounts
    /// 5. Movement is added to Collections.Movements
    /// 6. Snapshots are recalculated with deposit included
    ///
    /// The test uses signal-based verification instead of timing-based assertions,
    /// making it robust and portable across platforms. This is a two-phase test:
    /// - Phase 1: Create account and wait for account signals
    /// - Phase 2: Create deposit and wait for movement signals
    /// </summary>
    [<TestMethod>]
    [<TestCategory("Integration")>]
    member this.``BrokerAccount creation with deposit updates collections``() =
        async {
            CoreLogger.logInfo "Test" "=== TEST: BrokerAccount Creation with Deposit Updates Collections ==="

            let actions = this.Actions

            // ==================== PHASE 1: SETUP ====================
            TestSetup.printPhaseHeader 1 "Database Initialization"

            // Wipe all data for clean slate
            let! (ok, _, error) = actions.wipeDataForTesting ()
            Assert.IsTrue(ok, sprintf "Wipe should succeed: %A" error)
            CoreLogger.logInfo "TestSetup" "✅ Data wiped successfully"

            // Initialize database (includes schema init and data loading)
            let! (ok, _, error) = actions.initDatabase ()
            Assert.IsTrue(ok, sprintf "Database initialization should succeed: %A" error)
            CoreLogger.logInfo "TestSetup" "✅ Database initialized successfully"

            // ==================== PHASE 2: CREATE BROKER ACCOUNT ====================
            TestSetup.printPhaseHeader 2 "Create BrokerAccount"

            // EXPECT: Declare expected signals BEFORE operation
            StreamObserver.expectSignals (
                [ Accounts_Updated // Account added to Collections.Accounts
                  Snapshots_Updated ] // Snapshot calculated in Collections.Snapshots
            )

            CoreLogger.logDebug "StreamObserver" "🎯 Expecting signals: Accounts_Updated, Snapshots_Updated"

            // EXECUTE: Create account
            let! (ok, details, error) = actions.createBrokerAccount ("Trading")
            Assert.IsTrue(ok, sprintf "Account creation should succeed: %s - %A" details error)
            CoreLogger.logInfo "TestActions" (sprintf "✅ BrokerAccount created: %s" details)

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            CoreLogger.logInfo "TestActions" "⏳ Waiting for account creation reactive signals..."
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.IsTrue(signalsReceived, "Account creation signals should have been received")
            CoreLogger.logInfo "StreamObserver" "✅ Account creation signals received successfully"

            // ==================== PHASE 3: CREATE DEPOSIT MOVEMENT ====================
            TestSetup.printPhaseHeader 3 "Create Deposit Movement"

            // EXPECT: Declare expected signals BEFORE operation
            StreamObserver.expectSignals (
                [ Movements_Updated // Movement added to Collections.Movements
                  Snapshots_Updated ] // Snapshot recalculated with deposit
            )

            CoreLogger.logDebug "StreamObserver" "🎯 Expecting signals: Movements_Updated, Snapshots_Updated"

            // EXECUTE: Create deposit movement (historical, -30 days)
            let! (ok, details, error) =
                actions.createMovement (5000m, BrokerMovementType.Deposit, -30, "Reactive deposit test")

            Assert.IsTrue(ok, sprintf "Deposit creation should succeed: %s - %A" details error)
            CoreLogger.logInfo "TestActions" (sprintf "✅ Deposit movement created: %s" details)

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            CoreLogger.logInfo "TestActions" "⏳ Waiting for deposit creation reactive signals..."
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.IsTrue(signalsReceived, "Deposit creation signals should have been received")
            CoreLogger.logInfo "StreamObserver" "✅ Deposit creation signals received successfully"

            // ==================== PHASE 4: VERIFY ====================
            TestSetup.printPhaseHeader 4 "Verify Final State"

            // Verify account was created
            let! (verified, count, error) = actions.verifyAccountCount (1)
            Assert.IsTrue(verified, sprintf "Account count verification should succeed: %s - %A" count error)

            Assert.AreEqual("Account count: expected=1, actual=1", count, sprintf "Should have exactly 1 account, but got: %s" count)

            CoreLogger.logInfo "Verification" "✅ Account count verified: 1"

            // Verify movement was created
            let! (verified, count, error) = actions.verifyMovementCount (1)
            Assert.IsTrue(verified, sprintf "Movement count verification should succeed: %s - %A" count error)

            Assert.AreEqual("Movement count: expected=1, actual=1", count, sprintf "Should have exactly 1 movement, but got: %s" count)

            CoreLogger.logInfo "Verification" "✅ Movement count verified: 1"

            // Verify snapshots were calculated
            let! (verified, count, error) = actions.verifySnapshotCount (1)
            Assert.IsTrue(verified, sprintf "Snapshot count verification should succeed: %s - %A" count error)
            CoreLogger.logInfo "Verification" (sprintf "✅ Snapshot count verified: >= 1 (%s)" count)

            // ==================== SUMMARY ====================
            TestSetup.printTestCompletionSummary
                "BrokerAccount + Deposit Creation"
                "Successfully created BrokerAccount, added deposit movement, received all signals, and verified state in Collections"

            CoreLogger.logInfo "Test" "=== TEST COMPLETED SUCCESSFULLY ==="
        }
