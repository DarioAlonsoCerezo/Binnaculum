namespace Core.Tests.Integration

open NUnit.Framework
open System
open System.IO
open Binnaculum.Core.Models
open Binnaculum.Core.UI

/// <summary>
/// Options import signal-based reactive integration tests.
/// Validates CSV import workflows with realistic Tastytrade options trading data.
///
/// This test replicates the exact workflow of the MAUI Tester's
/// "RunOptionsImportIntegrationSignalBasedTestButton" test.
///
/// Inherits from ReactiveTestFixtureBase - no setup/teardown boilerplate needed.
///
/// See README.md for pattern documentation and more examples.
/// See PATTERN_GUIDE.fs for detailed implementation guide.
/// </summary>
[<TestFixture>]
type ReactiveOptionsImportTests() =
    inherit ReactiveTestFixtureBase()

    /// <summary>
    /// Helper method to get path to embedded CSV test data
    /// </summary>
    member private _.getCsvPath(filename: string) : string =
        let testDataPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Tastytrade_Samples", filename)

        if not (File.Exists(testDataPath)) then
            failwith (sprintf "CSV test data not found: %s" testDataPath)

        testDataPath

    /// <summary>
    /// Test: Options import from CSV updates collections
    /// Mirrors Core.Platform.MauiTester's "RunOptionsImportIntegrationSignalBasedTestButton" test.
    ///
    /// This test validates:
    /// 1. Database initialization completes successfully
    /// 2. BrokerAccount creation triggers Accounts_Updated and Snapshots_Updated signals
    /// 3. CSV import triggers Movements_Updated, Tickers_Updated, and Snapshots_Updated signals
    /// 4. 16 movements are imported (12 option trades + 4 money movements)
    /// 5. 4 tickers are present (SOFI, PLTR, MPW + SPY)
    /// 6. Financial calculations are correct (options income, realized/unrealized gains)
    ///
    /// The test uses signal-based verification instead of timing-based assertions,
    /// making it robust and portable across platforms.
    ///
    /// CSV Data: TastytradeOptionsTest.csv
    /// - 12 option trades (various open/close positions)
    /// - 4 money movements (3 deposits + 1 balance adjustment)
    ///
    /// Expected Results:
    /// - 16 total movements
    /// - 4 tickers (SOFI, PLTR, MPW + SPY default)
    /// - Options income: $54.37 (sum of all option trade premiums)
    /// - Realized gains: -$28.67 (sum of closed position premiums)
    /// - Unrealized gains: $83.04 (sum of open position premiums)
    /// </summary>
    [<Test>]
    [<Category("Integration")>]
    member this.``Options import from CSV updates collections``() =
        async {
            printfn "\n=== TEST: Options Import from CSV Updates Collections ==="

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
            ReactiveTestSetup.printPhaseHeader 2 "Create BrokerAccount for Import"

            // EXPECT: Declare expected signals BEFORE operation
            ReactiveStreamObserver.expectSignals (
                [ Accounts_Updated // Account added to Collections.Accounts
                  Snapshots_Updated ] // Snapshot calculated in Collections.Snapshots
            )

            printfn "🎯 Expecting signals: Accounts_Updated, Snapshots_Updated"

            // EXECUTE: Create account
            let! (ok, details, error) = actions.createBrokerAccount ("Options-Import-Test")
            Assert.That(ok, Is.True, sprintf "Account creation should succeed: %s - %A" details error)
            printfn "✅ BrokerAccount created: %s" details

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            printfn "⏳ Waiting for account creation reactive signals..."
            let! signalsReceived = ReactiveStreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.That(signalsReceived, Is.True, "Account creation signals should have been received")
            printfn "✅ Account creation signals received successfully"

            // ==================== PHASE 3: IMPORT OPTIONS CSV ====================
            ReactiveTestSetup.printPhaseHeader 3 "Import Options CSV File"

            // Get CSV path
            let csvPath = this.getCsvPath ("TastytradeOptionsTest.csv")
            printfn "📄 CSV file path: %s" csvPath
            Assert.That(File.Exists(csvPath), Is.True, sprintf "CSV file should exist: %s" csvPath)

            // EXPECT: Declare expected signals BEFORE import operation
            ReactiveStreamObserver.expectSignals (
                [ Movements_Updated // Option trades and money movements added
                  Tickers_Updated // New tickers (SOFI, PLTR, MPW) added
                  Snapshots_Updated ] // Snapshots recalculated
            )

            printfn "🎯 Expecting signals: Movements_Updated, Tickers_Updated, Snapshots_Updated"

            // EXECUTE: Import CSV file
            let tastytradeId = actions.Context.TastytradeId
            let accountId = actions.Context.BrokerAccountId
            printfn "🔧 Import parameters: Tastytrade ID=%d, Account ID=%d" tastytradeId accountId

            let! (ok, importDetails, error) = actions.importFile (tastytradeId, accountId, csvPath)
            Assert.That(ok, Is.True, sprintf "Import should succeed: %s - %A" importDetails error)
            printfn "✅ CSV import completed: %s" importDetails

            // WAIT: Wait for import signals (longer timeout for import processing)
            printfn "⏳ Waiting for import reactive signals..."
            let! signalsReceived = ReactiveStreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(15.0))
            Assert.That(signalsReceived, Is.True, "Import signals should have been received")
            printfn "✅ Import signals received successfully"

            // ==================== PHASE 4: VERIFY DATA COUNTS ====================
            ReactiveTestSetup.printPhaseHeader 4 "Verify Imported Data Counts"

            // Verify movement count (12 option trades + 4 money movements = 16)
            let! (verified, movementCount, error) = actions.verifyMovementCount (16)

            Assert.That(
                verified,
                Is.True,
                sprintf "Movement count verification should succeed: %s - %A" movementCount error
            )

            printfn "✅ Movement count verified: 16 movements (12 option trades + 4 money movements)"

            // Verify ticker count (SOFI, PLTR, MPW + SPY default = 4)
            let! (verified, tickerCount, error) = actions.verifyTickerCount (4)

            Assert.That(
                verified,
                Is.True,
                sprintf "Ticker count verification should succeed: %s - %A" tickerCount error
            )

            printfn "✅ Ticker count verified: 4 tickers (SOFI, PLTR, MPW + SPY)"

            // Verify snapshots were calculated
            let! (verified, snapshotCount, error) = actions.verifySnapshotCount (1)

            Assert.That(
                verified,
                Is.True,
                sprintf "Snapshot count verification should succeed: %s - %A" snapshotCount error
            )

            printfn "✅ Snapshot count verified: >= 1 (%s)" snapshotCount

            // ==================== PHASE 5: VERIFY FINANCIAL CALCULATIONS ====================
            ReactiveTestSetup.printPhaseHeader 5 "Verify Financial Calculations"

            // Verify options income calculation
            // Total options income from all option trades (sum of NetPremium)
            let! (verified, income, error) = actions.verifyOptionsIncome (54.37m)
            Assert.That(verified, Is.True, sprintf "Options income verification should succeed: %s - %A" income error)
            printfn "✅ Options income verified: $54.37"

            // Verify realized gains calculation
            // Realized gains from closed option positions (sum of close trades NetPremium)
            let! (verified, realized, error) = actions.verifyRealizedGains (-28.67m)
            Assert.That(verified, Is.True, sprintf "Realized gains verification should succeed: %s - %A" realized error)
            printfn "✅ Realized gains verified: -$28.67"

            // Verify unrealized gains calculation
            // Unrealized gains from open option positions (sum of open trades NetPremium)
            let! (verified, unrealized, error) = actions.verifyUnrealizedGains (83.04m)

            Assert.That(
                verified,
                Is.True,
                sprintf "Unrealized gains verification should succeed: %s - %A" unrealized error
            )

            printfn "✅ Unrealized gains verified: $83.04"

            // ==================== SUMMARY ====================
            ReactiveTestSetup.printTestCompletionSummary
                "Options Import from CSV"
                "Successfully created BrokerAccount, imported options CSV, received all signals, and verified data counts in Collections"

            printfn "=== TEST COMPLETED SUCCESSFULLY ==="
        }
