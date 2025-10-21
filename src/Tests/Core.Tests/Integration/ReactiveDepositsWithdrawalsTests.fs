namespace Core.Tests.Integration

open NUnit.Framework
open System
open System.IO
open Binnaculum.Core.Models
open Binnaculum.Core.UI

/// <summary>
/// Deposits & Withdrawals signal-based reactive integration tests.
/// Validates money movements import with comprehensive financial snapshot validation.
///
/// This test replicates the exact workflow of the MAUI Tester's
/// "RunDepositsWithdrawalsIntegrationTestButton" test.
///
/// Inherits from ReactiveTestFixtureBase - no setup/teardown boilerplate needed.
///
/// See README.md for pattern documentation and more examples.
/// See PATTERN_GUIDE.fs for detailed implementation guide.
/// </summary>
[<TestFixture>]
type ReactiveDepositsWithdrawalsTests() =
    inherit ReactiveTestFixtureBase()

    /// <summary>
    /// Helper method to get path to embedded CSV test data
    /// </summary>
    member private _.getCsvPath(filename: string) : string =
        let testDataPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "TestData",
            "Tastytrade_Samples",
            filename)
        if not (File.Exists(testDataPath)) then
            failwith (sprintf "CSV test data not found: %s" testDataPath)
        testDataPath

    /// <summary>
    /// Test: Money movements import CSV workflow updates snapshots
    /// Mirrors Core.Platform.MauiTester's "RunDepositsWithdrawalsIntegrationTestButton" test.
    ///
    /// This test validates:
    /// 1. Database initialization completes successfully
    /// 2. BrokerAccount creation triggers Accounts_Updated and Snapshots_Updated signals
    /// 3. CSV import triggers Movements_Updated and Snapshots_Updated signals
    /// 4. 20 movements are imported (19 deposits + 1 withdrawal)
    /// 5. Financial calculations are correct (deposited, withdrawn)
    /// 6. 21 snapshots are created (1 initial + 20 from movements)
    /// 7. BrokerAccounts.GetSnapshots retrieves exactly 21 snapshots
    ///
    /// The test uses signal-based verification instead of timing-based assertions,
    /// making it robust and portable across platforms.
    ///
    /// CSV Data: TastytradeDeposits.csv
    /// - 19 deposits (various amounts totaling $19,388.40)
    /// - 1 withdrawal ($25.00)
    ///
    /// Expected Results:
    /// - 20 total movements
    /// - Total deposited: $19,388.40
    /// - Total withdrawn: $25.00
    /// - Net cash flow: $19,363.40
    /// - 21 snapshots (1 initial + 20 movements)
    /// - BrokerAccounts.GetSnapshots returns exactly 21 snapshots
    /// </summary>
    [<Test>]
    [<Category("Integration")>]
    member this.``Money movements import CSV workflow updates snapshots``() =
        async {
            printfn "\n=== TEST: Money Movements Import CSV Workflow Updates Snapshots ==="

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
            ReactiveTestSetup.printPhaseHeader 2 "Create BrokerAccount for Import"

            // EXPECT: Declare expected signals BEFORE operation
            ReactiveStreamObserver.expectSignals([
                Accounts_Updated      // Account added to Collections.Accounts
                Snapshots_Updated     // Snapshot calculated in Collections.Snapshots
            ])
            printfn "🎯 Expecting signals: Accounts_Updated, Snapshots_Updated"

            // EXECUTE: Create account
            let! (ok, details, error) = actions.createBrokerAccount("Deposits-Withdrawals-Test")
            Assert.That(ok, Is.True, sprintf "Account creation should succeed: %s - %A" details error)
            printfn "✅ BrokerAccount created: %s" details

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            printfn "⏳ Waiting for account creation reactive signals..."
            let! signalsReceived = ReactiveStreamObserver.waitForAllSignalsAsync(TimeSpan.FromSeconds(10.0))
            Assert.That(signalsReceived, Is.True, "Account creation signals should have been received")
            printfn "✅ Account creation signals received successfully"

            // ==================== PHASE 3: IMPORT DEPOSITS/WITHDRAWALS CSV ====================
            ReactiveTestSetup.printPhaseHeader 3 "Import Deposits/Withdrawals CSV File"

            // Get CSV path
            let csvPath = this.getCsvPath("TastytradeDeposits.csv")
            printfn "📄 CSV file path: %s" csvPath
            Assert.That(File.Exists(csvPath), Is.True, sprintf "CSV file should exist: %s" csvPath)

            // EXPECT: Declare expected signals BEFORE import operation
            ReactiveStreamObserver.expectSignals([
                Movements_Updated     // Money movements (deposits/withdrawals) added
                Snapshots_Updated     // Snapshots recalculated
            ])
            printfn "🎯 Expecting signals: Movements_Updated, Snapshots_Updated"

            // EXECUTE: Import CSV file
            let tastytradeId = actions.Context.TastytradeId
            let accountId = actions.Context.BrokerAccountId
            printfn "🔧 Import parameters: Tastytrade ID=%d, Account ID=%d" tastytradeId accountId
            
            let! (ok, importDetails, error) = actions.importFile(tastytradeId, accountId, csvPath)
            Assert.That(ok, Is.True, sprintf "Import should succeed: %s - %A" importDetails error)
            printfn "✅ CSV import completed: %s" importDetails

            // WAIT: Wait for import signals (longer timeout for import processing)
            printfn "⏳ Waiting for import reactive signals..."
            let! signalsReceived = ReactiveStreamObserver.waitForAllSignalsAsync(TimeSpan.FromSeconds(15.0))
            Assert.That(signalsReceived, Is.True, "Import signals should have been received")
            printfn "✅ Import signals received successfully"

            // ==================== PHASE 4: VERIFY DATA COUNTS ====================
            ReactiveTestSetup.printPhaseHeader 4 "Verify Imported Data Counts"

            // Verify movement count (19 deposits + 1 withdrawal = 20)
            let! (verified, movementCount, error) = actions.verifyMovementCount(20)
            Assert.That(verified, Is.True, sprintf "Movement count verification should succeed: %s - %A" movementCount error)
            printfn "✅ Movement count verified: 20 movements (19 deposits + 1 withdrawal)"

            // Verify Collections.Snapshots has at least 1 snapshot (latest only)
            let! (verified, snapshotCount, error) = actions.verifySnapshotCount(1)
            Assert.That(verified, Is.True, sprintf "Snapshot count verification should succeed: %s - %A" snapshotCount error)
            printfn "✅ Collections.Snapshots verified: >= 1 (%s) [Collections only contains latest snapshot]" snapshotCount

            // ==================== PHASE 5: VERIFY FINANCIAL CALCULATIONS ====================
            ReactiveTestSetup.printPhaseHeader 5 "Verify Financial Calculations"

            // Verify deposited amount ($19,388.40 total from 19 deposits)
            let! (verified, deposited, error) = actions.verifyDeposited(19388.40m)
            Assert.That(verified, Is.True, sprintf "Deposited amount verification should succeed: %s - %A" deposited error)
            printfn "✅ Deposited amount verified: $19,388.40"

            // Verify withdrawn amount ($25.00 from 1 withdrawal)
            let! (verified, withdrawn, error) = actions.verifyWithdrawn(25.00m)
            Assert.That(verified, Is.True, sprintf "Withdrawn amount verification should succeed: %s - %A" withdrawn error)
            printfn "✅ Withdrawn amount verified: $25.00"

            // Verify movement counter (should be 20)
            let! (verified, counter, error) = actions.verifyMovementCounter(20)
            Assert.That(verified, Is.True, sprintf "MovementCounter verification should succeed: %s - %A" counter error)
            printfn "✅ MovementCounter verified: 20"

            // ==================== PHASE 6: VERIFY BROKERACCOUNTS.GETSNAPSHOTS ====================
            ReactiveTestSetup.printPhaseHeader 6 "Verify BrokerAccounts.GetSnapshots"

            // Verify BrokerAccounts.GetSnapshots returns exactly 21 snapshots
            let! (verified, retrievedCount, error) = actions.verifyBrokerAccountSnapshots(accountId, 21)
            Assert.That(verified, Is.True, sprintf "BrokerAccounts.GetSnapshots verification should succeed: %s - %A" retrievedCount error)
            printfn "✅ BrokerAccounts.GetSnapshots verified: 21 snapshots retrieved"

            // ==================== SUMMARY ====================
            ReactiveTestSetup.printTestCompletionSummary
                "Money Movements Import from CSV"
                "Successfully created BrokerAccount, imported deposits/withdrawals CSV, received all signals, verified financial data and snapshot counts"

            printfn "=== TEST COMPLETED SUCCESSFULLY ==="
        }
