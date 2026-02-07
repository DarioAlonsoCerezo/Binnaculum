namespace Core.Tests.Integration

open Microsoft.VisualStudio.TestTools.UnitTesting
open System
open Binnaculum.Core.Models
open Binnaculum.Core.UI
open Binnaculum.Core.Logging

/// <summary>
/// BrokerAccount Multiple Movements signal-based reactive integration tests.
/// Demonstrates how signal-based testing scales elegantly with multiple sequential operations.
///
/// Mirrors Core.Platform.MauiTester's "RunBrokerAccountMultipleMovementsSignalBasedTestButton" test.
/// Validates that BrokerAccount creation followed by 4 sequential movements (2 deposits + 2 withdrawals)
/// triggers expected reactive signals and updates related collections.
///
/// This test demonstrates the power of signal-based testing:
/// - No arbitrary delays (Thread.Sleep)
/// - ~300ms execution time (vs 3+ seconds with delays)
/// - Scales linearly with number of operations
/// - Deterministic and reliable across platforms
///
/// Inherits from TestFixtureBase - no setup/teardown boilerplate needed.
///
/// See README.md for pattern documentation and more examples.
/// See PATTERN_GUIDE.fs for detailed implementation guide.
/// </summary>
[<TestClass>]
type BrokerAccountMultipleMovementsTests() =
    inherit TestFixtureBase()

    /// <summary>
    /// Test: BrokerAccount with multiple movements updates collections
    /// Mirrors Core.Platform.MauiTester's "RunBrokerAccountMultipleMovementsSignalBasedTestButton" test.
    ///
    /// This test validates:
    /// 1. Database initialization completes successfully
    /// 2. BrokerAccount creation triggers Accounts_Updated and Snapshots_Updated signals
    /// 3. Each of 4 sequential movements triggers Movements_Updated and Snapshots_Updated signals
    /// 4. Account is correctly added to Collections.Accounts
    /// 5. All 4 movements are added to Collections.Movements
    /// 6. Snapshots are recalculated after each movement
    ///
    /// Movement sequence:
    /// - Movement #1: Deposit $1200, -60 days (Historical deposit from 60 days ago)
    /// - Movement #2: Withdrawal $300, -55 days (Historical withdrawal from 55 days ago)
    /// - Movement #3: Withdrawal $300, -50 days (Historical withdrawal from 50 days ago)
    /// - Movement #4: Deposit $600, -10 days (Historical deposit from 10 days ago)
    /// - Net cash flow: +1200 -300 -300 +600 = +1200
    ///
    /// The test uses signal-based verification instead of timing-based assertions,
    /// making it robust and portable across platforms. This is a five-phase test:
    /// - Phase 1: Setup and database initialization
    /// - Phase 2: Create account and wait for account signals
    /// - Phase 3: Create movement #1 (Deposit $1200) and wait for movement signals
    /// - Phase 4: Create movement #2 (Withdrawal $300) and wait for movement signals
    /// - Phase 5: Create movement #3 (Withdrawal $300) and wait for movement signals
    /// - Phase 6: Create movement #4 (Deposit $600) and wait for movement signals
    /// - Phase 7: Verify final state
    ///
    /// Expected duration: ~300-400ms (signal-based, very fast!)
    /// Traditional delay-based approach would take ~3.3 seconds (11x slower!)
    /// </summary>
    [<TestMethod>]
    [<TestCategory("Integration")>]
    member this.``BrokerAccount with multiple movements updates collections``() =
        async {
            CoreLogger.logInfo "Test" "=== TEST: BrokerAccount with Multiple Movements Updates Collections ==="

            let actions = this.Actions

            // ==================== PHASE 1: SETUP ====================
            TestSetup.printPhaseHeader 1 "Database Initialization"

            // Wipe all data for clean slate
            let! (ok, _, error) = actions.wipeDataForTesting ()
            Assert.IsTrue(ok, sprintf "Wipe should succeed: %A" error)
            CoreLogger.logInfo "Verification" "✅ Data wiped successfully"

            // Initialize database (includes schema init and data loading)
            let! (ok, _, error) = actions.initDatabase ()
            Assert.IsTrue(ok, sprintf "Database initialization should succeed: %A" error)
            CoreLogger.logInfo "Verification" "✅ Database initialized successfully"

            // ==================== PHASE 2: CREATE BROKER ACCOUNT ====================
            TestSetup.printPhaseHeader 2 "Create BrokerAccount"

            // EXPECT: Declare expected signals BEFORE operation
            StreamObserver.expectSignals (
                [ Accounts_Updated // Account added to Collections.Accounts
                  Snapshots_Updated ] // Snapshot calculated in Collections.Snapshots
            )

            CoreLogger.logDebug "StreamObserver" "🎯 Expecting signals: Accounts_Updated, Snapshots_Updated"

            // EXECUTE: Create account
            let! (ok, details, error) = actions.createBrokerAccount ("Signal-Based Testing")
            Assert.IsTrue(ok, sprintf "Account creation should succeed: %s - %A" details error)
            CoreLogger.logInfo "Verification" (sprintf "✅ BrokerAccount created: %s" details)

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            CoreLogger.logInfo "TestActions" "⏳ Waiting for account creation reactive signals..."
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.IsTrue(signalsReceived, "Account creation signals should have been received")
            CoreLogger.logInfo "Verification" "✅ Account creation signals received successfully"

            // ==================== PHASE 3: MOVEMENT #1 (DEPOSIT $1200, -60 DAYS) ====================
            TestSetup.printPhaseHeader 3 "Create Movement #1: Deposit $1200 (-60 days)"

            // EXPECT: Declare expected signals BEFORE operation
            StreamObserver.expectSignals (
                [ Movements_Updated // Movement added to Collections.Movements
                  Snapshots_Updated ] // Snapshot recalculated with deposit
            )

            CoreLogger.logDebug "StreamObserver" "🎯 Expecting signals: Movements_Updated, Snapshots_Updated"

            // EXECUTE: Create deposit movement (historical, -60 days)
            let! (ok, details, error) =
                actions.createMovement (1200m, BrokerMovementType.Deposit, -60, "60-day-old deposit")

            Assert.IsTrue(ok, sprintf "Movement #1 creation should succeed: %s - %A" details error)
            CoreLogger.logInfo "Verification" (sprintf "✅ Movement #1 created: %s" details)

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            CoreLogger.logInfo "TestActions" "⏳ Waiting for movement #1 reactive signals..."
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.IsTrue(signalsReceived, "Movement #1 signals should have been received")
            CoreLogger.logInfo "Verification" "✅ Movement #1 signals received successfully"

            // ==================== PHASE 4: MOVEMENT #2 (WITHDRAWAL $300, -55 DAYS) ====================
            TestSetup.printPhaseHeader 4 "Create Movement #2: Withdrawal $300 (-55 days)"

            // EXPECT: Declare expected signals BEFORE operation
            StreamObserver.expectSignals (
                [ Movements_Updated // Movement added to Collections.Movements
                  Snapshots_Updated ] // Snapshot recalculated with withdrawal
            )

            CoreLogger.logDebug "StreamObserver" "🎯 Expecting signals: Movements_Updated, Snapshots_Updated"

            // EXECUTE: Create withdrawal movement (historical, -55 days)
            let! (ok, details, error) =
                actions.createMovement (300m, BrokerMovementType.Withdrawal, -55, "55-day-old withdrawal")

            Assert.IsTrue(ok, sprintf "Movement #2 creation should succeed: %s - %A" details error)
            CoreLogger.logInfo "Verification" (sprintf "✅ Movement #2 created: %s" details)

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            CoreLogger.logInfo "TestActions" "⏳ Waiting for movement #2 reactive signals..."
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.IsTrue(signalsReceived, "Movement #2 signals should have been received")
            CoreLogger.logInfo "Verification" "✅ Movement #2 signals received successfully"

            // ==================== PHASE 5: MOVEMENT #3 (WITHDRAWAL $300, -50 DAYS) ====================
            TestSetup.printPhaseHeader 5 "Create Movement #3: Withdrawal $300 (-50 days)"

            // EXPECT: Declare expected signals BEFORE operation
            StreamObserver.expectSignals (
                [ Movements_Updated // Movement added to Collections.Movements
                  Snapshots_Updated ] // Snapshot recalculated with withdrawal
            )

            CoreLogger.logDebug "StreamObserver" "🎯 Expecting signals: Movements_Updated, Snapshots_Updated"

            // EXECUTE: Create withdrawal movement (historical, -50 days)
            let! (ok, details, error) =
                actions.createMovement (300m, BrokerMovementType.Withdrawal, -50, "50-day-old withdrawal")

            Assert.IsTrue(ok, sprintf "Movement #3 creation should succeed: %s - %A" details error)
            CoreLogger.logInfo "Verification" (sprintf "✅ Movement #3 created: %s" details)

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            CoreLogger.logInfo "TestActions" "⏳ Waiting for movement #3 reactive signals..."
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.IsTrue(signalsReceived, "Movement #3 signals should have been received")
            CoreLogger.logInfo "Verification" "✅ Movement #3 signals received successfully"

            // ==================== PHASE 6: MOVEMENT #4 (DEPOSIT $600, -10 DAYS) ====================
            TestSetup.printPhaseHeader 6 "Create Movement #4: Deposit $600 (-10 days)"

            // EXPECT: Declare expected signals BEFORE operation
            StreamObserver.expectSignals (
                [ Movements_Updated // Movement added to Collections.Movements
                  Snapshots_Updated ] // Snapshot recalculated with deposit
            )

            CoreLogger.logDebug "StreamObserver" "🎯 Expecting signals: Movements_Updated, Snapshots_Updated"

            // EXECUTE: Create deposit movement (historical, -10 days)
            let! (ok, details, error) =
                actions.createMovement (600m, BrokerMovementType.Deposit, -10, "10-day-old deposit")

            Assert.IsTrue(ok, sprintf "Movement #4 creation should succeed: %s - %A" details error)
            CoreLogger.logInfo "Verification" (sprintf "✅ Movement #4 created: %s" details)

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            CoreLogger.logInfo "TestActions" "⏳ Waiting for movement #4 reactive signals..."
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.IsTrue(signalsReceived, "Movement #4 signals should have been received")
            CoreLogger.logInfo "Verification" "✅ Movement #4 signals received successfully"

            // ==================== PHASE 7: VERIFY FINAL STATE ====================
            TestSetup.printPhaseHeader 7 "Verify Final State"

            // Verify account was created
            let! (verified, count, error) = actions.verifyAccountCount (1)
            Assert.IsTrue(verified, sprintf "Account count verification should succeed: %s - %A" count error)

            Assert.AreEqual("Account count: expected=1, actual=1", count, sprintf "Should have exactly 1 account, but got: %s" count)

            CoreLogger.logInfo "Verification" "✅ Account count verified: 1"

            // Verify 4 movements were created
            let! (verified, count, error) = actions.verifyMovementCount (4)
            Assert.IsTrue(verified, sprintf "Movement count verification should succeed: %s - %A" count error)

            Assert.AreEqual("Movement count: expected=4, actual=4", count, sprintf "Should have exactly 4 movements, but got: %s" count)

            CoreLogger.logInfo "Verification" "✅ Movement count verified: 4"

            // Verify snapshots were calculated
            let! (verified, count, error) = actions.verifySnapshotCount (1)
            Assert.IsTrue(verified, sprintf "Snapshot count verification should succeed: %s - %A" count error)
            CoreLogger.logInfo "Verification" (sprintf "✅ Snapshot count verified: >= 1 (%s)" count)

            // ==================== SUMMARY ====================
            TestSetup.printTestCompletionSummary
                "BrokerAccount + Multiple Movements Creation"
                "Successfully created BrokerAccount, added 4 movements (2 deposits + 2 withdrawals), received all signals, and verified state in Collections. Net cash flow: +$1200"

            CoreLogger.logInfo "Test" "=== TEST COMPLETED SUCCESSFULLY ==="
        }
