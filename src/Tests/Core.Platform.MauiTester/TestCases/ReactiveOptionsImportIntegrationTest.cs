using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Binnaculum.Core.Import;
using Binnaculum.Core.UI;
using Core.Platform.MauiTester.Services;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using CoreModels = Binnaculum.Core.Models;

namespace Core.Platform.MauiTester.TestCases
{
    /// <summary>
    /// Reactive signal-based version of Options Import Integration Test.
    /// Uses signal-based approach to wait for actual reactive stream updates instead of delays.
    /// </summary>
    public class ReactiveOptionsImportIntegrationTest : TestStep
    {
        private readonly TestExecutionContext _context;

        public ReactiveOptionsImportIntegrationTest(TestExecutionContext context)
            : base("Execute Reactive Options Import Integration Test")
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
                results.Add("=== Reactive Options Import Integration Test ===");

                // Extract embedded CSV file
                var tempCsvPath = await ExtractTestCsvFile();
                if (!File.Exists(tempCsvPath))
                {
                    results.Add("❌ CSV file extraction failed");
                    return (false, string.Join("\n", results), "CSV extraction failed");
                }

                // Use the existing Tastytrade broker from test context
                var tastytradeId = _context.TastytradeId;
                var accountNumber = $"REACTIVE-TEST-{DateTime.Now:yyyyMMdd-HHmmss}";

                // Add timeout for account creation to prevent hanging
                using var accountCreationCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                try
                {
                    await Creator.SaveBrokerAccount(tastytradeId, accountNumber).WaitAsync(accountCreationCts.Token);
                }
                catch (OperationCanceledException)
                {
                    results.Add("❌ Account creation timed out");
                    return (false, string.Join("\n", results), "Account creation timeout");
                }

                // Find the created broker account
                var testAccount = Collections.Accounts.Items
                    .Where(a => a.Type == CoreModels.AccountType.BrokerAccount)
                    .Where(a => OptionModule.IsSome(a.Broker) && a.Broker.Value.AccountNumber == accountNumber)
                    .FirstOrDefault();

                if (testAccount == null || testAccount.Type == CoreModels.AccountType.EmptyAccount)
                {
                    results.Add("❌ Failed to create test broker account");
                    return (false, string.Join("\n", results), null);
                }

                var testBrokerAccountId = testAccount.Broker.Value.Id;

                // Set up signal monitoring for import
                ReactiveTestVerifications.ExpectSignals("Movements_Updated", "Tickers_Updated", "Snapshots_Updated");

                // Execute import
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                ImportResult importResult;
                try
                {
                    var importTask = ImportManager.importFile(tastytradeId, testBrokerAccountId, tempCsvPath);
                    importResult = await importTask.WaitAsync(cts.Token);
                }
                catch (Exception)
                {
                    results.Add("❌ Import operation timed out");
                    return (false, string.Join("\n", results), "Import timeout");
                }

                // Wait for import signals
                var importSignalsReceived = await ReactiveTestVerifications.WaitForAllSignalsAsync(TimeSpan.FromSeconds(15));
                if (!importSignalsReceived)
                {
                    var (expected, received, missing) = ReactiveTestVerifications.GetSignalStatus();
                    results.Add($"⚠️ Missing signals: [{string.Join(", ", missing)}]");
                }

                // Validation
                results.Add("=== Validation Results ===");

                var movementCount = Collections.Movements.Items.Count;
                var tickerCount = Collections.Tickers.Items.Count;
                var snapshotCount = Collections.Snapshots.Items.Count;

