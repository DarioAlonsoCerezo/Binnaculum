using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Binnaculum.Core.Import;
using Binnaculum.Core.UI;
using Core.Platform.MauiTester.Models;
using Core.Platform.MauiTester.Services;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using CoreModels = Binnaculum.Core.Models;

namespace Core.Platform.MauiTester.TestCases
{
    /// <summary>
    /// Reactive signal-based version of TSLL Multi-Asset Import Integration Test.
    /// Uses signal-based approach to wait for actual reactive stream updates instead of fixed delays.
    /// Validates comprehensive TSLL trading data including equities, options, and dividends.
    /// Structural validation: counts, snapshots, and progression logging - NOT exact financial values.
    /// </summary>
    public class ReactiveTsllImportIntegrationTest : TestStep
    {
        private readonly TestExecutionContext _context;

        public ReactiveTsllImportIntegrationTest(TestExecutionContext context)
            : base("Execute Reactive TSLL Multi-Asset Import Integration Test")
        {
            _context = context;
        }

        /// <summary>
        /// Execute the reactive signal-based integration test workflow
        /// </summary>
        public override async Task<(bool success, string details, string? error)> ExecuteAsync()
        {
            var startTime = DateTime.Now;
            var results = new List<string>();

            try
            {
                results.Add("=== Reactive TSLL Multi-Asset Import Integration Test ===");
                LogInfo("[ReactiveTsllTest] Test started");

                // CRITICAL: Start observing reactive streams BEFORE any operations
                LogInfo("[ReactiveTsllTest] Starting reactive stream observation...");
                ReactiveTestVerifications.StartObserving();
                LogInfo("[ReactiveTsllTest] ✅ Reactive stream observation started");

                // Extract embedded CSV file
                LogInfo("[ReactiveTsllTest] Extracting TSLL CSV file...");
                var tempCsvPath = await ExtractTestCsvFile();
                if (!File.Exists(tempCsvPath))
                {
                    LogError("[ReactiveTsllTest] ❌ CSV file extraction failed");
                    results.Add("❌ CSV file extraction failed");
                    return (false, string.Join("\n", results), "CSV extraction failed");
                }
                LogInfo($"[ReactiveTsllTest] ✅ CSV extracted to: {tempCsvPath}");

                // Use the existing Tastytrade broker from test context
                var tastytradeId = _context.TastytradeId;
                var accountNumber = $"REACTIVE-TSLL-{DateTime.Now:yyyyMMdd-HHmmss}";
                LogInfo($"[ReactiveTsllTest] Creating broker account: {accountNumber}");

                // Add timeout for account creation to prevent hanging
                using var accountCreationCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                try
                {
                    LogInfo("[ReactiveTsllTest] Calling SaveBrokerAccount...");
                    await Creator.SaveBrokerAccount(tastytradeId, accountNumber).WaitAsync(accountCreationCts.Token);
                    LogInfo("[ReactiveTsllTest] ✅ SaveBrokerAccount completed");
                }
                catch (OperationCanceledException)
                {
                    LogError("[ReactiveTsllTest] ❌ Account creation timed out");
                    results.Add("❌ Account creation timed out");
                    return (false, string.Join("\n", results), "Account creation timeout");
                }

                // Find the created broker account
                LogInfo("[ReactiveTsllTest] Looking for created account...");
                var testAccount = Collections.Accounts.Items
                    .Where(a => a.Type == CoreModels.AccountType.BrokerAccount)
                    .Where(a => OptionModule.IsSome(a.Broker) && a.Broker.Value.AccountNumber == accountNumber)
                    .FirstOrDefault();

                if (testAccount == null || testAccount.Type == CoreModels.AccountType.EmptyAccount)
                {
                    LogError($"[ReactiveTsllTest] ❌ Failed to find account. Total accounts: {Collections.Accounts.Items.Count}");
                    results.Add("❌ Failed to create test broker account");
                    return (false, string.Join("\n", results), null);
                }

                var testBrokerAccountId = testAccount.Broker.Value.Id;
                LogInfo($"[ReactiveTsllTest] ✅ Found account with ID: {testBrokerAccountId}");

                // Set up signal monitoring BEFORE import to avoid race conditions
                LogInfo("[ReactiveTsllTest] Setting up signal monitoring BEFORE import...");
                ReactiveTestVerifications.ExpectSignals("Movements_Updated", "Tickers_Updated", "Snapshots_Updated");

                // PRE-IMPORT STATE LOGGING
                LogInfo("[ReactiveTsllTest] === PRE-IMPORT STATE ===");
                LogInfo($"[ReactiveTsllTest] Movements before import: {Collections.Movements.Items.Count}");
                LogInfo($"[ReactiveTsllTest] Tickers before import: {Collections.Tickers.Items.Count}");
                LogInfo($"[ReactiveTsllTest] Snapshots before import: {Collections.Snapshots.Items.Count}");
                results.Add("");
                results.Add("=== Pre-Import State ===");
                results.Add($"Movements: {Collections.Movements.Items.Count}");
                results.Add($"Tickers: {Collections.Tickers.Items.Count}");
                results.Add($"Snapshots: {Collections.Snapshots.Items.Count}");

                // Execute import
                LogInfo($"[ReactiveTsllTest] Starting import from: {tempCsvPath}");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(360));
                ImportResult importResult;
                try
                {
                    var importTask = ImportManager.importFile(tastytradeId, testBrokerAccountId, tempCsvPath);
                    importResult = await importTask.WaitAsync(cts.Token);
                    LogInfo($"[ReactiveTsllTest] ✅ Import completed. Success: {importResult.Success}");
                }
                catch (Exception ex)
                {
                    LogError($"[ReactiveTsllTest] ❌ Import operation exception: {ex.Message}");
                    results.Add("❌ Import operation timed out");
                    return (false, string.Join("\n", results), "Import timeout");
                }

                // IMMEDIATE POST-IMPORT STATE LOGGING
                LogInfo("[ReactiveTsllTest] === IMMEDIATE POST-IMPORT STATE ===");
                LogInfo($"[ReactiveTsllTest] Movements after import: {Collections.Movements.Items.Count}");
                LogInfo($"[ReactiveTsllTest] Tickers after import: {Collections.Tickers.Items.Count}");
                LogInfo($"[ReactiveTsllTest] Snapshots after import: {Collections.Snapshots.Items.Count}");
                results.Add("");
                results.Add("=== Post-Import State (before signals) ===");
                results.Add($"Movements: {Collections.Movements.Items.Count}");
                results.Add($"Tickers: {Collections.Tickers.Items.Count}");
                results.Add($"Snapshots: {Collections.Snapshots.Items.Count}");

                // Wait for signals that should have been emitted by import
                bool importSignalsReceived = true;
                if (importResult.Success)
                {
                    LogInfo("[ReactiveTsllTest] Import succeeded, waiting for reactive signals...");
                    LogInfo("[ReactiveTsllTest] Waiting for reactive signals...");
                    importSignalsReceived = await ReactiveTestVerifications.WaitForAllSignalsAsync(TimeSpan.FromSeconds(15));
                    if (!importSignalsReceived)
                    {
                        var (expected, received, missing) = ReactiveTestVerifications.GetSignalStatus();
                        LogWarning($"[ReactiveTsllTest] ⚠️ Missing signals: [{string.Join(", ", missing)}]");
                        LogWarning($"[ReactiveTsllTest] Expected: [{string.Join(", ", expected)}]");
                        LogWarning($"[ReactiveTsllTest] Received: [{string.Join(", ", received)}]");
                        results.Add($"⚠️ Missing signals: [{string.Join(", ", missing)}]");
                    }
                    else
                    {
                        LogInfo("[ReactiveTsllTest] ✅ All signals received");
                    }
                }
                else
                {
                    LogError("[ReactiveTsllTest] Import failed - signals may not have been emitted");
                    importSignalsReceived = false;
                }

                // POST-SIGNALS STATE LOGGING
                LogInfo("[ReactiveTsllTest] === POST-SIGNALS STATE ===");
                LogInfo($"[ReactiveTsllTest] Movements after signals: {Collections.Movements.Items.Count}");
                LogInfo($"[ReactiveTsllTest] Tickers after signals: {Collections.Tickers.Items.Count}");
                LogInfo($"[ReactiveTsllTest] Snapshots after signals: {Collections.Snapshots.Items.Count}");
                results.Add("");
                results.Add("=== Post-Signals State ===");
                results.Add($"Movements: {Collections.Movements.Items.Count}");
                results.Add($"Tickers: {Collections.Tickers.Items.Count}");
                results.Add($"Snapshots: {Collections.Snapshots.Items.Count}");

                // Data Collection and Logging Phase - NO STRICT VALIDATION YET
                LogInfo("[ReactiveTsllTest] Starting data collection and logging...");
                results.Add("");
                results.Add("=== Import Data Summary ===");

                var movementCount = Collections.Movements.Items.Count;
                var tickerCount = Collections.Tickers.Items.Count;
                var snapshotCount = Collections.Snapshots.Items.Count;
                var accountCount = Collections.Accounts.Items.Count;

                LogInfo($"[ReactiveTsllTest] ✓ Movements count: {movementCount}");
                LogInfo($"[ReactiveTsllTest] ✓ Tickers count: {tickerCount}");
                LogInfo($"[ReactiveTsllTest] ✓ Snapshots count: {snapshotCount}");
                LogInfo($"[ReactiveTsllTest] ✓ Accounts count: {accountCount}");

                // Log all collection data WITHOUT VALIDATION CHECKS
                results.Add($"Movements collected: {movementCount}");
                results.Add($"Tickers collected: {tickerCount}");
                results.Add($"Snapshots collected: {snapshotCount}");
                results.Add($"Accounts in system: {accountCount}");
                results.Add("");

                // Log movement types breakdown
                LogInfo("[ReactiveTsllTest] Movement types breakdown:");
                results.Add("=== Movement Types ===");
                var movementsByType = Collections.Movements.Items
                    .GroupBy(m => m.Type)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count);

