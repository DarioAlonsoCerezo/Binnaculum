namespace Core.Tests.Integration

open NUnit.Framework
open System
open System.IO
open Binnaculum.Core.Models
open Binnaculum.Core.UI
open Binnaculum.Core.Logging

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
            CoreLogger.logInfo "[Test]" "=== TEST: Pfizer Options Import CSV Workflow with FIFO Matching ==="

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
            TestSetup.printPhaseHeader 2 "Create BrokerAccount for Pfizer Import"

            // EXPECT: Declare expected signals BEFORE operation
            StreamObserver.expectSignals (
                [ Accounts_Updated // Account added to Collections.Accounts
                  Snapshots_Updated ] // Snapshot calculated in Collections.Snapshots
            )

            CoreLogger.logDebug "[StreamObserver]" "🎯 Expecting signals: Accounts_Updated, Snapshots_Updated"

            // EXECUTE: Create account
            let! (ok, details, error) = actions.createBrokerAccount ("Pfizer-Import-Test")
            Assert.That(ok, Is.True, sprintf "Account creation should succeed: %s - %A" details error)
            CoreLogger.logInfo "[Verification]" (sprintf "✅ BrokerAccount created: %s" details)

            // WAIT: Wait for signals (NOT Thread.Sleep!)
            CoreLogger.logInfo "[TestActions]" "⏳ Waiting for account creation reactive signals..."
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync (TimeSpan.FromSeconds(10.0))
            Assert.That(signalsReceived, Is.True, "Account creation signals should have been received")
            CoreLogger.logInfo "[Verification]" "✅ Account creation signals received successfully"

            // ==================== PHASE 3: IMPORT PFIZER OPTIONS CSV ====================
            TestSetup.printPhaseHeader 3 "Import Pfizer Options CSV File"

            // Get CSV path
            let csvPath = this.getCsvPath ("PfizerImportTest.csv")
            CoreLogger.logDebug "[Import]" (sprintf "📄 CSV file path: %s" csvPath)
            Assert.That(File.Exists(csvPath), Is.True, sprintf "CSV file should exist: %s" csvPath)

            // EXPECT: Declare expected signals BEFORE import operation
            StreamObserver.expectSignals (
                [ Movements_Updated // Option trades added
                  Tickers_Updated // PFE ticker added
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

            // ==================== PHASE 4: VERIFY DATA COUNTS ====================
            TestSetup.printPhaseHeader 4 "Verify Imported Data Counts"

            // Verify movement count (4 option trades)
            let! (verified, movementCount, error) = actions.verifyMovementCount (4)

            Assert.That(
                verified,
                Is.True,
                sprintf "Movement count verification should succeed: %s - %A" movementCount error
            )

            CoreLogger.logInfo "[Verification]" "✅ Movement count verified: 4 option trades"

            // Verify ticker count (PFE + SPY default = 2)
            let! (verified, tickerCount, error) = actions.verifyTickerCount (2)

            Assert.That(
                verified,
                Is.True,
                sprintf "Ticker count verification should succeed: %s - %A" tickerCount error
            )

            CoreLogger.logInfo "[Verification]" "✅ Ticker count verified: 2 tickers (PFE + SPY)"

            // Verify snapshots were calculated
            let! (verified, snapshotCount, error) = actions.verifySnapshotCount (1)

            Assert.That(
                verified,
                Is.True,
                sprintf "Snapshot count verification should succeed: %s - %A" snapshotCount error
            )

            CoreLogger.logInfo "[Verification]" (sprintf "✅ Snapshot count verified: >= 1 (%s)" snapshotCount)

            // ==================== PHASE 5: VERIFY PFE TICKER SNAPSHOTS CHRONOLOGICALLY ====================
            TestSetup.printPhaseHeader 5 "Verify PFE Ticker Snapshots with Complete Financial State"

            // Get PFE ticker from Collections
            let pfeTicker = Collections.Tickers.Items |> Seq.tryFind (fun t -> t.Symbol = "PFE")

            Assert.That(pfeTicker.IsSome, Is.True, "PFE ticker should exist in Collections")

            let pfeTickerId = pfeTicker.Value.Id
            CoreLogger.logInfo "[Verification]" (sprintf "📊 PFE Ticker ID: %d" pfeTickerId)

            // Get all PFE snapshots using Tickers.GetSnapshots from Core (returns Task<TickerSnapshot list>)
            let! pfeSnapshots = Tickers.GetSnapshots(pfeTickerId) |> Async.AwaitTask
            let sortedSnapshots = pfeSnapshots |> List.sortBy (fun s -> s.Date)

            CoreLogger.logInfo "[Verification]" (sprintf "📊 Found %d PFE snapshots" sortedSnapshots.Length)
            Assert.That(sortedSnapshots.Length, Is.EqualTo(4), "Should have 4 PFE snapshots (3 trade dates + today)")

            // Verify Snapshot 1: 2025-08-25 (After BUY_TO_OPEN -$555.12)
            CoreLogger.logInfo "[Verification]" "📅 Verifying Snapshot 1: 2025-08-25 (After BUY_TO_OPEN)"
            let snapshot1 = sortedSnapshots.[0]
            let snapshot1Currency = snapshot1.MainCurrency
            Assert.That(snapshot1.Date, Is.EqualTo(DateOnly(2025, 8, 25)), "Snapshot 1 date should be 2025-08-25")

            let expected1: TickerCurrencySnapshot =
                { Id = snapshot1Currency.Id // Use actual ID
                  Date = DateOnly(2025, 8, 25)
                  Ticker = snapshot1Currency.Ticker
                  Currency = snapshot1Currency.Currency
                  TotalShares = 0m // Options only, no shares
                  Weight = 0m
                  CostBasis = 0m
                  RealCost = 0m
                  Dividends = 0m
                  Options = -555.12m // BUY_TO_OPEN premium
                  TotalIncomes = -555.12m
                  Unrealized = 0m
                  Realized = 0m
                  Performance = 0m
                  LatestPrice = 0m
                  OpenTrades = true }

            let (match1, results1) =
                TestVerifications.verifyTickerCurrencySnapshot expected1 snapshot1Currency

            Assert.That(
                match1,
                Is.True,
                sprintf
                    "Snapshot 1 verification failed:\n%s"
                    (results1
                     |> List.filter (fun r -> not r.Match)
                     |> List.map (fun r -> sprintf "  %s: expected=%s, actual=%s" r.Field r.Expected r.Actual)
                     |> String.concat "\n")
            )

            CoreLogger.logInfo "[Verification]" "✅ Snapshot 1 verified: Options=-$555.12"

            // Verify Snapshot 2: 2025-10-01 (After SELL_TO_OPEN +$49.88)
            CoreLogger.logInfo "[Verification]" "📅 Verifying Snapshot 2: 2025-10-01 (After SELL_TO_OPEN)"
            let snapshot2 = sortedSnapshots.[1]
            let snapshot2Currency = snapshot2.MainCurrency
            Assert.That(snapshot2.Date, Is.EqualTo(DateOnly(2025, 10, 1)), "Snapshot 2 date should be 2025-10-01")

            let expected2: TickerCurrencySnapshot =
                { Id = snapshot2Currency.Id
                  Date = DateOnly(2025, 10, 1)
                  Ticker = snapshot2Currency.Ticker
                  Currency = snapshot2Currency.Currency
                  TotalShares = 0m
                  Weight = 0m
                  CostBasis = 0m
                  RealCost = 0m
                  Dividends = 0m
                  Options = -505.24m // Cumulative: -$555.12 + $49.88
                  TotalIncomes = -505.24m
                  Unrealized = 0m
                  Realized = 0m
                  Performance = 0m
                  LatestPrice = 0m
                  OpenTrades = true }

            let (match2, results2) =
                TestVerifications.verifyTickerCurrencySnapshot expected2 snapshot2Currency

            Assert.That(
                match2,
                Is.True,
                sprintf
                    "Snapshot 2 verification failed:\n%s"
                    (results2
                     |> List.filter (fun r -> not r.Match)
                     |> List.map (fun r -> sprintf "  %s: expected=%s, actual=%s" r.Field r.Expected r.Actual)
                     |> String.concat "\n")
            )

            CoreLogger.logInfo "[Verification]" "✅ Snapshot 2 verified: Options=-$505.24 (cumulative)"

            // Verify Snapshot 3: 2025-10-03 (After SELL_TO_CLOSE +$744.88 and BUY_TO_CLOSE -$64.12)
            CoreLogger.logInfo "[Verification]" "📅 Verifying Snapshot 3: 2025-10-03 (After both close trades)"
            let snapshot3 = sortedSnapshots.[2]
            let snapshot3Currency = snapshot3.MainCurrency
            Assert.That(snapshot3.Date, Is.EqualTo(DateOnly(2025, 10, 3)), "Snapshot 3 date should be 2025-10-03")

            let expected3: TickerCurrencySnapshot =
                { Id = snapshot3Currency.Id
                  Date = DateOnly(2025, 10, 3)
                  Ticker = snapshot3Currency.Ticker
                  Currency = snapshot3Currency.Currency
                  TotalShares = 0m
                  Weight = 0m
                  CostBasis = 0m
                  RealCost = 0m
                  Dividends = 0m
                  Options = 175.52m // Cumulative: -$505.24 + $744.88 - $64.12
                  TotalIncomes = 175.52m
                  Unrealized = 0m
                  Realized = 175.52m // FIFO matched realized gains from closed positions
                  Performance = 0m
                  LatestPrice = 0m
                  OpenTrades = false // All positions closed
                }

            let (match3, results3) =
                TestVerifications.verifyTickerCurrencySnapshot expected3 snapshot3Currency

            Assert.That(
                match3,
                Is.True,
                sprintf
                    "Snapshot 3 verification failed:\n%s"
                    (results3
                     |> List.filter (fun r -> not r.Match)
                     |> List.map (fun r -> sprintf "  %s: expected=%s, actual=%s" r.Field r.Expected r.Actual)
                     |> String.concat "\n")
            )

            CoreLogger.logInfo "[Verification]" "✅ Snapshot 3 verified: Options=$175.52 (all closed)"

            // Verify Snapshot 4: Today (Current snapshot - should be same as snapshot 3)
            CoreLogger.logInfo
                "[Verification]"
                (sprintf "📅 Verifying Snapshot 4: %s (Current snapshot)" (DateTime.Now.ToString("yyyy-MM-dd")))

            let snapshot4 = sortedSnapshots.[3]
            let snapshot4Currency = snapshot4.MainCurrency
            let today = DateOnly.FromDateTime(DateTime.Now)
            Assert.That(snapshot4.Date, Is.EqualTo(today), "Snapshot 4 date should be today")

            let expected4: TickerCurrencySnapshot =
                { Id = snapshot4Currency.Id
                  Date = today
                  Ticker = snapshot4Currency.Ticker
                  Currency = snapshot4Currency.Currency
                  TotalShares = 0m
                  Weight = 0m
                  CostBasis = 0m
                  RealCost = 0m
                  Dividends = 0m
                  Options = 175.52m // Same as snapshot 3 (no new trades)
                  TotalIncomes = 175.52m
                  Unrealized = 0m
                  Realized = 175.52m // Same as snapshot 3 (no new trades)
                  Performance = 0m
                  LatestPrice = 0m
                  OpenTrades = false }

            let (match4, results4) =
                TestVerifications.verifyTickerCurrencySnapshot expected4 snapshot4Currency

            Assert.That(
                match4,
                Is.True,
                sprintf
                    "Snapshot 4 verification failed:\n%s"
                    (results4
                     |> List.filter (fun r -> not r.Match)
                     |> List.map (fun r -> sprintf "  %s: expected=%s, actual=%s" r.Field r.Expected r.Actual)
                     |> String.concat "\n")
            )

            CoreLogger.logInfo "[Verification]" "✅ Snapshot 4 verified: Options=$175.52 (current)"

            CoreLogger.logInfo "[Verification]" "✅ All 4 PFE ticker snapshots verified chronologically"

            // ==================== PHASE 6: COMMENTED OUT - REPLACED BY PHASE 5 ====================
            // TestSetup.printPhaseHeader 6 "Verify PFE Ticker Snapshots"

            // // Verify PFE ticker snapshots (4 snapshots: 3 from trade dates + 1 today)
            // // - 2025-08-25: BUY_TO_OPEN
            // // - 2025-10-01: SELL_TO_OPEN
            // // - 2025-10-03: BUY_TO_CLOSE and SELL_TO_CLOSE
            // // - Today: Current snapshot
            // let! (verified, snapshotCount, error) = actions.verifyPfizerSnapshots (4)

            // Assert.That(
            //     verified,
            //     Is.True,
            //     sprintf "PFE snapshots verification should succeed: %s - %A" snapshotCount error
            // )

            // CoreLogger.logInfo "[Verification]" "✅ PFE ticker snapshots verified: 4 snapshots (3 trade dates + today)"

            // ==================== SUMMARY ====================
            TestSetup.printTestCompletionSummary
                "Pfizer Options Import with FIFO Matching"
                "Successfully created BrokerAccount, imported Pfizer options CSV, received all signals, verified FIFO matching ($175.52), and validated PFE ticker snapshots"

            CoreLogger.logInfo "[Test]" "=== TEST COMPLETED SUCCESSFULLY ==="
        }