                // Expected values based on TastytradeOptionsTest.csv analysis:
                // - 12 options trades (BUY_TO_OPEN, SELL_TO_OPEN, BUY_TO_CLOSE, SELL_TO_CLOSE)
                // - 4 money movements (deposits, balance adjustments)
                // - Total database movements: 16 (all individual records)
                // - Collections.Movements count: 16 (12 option trades + 4 money movements)
                //   * Option trades are grouped by (ticker, type, strike, expiration, code, date) when GroupOptions=true
                //   * This keeps opening/closing trades separate and trades from different days separate
                //   * Each of the 12 trades in the CSV represents a unique combination, so NO grouping occurs:
                //     1. 2024-04-25 SELL_TO_OPEN SOFI PUT 7.0 5/03/24
                //     2. 2024-04-26 BUY_TO_OPEN MPW PUT 4.0 5/03/24
                //     3. 2024-04-26 SELL_TO_OPEN MPW PUT 4.5 5/03/24
                //     4. 2024-04-26 BUY_TO_OPEN PLTR PUT 21.0 5/03/24
                //     5. 2024-04-26 SELL_TO_OPEN PLTR PUT 21.5 5/03/24
                //     6. 2024-04-29 SELL_TO_CLOSE MPW PUT 4.0 5/03/24
                //     7. 2024-04-29 BUY_TO_CLOSE MPW PUT 4.5 5/03/24
                //     8. 2024-04-29 SELL_TO_CLOSE PLTR PUT 21.0 5/03/24
                //     9. 2024-04-29 BUY_TO_CLOSE PLTR PUT 21.5 5/03/24
                //     10. 2024-04-29 SELL_TO_OPEN SOFI PUT 7.0 5/03/24
                //     11. 2024-04-29 BUY_TO_CLOSE SOFI PUT 7.0 5/03/24
                //     12. 2024-04-30 SELL_TO_OPEN SOFI PUT 6.5 5/10/24
                // - Unique tickers from CSV: 3 (SOFI, PLTR, MPW)
                // - Default system ticker: 1 (SPY from TickerExtensions.tickerList)
                // - Total tickers: 4
                const int EXPECTED_COLLECTIONS_MOVEMENTS = 16; // 12 option trades (no grouping) + 4 money movements
                const int EXPECTED_DATABASE_MOVEMENTS = 16; // All individual records in database
                const int EXPECTED_UNIQUE_TICKERS = 4; // 3 from CSV + 1 default (SPY)
                const int EXPECTED_MIN_SNAPSHOTS = 1; // At least one broker account snapshot should be created

                // Expected financial data from CSV:
                // Money movements: $844.56 + $24.23 + $10.00 = $878.79 (deposits only, excluding -$0.02 balance adjustment)
                const decimal EXPECTED_DEPOSITED = 878.79m;

                // Options Income: Net profit/loss from ALL options trading activity
                // OptionsIncome = Sum of ALL NetPremium values (sells positive, buys negative)
                // SELL_TO_OPEN (5 trades): $14.86 + $15.86 + $17.86 + $17.86 + $33.86 = $100.30
                // SELL_TO_CLOSE (2 trades): $4.86 + $0.86 = $5.72
                // BUY_TO_OPEN (3 trades): -$12.13 + -$5.13 = -$17.26
                // BUY_TO_CLOSE (3 trades): -$9.13 + -$17.13 + -$8.13 = -$34.39
                // Total: $100.30 + $5.72 - $17.26 - $34.39 = $54.37
                // This represents the actual net income from options trading after all costs
                const decimal EXPECTED_OPTIONS_INCOME = 54.37m;                // Validate exact Collections.Movements count (option trades + money movements)
                bool movementCountValid = movementCount == EXPECTED_COLLECTIONS_MOVEMENTS;
                results.Add($"Collections.Movements: Expected {EXPECTED_COLLECTIONS_MOVEMENTS}, Got {movementCount} - {(movementCountValid ? "✅ PASS" : "❌ FAIL")}");

                // Validate exact ticker count (3 from CSV data + 1 default SPY ticker)
                bool tickerCountValid = tickerCount == EXPECTED_UNIQUE_TICKERS;
                results.Add($"Tickers: Expected {EXPECTED_UNIQUE_TICKERS}, Got {tickerCount} - {(tickerCountValid ? "✅ PASS" : "❌ FAIL")}");