                foreach (var typeGroup in movementsByType)
                {
                    LogInfo($"  {typeGroup.Type}: {typeGroup.Count}");
                    results.Add($"  {typeGroup.Type}: {typeGroup.Count}");
                }

                // Log tickers details
                LogInfo("[ReactiveTsllTest] Tickers imported:");
                results.Add("");
                results.Add("=== Tickers Imported ===");
                foreach (var ticker in Collections.Tickers.Items.Take(20))
                {
                    LogInfo($"  {ticker.Symbol} (ID: {ticker.Id})");
                    results.Add($"  {ticker.Symbol} (ID: {ticker.Id})");
                }
                if (Collections.Tickers.Items.Count > 20)
                {
                    LogInfo($"  ... and {Collections.Tickers.Items.Count - 20} more tickers");
                    results.Add($"  ... and {Collections.Tickers.Items.Count - 20} more tickers");
                }

                // Log broker account snapshots summary using public API
                LogInfo("[ReactiveTsllTest] === Broker Account Snapshots Summary ===");
                results.Add("");
                results.Add("=== Broker Account Snapshots ===");

                try
                {
                    var brokerAccountId = testAccount.Broker.Value.Id;
                    var brokerSnapshots = await BrokerAccounts.GetSnapshots(brokerAccountId);
                    LogInfo($"[ReactiveTsllTest] Broker Account Snapshots: {brokerSnapshots.Count()}");
                    results.Add($"Broker Account Snapshots: {brokerSnapshots.Count()}");
                }
                catch (Exception ex)
                {
                    LogWarning($"[ReactiveTsllTest] Could not load broker snapshots: {ex.Message}");
                }

