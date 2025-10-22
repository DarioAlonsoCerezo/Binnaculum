namespace Core.Tests.Integration

open NUnit.Framework
open System
open Binnaculum.Core.Models
open Binnaculum.Core.UI

/// <summary>
/// BrokerAccount + Deposit reactive integration tests.
/// Validates that BrokerAccount creation followed by deposit movement
/// triggers expected reactive signals and updates related collections.
///
/// Mirrors Core.Platform.MauiTester's "RunBrokerAccountDepositReactiveTestButton" test.
/// Demonstrates the Setup/Expect/Execute/Wait/Verify pattern for multi-phase operations.
///
/// Inherits from ReactiveTestFixtureBase - no setup/teardown boilerplate needed.
///
/// See README.md for pattern documentation and more examples.
/// See PATTERN_GUIDE.fs for detailed implementation guide.
/// </summary>
[<TestFixture>]
type ReactiveBrokerAccountDepositTests() =
    inherit ReactiveTestFixtureBase()

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
    [<Test>]
    [<Category("Integration")>]
    member this.``BrokerAccount creation with deposit updates collections``() =
        async {
            printfn "\n=== TEST: BrokerAccount Creation with Deposit Updates Collections ==="

            let actions = this.Actions

            // ==================== PHASE 1: SETUP ====================
            ReactiveTestSetup.printPhaseHeader 1 "Database Initialization"

            // Wipe all data for clean slate
            let! (ok, _, error) = actions.wipeDataForTesting ()
            Assert.That(ok, Is.True, sprintf "Wipe should succeed: %A" error)
            printfn "✅ Data wiped successfully"

            // Initialize database (includes schema init and data loading)
            let! (ok, _, error) = actions.initDatabase ()
            Assert.That(ok, Is.True, sprintf "Database initialization should succeed: %A" error)
            printfn "✅ Database initialized successfully"

            // ==================== PHASE 2: CREATE BROKER ACCOUNT ====================
            ReactiveTestSetup.printPhaseHeader 2 "Create BrokerAccount"

            // EXPECT: Declare expected signals BEFORE operation
            ReactiveStreamObserver.expectSignals (
                [ Accounts_Updated // Account added to Collections.Accounts
                  Snapshots_Updated ] // Snapshot calculated in Collections.Snapshots
            )

            printfn "🎯 Expecting signals: Accounts_Updated, Snapshots_Updated"

            // EXECUTE: Create account
            let! (ok, details, error) = actions.createBrokerAccount ("Trading")
            Assert.That(ok, Is.True, sprintf "Account creation should succeed: %s - %A" details error)
            printfn "✅ BrokerAccount created: %s" details

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            printfn "⏳ Waiting for account creation reactive signals..."
            let! signalsReceived = ReactiveStreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.That(signalsReceived, Is.True, "Account creation signals should have been received")
            printfn "✅ Account creation signals received successfully"

            // ==================== PHASE 3: CREATE DEPOSIT MOVEMENT ====================
            ReactiveTestSetup.printPhaseHeader 3 "Create Deposit Movement"

            // EXPECT: Declare expected signals BEFORE operation
            ReactiveStreamObserver.expectSignals (
                [ Movements_Updated // Movement added to Collections.Movements
                  Snapshots_Updated ] // Snapshot recalculated with deposit
            )

            printfn "🎯 Expecting signals: Movements_Updated, Snapshots_Updated"

            // EXECUTE: Create deposit movement (historical, -30 days)
            let! (ok, details, error) =
                actions.createMovement (5000m, BrokerMovementType.Deposit, -30, "Reactive deposit test")

            Assert.That(ok, Is.True, sprintf "Deposit creation should succeed: %s - %A" details error)
            printfn "✅ Deposit movement created: %s" details

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            printfn "⏳ Waiting for deposit creation reactive signals..."
            let! signalsReceived = ReactiveStreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.That(signalsReceived, Is.True, "Deposit creation signals should have been received")
            printfn "✅ Deposit creation signals received successfully"

            // ==================== PHASE 4: VERIFY ====================
            ReactiveTestSetup.printPhaseHeader 4 "Verify Final State"

            // Verify account was created
            let! (verified, count, error) = actions.verifyAccountCount (1)
            Assert.That(verified, Is.True, sprintf "Account count verification should succeed: %s - %A" count error)

            Assert.That(
                count,
                Is.EqualTo("Account count: expected=1, actual=1"),
                sprintf "Should have exactly 1 account, but got: %s" count
            )

            printfn "✅ Account count verified: 1"

            // Verify movement was created
            let! (verified, count, error) = actions.verifyMovementCount (1)
            Assert.That(verified, Is.True, sprintf "Movement count verification should succeed: %s - %A" count error)

            Assert.That(
                count,
                Is.EqualTo("Movement count: expected=1, actual=1"),
                sprintf "Should have exactly 1 movement, but got: %s" count
            )

            printfn "✅ Movement count verified: 1"

            // Verify snapshots were calculated
            let! (verified, count, error) = actions.verifySnapshotCount (1)
            Assert.That(verified, Is.True, sprintf "Snapshot count verification should succeed: %s - %A" count error)
            printfn "✅ Snapshot count verified: >= 1 (%s)" count

            // ==================== SUMMARY ====================
            ReactiveTestSetup.printTestCompletionSummary
                "BrokerAccount + Deposit Creation"
                "Successfully created BrokerAccount, added deposit movement, received all signals, and verified state in Collections"

            printfn "=== TEST COMPLETED SUCCESSFULLY ==="
        }