                // Validate minimum snapshot count
                bool snapshotCountValid = snapshotCount >= EXPECTED_MIN_SNAPSHOTS;
                results.Add($"Snapshots: Expected >= {EXPECTED_MIN_SNAPSHOTS}, Got {snapshotCount} - {(snapshotCountValid ? "✅ PASS" : "❌ FAIL")}");

                // Enhanced validation: Check broker account snapshot movement counter (database records)
                try
                {
                    // Get the broker account snapshot to validate movement counter matches expected database count
                    var brokerAccountSnapshot = Collections.Snapshots.Items
                        .FirstOrDefault(s => s.Type == Binnaculum.Core.Models.OverviewSnapshotType.BrokerAccount);

                    if (brokerAccountSnapshot?.BrokerAccount?.Value?.Financial != null)
                    {
                        var movementCounter = brokerAccountSnapshot.BrokerAccount.Value.Financial.MovementCounter;
                        bool movementCounterValid = movementCounter == EXPECTED_DATABASE_MOVEMENTS;
                        results.Add($"Database MovementCounter: Expected {EXPECTED_DATABASE_MOVEMENTS}, Got {movementCounter} - {(movementCounterValid ? "✅ PASS" : "❌ FAIL")}");

                        // Financial data validation - Deposited amount
                        var deposited = brokerAccountSnapshot.BrokerAccount.Value.Financial.Deposited;
                        bool depositedValid = deposited == EXPECTED_DEPOSITED;
                        results.Add($"Deposited: Expected ${EXPECTED_DEPOSITED:F2}, Got ${deposited:F2} - {(depositedValid ? "✅ PASS" : "❌ FAIL")}");

                        // Financial data validation - Options Income
                        var optionsIncome = brokerAccountSnapshot.BrokerAccount.Value.Financial.OptionsIncome;
                        bool optionsIncomeValid = optionsIncome == EXPECTED_OPTIONS_INCOME;
                        results.Add($"OptionsIncome: Expected ${EXPECTED_OPTIONS_INCOME:F2}, Got ${optionsIncome:F2} - {(optionsIncomeValid ? "✅ PASS" : "❌ FAIL")}");

                        snapshotCountValid = snapshotCountValid && movementCounterValid && depositedValid && optionsIncomeValid;
                    }
                    else
                    {
                        results.Add("❌ BrokerAccount snapshot not found");
                        snapshotCountValid = false;
                    }
                }
                catch (Exception snapValidationEx)
                {
                    results.Add($"❌ Snapshot validation error: {snapValidationEx.Message}");
                    snapshotCountValid = false;
                }
                var success = importResult.Success &&
                             movementCountValid &&
                             tickerCountValid &&
                             snapshotCountValid &&
                             importSignalsReceived; // All validations must pass

                // Cleanup
                if (!string.IsNullOrEmpty(tempCsvPath) && File.Exists(tempCsvPath))
                {
                    File.Delete(tempCsvPath);
                }

                results.Add(success ? "\n✅ TEST PASSED" : "\n❌ TEST FAILED");

                return (success, string.Join("\n", results), null);
            }
            catch (Exception ex)
            {
                results.Add($"❌ EXCEPTION: {ex.Message}");
                return (false, string.Join("\n", results), ex.ToString());
            }
            finally
            {
                ReactiveTestVerifications.StopObserving();
            }
        }

        /// <summary>
        /// Extract embedded CSV test file
        /// </summary>
        private async Task<string> ExtractTestCsvFile()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Core.Platform.MauiTester.Resources.TestData.TastytradeOptionsTest.csv";

            var tempPath = Path.Combine(Path.GetTempPath(), $"tastytrade_test_{Guid.NewGuid()}.csv");

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

            using var fileStream = File.Create(tempPath);
            await stream.CopyToAsync(fileStream);

            return tempPath;
        }
    }
}