                // Log ticker snapshots summary using public API
                results.Add("");
                results.Add("=== Ticker Snapshots Summary ===");
                try
                {
                    int totalTickerSnapshots = 0;
                    foreach (var ticker in Collections.Tickers.Items)
                    {
                        var tickerSnapshots = await Tickers.GetSnapshots(ticker.Id);
                        totalTickerSnapshots += tickerSnapshots.Count();
                        LogInfo($"[ReactiveTsllTest] Ticker '{ticker.Symbol}' Snapshots: {tickerSnapshots.Count()}");
                        results.Add($"Ticker '{ticker.Symbol}': {tickerSnapshots.Count()} snapshots");
                    }
                    results.Add($"\nTotal Ticker Snapshots: {totalTickerSnapshots}");
                }
                catch (Exception ex)
                {
                    LogWarning($"[ReactiveTsllTest] Could not load ticker snapshots: {ex.Message}");
                }

                // Log final snapshot details if broker account exists
                LogInfo("[ReactiveTsllTest] === Final State Analysis ===");
                results.Add("");
                results.Add("=== Final State ===");

                try
                {
                    var lastBrokerAccountSnapshot = Collections.Snapshots.Items
                        .Where(s => OptionModule.IsSome(s.BrokerAccount))
                        .LastOrDefault();

                    if (lastBrokerAccountSnapshot != null && OptionModule.IsSome(lastBrokerAccountSnapshot.BrokerAccount))
                    {
                        var baSnapshot = lastBrokerAccountSnapshot.BrokerAccount.Value;
                        var financial = baSnapshot.Financial;

                        LogInfo($"[ReactiveTsllTest] Final Financial Data (BROKER ACCOUNT):");
                        LogInfo($"  Commissions: ${financial.Commissions:F2}");
                        LogInfo($"  Fees: ${financial.Fees:F2}");
                        LogInfo($"  RealizedGains: ${financial.RealizedGains:F2}");
                        LogInfo($"  UnrealizedGains: ${financial.UnrealizedGains:F2}");
                        LogInfo($"  Invested: ${financial.Invested:F2}");
                        LogInfo($"  Deposited: ${financial.Deposited:F2}");
                        LogInfo($"  Withdrawn: ${financial.Withdrawn:F2}");
                        LogInfo($"  DividendsReceived: ${financial.DividendsReceived:F2}");
                        LogInfo($"  OptionsIncome: ${financial.OptionsIncome:F2}");
                        LogInfo($"  OpenTrades: {financial.OpenTrades}");

                        results.Add($"Final Commissions: ${financial.Commissions:F2}");
                        results.Add($"Final Fees: ${financial.Fees:F2}");
                        results.Add($"Final RealizedGains: ${financial.RealizedGains:F2}");
                        results.Add($"Final UnrealizedGains: ${financial.UnrealizedGains:F2}");
                        results.Add($"Final Invested: ${financial.Invested:F2}");
                        results.Add($"Final Deposited: ${financial.Deposited:F2}");
                        results.Add($"Final Withdrawn: ${financial.Withdrawn:F2}");
                        results.Add($"Final DividendsReceived: ${financial.DividendsReceived:F2}");
                        results.Add($"Final OptionsIncome: ${financial.OptionsIncome:F2}");
                        results.Add($"OpenTrades: {financial.OpenTrades}");
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"[ReactiveTsllTest] Could not extract final snapshot details: {ex.Message}");
                }

