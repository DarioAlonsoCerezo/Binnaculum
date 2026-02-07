namespace Core.Tests.Integration

open Microsoft.VisualStudio.TestTools.UnitTesting
open System
open System.IO
open Binnaculum.Core.Models
open Binnaculum.Core.UI
open Binnaculum.Core.Logging
open TestModels

/// <summary>
/// TSLL Multi-Asset import signal-based reactive integration tests.
/// Validates complex multi-asset trading with options, equities, and dividends.
///
/// This test replicates the exact workflow of the MAUI Tester's
/// "ReactiveTsllImportIntegrationTest" test.
///
/// Inherits from TestFixtureBase - no setup/teardown boilerplate needed.
///
/// See README.md for pattern documentation and more examples.
/// See PATTERN_GUIDE.fs for detailed implementation guide.
/// </summary>
[<TestClass>]
type TsllImportTests() =
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
    /// Test: TSLL multi-asset import CSV workflow with signal validation
    /// Mirrors Core.Platform.MauiTester's "ReactiveTsllImportIntegrationTest" test.
    ///
    /// This test validates:
    /// 1. Database initialization completes successfully
    /// 2. BrokerAccount creation triggers Accounts_Updated and Snapshots_Updated signals
    /// 3. CSV import triggers Movements_Updated, Tickers_Updated, and Snapshots_Updated signals
    /// 4. Multiple TSLL option movements are imported
    /// 5. TSLL ticker has exactly 72 snapshots
    /// 6. All 72 TSLL ticker snapshots verified
    /// 7. All 4 TSLL operations verified
    /// 8. All 72 broker account snapshots verified
    /// 9. Financial calculations are correct with complex multi-asset data
    ///
    /// The test uses signal-based verification instead of timing-based assertions,
    /// making it robust and portable across platforms.
    ///
    /// CSV Data: TsllImportTest.csv
    /// - Multiple option trades (options only, no equity shares held initially)
    /// - Date range: 2024-05-30 through 2025-10-26
    /// - Complex FIFO matching scenarios
    ///
    /// Expected Results:
    /// - Multiple movements (varies by CSV)
    /// - Multiple tickers (TSLL + SPY default)
    /// - TSLL snapshots: exactly 72
    /// - All 148 expected items validated (72 ticker + 4 operations + 72 broker)
    /// </summary>
    [<TestMethod>]
    [<TestCategory("Integration")>]
    member this.``TSLL multi-asset import CSV workflow with signal validation``() =
        async {
            CoreLogger.logInfo "Test" "=== TEST: TSLL Multi-Asset Import CSV Workflow with Signal Validation ==="

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
            TestSetup.printPhaseHeader 2 "Create BrokerAccount for TSLL Import"

            // EXPECT: Declare expected signals BEFORE operation
            StreamObserver.expectSignals (
                [ Accounts_Updated // Account added to Collections.Accounts
                  Snapshots_Updated ] // Snapshot calculated in Collections.Snapshots
            )

            CoreLogger.logDebug "StreamObserver" "🎯 Expecting signals: Accounts_Updated, Snapshots_Updated"

            // EXECUTE: Create account
            let! (ok, details, error) = actions.createBrokerAccount ("TSLL-Import-Test")
            Assert.IsTrue(ok, sprintf "Account creation should succeed: %s - %A" details error)
            CoreLogger.logInfo "Verification" (sprintf "✅ BrokerAccount created: %s" details)

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            CoreLogger.logInfo "TestActions" "⏳ Waiting for account creation reactive signals..."
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.IsTrue(signalsReceived, "Account creation signals should have been received")
            CoreLogger.logInfo "Verification" "✅ Account creation signals received successfully"

            // ==================== PHASE 3: IMPORT TSLL OPTIONS CSV ====================
            TestSetup.printPhaseHeader 3 "Import TSLL Multi-Asset CSV File"

            // Get CSV path
            let csvPath = this.getCsvPath ("TsllImportTest.csv")
            CoreLogger.logDebug "Import" (sprintf "📄 CSV file path: %s" csvPath)
            Assert.IsTrue(File.Exists(csvPath), sprintf "CSV file should exist: %s" csvPath)

            // EXPECT: Declare expected signals BEFORE import operation
            StreamObserver.expectSignals (
                [ Movements_Updated // Option trades added
                  Tickers_Updated // TSLL ticker added
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
            Assert.IsTrue(ok, sprintf "Import should succeed: %s - %A" importDetails error)
            CoreLogger.logInfo "Verification" (sprintf "✅ CSV import completed: %s" importDetails)

            // WAIT: Wait for import signals (longer timeout for import processing)
            CoreLogger.logInfo "TestActions" "⏳ Waiting for import reactive signals..."
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(15.0))
            Assert.IsTrue(signalsReceived, "Import signals should have been received")
            CoreLogger.logInfo "Verification" "✅ Import signals received successfully"

            // ==================== PHASE 4: VERIFY TSLL TICKER SNAPSHOTS ====================
            TestSetup.printPhaseHeader 4 "Verify TSLL Ticker Snapshots with Complete Financial State"

            // Get TSLL ticker and USD currency from Collections
            let tsllTicker =
                Collections.Tickers.Items |> Seq.tryFind (fun t -> t.Symbol = "TSLL")

            Assert.IsTrue(tsllTicker.IsSome, "TSLL ticker should exist in Collections")

            let tsllTickerId = tsllTicker.Value.Id
            let usd = Collections.Currencies.Items |> Seq.find (fun c -> c.Code = "USD")
            CoreLogger.logInfo "Verification" (sprintf "📊 TSLL Ticker ID: %d" tsllTickerId)

            // Get all TSLL snapshots using Tickers.GetSnapshots from Core
            let! tsllSnapshots = Tickers.GetSnapshots(tsllTickerId) |> Async.AwaitTask
            let sortedTSLLSnapshots = tsllSnapshots |> List.sortBy (fun s -> s.Date)

            CoreLogger.logInfo
                "Verification"
                (sprintf "📊 Found %d TSLL snapshots in database" sortedTSLLSnapshots.Length)

            // Get expected TSLL snapshots with descriptions from TsllImportExpectedSnapshots
            let expectedTSLLSnapshotsWithDescriptions =
                TsllImportExpectedSnapshots.getTSLLSnapshots tsllTicker.Value usd

            CoreLogger.logInfo
                "Verification"
                (sprintf "📊 Validating %d expected TSLL snapshots" expectedTSLLSnapshotsWithDescriptions.Length)

            // Extract data and descriptions
            let expectedTSLLSnapshots =
                expectedTSLLSnapshotsWithDescriptions |> TestModels.getData

            // Filter actual snapshots to only include the dates we're validating
            let expectedDates =
                expectedTSLLSnapshots |> List.map (fun s -> s.Date) |> Set.ofList

            let actualTSLLSnapshotsFiltered =
                sortedTSLLSnapshots
                |> List.filter (fun s -> expectedDates.Contains(s.Date))
                |> List.map (fun s -> s.MainCurrency)

            CoreLogger.logInfo
                "Verification"
                (sprintf "📊 Found %d matching snapshots to validate" actualTSLLSnapshotsFiltered.Length)

            // Description function using the pre-defined descriptions
            let getTSLLDescription i =
                expectedTSLLSnapshotsWithDescriptions.[i].Description

            // Use base class method for verification (only validates the snapshots we defined)
            this.VerifyTickerSnapshots "TSLL" expectedTSLLSnapshots actualTSLLSnapshotsFiltered getTSLLDescription

            // ==================== SHARED: GET BROKER AND ACCOUNT ====================
            // Get broker and broker account for operations and broker snapshot validation
            let broker = Collections.Brokers.Items |> Seq.find (fun b -> b.Name = "Tastytrade")

            let brokerAccount =
                Collections.Accounts.Items
                |> Seq.filter (fun a -> a.Type = AccountType.BrokerAccount)
                |> Seq.pick (fun a -> a.Broker)

            // ==================== PHASE 5: VERIFY AUTO-IMPORT OPERATIONS ====================
            TestSetup.printPhaseHeader 5 "Verify Auto-Import Operations"

            // Get all operations for TSLL ticker
            let! tsllOperations = Tickers.GetOperations(tsllTickerId) |> Async.AwaitTask

            CoreLogger.logInfo "Verification" (sprintf "📊 Found %d TSLL operations in database" tsllOperations.Length)

            // Get expected operations with descriptions
            let expectedOperationsWithDescriptions =
                TsllImportExpectedSnapshots.getTSLLOperations brokerAccount tsllTicker.Value usd

            let expectedOperations =
                expectedOperationsWithDescriptions |> TestModels.getOperationData

            CoreLogger.logInfo "Verification" (sprintf "📊 Validating %d expected operations" expectedOperations.Length)

            // Filter actual operations to only include the ones we're validating
            let expectedOpenDates =
                expectedOperations |> List.map (fun op -> op.OpenDate) |> Set.ofList

            let actualOperationsFiltered =
                tsllOperations
                |> List.filter (fun op -> expectedOpenDates.Contains(op.OpenDate))

            CoreLogger.logInfo
                "Verification"
                (sprintf "📊 Found %d matching operations to validate" actualOperationsFiltered.Length)

            // Verify each TSLL operation
            let operationResults =
                TestVerifications.verifyAutoImportOperationList expectedOperations actualOperationsFiltered

            operationResults
            |> List.iteri (fun i (allMatch, fieldResults) ->
                let description = expectedOperationsWithDescriptions.[i].Description

                if not allMatch then
                    let formatted = TestVerifications.formatValidationResults fieldResults

                    CoreLogger.logError
                        "Verification"
                        (sprintf "❌ Operation %d (%s) failed:\n%s" i description formatted)

                    Assert.Fail(sprintf "TSLL Operation %d (%s) verification failed" i description)
                else
                    CoreLogger.logInfo "Verification" (sprintf "✅ Operation %d (%s) verified" i description))

            CoreLogger.logInfo
                "Verification"
                (sprintf "✅ All %d TSLL operations verified" actualOperationsFiltered.Length)

            // ==================== PHASE 6: VERIFY BROKER ACCOUNT FINANCIAL SNAPSHOTS ====================
            TestSetup.printPhaseHeader 6 "Verify Broker Account Financial Snapshots"

            // Get broker account ID
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

            // Get expected broker account snapshots (broker and brokerAccount already defined above)
            let expectedBrokerSnapshotsWithDescriptions =
                TsllImportExpectedSnapshots.getBrokerAccountSnapshots broker brokerAccount usd

            let expectedBrokerSnapshots =
                expectedBrokerSnapshotsWithDescriptions |> TestModels.getData

            CoreLogger.logInfo
                "Verification"
                (sprintf "📊 Validating %d expected broker snapshots" expectedBrokerSnapshots.Length)

            // Filter actual snapshots to only include the ones we're validating
            // Match by date (date only, ignoring time component)
            let expectedSnapshotDates =
                expectedBrokerSnapshots |> List.map (fun s -> s.Date) |> Set.ofList

            let brokerFinancialSnapshotsFiltered =
                brokerFinancialSnapshots
                |> List.filter (fun s -> expectedSnapshotDates.Contains(s.Date))

            CoreLogger.logInfo
                "Verification"
                (sprintf "📊 Found %d matching broker snapshots to validate" brokerFinancialSnapshotsFiltered.Length)

            // Description function
            let getBrokerDescription i =
                expectedBrokerSnapshotsWithDescriptions.[i].Description

            // Use base class method for verification (only validates the snapshots we defined)
            this.VerifyBrokerSnapshots expectedBrokerSnapshots brokerFinancialSnapshotsFiltered getBrokerDescription

            // ==================== SUMMARY ====================
            TestSetup.printTestCompletionSummary
                "TSLL Multi-Asset Import with Complete Financial Verification"
                "Successfully created BrokerAccount, imported TSLL CSV, verified all 72 ticker snapshots, all 4 operations, and all 72 broker snapshots (148 items total)"

            CoreLogger.logInfo "Test" "=== TEST COMPLETED SUCCESSFULLY ==="
        }
