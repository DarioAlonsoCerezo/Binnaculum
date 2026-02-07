namespace Core.Tests.Integration

open Microsoft.VisualStudio.TestTools.UnitTesting
open System
open System.IO
open Binnaculum.Core.Models
open Binnaculum.Core.UI
open Binnaculum.Core.Logging
open TestModels

/// <summary>
/// Options import signal-based reactive integration tests.
/// Validates CSV import workflows with realistic Tastytrade options trading data.
///
/// This test replicates the exact workflow of the MAUI Tester's
/// "RunOptionsImportIntegrationSignalBasedTestButton" test.
///
/// Inherits from TestFixtureBase - no setup/teardown boilerplate needed.
///
/// See README.md for pattern documentation and more examples.
/// See PATTERN_GUIDE.fs for detailed implementation guide.
/// </summary>
[<TestClass>]
type OptionsImportTests() =
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
    [<TestMethod>]
    [<TestCategory("Integration")>]
    member this.``Options import from CSV updates collections``() =
        async {
            CoreLogger.logInfo "Test" "=== TEST: Options Import from CSV Updates Collections ==="

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
            TestSetup.printPhaseHeader 2 "Create BrokerAccount for Import"

            // EXPECT: Declare expected signals BEFORE operation
            StreamObserver.expectSignals (
                [ Accounts_Updated // Account added to Collections.Accounts
                  Snapshots_Updated ] // Snapshot calculated in Collections.Snapshots
            )

            CoreLogger.logDebug "StreamObserver" "🎯 Expecting signals: Accounts_Updated, Snapshots_Updated"

            // EXECUTE: Create account
            let! (ok, details, error) = actions.createBrokerAccount ("Options-Import-Test")
            Assert.IsTrue(ok, sprintf "Account creation should succeed: %s - %A" details error)
            CoreLogger.logInfo "Verification" (sprintf "✅ BrokerAccount created: %s" details)

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            CoreLogger.logInfo "TestActions" "⏳ Waiting for account creation reactive signals..."
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.IsTrue(signalsReceived, "Account creation signals should have been received")
            CoreLogger.logInfo "Verification" "✅ Account creation signals received successfully"

            // ==================== PHASE 3: IMPORT OPTIONS CSV ====================
            TestSetup.printPhaseHeader 3 "Import Options CSV File"

            // Get CSV path
            let csvPath = this.getCsvPath ("TastytradeOptionsTest.csv")
            CoreLogger.logDebug "Import" (sprintf "📄 CSV file path: %s" csvPath)
            Assert.IsTrue(File.Exists(csvPath), sprintf "CSV file should exist: %s" csvPath)

            // EXPECT: Declare expected signals BEFORE import operation
            StreamObserver.expectSignals (
                [ Movements_Updated // Option trades and money movements added
                  Tickers_Updated // New tickers (SOFI, PLTR, MPW) added
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

            // ==================== PHASE 4: VERIFY SOFI TICKER SNAPSHOTS CHRONOLOGICALLY ====================
            TestSetup.printPhaseHeader 4 "Verify SOFI Ticker Snapshots with Complete Financial State"

            // Get SOFI ticker and USD currency from Collections
            let sofiTicker =
                Collections.Tickers.Items |> Seq.tryFind (fun t -> t.Symbol = "SOFI")

            Assert.IsTrue(sofiTicker.IsSome, "SOFI ticker should exist in Collections")

            let sofiTickerId = sofiTicker.Value.Id
            let usd = Collections.Currencies.Items |> Seq.find (fun c -> c.Code = "USD")
            CoreLogger.logInfo "Verification" (sprintf "📊 SOFI Ticker ID: %d" sofiTickerId)

            // Get all SOFI snapshots using Tickers.GetSnapshots from Core
            let! sofiSnapshots = Tickers.GetSnapshots(sofiTickerId) |> Async.AwaitTask
            let sortedSOFISnapshots = sofiSnapshots |> List.sortBy (fun s -> s.Date)

            CoreLogger.logInfo "Verification" (sprintf "📊 Found %d SOFI snapshots" sortedSOFISnapshots.Length)

            Assert.AreEqual(4, sortedSOFISnapshots.Length, "Should have 4 SOFI snapshots (2024-04-25, 04-29, 04-30 + today)"
            )

            // Get expected SOFI snapshots with descriptions from OptionsImportExpectedSnapshots
            let expectedSOFISnapshotsWithDescriptions =
                OptionsImportExpectedSnapshots.getSOFISnapshots sofiTicker.Value usd

            // Extract data and descriptions
            let expectedSOFISnapshots =
                expectedSOFISnapshotsWithDescriptions |> TestModels.getData

            let actualSOFISnapshots = sortedSOFISnapshots |> List.map (fun s -> s.MainCurrency)

            // Description function using the pre-defined descriptions
            let getSOFIDescription i =
                expectedSOFISnapshotsWithDescriptions.[i].Description

            // Use base class method for verification
            this.VerifyTickerSnapshots "SOFI" expectedSOFISnapshots actualSOFISnapshots getSOFIDescription

            // ==================== PHASE 5: VERIFY MPW TICKER SNAPSHOTS CHRONOLOGICALLY ====================
            TestSetup.printPhaseHeader 5 "Verify MPW Ticker Snapshots with Complete Financial State"

            // Get MPW ticker from Collections
            let mpwTicker = Collections.Tickers.Items |> Seq.tryFind (fun t -> t.Symbol = "MPW")
            Assert.IsTrue(mpwTicker.IsSome, "MPW ticker should exist in Collections")

            let mpwTickerId = mpwTicker.Value.Id
            CoreLogger.logInfo "Verification" (sprintf "📊 MPW Ticker ID: %d" mpwTickerId)

            // Get all MPW snapshots using Tickers.GetSnapshots from Core
            let! mpwSnapshots = Tickers.GetSnapshots(mpwTickerId) |> Async.AwaitTask
            let sortedMPWSnapshots = mpwSnapshots |> List.sortBy (fun s -> s.Date)

            CoreLogger.logInfo "Verification" (sprintf "📊 Found %d MPW snapshots" sortedMPWSnapshots.Length)

            Assert.AreEqual(3, sortedMPWSnapshots.Length, "Should have 3 MPW snapshots (2024-04-26, 04-29 + today)"
            )

            // Get expected MPW snapshots with descriptions from OptionsImportExpectedSnapshots
            let expectedMPWSnapshotsWithDescriptions =
                OptionsImportExpectedSnapshots.getMPWSnapshots mpwTicker.Value usd

            // Extract data and descriptions
            let expectedMPWSnapshots =
                expectedMPWSnapshotsWithDescriptions |> TestModels.getData

            let actualMPWSnapshots = sortedMPWSnapshots |> List.map (fun s -> s.MainCurrency)

            // Description function using the pre-defined descriptions
            let getMPWDescription i =
                expectedMPWSnapshotsWithDescriptions.[i].Description

            // Use base class method for verification
            this.VerifyTickerSnapshots "MPW" expectedMPWSnapshots actualMPWSnapshots getMPWDescription

            // ==================== PHASE 6: VERIFY PLTR TICKER SNAPSHOTS CHRONOLOGICALLY ====================
            TestSetup.printPhaseHeader 6 "Verify PLTR Ticker Snapshots with Complete Financial State"

            // Get PLTR ticker from Collections
            let pltrTicker =
                Collections.Tickers.Items |> Seq.tryFind (fun t -> t.Symbol = "PLTR")

            Assert.IsTrue(pltrTicker.IsSome, "PLTR ticker should exist in Collections")

            let pltrTickerId = pltrTicker.Value.Id
            CoreLogger.logInfo "Verification" (sprintf "📊 PLTR Ticker ID: %d" pltrTickerId)

            // Get all PLTR snapshots using Tickers.GetSnapshots from Core
            let! pltrSnapshots = Tickers.GetSnapshots(pltrTickerId) |> Async.AwaitTask
            let sortedPLTRSnapshots = pltrSnapshots |> List.sortBy (fun s -> s.Date)

            CoreLogger.logInfo "Verification" (sprintf "📊 Found %d PLTR snapshots" sortedPLTRSnapshots.Length)

            Assert.AreEqual(3, sortedPLTRSnapshots.Length, "Should have 3 PLTR snapshots (2024-04-26, 04-29 + today)"
            )

            // Get expected PLTR snapshots with descriptions from OptionsImportExpectedSnapshots
            let expectedPLTRSnapshotsWithDescriptions =
                OptionsImportExpectedSnapshots.getPLTRSnapshots pltrTicker.Value usd

            // Extract data and descriptions
            let expectedPLTRSnapshots =
                expectedPLTRSnapshotsWithDescriptions |> TestModels.getData

            let actualPLTRSnapshots = sortedPLTRSnapshots |> List.map (fun s -> s.MainCurrency)

            // Description function using the pre-defined descriptions
            let getPLTRDescription i =
                expectedPLTRSnapshotsWithDescriptions.[i].Description

            // Use base class method for verification
            this.VerifyTickerSnapshots "PLTR" expectedPLTRSnapshots actualPLTRSnapshots getPLTRDescription

            // ==================== PHASE 7: VERIFY AUTO-IMPORT OPERATIONS ====================
            TestSetup.printPhaseHeader 7 "Verify Auto-Import Operations"

            // Get broker and broker account (needed for operation expectations)
            let broker = Collections.Brokers.Items |> Seq.find (fun b -> b.Name = "Tastytrade")

            let brokerAccount =
                Collections.Accounts.Items
                |> Seq.filter (fun a -> a.Type = AccountType.BrokerAccount)
                |> Seq.pick (fun a -> a.Broker)

            // --- SOFI Operations ---
            CoreLogger.logInfo "Verification" (sprintf "📊 SOFI Ticker ID: %d" sofiTickerId)

            // Get all SOFI operations from database
            let! sofiOperations = Tickers.GetOperations(sofiTickerId) |> Async.AwaitTask
            CoreLogger.logInfo "Verification" (sprintf "📊 Found %d operations for SOFI" sofiOperations.Length)

            // Get expected SOFI operations from OptionsImportExpectedSnapshots
            let expectedSOFIOperationsWithDescriptions =
                OptionsImportExpectedSnapshots.getSOFIOperations brokerAccount sofiTicker.Value usd

            let expectedSOFIOperations =
                expectedSOFIOperationsWithDescriptions |> TestModels.getOperationData

            // Verify SOFI operation count
            Assert.AreEqual(expectedSOFIOperations.Length, sofiOperations.Length, sprintf "Expected %d SOFI operations but found %d" expectedSOFIOperations.Length sofiOperations.Length)

            // Verify each SOFI operation
            let sofiOperationResults =
                TestVerifications.verifyAutoImportOperationList expectedSOFIOperations sofiOperations

            sofiOperationResults
            |> List.iteri (fun i (allMatch, fieldResults) ->
                let description = expectedSOFIOperationsWithDescriptions.[i].Description

                if not allMatch then
                    let formatted = TestVerifications.formatValidationResults fieldResults

                    CoreLogger.logError
                        "Verification"
                        (sprintf "❌ Operation %d (%s) failed:\n%s" i description formatted)

                    Assert.Fail(sprintf "SOFI Operation %d (%s) verification failed" i description)
                else
                    CoreLogger.logInfo "Verification" (sprintf "✅ Operation %d (%s) verified" i description))

            CoreLogger.logInfo "Verification" (sprintf "✅ All %d operations verified" sofiOperations.Length)

            // --- MPW Operations ---
            CoreLogger.logInfo "Verification" (sprintf "📊 MPW Ticker ID: %d" mpwTickerId)

            // Get all MPW operations from database
            let! mpwOperations = Tickers.GetOperations(mpwTickerId) |> Async.AwaitTask
            CoreLogger.logInfo "Verification" (sprintf "📊 Found %d operations for MPW" mpwOperations.Length)

            // Get expected MPW operations from OptionsImportExpectedSnapshots
            let expectedMPWOperationsWithDescriptions =
                OptionsImportExpectedSnapshots.getMPWOperations brokerAccount mpwTicker.Value usd

            let expectedMPWOperations =
                expectedMPWOperationsWithDescriptions |> TestModels.getOperationData

            // Verify MPW operation count
            Assert.AreEqual(expectedMPWOperations.Length, mpwOperations.Length, sprintf "Expected %d MPW operations but found %d" expectedMPWOperations.Length mpwOperations.Length)

            // Verify each MPW operation
            let mpwOperationResults =
                TestVerifications.verifyAutoImportOperationList expectedMPWOperations mpwOperations

            mpwOperationResults
            |> List.iteri (fun i (allMatch, fieldResults) ->
                let description = expectedMPWOperationsWithDescriptions.[i].Description

                if not allMatch then
                    let formatted = TestVerifications.formatValidationResults fieldResults

                    CoreLogger.logError
                        "Verification"
                        (sprintf "❌ Operation %d (%s) failed:\n%s" i description formatted)

                    Assert.Fail(sprintf "MPW Operation %d (%s) verification failed" i description)
                else
                    CoreLogger.logInfo "Verification" (sprintf "✅ Operation %d (%s) verified" i description))

            CoreLogger.logInfo "Verification" (sprintf "✅ All %d operations verified" mpwOperations.Length)

            // --- PLTR Operations ---
            CoreLogger.logInfo "Verification" (sprintf "📊 PLTR Ticker ID: %d" pltrTickerId)

            // Get all PLTR operations from database
            let! pltrOperations = Tickers.GetOperations(pltrTickerId) |> Async.AwaitTask
            CoreLogger.logInfo "Verification" (sprintf "📊 Found %d operations for PLTR" pltrOperations.Length)

            // Get expected PLTR operations from OptionsImportExpectedSnapshots
            let expectedPLTROperationsWithDescriptions =
                OptionsImportExpectedSnapshots.getPLTROperations brokerAccount pltrTicker.Value usd

            let expectedPLTROperations =
                expectedPLTROperationsWithDescriptions |> TestModels.getOperationData

            // Verify PLTR operation count
            Assert.AreEqual(expectedPLTROperations.Length, pltrOperations.Length, sprintf "Expected %d PLTR operations but found %d" expectedPLTROperations.Length pltrOperations.Length)

            // Verify each PLTR operation
            let pltrOperationResults =
                TestVerifications.verifyAutoImportOperationList expectedPLTROperations pltrOperations

            pltrOperationResults
            |> List.iteri (fun i (allMatch, fieldResults) ->
                let description = expectedPLTROperationsWithDescriptions.[i].Description

                if not allMatch then
                    let formatted = TestVerifications.formatValidationResults fieldResults

                    CoreLogger.logError
                        "Verification"
                        (sprintf "❌ Operation %d (%s) failed:\n%s" i description formatted)

                    Assert.Fail(sprintf "PLTR Operation %d (%s) verification failed" i description)
                else
                    CoreLogger.logInfo "Verification" (sprintf "✅ Operation %d (%s) verified" i description))

            CoreLogger.logInfo "Verification" (sprintf "✅ All %d operations verified" pltrOperations.Length)

            // ==================== PHASE 8: VERIFY BROKER ACCOUNT FINANCIAL SNAPSHOTS ====================
            TestSetup.printPhaseHeader 8 "Verify Broker Account Financial Snapshots"

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

            Assert.AreEqual(9, brokerFinancialSnapshots.Length, "Should have 9 BrokerAccount snapshots")

            // Get broker and currency for snapshot construction
            let broker = Collections.Brokers.Items |> Seq.find (fun b -> b.Name = "Tastytrade")

            let brokerAccount =
                Collections.Accounts.Items
                |> Seq.filter (fun a -> a.Type = AccountType.BrokerAccount)
                |> Seq.pick (fun a -> a.Broker)

            let usd = Collections.Currencies.Items |> Seq.find (fun c -> c.Code = "USD")

            // Get expected BrokerAccount snapshots with descriptions from OptionsImportExpectedSnapshots
            let expectedBrokerSnapshotsWithDescriptions =
                OptionsImportExpectedSnapshots.getBrokerAccountSnapshots broker brokerAccount usd

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
                "Options Import from CSV"
                "Successfully created BrokerAccount, imported options CSV, received all signals, and verified SOFI, MPW, and PLTR ticker snapshots with complete financial state"

            CoreLogger.logInfo "Test" "=== TEST COMPLETED SUCCESSFULLY ==="
        } |> Async.StartAsTask :> System.Threading.Tasks.Task