                // Log ticker snapshot details
                LogInfo("[ReactiveTsllTest] === Ticker Snapshot Details ===");
                results.Add("");
                results.Add("=== Ticker Snapshot Details ===");
                try
                {
                    foreach (var ticker in Collections.Tickers.Items)
                    {
                        LogInfo($"[ReactiveTsllTest] Ticker: {ticker.Symbol}");
                        var tickerSnapshots = await Tickers.GetSnapshots(ticker.Id);
                        var sortedSnapshots = tickerSnapshots.OrderBy(s => s.MainCurrency.Date).ToList();

                        results.Add($"Ticker: {ticker.Symbol} ({sortedSnapshots.Count} snapshots)");
                        LogInfo($"[ReactiveTsllTest] Total snapshots for {ticker.Symbol}: {sortedSnapshots.Count}");

                        foreach (var tickerSnapshot in sortedSnapshots)
                        {
                            var mainCurrency = tickerSnapshot.MainCurrency;
                            LogInfo($"  [Snapshot] Date: {mainCurrency.Date}");
                            LogInfo($"  [MainCurrency] Currency: {mainCurrency.Currency.Code}");
                            LogInfo($"  [MainCurrency] TotalShares: {mainCurrency.TotalShares:F4}");
                            LogInfo($"  [MainCurrency] Weight: {mainCurrency.Weight:F2}%");
                            LogInfo($"  [MainCurrency] CostBasis: ${mainCurrency.CostBasis:F2}");
                            LogInfo($"  [MainCurrency] RealCost: ${mainCurrency.RealCost:F2}");
                            LogInfo($"  [MainCurrency] Dividends: ${mainCurrency.Dividends:F2}");
                            LogInfo($"  [MainCurrency] Options: ${mainCurrency.Options:F2}");
                            LogInfo($"  [MainCurrency] TotalIncomes: ${mainCurrency.TotalIncomes:F2}");
                            LogInfo($"  [MainCurrency] Unrealized: ${mainCurrency.Unrealized:F2}");
                            LogInfo($"  [MainCurrency] Realized: ${mainCurrency.Realized:F2}");
                            LogInfo($"  [MainCurrency] Performance: {mainCurrency.Performance:F2}%");
                            LogInfo($"  [MainCurrency] LatestPrice: ${mainCurrency.LatestPrice:F2}");
                            LogInfo($"  [MainCurrency] OpenTrades: {mainCurrency.OpenTrades}");

                            results.Add($"  Snapshot - Date: {mainCurrency.Date}");
                            results.Add($"    Currency: {mainCurrency.Currency.Code}");
                            results.Add($"    TotalShares: {mainCurrency.TotalShares:F4}");
                            results.Add($"    Weight: {mainCurrency.Weight:F2}%");
                            results.Add($"    CostBasis: ${mainCurrency.CostBasis:F2}");
                            results.Add($"    RealCost: ${mainCurrency.RealCost:F2}");
                            results.Add($"    Dividends: ${mainCurrency.Dividends:F2}");
                            results.Add($"    Options: ${mainCurrency.Options:F2}");
                            results.Add($"    TotalIncomes: ${mainCurrency.TotalIncomes:F2}");
                            results.Add($"    Unrealized: ${mainCurrency.Unrealized:F2}");
                            results.Add($"    Realized: ${mainCurrency.Realized:F2}");
                            results.Add($"    Performance: {mainCurrency.Performance:F2}%");
                            results.Add($"    LatestPrice: ${mainCurrency.LatestPrice:F2}");
                            results.Add($"    OpenTrades: {mainCurrency.OpenTrades}");

                            if (tickerSnapshot.OtherCurrencies.Length > 0)
                            {
                                LogInfo($"    [OtherCurrencies] Count: {tickerSnapshot.OtherCurrencies.Length}");
                                results.Add($"    OtherCurrencies: {tickerSnapshot.OtherCurrencies.Length}");
                                foreach (var otherCurrency in tickerSnapshot.OtherCurrencies)
                                {
                                    LogInfo($"      Currency: {otherCurrency.Currency.Code}");
                                    LogInfo($"        TotalShares: {otherCurrency.TotalShares:F4}");
                                    LogInfo($"        CostBasis: ${otherCurrency.CostBasis:F2}");
                                    LogInfo($"        Unrealized: ${otherCurrency.Unrealized:F2}");
                                    LogInfo($"        Realized: ${otherCurrency.Realized:F2}");
                                    results.Add($"      {otherCurrency.Currency.Code}: Shares={otherCurrency.TotalShares:F4}, CostBasis=${otherCurrency.CostBasis:F2}");
                                }
                            }
                            results.Add("");
                        }
                        results.Add("");
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"[ReactiveTsllTest] Could not extract ticker snapshot details: {ex.Message}");
                }

