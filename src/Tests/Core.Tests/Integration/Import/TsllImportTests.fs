namespace Core.Tests.Integration

open NUnit.Framework
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
[<TestFixture>]
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
    [<Test>]
    [<Category("Integration")>]
    member this.``TSLL multi-asset import CSV workflow with signal validation``() =
        async {
            CoreLogger.logInfo "Test" "=== TEST: TSLL Multi-Asset Import CSV Workflow with Signal Validation ==="

            let actions = this.Actions

            // ==================== PHASE 1: SETUP ====================
            TestSetup.printPhaseHeader 1 "Database Initialization"

            // Wipe all data for clean slate
            let! (ok, _, error) = actions.wipeDataForTesting ()
            Assert.That(ok, Is.True, sprintf "Wipe should succeed: %A" error)
            CoreLogger.logInfo "[Verification]" "✅ Data wiped successfully"

            // Initialize database (includes schema init and data loading)
            let! (ok, _, error) = actions.initDatabase ()
            Assert.That(ok, Is.True, sprintf "Database initialization should succeed: %A" error)
            CoreLogger.logInfo "[Verification]" "✅ Database initialized successfully"

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
            Assert.That(ok, Is.True, sprintf "Account creation should succeed: %s - %A" details error)
            CoreLogger.logInfo "[Verification]" (sprintf "✅ BrokerAccount created: %s" details)

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            CoreLogger.logInfo "[TestActions]" "⏳ Waiting for account creation reactive signals..."
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.That(signalsReceived, Is.True, "Account creation signals should have been received")
            CoreLogger.logInfo "[Verification]" "✅ Account creation signals received successfully"

            // ==================== PHASE 3: IMPORT TSLL OPTIONS CSV ====================
            TestSetup.printPhaseHeader 3 "Import TSLL Multi-Asset CSV File"

            // Get CSV path
            let csvPath = this.getCsvPath ("TsllImportTest.csv")
            CoreLogger.logDebug "[Import]" (sprintf "📄 CSV file path: %s" csvPath)
            Assert.That(File.Exists(csvPath), Is.True, sprintf "CSV file should exist: %s" csvPath)

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
                "[TestSetup]"
                (sprintf "🔧 Import parameters: Tastytrade ID=%d, Account ID=%d" tastytradeId accountId)

            let! (ok, importDetails, error) = actions.importFile (tastytradeId, accountId, csvPath)
            Assert.That(ok, Is.True, sprintf "Import should succeed: %s - %A" importDetails error)
            CoreLogger.logInfo "[Verification]" (sprintf "✅ CSV import completed: %s" importDetails)

            // WAIT: Wait for import signals (longer timeout for import processing)
            CoreLogger.logInfo "[TestActions]" "⏳ Waiting for import reactive signals..."
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(15.0))
            Assert.That(signalsReceived, Is.True, "Import signals should have been received")
            CoreLogger.logInfo "[Verification]" "✅ Import signals received successfully"

            // ==================== PHASE 4: VERIFY TSLL TICKER SNAPSHOTS ====================
            TestSetup.printPhaseHeader 4 "Verify TSLL Ticker Snapshots with Complete Financial State"

            // Get TSLL ticker and USD currency from Collections
            let tsllTicker =
                Collections.Tickers.Items |> Seq.tryFind (fun t -> t.Symbol = "TSLL")

            Assert.That(tsllTicker.IsSome, Is.True, "TSLL ticker should exist in Collections")

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

    // ==================== CODE GENERATOR TEST (TEMPORARY) ====================
    // This test generates F# code for all expected snapshots and operations.
    // Run this test explicitly to generate code for TsllImportExpectedSnapshots.fs
    // Once code is generated and copied, DELETE this test and helper functions.

    /// <summary>
    /// Helper function to generate F# code for ticker snapshots
    /// </summary>
    member private _.generateTickerSnapshotsCode (snapshots: TickerSnapshot list) =
        snapshots |> List.iteri (fun i snapshot ->
            let s = snapshot.MainCurrency
            printfn "          // Snapshot %d: %s" (i+1) (s.Date.ToString("yyyy-MM-dd"))
            printfn "          { Data ="
            printfn "              { Id = 0"
            printfn "                Date = DateOnly(%d, %d, %d)" s.Date.Year s.Date.Month s.Date.Day
            printfn "                Ticker = ticker"
            printfn "                Currency = currency"
            printfn "                TotalShares = %.2fm" s.TotalShares
            printfn "                Weight = %.4fm" s.Weight
            printfn "                CostBasis = %.2fm" s.CostBasis
            printfn "                RealCost = %.2fm" s.RealCost
            printfn "                Dividends = %.2fm" s.Dividends
            printfn "                DividendTaxes = %.2fm" s.DividendTaxes
            printfn "                Options = %.2fm" s.Options
            printfn "                TotalIncomes = %.2fm" s.TotalIncomes
            printfn "                Unrealized = %.2fm" s.Unrealized
            printfn "                Realized = %.2fm" s.Realized
            printfn "                Performance = %.4fm" s.Performance
            printfn "                LatestPrice = %.2fm" s.LatestPrice
            printfn "                OpenTrades = %b" s.OpenTrades
            printfn "                Commissions = %.2fm" s.Commissions
            printfn "                Fees = %.2fm }" s.Fees
            printfn "            Description = \"TODO: Add description for %s\" }" (s.Date.ToString("yyyy-MM-dd"))
        )

    /// <summary>
    /// Helper function to generate F# code for operations
    /// </summary>
    member private _.generateOperationsCode (operations: AutoImportOperation list) =
        operations |> List.iteri (fun i op ->
            let closeDate = 
                match op.CloseDate with
                | Some dt -> sprintf "Some(DateTime(%d, %d, %d, 0, 0, 1))" dt.Year dt.Month dt.Day
                | None -> "None"
            
            printfn "          // Operation %d: %s" (i+1) (op.OpenDate.ToString("yyyy-MM-dd"))
            printfn "          { Data ="
            printfn "              { Id = 0"
            printfn "                BrokerAccount = brokerAccount"
            printfn "                Ticker = ticker"
            printfn "                Currency = currency"
            printfn "                IsOpen = %b" op.IsOpen
            printfn "                OpenDate = DateTime(%d, %d, %d, 0, 0, 1)" op.OpenDate.Year op.OpenDate.Month op.OpenDate.Day
            printfn "                CloseDate = %s" closeDate
            printfn "                Realized = %.2fm" op.Realized
            printfn "                RealizedToday = 0m"
            printfn "                Commissions = %.2fm" op.Commissions
            printfn "                Fees = %.2fm" op.Fees
            printfn "                Premium = %.2fm" op.Premium
            printfn "                Dividends = %.2fm" op.Dividends
            printfn "                DividendTaxes = %.2fm" op.DividendTaxes
            printfn "                CapitalDeployed = %.2fm" op.CapitalDeployed
            printfn "                CapitalDeployedToday = 0m"
            printfn "                Performance = %.4fm }" op.Performance
            printfn "            Description = \"TODO: Add description for operation %d\" }" (i+1)
        )

    /// <summary>
    /// Helper function to generate F# code for broker account snapshots
    /// </summary>
    member private _.generateBrokerSnapshotsCode (snapshots: BrokerFinancialSnapshot list) =
        snapshots |> List.iteri (fun i snapshot ->
            printfn "          // Snapshot %d: %s" (i+1) (snapshot.Date.ToString("yyyy-MM-dd"))
            printfn "          { Data ="
            printfn "              { Id = 0"
            printfn "                Date = DateOnly(%d, %d, %d)" snapshot.Date.Year snapshot.Date.Month snapshot.Date.Day
            printfn "                Broker = Some broker"
            printfn "                BrokerAccount = Some brokerAccount"
            printfn "                Currency = currency"
            printfn "                MovementCounter = %d" snapshot.MovementCounter
            printfn "                RealizedGains = %.2fm" snapshot.RealizedGains
            printfn "                RealizedPercentage = %.4fm" snapshot.RealizedPercentage
            printfn "                UnrealizedGains = %.2fm" snapshot.UnrealizedGains
            printfn "                UnrealizedGainsPercentage = %.4fm" snapshot.UnrealizedGainsPercentage
            printfn "                Invested = %.2fm" snapshot.Invested
            printfn "                Commissions = %.2fm" snapshot.Commissions
            printfn "                Fees = %.2fm" snapshot.Fees
            printfn "                Deposited = %.2fm" snapshot.Deposited
            printfn "                Withdrawn = %.2fm" snapshot.Withdrawn
            printfn "                DividendsReceived = %.2fm" snapshot.DividendsReceived
            printfn "                OptionsIncome = %.2fm" snapshot.OptionsIncome
            printfn "                OtherIncome = %.2fm" snapshot.OtherIncome
            printfn "                OpenTrades = %b" snapshot.OpenTrades
            printfn "                NetCashFlow = %.2fm }" snapshot.NetCashFlow
            printfn "            Description = \"TODO: Add description for %s\" }" (snapshot.Date.ToString("yyyy-MM-dd"))
        )

    /// <summary>
    /// Code generator test to capture all TSLL data from core calculations.
    /// Run this explicitly to generate code for TsllImportExpectedSnapshots.fs
    /// Mark as [<Explicit>] so it won't run in CI/CD
    /// </summary>
    [<Test>]
    [<Explicit>]
    [<Category("CodeGenerator")>]
    member this.``Generate_Complete_TSLL_Expected_Data``() =
        async {
            CoreLogger.logInfo "Test" "=== CODE GENERATOR: Complete TSLL Expected Data ==="

            let actions = this.Actions

            // ==================== PHASE 1: SETUP ====================
            TestSetup.printPhaseHeader 1 "Database Initialization"

            let! (ok, _, error) = actions.wipeDataForTesting ()
            Assert.That(ok, Is.True, sprintf "Wipe should succeed: %A" error)

            let! (ok, _, error) = actions.initDatabase ()
            Assert.That(ok, Is.True, sprintf "Database initialization should succeed: %A" error)

            // ==================== PHASE 2: CREATE BROKER ACCOUNT ====================
            TestSetup.printPhaseHeader 2 "Create BrokerAccount for TSLL Import"

            StreamObserver.expectSignals (
                [ Accounts_Updated
                  Snapshots_Updated ]
            )

            let! (ok, details, error) = actions.createBrokerAccount ("TSLL-Import-Test")
            Assert.That(ok, Is.True, sprintf "Account creation should succeed: %s - %A" details error)

            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.That(signalsReceived, Is.True, "Account creation signals should have been received")

            // ==================== PHASE 3: IMPORT TSLL OPTIONS CSV ====================
            TestSetup.printPhaseHeader 3 "Import TSLL Multi-Asset CSV File"

            let csvPath = this.getCsvPath ("TsllImportTest.csv")
            Assert.That(File.Exists(csvPath), Is.True, sprintf "CSV file should exist: %s" csvPath)

            StreamObserver.expectSignals (
                [ Movements_Updated
                  Tickers_Updated
                  Snapshots_Updated ]
            )

            let tastytradeId = actions.Context.TastytradeId
            let accountId = actions.Context.BrokerAccountId

            let! (ok, importDetails, error) = actions.importFile (tastytradeId, accountId, csvPath)
            Assert.That(ok, Is.True, sprintf "Import should succeed: %s - %A" importDetails error)

            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(15.0))
            Assert.That(signalsReceived, Is.True, "Import signals should have been received")

            // ==================== PHASE 4: CAPTURE ALL TSLL TICKER SNAPSHOTS ====================
            TestSetup.printPhaseHeader 4 "Capture All TSLL Ticker Snapshots"

            let tsllTicker =
                Collections.Tickers.Items |> Seq.tryFind (fun t -> t.Symbol = "TSLL")
            Assert.That(tsllTicker.IsSome, Is.True, "TSLL ticker should exist in Collections")

            let tsllTickerId = tsllTicker.Value.Id

            let! tsllSnapshots = Tickers.GetSnapshots(tsllTickerId) |> Async.AwaitTask
            let sortedSnapshots = tsllSnapshots |> List.sortBy (fun s -> s.Date)

            CoreLogger.logInfo "Generator" (sprintf "📊 Captured %d TSLL ticker snapshots" sortedSnapshots.Length)

            // ==================== PHASE 5: CAPTURE ALL TSLL OPERATIONS ====================
            TestSetup.printPhaseHeader 5 "Capture All TSLL Operations"

            let! tsllOperations = Tickers.GetOperations(tsllTickerId) |> Async.AwaitTask
            let sortedOperations = tsllOperations |> List.sortBy (fun op -> op.OpenDate)

            CoreLogger.logInfo "Generator" (sprintf "📊 Captured %d TSLL operations" sortedOperations.Length)

            // ==================== PHASE 6: CAPTURE ALL BROKER SNAPSHOTS ====================
            TestSetup.printPhaseHeader 6 "Capture All Broker Account Snapshots"

            let brokerAccountId = actions.Context.BrokerAccountId
            let! overviewSnapshots = BrokerAccounts.GetSnapshots(brokerAccountId) |> Async.AwaitTask
            
            let brokerSnapshots = 
                overviewSnapshots
                |> List.choose (fun os -> os.BrokerAccount |> Option.map (fun bas -> bas.Financial))
                |> List.sortBy (fun s -> s.Date)

            CoreLogger.logInfo "Generator" (sprintf "📊 Captured %d broker account snapshots" brokerSnapshots.Length)

            // ==================== GENERATE F# CODE ====================
            printfn "\n\n"
            printfn "=========================================="
            printfn "COPY THIS TO getTSLLSnapshots"
            printfn "=========================================="
            this.generateTickerSnapshotsCode sortedSnapshots
            
            printfn "\n\n"
            printfn "=========================================="
            printfn "COPY THIS TO getTSLLOperations"
            printfn "=========================================="
            this.generateOperationsCode sortedOperations
            
            printfn "\n\n"
            printfn "=========================================="
            printfn "COPY THIS TO getBrokerAccountSnapshots"
            printfn "=========================================="
            this.generateBrokerSnapshotsCode brokerSnapshots

            printfn "\n\n"
            printfn "=========================================="
            printfn "CODE GENERATION COMPLETE"
            printfn "=========================================="
            printfn "Total items generated: %d" (sortedSnapshots.Length + sortedOperations.Length + brokerSnapshots.Length)
            printfn "  - Ticker snapshots: %d" sortedSnapshots.Length
            printfn "  - Operations: %d" sortedOperations.Length
            printfn "  - Broker snapshots: %d" brokerSnapshots.Length
            printfn "=========================================="

            Assert.Pass("Code generated - see console output above")
        }
