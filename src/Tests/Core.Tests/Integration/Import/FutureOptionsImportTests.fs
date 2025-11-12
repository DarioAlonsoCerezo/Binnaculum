namespace Core.Tests.Integration

open NUnit.Framework
open System
open System.IO
open Binnaculum.Core.Models
open Binnaculum.Core.UI
open Binnaculum.Core.Logging
open TestModels

/// <summary>
/// Future Options import signal-based reactive integration tests.
/// Validates complex future options trading with multiple underlying contracts.
///
/// This test follows the same pattern as the MpwImportTests and TsllImportTests.
///
/// Inherits from TestFixtureBase - no setup/teardown boilerplate needed.
///
/// See README.md for pattern documentation and more examples.
/// See PATTERN_GUIDE.fs for detailed implementation guide.
/// </summary>
[<TestFixture>]
type FutureOptionsImportTests() =
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
    /// Test: Future Options multi-strategy import CSV workflow with signal validation
    /// Follows the pattern of MpwImportTests and TsllImportTests.
    ///
    /// This test validates:
    /// 1. Database initialization completes successfully
    /// 2. BrokerAccount creation triggers Accounts_Updated and Snapshots_Updated signals
    /// 3. CSV import triggers Movements_Updated, Tickers_Updated, and Snapshots_Updated signals
    /// 4. Multiple future options movements are imported (3 underlying tickers)
    /// 5. All 3 tickers exist (/MESU5, /MESZ5, /MESH6)
    /// 6. All future options operations verified
    /// 7. All broker account snapshots verified
    /// 8. Financial calculations are correct with complex multi-leg options strategies
    ///
    /// The test uses signal-based verification instead of timing-based assertions,
    /// making it robust and portable across platforms.
    ///
    /// CSV Data: FutureOptions.csv
    /// - Multiple future option trades on 3 different underlying futures
    /// - /MESU5: Aug 25 Call (expired worthless)
    /// - /MESZ5: Oct 31 Put Butterfly + Nov 28 Put Butterfly
    /// - /MESH6: Feb 20 Multi-leg strategy
    /// - Date range: 2025-08-24 through 2025-10-31 (with expirations and early closures)
    /// - Complex FIFO matching scenarios with multi-leg orders
    ///
    /// Expected Results:
    /// - Multiple movements (21 transactions from CSV)
    /// - 3 tickers: /MESU5, /MESZ5, /MESH6 (+ SPY default)
    /// - Operations and broker snapshots to be determined by exploratory run
    /// </summary>
    [<Test>]
    [<Category("Integration")>]
    member this.``Future Options multi-strategy import CSV workflow with signal validation``() =
        async {
            CoreLogger.logInfo
                "Test"
                "=== TEST: Future Options Multi-Strategy Import CSV Workflow with Signal Validation ==="

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
            TestSetup.printPhaseHeader 2 "Create BrokerAccount for Future Options Import"

            // EXPECT: Declare expected signals BEFORE operation
            StreamObserver.expectSignals (
                [ Accounts_Updated // Account added to Collections.Accounts
                  Snapshots_Updated ] // Snapshot calculated in Collections.Snapshots
            )

            CoreLogger.logDebug "StreamObserver" "🎯 Expecting signals: Accounts_Updated, Snapshots_Updated"

            // EXECUTE: Create account
            let! (ok, details, error) = actions.createBrokerAccount ("FutureOptions-Import-Test")
            Assert.That(ok, Is.True, sprintf "Account creation should succeed: %s - %A" details error)
            CoreLogger.logInfo "Verification" (sprintf "✅ BrokerAccount created: %s" details)

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            CoreLogger.logInfo "TestActions" "⏳ Waiting for account creation reactive signals..."
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.That(signalsReceived, Is.True, "Account creation signals should have been received")
            CoreLogger.logInfo "Verification" "✅ Account creation signals received successfully"

            // ==================== PHASE 3: IMPORT FUTURE OPTIONS CSV ====================
            TestSetup.printPhaseHeader 3 "Import Future Options Multi-Strategy CSV File"

            // Get CSV path
            let csvPath = this.getCsvPath ("FutureOptions.csv")
            CoreLogger.logDebug "Import" (sprintf "📄 CSV file path: %s" csvPath)
            Assert.That(File.Exists(csvPath), Is.True, sprintf "CSV file should exist: %s" csvPath)

            // EXPECT: Declare expected signals BEFORE import operation
            StreamObserver.expectSignals (
                [ Movements_Updated // Trades added
                  Tickers_Updated // Future tickers added
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

            // ==================== PHASE 4: VERIFY TICKERS EXIST ====================
            TestSetup.printPhaseHeader 4 "Verify All 3 Future Tickers Exist"

            // Get all tickers from Collections
            let allTickers = Collections.Tickers.Items |> List.ofSeq

            CoreLogger.logInfo "Verification" (sprintf "📊 Found %d total tickers in Collections" allTickers.Length)

            // Verify /MESU5 ticker exists
            let mesu5Ticker = allTickers |> List.tryFind (fun t -> t.Symbol = "/MESU5")

            Assert.That(mesu5Ticker.IsSome, Is.True, "/MESU5 ticker should exist in Collections")
            CoreLogger.logInfo "Verification" (sprintf "✅ /MESU5 Ticker ID: %d" mesu5Ticker.Value.Id)

            // Verify /MESZ5 ticker exists
            let mesz5Ticker = allTickers |> List.tryFind (fun t -> t.Symbol = "/MESZ5")

            Assert.That(mesz5Ticker.IsSome, Is.True, "/MESZ5 ticker should exist in Collections")
            CoreLogger.logInfo "Verification" (sprintf "✅ /MESZ5 Ticker ID: %d" mesz5Ticker.Value.Id)

            // Verify /MESH6 ticker exists
            let mesh6Ticker = allTickers |> List.tryFind (fun t -> t.Symbol = "/MESH6")

            Assert.That(mesh6Ticker.IsSome, Is.True, "/MESH6 ticker should exist in Collections")
            CoreLogger.logInfo "Verification" (sprintf "✅ /MESH6 Ticker ID: %d" mesh6Ticker.Value.Id)

            // ==================== SHARED: GET BROKER AND ACCOUNT ====================
            // Get broker and broker account for operations and broker snapshot validation
            let broker = Collections.Brokers.Items |> Seq.find (fun b -> b.Name = "Tastytrade")

            let brokerAccount =
                Collections.Accounts.Items
                |> Seq.filter (fun a -> a.Type = AccountType.BrokerAccount)
                |> Seq.pick (fun a -> a.Broker)

            let usd = Collections.Currencies.Items |> Seq.find (fun c -> c.Code = "USD")

            // ==================== PHASE 5: VERIFY AUTO-IMPORT OPERATIONS ====================
            TestSetup.printPhaseHeader 5 "Verify Auto-Import Operations"

            // Get all operations for all 3 future tickers
            let! mesu5Operations = Tickers.GetOperations(mesu5Ticker.Value.Id) |> Async.AwaitTask
            let! mesz5Operations = Tickers.GetOperations(mesz5Ticker.Value.Id) |> Async.AwaitTask
            let! mesh6Operations = Tickers.GetOperations(mesh6Ticker.Value.Id) |> Async.AwaitTask

            let allOperations =
                List.concat [ mesu5Operations; mesz5Operations; mesh6Operations ]

            CoreLogger.logInfo "Verification" (sprintf "📊 Found %d total operations in database" allOperations.Length)
            CoreLogger.logInfo "Verification" (sprintf "  - /MESU5: %d operations" mesu5Operations.Length)
            CoreLogger.logInfo "Verification" (sprintf "  - /MESZ5: %d operations" mesz5Operations.Length)
            CoreLogger.logInfo "Verification" (sprintf "  - /MESH6: %d operations" mesh6Operations.Length)

            // TEMPORARY: Output actual operations data for population
            CoreLogger.logInfo "DataCapture" "============ ACTUAL OPERATIONS DATA ============"

            allOperations
            |> List.iteri (fun i op ->
                CoreLogger.logInfo "DataCapture" (sprintf "Operation %d:" i)

                CoreLogger.logInfo
                    "DataCapture"
                    (sprintf
                        "  Ticker: %s"
                        (if op.Ticker = mesu5Ticker.Value then "/MESU5"
                         elif op.Ticker = mesz5Ticker.Value then "/MESZ5"
                         else "/MESH6"))

                CoreLogger.logInfo
                    "DataCapture"
                    (sprintf "  OpenDate: %s" (op.OpenDate.ToString("yyyy-MM-dd HH:mm:ss")))

                CoreLogger.logInfo
                    "DataCapture"
                    (sprintf
                        "  CloseDate: %s"
                        (match op.CloseDate with
                         | Some d -> d.ToString("yyyy-MM-dd HH:mm:ss")
                         | None -> "None"))

                CoreLogger.logInfo "DataCapture" (sprintf "  IsOpen: %b" op.IsOpen)
                CoreLogger.logInfo "DataCapture" (sprintf "  Realized: %.2f" op.Realized)
                CoreLogger.logInfo "DataCapture" (sprintf "  RealizedToday: %.2f" op.RealizedToday)
                CoreLogger.logInfo "DataCapture" (sprintf "  Commissions: %.2f" op.Commissions)
                CoreLogger.logInfo "DataCapture" (sprintf "  Fees: %.2f" op.Fees)
                CoreLogger.logInfo "DataCapture" (sprintf "  Premium: %.2f" op.Premium)
                CoreLogger.logInfo "DataCapture" (sprintf "  Dividends: %.2f" op.Dividends)
                CoreLogger.logInfo "DataCapture" (sprintf "  DividendTaxes: %.2f" op.DividendTaxes)
                CoreLogger.logInfo "DataCapture" (sprintf "  CapitalDeployed: %.2f" op.CapitalDeployed)
                CoreLogger.logInfo "DataCapture" (sprintf "  CapitalDeployedToday: %.2f" op.CapitalDeployedToday)
                CoreLogger.logInfo "DataCapture" (sprintf "  Performance: %.4f" op.Performance))

            CoreLogger.logInfo "DataCapture" "================================================"

            // Get expected operations with descriptions
            let expectedOperationsWithDescriptions =
                FutureOptionsImportExpectedSnapshots.getFutureOptionsOperations
                    brokerAccount
                    mesu5Ticker.Value
                    mesz5Ticker.Value
                    mesh6Ticker.Value
                    usd

            let expectedOperations =
                expectedOperationsWithDescriptions |> TestModels.getOperationData

            CoreLogger.logInfo "Verification" (sprintf "📊 Validating %d expected operations" expectedOperations.Length)

            // Filter actual operations to only include the ones we're validating
            let expectedOpenDates =
                expectedOperations |> List.map (fun op -> op.OpenDate) |> Set.ofList

            let actualOperationsFiltered =
                allOperations |> List.filter (fun op -> expectedOpenDates.Contains(op.OpenDate))

            CoreLogger.logInfo
                "Verification"
                (sprintf "📊 Found %d matching operations to validate" actualOperationsFiltered.Length)

            // Verify each operation
            let operationResults =
                TestVerifications.verifyAutoImportOperationList expectedOperations actualOperationsFiltered

            operationResults
            |> List.iteri (fun i (allMatch, fieldResults) ->
                let description = expectedOperationsWithDescriptions.[i].Description

                if not allMatch then
                    let formatted = TestVerifications.formatValidationResults fieldResults

                    CoreLogger.logError
                        "Verification"
                        (sprintf "❌ Operation %d (%s) verification failed:\n%s" i description formatted)

                    Assert.Fail(sprintf "Operation %d (%s) verification failed" i description)
                else
                    CoreLogger.logInfo "Verification" (sprintf "✅ Operation %d (%s) verified" i description))

            CoreLogger.logInfo "Verification" (sprintf "✅ All %d operations verified" actualOperationsFiltered.Length)

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

            // TEMPORARY: Output actual broker snapshots data for population
            CoreLogger.logInfo "DataCapture" "============ ACTUAL BROKER SNAPSHOTS DATA ============"

            brokerFinancialSnapshots
            |> List.iteri (fun i snap ->
                CoreLogger.logInfo "DataCapture" (sprintf "Snapshot %d:" i)
                CoreLogger.logInfo "DataCapture" (sprintf "  Date: %s" (snap.Date.ToString("yyyy-MM-dd")))
                CoreLogger.logInfo "DataCapture" (sprintf "  Deposited: %.2f" snap.Deposited)
                CoreLogger.logInfo "DataCapture" (sprintf "  Withdrawn: %.2f" snap.Withdrawn)
                CoreLogger.logInfo "DataCapture" (sprintf "  Invested: %.2f" snap.Invested)
                CoreLogger.logInfo "DataCapture" (sprintf "  RealizedGains: %.2f" snap.RealizedGains)
                CoreLogger.logInfo "DataCapture" (sprintf "  RealizedPercentage: %.4f" snap.RealizedPercentage)
                CoreLogger.logInfo "DataCapture" (sprintf "  UnrealizedGains: %.2f" snap.UnrealizedGains)

                CoreLogger.logInfo
                    "DataCapture"
                    (sprintf "  UnrealizedGainsPercentage: %.4f" snap.UnrealizedGainsPercentage)

                CoreLogger.logInfo "DataCapture" (sprintf "  DividendsReceived: %.2f" snap.DividendsReceived)
                CoreLogger.logInfo "DataCapture" (sprintf "  OptionsIncome: %.2f" snap.OptionsIncome)
                CoreLogger.logInfo "DataCapture" (sprintf "  OtherIncome: %.2f" snap.OtherIncome)
                CoreLogger.logInfo "DataCapture" (sprintf "  Commissions: %.2f" snap.Commissions)
                CoreLogger.logInfo "DataCapture" (sprintf "  Fees: %.2f" snap.Fees)
                CoreLogger.logInfo "DataCapture" (sprintf "  OpenTrades: %b" snap.OpenTrades))

            CoreLogger.logInfo "DataCapture" "======================================================="

            // Get expected broker account snapshots
            let expectedBrokerSnapshotsWithDescriptions =
                FutureOptionsImportExpectedSnapshots.getBrokerAccountSnapshots broker brokerAccount usd

            let expectedBrokerSnapshots =
                expectedBrokerSnapshotsWithDescriptions |> TestModels.getData

            CoreLogger.logInfo
                "Verification"
                (sprintf "📊 Validating %d expected broker snapshots" expectedBrokerSnapshots.Length)

            // Filter actual snapshots to only include the ones we're validating
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

            // Use base class method for verification
            this.VerifyBrokerSnapshots expectedBrokerSnapshots brokerFinancialSnapshotsFiltered getBrokerDescription

            // ==================== SUMMARY ====================
            let totalValidations = expectedOperations.Length + expectedBrokerSnapshots.Length

            TestSetup.printTestCompletionSummary
                "Future Options Multi-Strategy Import with Complete Verification"
                (sprintf
                    "Successfully created BrokerAccount, imported FutureOptions CSV, verified 3 tickers, all %d operations, and all %d broker snapshots (%d items total)"
                    expectedOperations.Length
                    expectedBrokerSnapshots.Length
                    totalValidations)

            CoreLogger.logInfo "Test" "=== TEST COMPLETED SUCCESSFULLY ==="
        }