                // Log all movements with details
                LogInfo("[ReactiveTsllTest] === All Movements Details ===");
                results.Add("");
                results.Add("=== All Movements Details ===");
                try
                {
                    var allMovements = Collections.Movements.Items.OrderBy(m => m.TimeStamp).ToList();
                    LogInfo($"[ReactiveTsllTest] Total movements: {allMovements.Count}");
                    results.Add($"Total movements: {allMovements.Count}");
                    results.Add("");

                    foreach (var movement in allMovements)
                    {
                        string movementInfo = "";
                        string optionStatus = "";
                        string strikeInfo = "";
                        string notesInfo = "";

                        // Check if movement is an option contract
                        if (OptionModule.IsSome(movement.OptionTrade))
                        {
                            var optionTrade = movement.OptionTrade.Value;
                            optionStatus = $" [OPTION] Code: {optionTrade.Code}, IsOpen: {optionTrade.IsOpen}";

                            // Extract strike price
                            strikeInfo = $", Strike: ${optionTrade.Strike:F2}";

                            // Extract notes from option trade
                            if (OptionModule.IsSome(optionTrade.Notes))
                            {
                                notesInfo = $" | Notes: {optionTrade.Notes.Value}";
                            }

                            movementInfo = $"[Movement] Date: {movement.TimeStamp:yyyy-MM-dd HH:mm:ss}, Type: {movement.Type}{optionStatus}{strikeInfo}{notesInfo}";
                        }
                        else
                        {
                            movementInfo = $"[Movement] Date: {movement.TimeStamp:yyyy-MM-dd HH:mm:ss}, Type: {movement.Type}";
                        }

                        LogInfo($"  {movementInfo}");
                        results.Add($"  {movementInfo}");
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"[ReactiveTsllTest] Could not extract movement details: {ex.Message}");
                }

