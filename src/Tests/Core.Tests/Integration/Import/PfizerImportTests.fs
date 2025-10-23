namespace Core.Tests.Integration

open NUnit.Framework
open System
open System.IO
open Binnaculum.Core.Models
open Binnaculum.Core.UI
open Binnaculum.Core.Logging
open TestModels

/// <summary>
/// Pfizer (PFE) options import signal-based reactive integration tests.
/// Validates complex FIFO pair matching with realistic options trading data.
///
/// This test replicates the exact workflow of the MAUI Tester's
/// "RunPfizerImportIntegrationTestButton" test.
///
/// Inherits from TestFixtureBase - no setup/teardown boilerplate needed.
///
/// See README.md for pattern documentation and more examples.
/// See PATTERN_GUIDE.fs for detailed implementation guide.
/// </summary>
[<TestFixture>]
type PfizerImportTests() =
    inherit TestFixtureBase()

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
    /// Test: Pfizer options import CSV workflow with FIFO matching
    /// Mirrors Core.Platform.MauiTester's "RunPfizerImportIntegrationTestButton" test.
    ///
    /// This test validates:
    /// 1. Database initialization completes successfully
    /// 2. BrokerAccount creation triggers Accounts_Updated and Snapshots_Updated signals
    /// 3. CSV import triggers Movements_Updated, Tickers_Updated, and Snapshots_Updated signals
    /// 4. 4 option movements are imported (2 SELL, 2 BUY - forming 2 complete round-trips)
    /// 5. 2 tickers are present (PFE + SPY default)
    /// 6. Financial calculations are correct with FIFO pair matching
    /// 7. PFE ticker has 4 snapshots (3 from trades + 1 today)
    ///
    /// The test uses signal-based verification instead of timing-based assertions,
    /// making it robust and portable across platforms.
    ///
    /// CSV Data: PfizerImportTest.csv
    /// - 4 option trades (2 complete round-trip pairs):
    ///   * PFE 20.00 CALL 01/16/26: BUY_TO_OPEN -> SELL_TO_CLOSE (+$189.76 profit)
    ///   * PFE 27.00 CALL 10/10/25: SELL_TO_OPEN -> BUY_TO_CLOSE (-$14.24 loss)
    ///
    /// Expected Results:
    /// - 4 total movements (2 BUY, 2 SELL)
    /// - 2 tickers (PFE + SPY default)
    /// - Options income: $175.52 (sum of all option trade premiums)
    /// - Realized gains: $175.52 (FIFO matching: -$14.24 + $189.76)
    /// - Unrealized gains: $0.00 (all positions closed)
    /// - PFE snapshots: 4 (2025-08-25, 2025-10-01, 2025-10-03, + today)
    /// </summary>
    [<Test>]
    [<Category("Integration")>]
    member this.``Pfizer options import CSV workflow with FIFO matching``() =
        async {
            CoreLogger.logInfo "Test" "=== TEST: Pfizer Options Import CSV Workflow with FIFO Matching ==="

            let actions = this.Actions

            // ==================== PHASE 1: SETUP ====================
            TestSetup.printPhaseHeader 1 "Database Initialization"

            // Wipe all data for clean slate
            let! (ok, _, error) = actions.wipeDataForTesting ()
            Assert.That(ok, Is.True, sprintf "Wipe should succeed: %A" error)
            CoreLogger.logInfo "Verification" "✅ Data wiped successfully"

            // Initialize database (includes schema init and data loading)
            let! (ok, _, error) = actions.initDatabase ()
            Assert.That(ok, Is.True, sprintf "Database initialization should succeed: %A" error)
            CoreLogger.logInfo "Verification" "✅ Database initialized successfully"

            // ==================== PHASE 2: CREATE BROKER ACCOUNT ====================
            TestSetup.printPhaseHeader 2 "Create BrokerAccount for Pfizer Import"

            // EXPECT: Declare expected signals BEFORE operation
            StreamObserver.expectSignals (
                [ Accounts_Updated // Account added to Collections.Accounts
                  Snapshots_Updated ] // Snapshot calculated in Collections.Snapshots
            )

            CoreLogger.logDebug "StreamObserver" "🎯 Expecting signals: Accounts_Updated, Snapshots_Updated"

            // EXECUTE: Create account
            let! (ok, details, error) = actions.createBrokerAccount ("Pfizer-Import-Test")
            Assert.That(ok, Is.True, sprintf "Account creation should succeed: %s - %A" details error)
            CoreLogger.logInfo "Verification" (sprintf "✅ BrokerAccount created: %s" details)

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            CoreLogger.logInfo "TestActions" "⏳ Waiting for account creation reactive signals..."
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.That(signalsReceived, Is.True, "Account creation signals should have been received")
            CoreLogger.logInfo "Verification" "✅ Account creation signals received successfully"

            // ==================== PHASE 3: IMPORT PFIZER OPTIONS CSV ====================
            TestSetup.printPhaseHeader 3 "Import Pfizer Options CSV File"

            // Get CSV path
            let csvPath = this.getCsvPath ("PfizerImportTest.csv")
            CoreLogger.logDebug "Import" (sprintf "📄 CSV file path: %s" csvPath)
            Assert.That(File.Exists(csvPath), Is.True, sprintf "CSV file should exist: %s" csvPath)

            // EXPECT: Declare expected signals BEFORE import operation
            StreamObserver.expectSignals (
                [ Movements_Updated // Option trades added
                  Tickers_Updated // PFE ticker added
                  Snapshots_Updated ] // Snapshots recalculated
            )

            CoreLogger.logDebug
                "StreamObserver"
                "🎯 Expecting signals: Movements_Updated, Tickers_Updated, Snapshots_Updated"

            // EXECUTE: Import CSV file
            let tastytradeId = actions.Context.TastytradeId
            let accountId = actions.Context.BrokerAccountId

            CoreLogger.logDebug
                "TestSetup"
                (sprintf "🔧 Import parameters: Tastytrade ID=%d, Account ID=%d" tastytradeId accountId)

            let! (ok, importDetails, error) = actions.importFile (tastytradeId, accountId, csvPath)
            Assert.That(ok, Is.True, sprintf "Import should succeed: %s - %A" importDetails error)
            CoreLogger.logInfo "Verification" (sprintf "✅ CSV import completed: %s" importDetails)

            // WAIT: Wait for import signals (longer timeout for import processing)
            CoreLogger.logInfo "TestActions" "⏳ Waiting for import reactive signals..."
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(15.0))
            Assert.That(signalsReceived, Is.True, "Import signals should have been received")
            CoreLogger.logInfo "Verification" "✅ Import signals received successfully"

            // ==================== PHASE 4: VERIFY TICKER COUNT ====================
            TestSetup.printPhaseHeader 4 "Verify Ticker Count"

            // Verify ticker count (PFE + SPY default = 2)
            let! (verified, tickerCount, error) = actions.verifyTickerCount (2)

            Assert.That(
                verified,
                Is.True,
                sprintf "Ticker count verification should succeed: %s - %A" tickerCount error
            )

            CoreLogger.logInfo "Verification" "✅ Ticker count verified: 2 tickers (PFE + SPY)"

            // ==================== PHASE 5: VERIFY PFE TICKER SNAPSHOTS CHRONOLOGICALLY ====================
            TestSetup.printPhaseHeader 5 "Verify PFE Ticker Snapshots with Complete Financial State"

            // Get PFE ticker and USD currency from Collections
            let pfeTicker = Collections.Tickers.Items |> Seq.tryFind (fun t -> t.Symbol = "PFE")

            Assert.That(pfeTicker.IsSome, Is.True, "PFE ticker should exist in Collections")

            let pfeTickerId = pfeTicker.Value.Id
            let usd = Collections.Currencies.Items |> Seq.find (fun c -> c.Code = "USD")
            CoreLogger.logInfo "Verification" (sprintf "📊 PFE Ticker ID: %d" pfeTickerId)

            // Get all PFE snapshots using Tickers.GetSnapshots from Core
            let! pfeSnapshots = Tickers.GetSnapshots(pfeTickerId) |> Async.AwaitTask
            let sortedPFESnapshots = pfeSnapshots |> List.sortBy (fun s -> s.Date)

            CoreLogger.logInfo "Verification" (sprintf "📊 Found %d PFE snapshots" sortedPFESnapshots.Length)

            Assert.That(
                sortedPFESnapshots.Length,
                Is.EqualTo(4),
                "Should have 4 PFE snapshots (2025-08-25, 2025-10-01, 2025-10-03 + today)"
            )

            // Get expected PFE snapshots with descriptions from PfizerImportExpectedSnapshots
            let expectedPFESnapshotsWithDescriptions =
                PfizerImportExpectedSnapshots.getPFESnapshots pfeTicker.Value usd

            // Extract data and descriptions
            let expectedPFESnapshots =
                expectedPFESnapshotsWithDescriptions |> TestModels.getData

            let actualPFESnapshots = sortedPFESnapshots |> List.map (fun s -> s.MainCurrency)

            // Description function using the pre-defined descriptions
            let getPFEDescription i =
                expectedPFESnapshotsWithDescriptions.[i].Description

            // Use base class method for verification
            this.VerifyTickerSnapshots "PFE" expectedPFESnapshots actualPFESnapshots getPFEDescription

            // ==================== PHASE 6: VERIFY BROKER ACCOUNT FINANCIAL SNAPSHOTS ====================
            TestSetup.printPhaseHeader 6 "Verify Broker Account Financial Snapshots"

            // Get broker account from context
            let brokerAccountId = actions.Context.BrokerAccountId
            CoreLogger.logInfo "Verification" (sprintf "📊 BrokerAccount ID: %d" brokerAccountId)

            // Get all broker account snapshots using BrokerAccounts.GetSnapshots from Core
            let! overviewSnapshots = BrokerAccounts.GetSnapshots(brokerAccountId) |> Async.AwaitTask

            // Extract BrokerFinancialSnapshot from OverviewSnapshots
            let brokerFinancialSnapshots =
                overviewSnapshots
                |> List.choose (fun os -> os.BrokerAccount |> Option.map (fun bas -> (bas.Date, bas.Financial)))
                |> List.sortBy fst
                |> List.map snd

            CoreLogger.logInfo
                "Verification"
                (sprintf "📊 Found %d BrokerAccount snapshots" brokerFinancialSnapshots.Length)

            Assert.That(brokerFinancialSnapshots.Length, Is.EqualTo(4), "Should have 4 BrokerAccount snapshots")

            // Get broker and currency for snapshot construction
            let broker = Collections.Brokers.Items |> Seq.find (fun b -> b.Name = "Tastytrade")

            let brokerAccount =
                Collections.Accounts.Items
                |> Seq.filter (fun a -> a.Type = AccountType.BrokerAccount)
                |> Seq.pick (fun a -> a.Broker)

            let usd = Collections.Currencies.Items |> Seq.find (fun c -> c.Code = "USD")

            // Get expected BrokerAccount snapshots with descriptions from PfizerImportExpectedSnapshots
            let expectedBrokerSnapshotsWithDescriptions =
                PfizerImportExpectedSnapshots.getBrokerAccountSnapshots broker brokerAccount usd

            // Extract data and descriptions
            let expectedBrokerSnapshots =
                expectedBrokerSnapshotsWithDescriptions |> TestModels.getData

            // Description function using the pre-defined descriptions
            let getBrokerDescription i =
                expectedBrokerSnapshotsWithDescriptions.[i].Description

            // Use base class method for verification
            this.VerifyBrokerSnapshots expectedBrokerSnapshots brokerFinancialSnapshots getBrokerDescription

            // ==================== SUMMARY ====================
            TestSetup.printTestCompletionSummary
                "Pfizer Options Import with FIFO Matching"
                "Successfully created BrokerAccount, imported Pfizer options CSV, received all signals, verified FIFO matching ($175.52), and validated PFE ticker snapshots"

            CoreLogger.logInfo "Test" "=== TEST COMPLETED SUCCESSFULLY ==="
        }
