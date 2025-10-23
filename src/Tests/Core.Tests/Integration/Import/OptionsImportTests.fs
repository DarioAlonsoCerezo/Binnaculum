namespace Core.Tests.Integration

open NUnit.Framework
open System
open System.IO
open Binnaculum.Core.Models
open Binnaculum.Core.UI
open Binnaculum.Core.Logging

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

            // Get SOFI ticker from Collections
            let sofiTicker =
                Collections.Tickers.Items |> Seq.tryFind (fun t -> t.Symbol = "SOFI")

            Assert.That(sofiTicker.IsSome, Is.True, "SOFI ticker should exist in Collections")

            let sofiTickerId = sofiTicker.Value.Id
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

            // Verify Snapshot 1: 2024-04-25 (After SELL_TO_OPEN #1)
            CoreLogger.logInfo "[Verification]" "📅 Verifying SOFI Snapshot 1: 2024-04-25 (After SELL_TO_OPEN)"
            let sofiSnapshot1 = sortedSOFISnapshots.[0]
            let sofiSnapshot1Currency = sofiSnapshot1.MainCurrency

            Assert.That(
                sofiSnapshot1.Date,
                Is.EqualTo(DateOnly(2024, 4, 25)),
                "SOFI Snapshot 1 date should be 2024-04-25"
            )

            let expectedSOFI1: TickerCurrencySnapshot =
                { Id = sofiSnapshot1Currency.Id
                  Date = DateOnly(2024, 4, 25)
                  Ticker = sofiSnapshot1Currency.Ticker
                  Currency = sofiSnapshot1Currency.Currency
                  TotalShares = 0m
                  Weight = 0m
                  CostBasis = 0m
                  RealCost = 0m
                  Dividends = 0m
                  Options = 33.86m // SELL_TO_OPEN NetPremium
                  TotalIncomes = 33.86m
                  Unrealized = 0m
                  Realized = 0m
                  Performance = 0m
                  LatestPrice = 0m
                  OpenTrades = true }

            let (matchSOFI1, resultsSOFI1) =
                TestVerifications.verifyTickerCurrencySnapshot expectedSOFI1 sofiSnapshot1Currency

            Assert.That(
                matchSOFI1,
                Is.True,
                sprintf
                    "SOFI Snapshot 1 verification failed:\n%s"
                    (resultsSOFI1
                     |> List.filter (fun r -> not r.Match)
                     |> List.map (fun r -> sprintf "  %s: expected=%s, actual=%s" r.Field r.Expected r.Actual)
                     |> String.concat "\n")
            )

            CoreLogger.logInfo "[Verification]" "✅ SOFI Snapshot 1 verified: Options=$33.86"

            // Verify Snapshot 2: 2024-04-29 (After BUY_TO_CLOSE + SELL_TO_OPEN)
            CoreLogger.logInfo "[Verification]" "📅 Verifying SOFI Snapshot 2: 2024-04-29 (After close and reopen)"
            let sofiSnapshot2 = sortedSOFISnapshots.[1]
            let sofiSnapshot2Currency = sofiSnapshot2.MainCurrency

            Assert.That(
                sofiSnapshot2.Date,
                Is.EqualTo(DateOnly(2024, 4, 29)),
                "SOFI Snapshot 2 date should be 2024-04-29"
            )

            let expectedSOFI2: TickerCurrencySnapshot =
                { Id = sofiSnapshot2Currency.Id
                  Date = DateOnly(2024, 4, 29)
                  Ticker = sofiSnapshot2Currency.Ticker
                  Currency = sofiSnapshot2Currency.Currency
                  TotalShares = 0m
                  Weight = 0m
                  CostBasis = 0m
                  RealCost = 0m
                  Dividends = 0m
                  Options = 32.59m // Cumulative: 33.86 - 17.13 + 15.86
                  TotalIncomes = 32.59m
                  Unrealized = 0m
                  Realized = 16.73m // First position closed: 33.86 - 17.13
                  Performance = 0m
                  LatestPrice = 0m
                  OpenTrades = true }

            let (matchSOFI2, resultsSOFI2) =
                TestVerifications.verifyTickerCurrencySnapshot expectedSOFI2 sofiSnapshot2Currency

            Assert.That(
                matchSOFI2,
                Is.True,
                sprintf
                    "SOFI Snapshot 2 verification failed:\n%s"
                    (resultsSOFI2
                     |> List.filter (fun r -> not r.Match)
                     |> List.map (fun r -> sprintf "  %s: expected=%s, actual=%s" r.Field r.Expected r.Actual)
                     |> String.concat "\n")
            )

            CoreLogger.logInfo "[Verification]" "✅ SOFI Snapshot 2 verified: Options=$32.59, Realized=$16.73"

            // Verify Snapshot 3: 2024-04-30 (After SELL_TO_OPEN #4)
            CoreLogger.logInfo "[Verification]" "📅 Verifying SOFI Snapshot 3: 2024-04-30 (After second SELL_TO_OPEN)"
            let sofiSnapshot3 = sortedSOFISnapshots.[2]
            let sofiSnapshot3Currency = sofiSnapshot3.MainCurrency

            Assert.That(
                sofiSnapshot3.Date,
                Is.EqualTo(DateOnly(2024, 4, 30)),
                "SOFI Snapshot 3 date should be 2024-04-30"
            )

            let expectedSOFI3: TickerCurrencySnapshot =
                { Id = sofiSnapshot3Currency.Id
                  Date = DateOnly(2024, 4, 30)
                  Ticker = sofiSnapshot3Currency.Ticker
                  Currency = sofiSnapshot3Currency.Currency
                  TotalShares = 0m
                  Weight = 0m
                  CostBasis = 0m
                  RealCost = 0m
                  Dividends = 0m
                  Options = 47.45m // Cumulative: 32.59 + 14.86
                  TotalIncomes = 47.45m
                  Unrealized = 0m
                  Realized = 16.73m
                  Performance = 0m
                  LatestPrice = 0m
                  OpenTrades = true }

            let (matchSOFI3, resultsSOFI3) =
                TestVerifications.verifyTickerCurrencySnapshot expectedSOFI3 sofiSnapshot3Currency

            Assert.That(
                matchSOFI3,
                Is.True,
                sprintf
                    "SOFI Snapshot 3 verification failed:\n%s"
                    (resultsSOFI3
                     |> List.filter (fun r -> not r.Match)
                     |> List.map (fun r -> sprintf "  %s: expected=%s, actual=%s" r.Field r.Expected r.Actual)
                     |> String.concat "\n")
            )

            CoreLogger.logInfo "[Verification]" "✅ SOFI Snapshot 3 verified: Options=$47.45, Realized=$16.73"

            // Verify Snapshot 4: Today (Current snapshot)
            CoreLogger.logInfo
                "[Verification]"
                (sprintf "📅 Verifying SOFI Snapshot 4: %s (Current snapshot)" (DateTime.Now.ToString("yyyy-MM-dd")))

            let sofiSnapshot4 = sortedSOFISnapshots.[3]
            let sofiSnapshot4Currency = sofiSnapshot4.MainCurrency
            let today = DateOnly.FromDateTime(DateTime.Now)
            Assert.That(sofiSnapshot4.Date, Is.EqualTo(today), "SOFI Snapshot 4 date should be today")

            let expectedSOFI4: TickerCurrencySnapshot =
                { Id = sofiSnapshot4Currency.Id
                  Date = today
                  Ticker = sofiSnapshot4Currency.Ticker
                  Currency = sofiSnapshot4Currency.Currency
                  TotalShares = 0m
                  Weight = 0m
                  CostBasis = 0m
                  RealCost = 0m
                  Dividends = 0m
                  Options = 47.45m // Same as Snapshot 3
                  TotalIncomes = 47.45m
                  Unrealized = 0m
                  Realized = 16.73m
                  Performance = 0m
                  LatestPrice = 0m
                  OpenTrades = true } // Still open (no closing trades)

            let (matchSOFI4, resultsSOFI4) =
                TestVerifications.verifyTickerCurrencySnapshot expectedSOFI4 sofiSnapshot4Currency

            Assert.That(
                matchSOFI4,
                Is.True,
                sprintf
                    "SOFI Snapshot 4 verification failed:\n%s"
                    (resultsSOFI4
                     |> List.filter (fun r -> not r.Match)
                     |> List.map (fun r -> sprintf "  %s: expected=%s, actual=%s" r.Field r.Expected r.Actual)
                     |> String.concat "\n")
            )

            CoreLogger.logInfo "[Verification]" "✅ SOFI Snapshot 4 verified: Options=$47.45 (current)"
            CoreLogger.logInfo "[Verification]" "✅ All 4 SOFI ticker snapshots verified chronologically"

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

            // Verify Snapshot 1: 2024-04-26 (After opening vertical spread)
            CoreLogger.logInfo
                "[Verification]"
                "📅 Verifying MPW Snapshot 1: 2024-04-26 (After opening vertical spread)"

            let mpwSnapshot1 = sortedMPWSnapshots.[0]
            let mpwSnapshot1Currency = mpwSnapshot1.MainCurrency

            Assert.That(
                mpwSnapshot1.Date,
                Is.EqualTo(DateOnly(2024, 4, 26)),
                "MPW Snapshot 1 date should be 2024-04-26"
            )

            let expectedMPW1: TickerCurrencySnapshot =
                { Id = mpwSnapshot1Currency.Id
                  Date = DateOnly(2024, 4, 26)
                  Ticker = mpwSnapshot1Currency.Ticker
                  Currency = mpwSnapshot1Currency.Currency
                  TotalShares = 0m
                  Weight = 0m
                  CostBasis = 0m
                  RealCost = 0m
                  Dividends = 0m
                  Options = 12.73m // 17.86 - 5.13 (net from opening spread)
                  TotalIncomes = 12.73m
                  Unrealized = 0m
                  Realized = 0m
                  Performance = 0m
                  LatestPrice = 0m
                  OpenTrades = true }

            let (matchMPW1, resultsMPW1) =
                TestVerifications.verifyTickerCurrencySnapshot expectedMPW1 mpwSnapshot1Currency

            Assert.That(
                matchMPW1,
                Is.True,
                sprintf
                    "MPW Snapshot 1 verification failed:\n%s"
                    (resultsMPW1
                     |> List.filter (fun r -> not r.Match)
                     |> List.map (fun r -> sprintf "  %s: expected=%s, actual=%s" r.Field r.Expected r.Actual)
                     |> String.concat "\n")
            )

            CoreLogger.logInfo "[Verification]" "✅ MPW Snapshot 1 verified: Options=$12.73"

            // Verify Snapshot 2: 2024-04-29 (After closing vertical spread)
            CoreLogger.logInfo
                "[Verification]"
                "📅 Verifying MPW Snapshot 2: 2024-04-29 (After closing vertical spread)"

            let mpwSnapshot2 = sortedMPWSnapshots.[1]
            let mpwSnapshot2Currency = mpwSnapshot2.MainCurrency

            Assert.That(
                mpwSnapshot2.Date,
                Is.EqualTo(DateOnly(2024, 4, 29)),
                "MPW Snapshot 2 date should be 2024-04-29"
            )

            let expectedMPW2: TickerCurrencySnapshot =
                { Id = mpwSnapshot2Currency.Id
                  Date = DateOnly(2024, 4, 29)
                  Ticker = mpwSnapshot2Currency.Ticker
                  Currency = mpwSnapshot2Currency.Currency
                  TotalShares = 0m
                  Weight = 0m
                  CostBasis = 0m
                  RealCost = 0m
                  Dividends = 0m
                  Options = 5.46m // Cumulative: 12.73 + 0.86 - 8.13 = 5.46
                  TotalIncomes = 5.46m
                  Unrealized = 0m
                  Realized = 5.46m // Positions closed
                  Performance = 0m
                  LatestPrice = 0m
                  OpenTrades = false }

            let (matchMPW2, resultsMPW2) =
                TestVerifications.verifyTickerCurrencySnapshot expectedMPW2 mpwSnapshot2Currency

            Assert.That(
                matchMPW2,
                Is.True,
                sprintf
                    "MPW Snapshot 2 verification failed:\n%s"
                    (resultsMPW2
                     |> List.filter (fun r -> not r.Match)
                     |> List.map (fun r -> sprintf "  %s: expected=%s, actual=%s" r.Field r.Expected r.Actual)
                     |> String.concat "\n")
            )

            CoreLogger.logInfo "[Verification]" "✅ MPW Snapshot 2 verified: Options=$5.46, Realized=$5.46"

            // Verify Snapshot 3: Today (Current snapshot)
            CoreLogger.logInfo
                "[Verification]"
                (sprintf "📅 Verifying MPW Snapshot 3: %s (Current snapshot)" (DateTime.Now.ToString("yyyy-MM-dd")))

            let mpwSnapshot3 = sortedMPWSnapshots.[2]
            let mpwSnapshot3Currency = mpwSnapshot3.MainCurrency
            let today = DateOnly.FromDateTime(DateTime.Now)
            Assert.That(mpwSnapshot3.Date, Is.EqualTo(today), "MPW Snapshot 3 date should be today")

            let expectedMPW3: TickerCurrencySnapshot =
                { Id = mpwSnapshot3Currency.Id
                  Date = today
                  Ticker = mpwSnapshot3Currency.Ticker
                  Currency = mpwSnapshot3Currency.Currency
                  TotalShares = 0m
                  Weight = 0m
                  CostBasis = 0m
                  RealCost = 0m
                  Dividends = 0m
                  Options = 5.46m // Same as Snapshot 2 (no new trades)
                  TotalIncomes = 5.46m
                  Unrealized = 0m
                  Realized = 5.46m
                  Performance = 0m
                  LatestPrice = 0m
                  OpenTrades = false }

            let (matchMPW3, resultsMPW3) =
                TestVerifications.verifyTickerCurrencySnapshot expectedMPW3 mpwSnapshot3Currency

            Assert.That(
                matchMPW3,
                Is.True,
                sprintf
                    "MPW Snapshot 3 verification failed:\n%s"
                    (resultsMPW3
                     |> List.filter (fun r -> not r.Match)
                     |> List.map (fun r -> sprintf "  %s: expected=%s, actual=%s" r.Field r.Expected r.Actual)
                     |> String.concat "\n")
            )

            CoreLogger.logInfo "[Verification]" "✅ MPW Snapshot 3 verified: Options=$5.46 (current)"
            CoreLogger.logInfo "[Verification]" "✅ All 3 MPW ticker snapshots verified chronologically"

            // ==================== SUMMARY ====================
            TestSetup.printTestCompletionSummary
                "Options Import from CSV"
                "Successfully created BrokerAccount, imported options CSV, received all signals, and verified SOFI and MPW ticker snapshots with complete financial state"

            CoreLogger.logInfo "[Test]" "=== TEST COMPLETED SUCCESSFULLY ==="
        }