                // SNAPSHOT VALIDATION FOR TSLL AND BROKER ACCOUNT
                // Based on analysis of TsllImportTest.csv: should produce exactly 71 snapshots for TSLL ticker
                results.Add("");
                results.Add("=== Snapshot Validation ===");

                int expectedTsllSnapshots = 71;
                int tsllSnapshotCount = 0;
                int brokerAccountSnapshotCount = 0;
                bool tsllValidationPassed = false;

                try
                {
                    // Validate TSLL ticker snapshots
                    var snapshotCountTicker = Collections.Tickers.Items.FirstOrDefault(t => t.Symbol == "TSLL");
                    if (snapshotCountTicker != null)
                    {
                        var snapshotCountSnapshots = await Tickers.GetSnapshots(snapshotCountTicker.Id);
                        tsllSnapshotCount = snapshotCountSnapshots.Count();

                        LogInfo($"[ReactiveTsllTest] TSLL Snapshots validation:");
                        LogInfo($"[ReactiveTsllTest]   Expected: {expectedTsllSnapshots} snapshots");
                        LogInfo($"[ReactiveTsllTest]   Actual: {tsllSnapshotCount} snapshots");

                        results.Add($"Expected TSLL Snapshots: {expectedTsllSnapshots}");
                        results.Add($"Actual TSLL Snapshots: {tsllSnapshotCount}");

                        if (tsllSnapshotCount == expectedTsllSnapshots)
                        {
                            LogInfo($"[ReactiveTsllTest] ✅ TSLL snapshot count validation PASSED");
                            results.Add("✅ TSLL snapshot count validation PASSED");
                            tsllValidationPassed = true;
                        }
                        else
                        {
                            LogWarning($"[ReactiveTsllTest] ⚠️ TSLL snapshot count mismatch: expected {expectedTsllSnapshots}, got {tsllSnapshotCount}");
                            results.Add($"⚠️ TSLL snapshot count mismatch: expected {expectedTsllSnapshots}, got {tsllSnapshotCount}");
                            results.Add($"   Difference: {Math.Abs(expectedTsllSnapshots - tsllSnapshotCount)} snapshots");
                            if (tsllSnapshotCount > expectedTsllSnapshots)
                            {
                                results.Add($"   Result: {tsllSnapshotCount - expectedTsllSnapshots} MORE snapshots than expected");
                            }
                            else
                            {
                                results.Add($"   Result: {expectedTsllSnapshots - tsllSnapshotCount} FEWER snapshots than expected");
                            }
                        }
                    }
                    else
                    {
                        LogWarning("[ReactiveTsllTest] ⚠️ TSLL ticker not found in collections");
                        results.Add("⚠️ TSLL ticker not found");
                    }

                    // Validate Broker Account snapshots
                    results.Add("");
                    var brokerAccountId = testAccount.Broker.Value.Id;
                    var brokerSnapshots = await BrokerAccounts.GetSnapshots(brokerAccountId);
                    brokerAccountSnapshotCount = brokerSnapshots.Count();

                    LogInfo($"[ReactiveTsllTest] Broker Account Snapshots validation:");
                    LogInfo($"[ReactiveTsllTest]   Actual: {brokerAccountSnapshotCount} snapshots");

                    results.Add($"Broker Account Snapshots: {brokerAccountSnapshotCount}");

                    if (brokerAccountSnapshotCount > 0)
                    {
                        LogInfo($"[ReactiveTsllTest] ✅ Broker account has snapshot history");
                        results.Add("✅ Broker account has snapshot history");
                    }
                    else
                    {
                        LogWarning($"[ReactiveTsllTest] ⚠️ Broker account has no snapshots");
                        results.Add("⚠️ Broker account has no snapshots");
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"[ReactiveTsllTest] Could not validate snapshots: {ex.Message}");
                    results.Add($"⚠️ Could not validate snapshots: {ex.Message}");
                }

