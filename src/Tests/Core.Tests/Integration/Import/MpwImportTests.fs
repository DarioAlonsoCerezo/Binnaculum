namespace Core.Tests.Integration

open NUnit.Framework
open System
open System.IO
open Binnaculum.Core.Models
open Binnaculum.Core.UI
open Binnaculum.Core.Logging
open TestModels

/// <summary>
/// MPW Multi-Asset import signal-based reactive integration tests.
/// Validates complex multi-asset trading with options, equities, and dividends.
///
/// This test follows the same pattern as the TsllImportTests test.
///
/// Inherits from TestFixtureBase - no setup/teardown boilerplate needed.
///
/// See README.md for pattern documentation and more examples.
/// See PATTERN_GUIDE.fs for detailed implementation guide.
/// </summary>
[<TestFixture>]
type MpwImportTests() =
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
    /// Test: MPW multi-asset import CSV workflow with signal validation
    /// Follows the pattern of TsllImportTests.
    ///
    /// This test validates:
    /// 1. Database initialization completes successfully
    /// 2. BrokerAccount creation triggers Accounts_Updated and Snapshots_Updated signals
    /// 3. CSV import triggers Movements_Updated, Tickers_Updated, and Snapshots_Updated signals
    /// 4. Multiple MPW movements are imported (equity shares + options)
    /// 5. MPW ticker snapshots are verified
    /// 6. All MPW operations verified
    /// 7. All broker account snapshots verified
    /// 8. Financial calculations are correct with complex multi-asset data
    ///
    /// The test uses signal-based verification instead of timing-based assertions,
    /// making it robust and portable across platforms.
    ///
    /// CSV Data: MPWImportTest.csv
    /// - Multiple equity share trades (buy/sell 1000 MPW shares)
    /// - Multiple option trades (calls and puts with various strikes/expirations)
    /// - Dividends and deposits
    /// - Date range: 2024-09-17 through 2025-10-17
    /// - Complex FIFO matching scenarios across asset types
    ///
    /// Expected Results:
    /// - Multiple movements (varies by CSV)
    /// - Multiple tickers (MPW + SPY default)
    /// - MPW ticker snapshots, operations, and broker snapshots to be determined by test run
    /// </summary>
    [<Test>]
    [<Category("Integration")>]
    member this.``MPW multi-asset import CSV workflow with signal validation``() =
        async {
            CoreLogger.logInfo "Test" "=== TEST: MPW Multi-Asset Import CSV Workflow with Signal Validation ==="

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
            TestSetup.printPhaseHeader 2 "Create BrokerAccount for MPW Import"

            // EXPECT: Declare expected signals BEFORE operation
            StreamObserver.expectSignals (
                [ Accounts_Updated // Account added to Collections.Accounts
                  Snapshots_Updated ] // Snapshot calculated in Collections.Snapshots
            )

            CoreLogger.logDebug "StreamObserver" "🎯 Expecting signals: Accounts_Updated, Snapshots_Updated"

            // EXECUTE: Create account
            let! (ok, details, error) = actions.createBrokerAccount ("MPW-Import-Test")
            Assert.That(ok, Is.True, sprintf "Account creation should succeed: %s - %A" details error)
            CoreLogger.logInfo "Verification" (sprintf "✅ BrokerAccount created: %s" details)

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            CoreLogger.logInfo "TestActions" "⏳ Waiting for account creation reactive signals..."
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.That(signalsReceived, Is.True, "Account creation signals should have been received")
            CoreLogger.logInfo "Verification" "✅ Account creation signals received successfully"

            // ==================== PHASE 3: IMPORT MPW CSV ====================
            TestSetup.printPhaseHeader 3 "Import MPW Multi-Asset CSV File"

            // Get CSV path
            let csvPath = this.getCsvPath ("MPWImportTest.csv")
            CoreLogger.logDebug "Import" (sprintf "📄 CSV file path: %s" csvPath)
            Assert.That(File.Exists(csvPath), Is.True, sprintf "CSV file should exist: %s" csvPath)

            // EXPECT: Declare expected signals BEFORE import operation
            StreamObserver.expectSignals (
                [ Movements_Updated // Trades added
                  Tickers_Updated // MPW ticker added
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

            // ==================== PHASE 4: VERIFY MPW TICKER SNAPSHOTS ====================
            TestSetup.printPhaseHeader 4 "Verify MPW Ticker Snapshots with Complete Financial State"

            // Get MPW ticker and USD currency from Collections
            let mpwTicker =
                Collections.Tickers.Items |> Seq.tryFind (fun t -> t.Symbol = "MPW")

            Assert.That(mpwTicker.IsSome, Is.True, "MPW ticker should exist in Collections")

            let mpwTickerId = mpwTicker.Value.Id
            let usd = Collections.Currencies.Items |> Seq.find (fun c -> c.Code = "USD")
            CoreLogger.logInfo "Verification" (sprintf "📊 MPW Ticker ID: %d" mpwTickerId)

            // Get all MPW snapshots using Tickers.GetSnapshots from Core
            let! mpwSnapshots = Tickers.GetSnapshots(mpwTickerId) |> Async.AwaitTask
            let sortedMPWSnapshots = mpwSnapshots |> List.sortBy (fun s -> s.Date)

            CoreLogger.logInfo
                "Verification"
                (sprintf "📊 Found %d MPW snapshots in database" sortedMPWSnapshots.Length)

            // TEMPORARY: Log actual snapshots for data population
            CoreLogger.logInfo "DataExtraction" "=== MPW TICKER SNAPSHOTS (F# format) ==="
            sortedMPWSnapshots
            |> List.iteri (fun i snapshot ->
                let s = snapshot.MainCurrency
                CoreLogger.logInfo "DataExtraction" (sprintf "// Snapshot %d: %s" i (snapshot.Date.ToString("yyyy-MM-dd")))
                CoreLogger.logInfo "DataExtraction" "{ Data ="
                CoreLogger.logInfo "DataExtraction" (sprintf "    { Id = 0; Date = DateOnly(%d, %d, %d)" snapshot.Date.Year snapshot.Date.Month snapshot.Date.Day)
                CoreLogger.logInfo "DataExtraction" (sprintf "      Ticker = ticker; Currency = currency")
                CoreLogger.logInfo "DataExtraction" (sprintf "      TotalShares = %.2fm; Weight = %.4fm" s.TotalShares s.Weight)
                CoreLogger.logInfo "DataExtraction" (sprintf "      CostBasis = %.2fm; RealCost = %.2fm" s.CostBasis s.RealCost)
                CoreLogger.logInfo "DataExtraction" (sprintf "      Dividends = %.2fm; DividendTaxes = %.2fm" s.Dividends s.DividendTaxes)
                CoreLogger.logInfo "DataExtraction" (sprintf "      Options = %.2fm; TotalIncomes = %.2fm" s.Options s.TotalIncomes)
                CoreLogger.logInfo "DataExtraction" (sprintf "      CapitalDeployed = %.2fm; Realized = %.2fm" s.CapitalDeployed s.Realized)
                CoreLogger.logInfo "DataExtraction" (sprintf "      Performance = %.4fm; OpenTrades = %b" s.Performance s.OpenTrades)
                CoreLogger.logInfo "DataExtraction" (sprintf "      Commissions = %.2fm; Fees = %.2fm }" s.Commissions s.Fees)
                CoreLogger.logInfo "DataExtraction" (sprintf "  Description = \"Snapshot %d: %s\" }" i (snapshot.Date.ToString("yyyy-MM-dd")))
            )

            // Get expected MPW snapshots with descriptions from MpwImportExpectedSnapshots
            let expectedMPWSnapshotsWithDescriptions =
                MpwImportExpectedSnapshots.getMPWSnapshots mpwTicker.Value usd

            CoreLogger.logInfo
                "Verification"
                (sprintf "📊 Validating %d expected MPW snapshots" expectedMPWSnapshotsWithDescriptions.Length)

            // Extract data and descriptions
            let expectedMPWSnapshots =
                expectedMPWSnapshotsWithDescriptions |> TestModels.getData

            // Filter actual snapshots to only include the dates we're validating
            let expectedDates =
                expectedMPWSnapshots |> List.map (fun s -> s.Date) |> Set.ofList

            let actualMPWSnapshotsFiltered =
                sortedMPWSnapshots
                |> List.filter (fun s -> expectedDates.Contains(s.Date))
                |> List.map (fun s -> s.MainCurrency)

            CoreLogger.logInfo
                "Verification"
                (sprintf "📊 Found %d matching snapshots to validate" actualMPWSnapshotsFiltered.Length)

            // Description function using the pre-defined descriptions
            let getMPWDescription i =
                expectedMPWSnapshotsWithDescriptions.[i].Description

            // Use base class method for verification (only validates the snapshots we defined)
            this.VerifyTickerSnapshots "MPW" expectedMPWSnapshots actualMPWSnapshotsFiltered getMPWDescription

            // ==================== SHARED: GET BROKER AND ACCOUNT ====================
            // Get broker and broker account for operations and broker snapshot validation
            let broker = Collections.Brokers.Items |> Seq.find (fun b -> b.Name = "Tastytrade")

            let brokerAccount =
                Collections.Accounts.Items
                |> Seq.filter (fun a -> a.Type = AccountType.BrokerAccount)
                |> Seq.pick (fun a -> a.Broker)

            // ==================== PHASE 5: VERIFY AUTO-IMPORT OPERATIONS ====================
            TestSetup.printPhaseHeader 5 "Verify Auto-Import Operations"

            // Get all operations for MPW ticker
            let! mpwOperations = Tickers.GetOperations(mpwTickerId) |> Async.AwaitTask

            CoreLogger.logInfo "Verification" (sprintf "📊 Found %d MPW operations in database" mpwOperations.Length)

            // TEMPORARY: Log actual operations for data population
            CoreLogger.logInfo "DataExtraction" "=== MPW OPERATIONS (F# format) ==="
            mpwOperations
            |> List.iteri (fun i op ->
                CoreLogger.logInfo "DataExtraction" (sprintf "// Operation %d: %s" i (op.OpenDate.ToString("yyyy-MM-dd")))
                CoreLogger.logInfo "DataExtraction" "{ Data ="
                CoreLogger.logInfo "DataExtraction" (sprintf "    { Id = 0; BrokerAccount = brokerAccount; Ticker = ticker; Currency = currency")
                CoreLogger.logInfo "DataExtraction" (sprintf "      IsOpen = %b" op.IsOpen)
                CoreLogger.logInfo "DataExtraction" (sprintf "      OpenDate = DateTime(%d, %d, %d, 0, 0, 1)" op.OpenDate.Year op.OpenDate.Month op.OpenDate.Day)
                let closeStr = match op.CloseDate with | Some d -> sprintf "Some(DateTime(%d, %d, %d, 0, 0, 1))" d.Year d.Month d.Day | None -> "None"
                CoreLogger.logInfo "DataExtraction" (sprintf "      CloseDate = %s" closeStr)
                CoreLogger.logInfo "DataExtraction" (sprintf "      Realized = %.2fm; RealizedToday = %.2fm" op.Realized op.RealizedToday)
                CoreLogger.logInfo "DataExtraction" (sprintf "      Commissions = %.2fm; Fees = %.2fm" op.Commissions op.Fees)
                CoreLogger.logInfo "DataExtraction" (sprintf "      Premium = %.2fm; Dividends = %.2fm; DividendTaxes = %.2fm" op.Premium op.Dividends op.DividendTaxes)
                CoreLogger.logInfo "DataExtraction" (sprintf "      CapitalDeployed = %.2fm; CapitalDeployedToday = %.2fm" op.CapitalDeployed op.CapitalDeployedToday)
                CoreLogger.logInfo "DataExtraction" (sprintf "      Performance = %.4fm }" op.Performance)
                CoreLogger.logInfo "DataExtraction" (sprintf "  Description = \"Operation %d: %s\" }" i (op.OpenDate.ToString("yyyy-MM-dd")))
            )

            // Get expected operations with descriptions
            let expectedOperationsWithDescriptions =
                MpwImportExpectedSnapshots.getMPWOperations brokerAccount mpwTicker.Value usd

            let expectedOperations =
                expectedOperationsWithDescriptions |> TestModels.getOperationData

            CoreLogger.logInfo "Verification" (sprintf "📊 Validating %d expected operations" expectedOperations.Length)

            // Filter actual operations to only include the ones we're validating
            let expectedOpenDates =
                expectedOperations |> List.map (fun op -> op.OpenDate) |> Set.ofList

            let actualOperationsFiltered =
                mpwOperations
                |> List.filter (fun op -> expectedOpenDates.Contains(op.OpenDate))

            CoreLogger.logInfo
                "Verification"
                (sprintf "📊 Found %d matching operations to validate" actualOperationsFiltered.Length)

            // Verify each MPW operation
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

                    Assert.Fail(sprintf "MPW Operation %d (%s) verification failed" i description)
                else
                    CoreLogger.logInfo "Verification" (sprintf "✅ Operation %d (%s) verified" i description))

            CoreLogger.logInfo
                "Verification"
                (sprintf "✅ All %d MPW operations verified" actualOperationsFiltered.Length)

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

            // TEMPORARY: Log actual broker snapshots for data population
            CoreLogger.logInfo "DataExtraction" "=== BROKER ACCOUNT SNAPSHOTS (F# format) ==="
            brokerFinancialSnapshots
            |> List.iteri (fun i bs ->
                CoreLogger.logInfo "DataExtraction" (sprintf "// BrokerSnapshot %d: %s" i (bs.Date.ToString("yyyy-MM-dd")))
                CoreLogger.logInfo "DataExtraction" "{ Data ="
                CoreLogger.logInfo "DataExtraction" (sprintf "    { Id = 0; Date = DateOnly(%d, %d, %d)" bs.Date.Year bs.Date.Month bs.Date.Day)
                CoreLogger.logInfo "DataExtraction" (sprintf "      Broker = broker; BrokerAccount = brokerAccount; Currency = currency")
                CoreLogger.logInfo "DataExtraction" (sprintf "      MovementCounter = %d" bs.MovementCounter)
                CoreLogger.logInfo "DataExtraction" (sprintf "      RealizedGains = %.2fm; RealizedPercentage = %.4fm" bs.RealizedGains bs.RealizedPercentage)
                CoreLogger.logInfo "DataExtraction" (sprintf "      UnrealizedGains = %.2fm; UnrealizedGainsPercentage = %.4fm" bs.UnrealizedGains bs.UnrealizedGainsPercentage)
                CoreLogger.logInfo "DataExtraction" (sprintf "      Invested = %.2fm; Commissions = %.2fm; Fees = %.2fm" bs.Invested bs.Commissions bs.Fees)
                CoreLogger.logInfo "DataExtraction" (sprintf "      Deposited = %.2fm; Withdrawn = %.2fm" bs.Deposited bs.Withdrawn)
                CoreLogger.logInfo "DataExtraction" (sprintf "      DividendsReceived = %.2fm; OptionsIncome = %.2fm; OtherIncome = %.2fm" bs.DividendsReceived bs.OptionsIncome bs.OtherIncome)
                CoreLogger.logInfo "DataExtraction" (sprintf "      OpenTrades = %b; NetCashFlow = %.2fm }" bs.OpenTrades bs.NetCashFlow)
                CoreLogger.logInfo "DataExtraction" (sprintf "  Description = \"BrokerSnapshot %d: %s\" }" i (bs.Date.ToString("yyyy-MM-dd")))
            )

            // Get expected broker account snapshots (broker and brokerAccount already defined above)
            let expectedBrokerSnapshotsWithDescriptions =
                MpwImportExpectedSnapshots.getBrokerAccountSnapshots broker brokerAccount usd

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
            let totalValidations = 
                expectedMPWSnapshots.Length + 
                expectedOperations.Length + 
                expectedBrokerSnapshots.Length
                
            TestSetup.printTestCompletionSummary
                "MPW Multi-Asset Import with Complete Financial Verification"
                (sprintf "Successfully created BrokerAccount, imported MPW CSV, verified all %d ticker snapshots, all %d operations, and all %d broker snapshots (%d items total)" 
                    expectedMPWSnapshots.Length 
                    expectedOperations.Length 
                    expectedBrokerSnapshots.Length 
                    totalValidations)

            CoreLogger.logInfo "Test" "=== TEST COMPLETED SUCCESSFULLY ==="
        }
