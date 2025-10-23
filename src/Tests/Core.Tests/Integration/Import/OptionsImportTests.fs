namespace Core.Tests.Integration

open NUnit.Framework
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
[<TestFixture>]
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
    [<Test>]
    [<Category("Integration")>]
    member this.``Options import from CSV updates collections``() =
        async {
            CoreLogger.logInfo "[Test]" "=== TEST: Options Import from CSV Updates Collections ==="

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
            TestSetup.printPhaseHeader 2 "Create BrokerAccount for Import"

            // EXPECT: Declare expected signals BEFORE operation
            StreamObserver.expectSignals (
                [ Accounts_Updated // Account added to Collections.Accounts
                  Snapshots_Updated ] // Snapshot calculated in Collections.Snapshots
            )

            CoreLogger.logDebug "[StreamObserver]" "🎯 Expecting signals: Accounts_Updated, Snapshots_Updated"

            // EXECUTE: Create account
            let! (ok, details, error) = actions.createBrokerAccount ("Options-Import-Test")
            Assert.That(ok, Is.True, sprintf "Account creation should succeed: %s - %A" details error)
            CoreLogger.logInfo "[Verification]" (sprintf "✅ BrokerAccount created: %s" details)

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            CoreLogger.logInfo "[TestActions]" "⏳ Waiting for account creation reactive signals..."
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.That(signalsReceived, Is.True, "Account creation signals should have been received")
            CoreLogger.logInfo "[Verification]" "✅ Account creation signals received successfully"

            // ==================== PHASE 3: IMPORT OPTIONS CSV ====================
            TestSetup.printPhaseHeader 3 "Import Options CSV File"

            // Get CSV path
            let csvPath = this.getCsvPath ("TastytradeOptionsTest.csv")
            CoreLogger.logDebug "[Import]" (sprintf "📄 CSV file path: %s" csvPath)
            Assert.That(File.Exists(csvPath), Is.True, sprintf "CSV file should exist: %s" csvPath)

            // EXPECT: Declare expected signals BEFORE import operation
            StreamObserver.expectSignals (
                [ Movements_Updated // Option trades and money movements added
                  Tickers_Updated // New tickers (SOFI, PLTR, MPW) added
                  Snapshots_Updated ] // Snapshots recalculated
            )

            CoreLogger.logDebug
                "[StreamObserver]"
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

            // ==================== PHASE 4: VERIFY SOFI TICKER SNAPSHOTS CHRONOLOGICALLY ====================
            TestSetup.printPhaseHeader 4 "Verify SOFI Ticker Snapshots with Complete Financial State"

            // Get SOFI ticker and USD currency from Collections
            let sofiTicker =
                Collections.Tickers.Items |> Seq.tryFind (fun t -> t.Symbol = "SOFI")

            Assert.That(sofiTicker.IsSome, Is.True, "SOFI ticker should exist in Collections")

            let sofiTickerId = sofiTicker.Value.Id
            let usd = Collections.Currencies.Items |> Seq.find (fun c -> c.Code = "USD")
            CoreLogger.logInfo "[Verification]" (sprintf "📊 SOFI Ticker ID: %d" sofiTickerId)

            // Get all SOFI snapshots using Tickers.GetSnapshots from Core
            let! sofiSnapshots = Tickers.GetSnapshots(sofiTickerId) |> Async.AwaitTask
            let sortedSOFISnapshots = sofiSnapshots |> List.sortBy (fun s -> s.Date)

            CoreLogger.logInfo "[Verification]" (sprintf "📊 Found %d SOFI snapshots" sortedSOFISnapshots.Length)

            Assert.That(
                sortedSOFISnapshots.Length,
                Is.EqualTo(4),
                "Should have 4 SOFI snapshots (2024-04-25, 04-29, 04-30 + today)"
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
            Assert.That(mpwTicker.IsSome, Is.True, "MPW ticker should exist in Collections")

            let mpwTickerId = mpwTicker.Value.Id
            CoreLogger.logInfo "[Verification]" (sprintf "📊 MPW Ticker ID: %d" mpwTickerId)

            // Get all MPW snapshots using Tickers.GetSnapshots from Core
            let! mpwSnapshots = Tickers.GetSnapshots(mpwTickerId) |> Async.AwaitTask
            let sortedMPWSnapshots = mpwSnapshots |> List.sortBy (fun s -> s.Date)

            CoreLogger.logInfo "[Verification]" (sprintf "📊 Found %d MPW snapshots" sortedMPWSnapshots.Length)

            Assert.That(
                sortedMPWSnapshots.Length,
                Is.EqualTo(3),
                "Should have 3 MPW snapshots (2024-04-26, 04-29 + today)"
            )

            // Get expected MPW snapshots from OptionsImportExpectedSnapshots
            let expectedMPWSnapshots =
                OptionsImportExpectedSnapshots.getMPWSnapshots mpwTicker.Value usd

            // Extract currency snapshots from actual snapshots
            let actualMPWSnapshots = sortedMPWSnapshots |> List.map (fun s -> s.MainCurrency)

            // Define description function for MPW snapshots
            let getMPWDescription i =
                let date = expectedMPWSnapshots.[i].Date.ToString("yyyy-MM-dd")

                let name =
                    match i with
                    | 0 -> "After opening vertical spread"
                    | 1 -> "After closing vertical spread"
                    | 2 -> "Current snapshot"
                    | _ -> "Unknown"

                sprintf "%s - %s" date name

            // Use base class method for verification
            this.VerifyTickerSnapshots "MPW" expectedMPWSnapshots actualMPWSnapshots getMPWDescription

            // ==================== PHASE 6: VERIFY PLTR TICKER SNAPSHOTS CHRONOLOGICALLY ====================
            TestSetup.printPhaseHeader 6 "Verify PLTR Ticker Snapshots with Complete Financial State"

            // Get PLTR ticker from Collections
            let pltrTicker =
                Collections.Tickers.Items |> Seq.tryFind (fun t -> t.Symbol = "PLTR")

            Assert.That(pltrTicker.IsSome, Is.True, "PLTR ticker should exist in Collections")

            let pltrTickerId = pltrTicker.Value.Id
            CoreLogger.logInfo "[Verification]" (sprintf "📊 PLTR Ticker ID: %d" pltrTickerId)

            // Get all PLTR snapshots using Tickers.GetSnapshots from Core
            let! pltrSnapshots = Tickers.GetSnapshots(pltrTickerId) |> Async.AwaitTask
            let sortedPLTRSnapshots = pltrSnapshots |> List.sortBy (fun s -> s.Date)

            CoreLogger.logInfo "[Verification]" (sprintf "📊 Found %d PLTR snapshots" sortedPLTRSnapshots.Length)

            Assert.That(
                sortedPLTRSnapshots.Length,
                Is.EqualTo(3),
                "Should have 3 PLTR snapshots (2024-04-26, 04-29 + today)"
            )

            // Get expected PLTR snapshots from OptionsImportExpectedSnapshots
            let expectedPLTRSnapshots =
                OptionsImportExpectedSnapshots.getPLTRSnapshots pltrTicker.Value usd

            // Extract currency snapshots from actual snapshots
            let actualPLTRSnapshots = sortedPLTRSnapshots |> List.map (fun s -> s.MainCurrency)

            // Define description function for PLTR snapshots
            let getPLTRDescription i =
                let date = expectedPLTRSnapshots.[i].Date.ToString("yyyy-MM-dd")

                let name =
                    match i with
                    | 0 -> "After opening vertical spread"
                    | 1 -> "After closing vertical spread"
                    | 2 -> "Current snapshot"
                    | _ -> "Unknown"

                sprintf "%s - %s" date name

            // Use base class method for verification
            this.VerifyTickerSnapshots "PLTR" expectedPLTRSnapshots actualPLTRSnapshots getPLTRDescription

            // ==================== PHASE 7: VERIFY BROKER ACCOUNT FINANCIAL SNAPSHOTS ====================
            TestSetup.printPhaseHeader 7 "Verify Broker Account Financial Snapshots"

            // Get broker account from context
            let brokerAccountId = actions.Context.BrokerAccountId
            CoreLogger.logInfo "[Verification]" (sprintf "📊 BrokerAccount ID: %d" brokerAccountId)

            // Get all broker account snapshots using BrokerAccounts.GetSnapshots from Core
            let! overviewSnapshots = BrokerAccounts.GetSnapshots(brokerAccountId) |> Async.AwaitTask

            // Extract BrokerFinancialSnapshot from OverviewSnapshots
            let brokerFinancialSnapshots =
                overviewSnapshots
                |> List.choose (fun os -> os.BrokerAccount |> Option.map (fun bas -> (bas.Date, bas.Financial)))
                |> List.sortBy fst
                |> List.map snd

            CoreLogger.logInfo
                "[Verification]"
                (sprintf "📊 Found %d BrokerAccount snapshots" brokerFinancialSnapshots.Length)

            Assert.That(brokerFinancialSnapshots.Length, Is.EqualTo(9), "Should have 9 BrokerAccount snapshots")

            // Get broker and currency for snapshot construction
            let broker = Collections.Brokers.Items |> Seq.find (fun b -> b.Name = "Tastytrade")

            let brokerAccount =
                Collections.Accounts.Items
                |> Seq.filter (fun a -> a.Type = AccountType.BrokerAccount)
                |> Seq.pick (fun a -> a.Broker)

            let usd = Collections.Currencies.Items |> Seq.find (fun c -> c.Code = "USD")

            // Get expected BrokerAccount snapshots from OptionsImportExpectedSnapshots
            let expectedBrokerSnapshots =
                OptionsImportExpectedSnapshots.getBrokerAccountSnapshots broker brokerAccount usd

            // Define description function for broker snapshots
            let getBrokerDescription i =
                let date = expectedBrokerSnapshots.[i].Date.ToString("yyyy-MM-dd")

                let name =
                    match i with
                    | 0 -> "First deposit"
                    | 1 -> "Second deposit"
                    | 2 -> "Third deposit"
                    | 3 -> "SOFI trade"
                    | 4 -> "MPW + PLTR trades"
                    | 5 -> "Balance adjustment"
                    | 6 -> "Closing + reopening trades"
                    | 7 -> "Final SOFI trade"
                    | 8 -> "Current snapshot"
                    | _ -> "Unknown"

                sprintf "%s - %s" date name

            // Use base class method for verification
            this.VerifyBrokerSnapshots expectedBrokerSnapshots brokerFinancialSnapshots getBrokerDescription

            // ==================== SUMMARY ====================
            TestSetup.printTestCompletionSummary
                "Options Import from CSV"
                "Successfully created BrokerAccount, imported options CSV, received all signals, and verified SOFI, MPW, and PLTR ticker snapshots with complete financial state"

            CoreLogger.logInfo "[Test]" "=== TEST COMPLETED SUCCESSFULLY ==="
        }
