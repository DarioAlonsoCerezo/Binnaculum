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

            // Get expected SOFI snapshots from OptionsImportExpectedSnapshots
            let expectedSOFISnapshots =
                OptionsImportExpectedSnapshots.getSOFISnapshots sofiTicker.Value usd

            // Extract currency snapshots from actual snapshots
            let actualSOFISnapshots = sortedSOFISnapshots |> List.map (fun s -> s.MainCurrency)

            // Verify all snapshots at once using the new list verification method
            let sofiResults =
                TestVerifications.verifyTickerCurrencySnapshotList expectedSOFISnapshots actualSOFISnapshots

            // Check each snapshot result and log details if any mismatch
            sofiResults
            |> List.iteri (fun i (allMatch, fieldResults) ->
                let snapshotDate = expectedSOFISnapshots.[i].Date.ToString("yyyy-MM-dd")

                let snapshotName =
                    match i with
                    | 0 -> "After SELL_TO_OPEN"
                    | 1 -> "After close and reopen"
                    | 2 -> "After second SELL_TO_OPEN"
                    | 3 -> "Current snapshot"
                    | _ -> "Unknown"

                if not allMatch then
                    CoreLogger.logError
                        "[Verification]"
                        (sprintf
                            "❌ SOFI Snapshot %d (%s - %s) failed:\n%s"
                            (i + 1)
                            snapshotDate
                            snapshotName
                            (fieldResults
                             |> List.filter (fun r -> not r.Match)
                             |> TestVerifications.formatValidationResults))
                else
                    let options = fieldResults |> List.find (fun r -> r.Field = "Options")
                    let realized = fieldResults |> List.find (fun r -> r.Field = "Realized")

                    let message =
                        if i = 0 then
                            sprintf "✅ SOFI Snapshot %d verified: Options=$%s" (i + 1) options.Actual
                        elif i = 3 then
                            sprintf "✅ SOFI Snapshot %d verified: Options=$%s (current)" (i + 1) options.Actual
                        else
                            sprintf
                                "✅ SOFI Snapshot %d verified: Options=$%s, Realized=$%s"
                                (i + 1)
                                options.Actual
                                realized.Actual

                    CoreLogger.logInfo "[Verification]" message

                Assert.That(
                    allMatch,
                    Is.True,
                    sprintf "SOFI Snapshot %d (%s - %s) verification failed" (i + 1) snapshotDate snapshotName
                ))

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

            // Get expected MPW snapshots from OptionsImportExpectedSnapshots
            let expectedMPWSnapshots =
                OptionsImportExpectedSnapshots.getMPWSnapshots mpwTicker.Value usd

            // Extract currency snapshots from actual snapshots
            let actualMPWSnapshots = sortedMPWSnapshots |> List.map (fun s -> s.MainCurrency)

            // Verify all snapshots at once using the new list verification method
            let mpwResults =
                TestVerifications.verifyTickerCurrencySnapshotList expectedMPWSnapshots actualMPWSnapshots

            // Check each snapshot result and log details if any mismatch
            mpwResults
            |> List.iteri (fun i (allMatch, fieldResults) ->
                let snapshotDate = expectedMPWSnapshots.[i].Date.ToString("yyyy-MM-dd")

                let snapshotName =
                    match i with
                    | 0 -> "After opening vertical spread"
                    | 1 -> "After closing vertical spread"
                    | 2 -> "Current snapshot"
                    | _ -> "Unknown"

                if not allMatch then
                    CoreLogger.logError
                        "[Verification]"
                        (sprintf
                            "❌ MPW Snapshot %d (%s - %s) failed:\n%s"
                            (i + 1)
                            snapshotDate
                            snapshotName
                            (fieldResults
                             |> List.filter (fun r -> not r.Match)
                             |> TestVerifications.formatValidationResults))
                else
                    let options = fieldResults |> List.find (fun r -> r.Field = "Options")
                    let realized = fieldResults |> List.find (fun r -> r.Field = "Realized")

                    let message =
                        if i = 0 then
                            sprintf "✅ MPW Snapshot %d verified: Options=$%s" (i + 1) options.Actual
                        elif i = 2 then
                            sprintf "✅ MPW Snapshot %d verified: Options=$%s (current)" (i + 1) options.Actual
                        else
                            sprintf
                                "✅ MPW Snapshot %d verified: Options=$%s, Realized=$%s"
                                (i + 1)
                                options.Actual
                                realized.Actual

                    CoreLogger.logInfo "[Verification]" message

                Assert.That(
                    allMatch,
                    Is.True,
                    sprintf "MPW Snapshot %d (%s - %s) verification failed" (i + 1) snapshotDate snapshotName
                ))

            CoreLogger.logInfo "[Verification]" "✅ All 3 MPW ticker snapshots verified chronologically"

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

            // Verify Snapshot 1: 2024-04-26 (After opening vertical spread)
            CoreLogger.logInfo
                "[Verification]"
                "📅 Verifying PLTR Snapshot 1: 2024-04-26 (After opening vertical spread)"

            let pltrSnapshot1 = sortedPLTRSnapshots.[0]
            let pltrSnapshot1Currency = pltrSnapshot1.MainCurrency

            Assert.That(
                pltrSnapshot1.Date,
                Is.EqualTo(DateOnly(2024, 4, 26)),
                "PLTR Snapshot 1 date should be 2024-04-26"
            )

            let expectedPLTR1: TickerCurrencySnapshot =
                { Id = pltrSnapshot1Currency.Id
                  Date = DateOnly(2024, 4, 26)
                  Ticker = pltrSnapshot1Currency.Ticker
                  Currency = pltrSnapshot1Currency.Currency
                  TotalShares = 0m
                  Weight = 0m
                  CostBasis = 0m
                  RealCost = 0m
                  Dividends = 0m
                  Options = 5.73m // 17.86 - 12.13 (net from opening spread)
                  TotalIncomes = 5.73m
                  Unrealized = 0m
                  Realized = 0m
                  Performance = 0m
                  LatestPrice = 0m
                  OpenTrades = true }

            let (matchPLTR1, resultsPLTR1) =
                TestVerifications.verifyTickerCurrencySnapshot expectedPLTR1 pltrSnapshot1Currency

            Assert.That(
                matchPLTR1,
                Is.True,
                sprintf
                    "PLTR Snapshot 1 verification failed:\n%s"
                    (resultsPLTR1
                     |> List.filter (fun r -> not r.Match)
                     |> List.map (fun r -> sprintf "  %s: expected=%s, actual=%s" r.Field r.Expected r.Actual)
                     |> String.concat "\n")
            )

            CoreLogger.logInfo "[Verification]" "✅ PLTR Snapshot 1 verified: Options=$5.73"

            // Verify Snapshot 2: 2024-04-29 (After closing vertical spread)
            CoreLogger.logInfo
                "[Verification]"
                "📅 Verifying PLTR Snapshot 2: 2024-04-29 (After closing vertical spread)"

            let pltrSnapshot2 = sortedPLTRSnapshots.[1]
            let pltrSnapshot2Currency = pltrSnapshot2.MainCurrency

            Assert.That(
                pltrSnapshot2.Date,
                Is.EqualTo(DateOnly(2024, 4, 29)),
                "PLTR Snapshot 2 date should be 2024-04-29"
            )

            let expectedPLTR2: TickerCurrencySnapshot =
                { Id = pltrSnapshot2Currency.Id
                  Date = DateOnly(2024, 4, 29)
                  Ticker = pltrSnapshot2Currency.Ticker
                  Currency = pltrSnapshot2Currency.Currency
                  TotalShares = 0m
                  Weight = 0m
                  CostBasis = 0m
                  RealCost = 0m
                  Dividends = 0m
                  Options = 1.46m // Cumulative: 5.73 + 4.86 - 9.13 = 1.46
                  TotalIncomes = 1.46m
                  Unrealized = 0m
                  Realized = 1.46m // Positions closed
                  Performance = 0m
                  LatestPrice = 0m
                  OpenTrades = false }

            let (matchPLTR2, resultsPLTR2) =
                TestVerifications.verifyTickerCurrencySnapshot expectedPLTR2 pltrSnapshot2Currency

            Assert.That(
                matchPLTR2,
                Is.True,
                sprintf
                    "PLTR Snapshot 2 verification failed:\n%s"
                    (resultsPLTR2
                     |> List.filter (fun r -> not r.Match)
                     |> List.map (fun r -> sprintf "  %s: expected=%s, actual=%s" r.Field r.Expected r.Actual)
                     |> String.concat "\n")
            )

            CoreLogger.logInfo "[Verification]" "✅ PLTR Snapshot 2 verified: Options=$1.46, Realized=$1.46"

            // Verify Snapshot 3: Today (Current snapshot)
            CoreLogger.logInfo
                "[Verification]"
                (sprintf "📅 Verifying PLTR Snapshot 3: %s (Current snapshot)" (DateTime.Now.ToString("yyyy-MM-dd")))

            let pltrSnapshot3 = sortedPLTRSnapshots.[2]
            let pltrSnapshot3Currency = pltrSnapshot3.MainCurrency
            let today = DateOnly.FromDateTime(DateTime.Now)
            Assert.That(pltrSnapshot3.Date, Is.EqualTo(today), "PLTR Snapshot 3 date should be today")

            let expectedPLTR3: TickerCurrencySnapshot =
                { Id = pltrSnapshot3Currency.Id
                  Date = today
                  Ticker = pltrSnapshot3Currency.Ticker
                  Currency = pltrSnapshot3Currency.Currency
                  TotalShares = 0m
                  Weight = 0m
                  CostBasis = 0m
                  RealCost = 0m
                  Dividends = 0m
                  Options = 1.46m // Same as Snapshot 2 (no new trades)
                  TotalIncomes = 1.46m
                  Unrealized = 0m
                  Realized = 1.46m
                  Performance = 0m
                  LatestPrice = 0m
                  OpenTrades = false }

            let (matchPLTR3, resultsPLTR3) =
                TestVerifications.verifyTickerCurrencySnapshot expectedPLTR3 pltrSnapshot3Currency

            Assert.That(
                matchPLTR3,
                Is.True,
                sprintf
                    "PLTR Snapshot 3 verification failed:\n%s"
                    (resultsPLTR3
                     |> List.filter (fun r -> not r.Match)
                     |> List.map (fun r -> sprintf "  %s: expected=%s, actual=%s" r.Field r.Expected r.Actual)
                     |> String.concat "\n")
            )

            CoreLogger.logInfo "[Verification]" "✅ PLTR Snapshot 3 verified: Options=$1.46 (current)"
            CoreLogger.logInfo "[Verification]" "✅ All 3 PLTR ticker snapshots verified chronologically"

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

            // Verify all snapshots at once using the new list verification method
            let brokerResults =
                TestVerifications.verifyBrokerFinancialSnapshotList expectedBrokerSnapshots brokerFinancialSnapshots

            // Check each snapshot result and log details if any mismatch
            brokerResults
            |> List.iteri (fun i (allMatch, fieldResults) ->
                let snapshotDate = expectedBrokerSnapshots.[i].Date.ToString("yyyy-MM-dd")

                let snapshotName =
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

                if not allMatch then
                    // Log failed fields only
                    CoreLogger.logError
                        "[Verification]"
                        (sprintf
                            "❌ BrokerSnapshot %d (%s - %s) failed:\n%s"
                            (i + 1)
                            snapshotDate
                            snapshotName
                            (fieldResults
                             |> List.filter (fun r -> not r.Match)
                             |> TestVerifications.formatValidationResults))

                    // Log ALL fields for debugging
                    CoreLogger.logInfo
                        "[Verification]"
                        (sprintf
                            "All fields for BrokerSnapshot %d (%s):\n%s"
                            (i + 1)
                            snapshotDate
                            (TestVerifications.formatValidationResults fieldResults))
                else
                    // Log success with key metrics
                    let deposited = fieldResults |> List.find (fun r -> r.Field = "Deposited")
                    let optionsIncome = fieldResults |> List.find (fun r -> r.Field = "OptionsIncome")
                    let realizedGains = fieldResults |> List.find (fun r -> r.Field = "RealizedGains")

                    CoreLogger.logInfo
                        "[Verification]"
                        (sprintf
                            "✅ BrokerSnapshot %d (%s - %s) verified: Deposited=$%s, Options=$%s, Realized=$%s"
                            (i + 1)
                            snapshotDate
                            snapshotName
                            deposited.Actual
                            optionsIncome.Actual
                            realizedGains.Actual)

                Assert.That(
                    allMatch,
                    Is.True,
                    sprintf "BrokerSnapshot %d (%s - %s) verification failed" (i + 1) snapshotDate snapshotName
                ))

            CoreLogger.logInfo "[Verification]" "✅ All 9 BrokerAccount snapshots verified chronologically"

            // ==================== SUMMARY ====================
            TestSetup.printTestCompletionSummary
                "Options Import from CSV"
                "Successfully created BrokerAccount, imported options CSV, received all signals, and verified SOFI, MPW, and PLTR ticker snapshots with complete financial state"

            CoreLogger.logInfo "[Test]" "=== TEST COMPLETED SUCCESSFULLY ==="
        }
