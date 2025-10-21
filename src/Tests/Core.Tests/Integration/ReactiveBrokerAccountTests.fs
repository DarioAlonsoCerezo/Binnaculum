namespace Core.Tests.Integration

open NUnit.Framework
open System
open Binnaculum.Core.Models
open Binnaculum.Core.UI

/// <summary>
/// BrokerAccount reactive integration tests.
/// Validates that BrokerAccount creation triggers expected reactive signals
/// and updates related collections.
///
/// Mirrors Core.Platform.MauiTester's "RunBrokerAccountReactiveTestButton" test.
/// Demonstrates the Setup/Expect/Execute/Wait/Verify pattern for BrokerAccount operations.
///
/// Inherits from ReactiveTestFixtureBase - no setup/teardown boilerplate needed.
///
/// See README.md for pattern documentation and more examples.
/// See PATTERN_GUIDE.fs for detailed implementation guide.
/// </summary>
[<TestFixture>]
type ReactiveBrokerAccountTests() =
    inherit ReactiveTestFixtureBase()

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
            printfn "\n=== TEST: BrokerAccount Creation Updates Collections ==="

            let actions = this.Actions

            // ==================== PHASE 1: SETUP ====================
            ReactiveTestSetup.printPhaseHeader 1 "Database Initialization"

            // Wipe all data for clean slate
            let! (ok, _, error) = actions.wipeDataForTesting()
            Assert.That(ok, Is.True, sprintf "Wipe should succeed: %A" error)
            printfn "✅ Data wiped successfully"

            // Initialize database
            let! (ok, _, error) = actions.initDatabase()
            Assert.That(ok, Is.True, sprintf "Database initialization should succeed: %A" error)
            printfn "✅ Database initialized successfully"

            // Load data
            let! (ok, _, error) = actions.loadData()
            Assert.That(ok, Is.True, sprintf "Data loading should succeed: %A" error)
            printfn "✅ Data loaded successfully"

            // ==================== PHASE 2: CREATE BROKER ACCOUNT ====================
            ReactiveTestSetup.printPhaseHeader 2 "Create BrokerAccount"

            // EXPECT: Declare expected signals BEFORE operation
            ReactiveStreamObserver.expectSignals([
                Accounts_Updated      // Account added to Collections.Accounts
                Snapshots_Updated     // Snapshot calculated in Collections.Snapshots
            ])
            printfn "🎯 Expecting signals: Accounts_Updated, Snapshots_Updated"

            // EXECUTE: Create account
            let! (ok, details, error) = actions.createBrokerAccount("TestAccount")
            Assert.That(ok, Is.True, sprintf "Account creation should succeed: %s - %A" details error)
            printfn "✅ BrokerAccount created: %s" details

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            printfn "⏳ Waiting for reactive signals..."
            let! signalsReceived = ReactiveStreamObserver.waitForAllSignalsAsync(TimeSpan.FromSeconds(10.0))
            Assert.That(signalsReceived, Is.True, "Expected signals should have been received")
            printfn "✅ All signals received successfully"

            // ==================== PHASE 3: VERIFY ====================
            ReactiveTestSetup.printPhaseHeader 3 "Verify Account Created"

            // Verify account was created
            let! (verified, count, error) = actions.verifyAccountCount(1)
            Assert.That(verified, Is.True, sprintf "Account count verification should succeed: %s - %A" count error)
            Assert.That(count, Is.EqualTo("Account count: expected=1, actual=1"), 
                sprintf "Should have exactly 1 account, but got: %s" count)
            printfn "✅ Account count verified: 1"

            // ==================== SUMMARY ====================
            ReactiveTestSetup.printTestCompletionSummary
                "BrokerAccount Creation"
                "Successfully created BrokerAccount, received all signals, and verified account in Collections"

            printfn "=== TEST COMPLETED SUCCESSFULLY ==="
        }
