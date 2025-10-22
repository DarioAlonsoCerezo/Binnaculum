namespace Core.Tests.Integration

open NUnit.Framework
open System
open System.IO
open Binnaculum.Core.Models
open Binnaculum.Core.UI

/// <summary>
/// TSLL Multi-Asset import signal-based reactive integration tests.
/// Validates complex multi-asset trading with options, equities, and dividends.
///
/// This test replicates the exact workflow of the MAUI Tester's
/// "ReactiveTsllImportIntegrationTest" test.
///
/// Inherits from ReactiveTestFixtureBase - no setup/teardown boilerplate needed.
///
/// See README.md for pattern documentation and more examples.
/// See PATTERN_GUIDE.fs for detailed implementation guide.
/// </summary>
[<TestFixture>]
type ReactiveTsllImportTests() =
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
    /// Test: TSLL multi-asset import CSV workflow with signal validation
    /// Mirrors Core.Platform.MauiTester's "ReactiveTsllImportIntegrationTest" test.
    ///
    /// This test validates:
    /// 1. Database initialization completes successfully
    /// 2. BrokerAccount creation triggers Accounts_Updated and Snapshots_Updated signals
    /// 3. CSV import triggers Movements_Updated, Tickers_Updated, and Snapshots_Updated signals
    /// 4. Multiple TSLL option movements are imported
    /// 5. TSLL ticker has exactly 71 snapshots
    /// 6. 4 specific snapshot validations pass (2024-05-30, 2024-06-07, 2024-10-15, 2024-10-18)
    /// 7. Financial calculations are correct with complex multi-asset data
    ///
    /// The test uses signal-based verification instead of timing-based assertions,
    /// making it robust and portable across platforms.
    ///
    /// CSV Data: TsllImportTest.csv
    /// - Multiple option trades (options only, no equity shares held)
    /// - Date range: 2024-05-30 through 2024-10-18
    /// - Complex FIFO matching scenarios
    ///
    /// Expected Results:
    /// - Multiple movements (varies by CSV)
    /// - Multiple tickers (TSLL + SPY default)
    /// - TSLL snapshots: exactly 71
    /// - 4 specific date validations pass
    /// </summary>
    [<Test>]
    [<Category("Integration")>]
    member this.``TSLL multi-asset import CSV workflow with signal validation``() =
        async {
            printfn "\n=== TEST: TSLL Multi-Asset Import CSV Workflow with Signal Validation ==="

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
            ReactiveTestSetup.printPhaseHeader 2 "Create BrokerAccount for TSLL Import"

            // EXPECT: Declare expected signals BEFORE operation
            ReactiveStreamObserver.expectSignals([
                Accounts_Updated      // Account added to Collections.Accounts
                Snapshots_Updated     // Snapshot calculated in Collections.Snapshots
            ])
            printfn "🎯 Expecting signals: Accounts_Updated, Snapshots_Updated"

            // EXECUTE: Create account
            let! (ok, details, error) = actions.createBrokerAccount("TSLL-Import-Test")
            Assert.That(ok, Is.True, sprintf "Account creation should succeed: %s - %A" details error)
            printfn "✅ BrokerAccount created: %s" details

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            printfn "⏳ Waiting for account creation reactive signals..."
            let! signalsReceived = ReactiveStreamObserver.waitForAllSignalsAsync(TimeSpan.FromSeconds(10.0))
            Assert.That(signalsReceived, Is.True, "Account creation signals should have been received")
            printfn "✅ Account creation signals received successfully"

            // ==================== PHASE 3: IMPORT TSLL OPTIONS CSV ====================
            ReactiveTestSetup.printPhaseHeader 3 "Import TSLL Multi-Asset CSV File"

            // Get CSV path
            let csvPath = this.getCsvPath("TsllImportTest.csv")
            printfn "📄 CSV file path: %s" csvPath
            Assert.That(File.Exists(csvPath), Is.True, sprintf "CSV file should exist: %s" csvPath)

            // EXPECT: Declare expected signals BEFORE import operation
            ReactiveStreamObserver.expectSignals([
                Movements_Updated     // Option trades added
                Tickers_Updated       // TSLL ticker added
                Snapshots_Updated     // Snapshots recalculated
            ])
            printfn "🎯 Expecting signals: Movements_Updated, Tickers_Updated, Snapshots_Updated"

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

            // ==================== PHASE 4: VERIFY TSLL SNAPSHOT COUNT ====================
            ReactiveTestSetup.printPhaseHeader 4 "Verify TSLL Ticker Snapshots (Exact Count)"

            // Verify TSLL ticker snapshots (exactly 71)
            let! (verified, snapshotCount, error) = actions.verifyTsllSnapshotCount(71)
            Assert.That(verified, Is.True, sprintf "TSLL snapshot count verification should succeed: %s - %A" snapshotCount error)
            Assert.That(snapshotCount, Is.EqualTo("71"), "Should have exactly 71 TSLL snapshots")
            printfn "✅ TSLL ticker snapshots verified: 71 snapshots"

            // ==================== PHASE 5: VALIDATE SPECIFIC SNAPSHOTS ====================
            ReactiveTestSetup.printPhaseHeader 5 "Validate 4 Specific TSLL Snapshots by Date"

            // Validate snapshot 1: 2024-05-30 (Oldest snapshot - initial put position)
            printfn "📅 Validating snapshot: 2024-05-30 (Oldest - initial put position)"
            let! (verified, details, error) = actions.validateTsllSnapshot(2024, 5, 30)
            Assert.That(verified, Is.True, sprintf "2024-05-30 snapshot validation should pass: %s - %A" details error)
            printfn "✅ 2024-05-30 snapshot validated"

            // Validate snapshot 2: 2024-06-07 (After expiration - put expired worthless)
            printfn "📅 Validating snapshot: 2024-06-07 (After expiration)"
            let! (verified, details, error) = actions.validateTsllSnapshot(2024, 6, 7)
            Assert.That(verified, Is.True, sprintf "2024-06-07 snapshot validation should pass: %s - %A" details error)
            printfn "✅ 2024-06-07 snapshot validated"

            // Validate snapshot 3: 2024-10-15 (Open calls - new call positions)
            printfn "📅 Validating snapshot: 2024-10-15 (Open calls)"
            let! (verified, details, error) = actions.validateTsllSnapshot(2024, 10, 15)
            Assert.That(verified, Is.True, sprintf "2024-10-15 snapshot validation should pass: %s - %A" details error)
            printfn "✅ 2024-10-15 snapshot validated"

            // Validate snapshot 4: 2024-10-18 (Additional trades - more call activity)
            printfn "📅 Validating snapshot: 2024-10-18 (Additional trades)"
            let! (verified, details, error) = actions.validateTsllSnapshot(2024, 10, 18)
            Assert.That(verified, Is.True, sprintf "2024-10-18 snapshot validation should pass: %s - %A" details error)
            printfn "✅ 2024-10-18 snapshot validated"

            // ==================== SUMMARY ====================
            ReactiveTestSetup.printTestCompletionSummary
                "TSLL Multi-Asset Import with Signal Validation"
                "Successfully created BrokerAccount, imported TSLL multi-asset CSV, received all signals, verified 71 snapshots, and validated 4 specific date-based snapshots"

            printfn "=== TEST COMPLETED SUCCESSFULLY ==="
        }