                results.Add("");
                results.Add("=== Snapshot Validation Summary ===");
                results.Add($"TSLL Ticker Snapshots: {tsllSnapshotCount}/{expectedTsllSnapshots}");
                results.Add($"Broker Account Snapshots: {brokerAccountSnapshotCount}");
                results.Add($"Validation Status: {(tsllValidationPassed ? "✅ PASSED" : "⚠️ REVIEW NEEDED")}");

                // Load TSLL snapshots ONCE for all validations
                LogInfo("[ReactiveTsllTest] Loading TSLL snapshots for validation...");
                var tsllTicker = Collections.Tickers.Items.FirstOrDefault(t => t.Symbol == "TSLL");
                IEnumerable<dynamic> tsllSnapshots = new List<dynamic>();

                if (tsllTicker != null)
                {
                    tsllSnapshots = await Tickers.GetSnapshots(tsllTicker.Id);
                    LogInfo($"[ReactiveTsllTest] ✅ Loaded {tsllSnapshots.Count()} TSLL snapshots for detailed validation");
                }
                else
                {
                    LogWarning("[ReactiveTsllTest] ⚠️ TSLL ticker not found - skipping detailed snapshot validations");
                }

                // Validate oldest TSLL snapshot baseline
                LogInfo("[ReactiveTsllTest] Starting TSLL snapshot validations...");
                var allSnapshotValidationsValid = true;

                foreach (var expectedData in TsllSnapshotValidations)
                {
                    LogInfo($"[ReactiveTsllTest] Validating: {expectedData.ValidationContext}");

                    // Find snapshot by expected date
                    var targetDate = new DateOnly(expectedData.ExpectedDate.Year, expectedData.ExpectedDate.Month, expectedData.ExpectedDate.Day);
                    var snapshot = FindSnapshotByDate(tsllSnapshots, targetDate, expectedData.ValidationContext);

                    if (snapshot == null)
                    {
                        LogWarning($"[ReactiveTsllTest] ⚠️ No snapshot found for {expectedData.ValidationContext}");
                        results.Add($"⚠️ No snapshot found for {expectedData.ValidationContext}");
                        allSnapshotValidationsValid = false;
                        continue;
                    }

                    // Validate snapshot - use Item property for tuple access since snapshot is dynamic
                    var validationResult = TestVerifications.ValidateTickerSnapshot(snapshot, expectedData);
                    results.AddRange(validationResult.Item2);

                    if (validationResult.Item1)
                    {
                        LogInfo($"[ReactiveTsllTest] ✅ {expectedData.ValidationContext} validation PASSED");
                    }
                    else
                    {
                        LogWarning($"[ReactiveTsllTest] ⚠️ {expectedData.ValidationContext} validation FAILED");
                        allSnapshotValidationsValid = false;
                    }
                }                // SUCCESS CONDITION - All validations must pass
                // Core import must succeed AND have data AND all snapshot validations must pass
                bool success = importResult.Success &&
                             (movementCount > 0 || tickerCount > 0 || snapshotCount > 0) &&
                             importSignalsReceived &&
                             allSnapshotValidationsValid;

                // Cleanup
                if (!string.IsNullOrEmpty(tempCsvPath) && File.Exists(tempCsvPath))
                {
                    LogInfo($"[ReactiveTsllTest] Cleaning up temp file: {tempCsvPath}");
                    File.Delete(tempCsvPath);
                }

                var finalMessage = success ? "\n✅ TEST PASSED" : "\n❌ TEST FAILED";
                results.Add(finalMessage);
                LogInfo($"[ReactiveTsllTest] {finalMessage}");

                var elapsedTime = DateTime.Now - startTime;
                LogInfo($"[ReactiveTsllTest] Test execution time: {elapsedTime.TotalSeconds:F1}s");
                results.Add($"\nExecution time: {elapsedTime.TotalSeconds:F1}s");

                return (success, string.Join("\n", results), success ? null : "Test validation failed");
            }
            catch (Exception ex)
            {
                LogError($"[ReactiveTsllTest] ❌ Unexpected error: {ex.Message}");
                Console.WriteLine($"[ReactiveTsllTest] ❌ Exception: {ex}");
                results.Add($"❌ Unexpected error: {ex.Message}");
                return (false, string.Join("\n", results), ex.Message);
            }
        }

        private void LogInfo(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
            Console.WriteLine(message);
        }

        private void LogError(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
            Console.WriteLine(message);
        }

        private void LogWarning(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
            Console.WriteLine(message);
        }

        private async Task<string?> ExtractTestCsvFile()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "Core.Platform.MauiTester.Resources.TestData.TsllImportTest.csv";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        LogError($"[ReactiveTsllTest] Could not find embedded resource: {resourceName}");
                        return null;
                    }

                    var tempPath = Path.Combine(Path.GetTempPath(), $"TsllImportTest_{Guid.NewGuid():N}.csv");
                    using (var fileStream = File.Create(tempPath))
                    {
                        await stream.CopyToAsync(fileStream);
                    }

                    return tempPath;
                }
            }
            catch (Exception ex)
            {
                LogError($"[ReactiveTsllTest] Error extracting CSV: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Finds a snapshot by specific date and logs debug info if not found.
        /// </summary>
        private dynamic? FindSnapshotByDate(
            IEnumerable<dynamic> snapshots,
            DateOnly targetDate,
            string snapshotDescription)
        {
            var snapshot = snapshots.FirstOrDefault(s => s.MainCurrency.Date == targetDate);

            if (snapshot == null)
            {
                // Debug: Log available dates around target date
                var snapshotsInMonth = snapshots
                    .Where(s => s.MainCurrency.Date.Month == targetDate.Month &&
                               s.MainCurrency.Date.Year == targetDate.Year)
                    .OrderBy(s => s.MainCurrency.Date)
                    .ToList();

                LogWarning($"[ReactiveTsllTest] No snapshot found for {targetDate:yyyy-MM-dd}. Found {snapshotsInMonth.Count} snapshots in {targetDate:MMMM yyyy}:");
                foreach (var snap in snapshotsInMonth.Take(5))
                {
                    LogWarning($"[ReactiveTsllTest]   - {snap.MainCurrency.Date:yyyy-MM-dd}");
                }
            }

            return snapshot;
        }

        /// <summary>
        /// Static list of TSLL snapshot validation scenarios.
        /// Data-driven approach: each entry defines expected values for a specific snapshot.
        /// </summary>
        private static readonly List<SnapshotValidationData> TsllSnapshotValidations = new()
        {
            new SnapshotValidationData
            {
                ExpectedDate = new DateTime(2024, 5, 30),
                Currency = "USD",
                TotalShares = 0.0000m,
                Weight = 0.00m,
                CostBasis = 0.00m,
                RealCost = 0.00m,
                Dividends = 0.00m,
                Options = 13.86m,
                TotalIncomes = 13.86m,
                Unrealized = 13.86m,
                Realized = 0.00m,
                Performance = 0.00m,
                LatestPrice = 0.00m,
                OpenTrades = true,
                ValidationContext = "TSLL Oldest Snapshot",
                Description = "Baseline snapshot at 5/30/2024"
            },
            new SnapshotValidationData
            {
                ExpectedDate = new DateTime(2024, 6, 7),
                Currency = "USD",
                TotalShares = 0.0000m,
                Weight = 0.00m,
                CostBasis = 0.00m,
                RealCost = 0.00m,
                Dividends = 0.00m,
                Options = 13.86m,
                TotalIncomes = 13.86m,
                Unrealized = 0.00m,
                Realized = 13.86m,
                Performance = 0.00m,
                LatestPrice = 0.00m,
                OpenTrades = false,
                ValidationContext = "TSLL Snapshot After Expiration",
                Description = "Put option expired worthless on 6/7/2024"
            },
            new SnapshotValidationData
            {
                ExpectedDate = new DateTime(2024, 10, 15),
                Currency = "USD",
                TotalShares = 0.0000m,
                Weight = 0.00m,
                CostBasis = 0.00m,
                RealCost = 0.00m,
                Dividends = 0.00m,
                Options = -474.41m,
                TotalIncomes = -474.41m,
                Unrealized = -488.27m,
                Realized = 13.86m,
                Performance = 0.00m,
                LatestPrice = 0.00m,
                OpenTrades = true,
                ValidationContext = "TSLL Snapshot with Unrealized Losses",
                Description = "Open call positions showing unrealized losses"
            }
        };
    }
}
