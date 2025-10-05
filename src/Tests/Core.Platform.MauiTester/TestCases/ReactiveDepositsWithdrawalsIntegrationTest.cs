using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Binnaculum.Core.Import;
using Binnaculum.Core.Logging;
using Binnaculum.Core.UI;
using Core.Platform.MauiTester.Services;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using CoreModels = Binnaculum.Core.Models;

namespace Core.Platform.MauiTester.TestCases
{
    /// <summary>
    /// Reactive signal-based test for Deposits and Withdrawals import.
    /// Validates that money movements are correctly imported and reflected in snapshots.
    /// </summary>
    public class ReactiveDepositsWithdrawalsIntegrationTest : TestStep
    {
        private readonly TestExecutionContext _context;

        public ReactiveDepositsWithdrawalsIntegrationTest(TestExecutionContext context)
            : base("Execute Reactive Deposits & Withdrawals Integration Test")
        {
            _context = context;
        }

        /// <summary>
        /// Execute the reactive signal-based integration test workflow for deposits/withdrawals
        /// </summary>
        public override async Task<(bool success, string details, string? error)> ExecuteAsync()
        {
            var startTime = DateTime.Now;
            var results = new List<string>();

            try
            {
                results.Add("=== Reactive Deposits & Withdrawals Integration Test ===");
                CoreLogger.logInfo("DepositsTest", "Test started");

                // Extract embedded CSV file
                CoreLogger.logInfo("DepositsTest", "Extracting CSV file...");
                var tempCsvPath = await ExtractTestCsvFile();
                if (!File.Exists(tempCsvPath))
                {
                    CoreLogger.logError("DepositsTest", "❌ CSV file extraction failed");
                    results.Add("❌ CSV file extraction failed");
                    return (false, string.Join("\n", results), "CSV extraction failed");
                }
                CoreLogger.logInfo("DepositsTest", $"✅ CSV extracted to: {tempCsvPath}");

                // Use the existing Tastytrade broker from test context
                var tastytradeId = _context.TastytradeId;
                var accountNumber = $"DEPOSITS-TEST-{DateTime.Now:yyyyMMdd-HHmmss}";
                CoreLogger.logInfo("DepositsTest", $"Creating broker account: {accountNumber}");

                // Add timeout for account creation to prevent hanging
                using var accountCreationCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                try
                {
                    CoreLogger.logInfo("DepositsTest", "Calling SaveBrokerAccount...");
                    await Creator.SaveBrokerAccount(tastytradeId, accountNumber).WaitAsync(accountCreationCts.Token);
                    CoreLogger.logInfo("DepositsTest", "✅ SaveBrokerAccount completed");
                }
                catch (OperationCanceledException)
                {
                    CoreLogger.logError("DepositsTest", "❌ Account creation timed out");
                    results.Add("❌ Account creation timed out");
                    return (false, string.Join("\n", results), "Account creation timeout");
                }

                // Find the created broker account
                CoreLogger.logInfo("DepositsTest", "Looking for created account...");
                var testAccount = Collections.Accounts.Items
                    .Where(a => a.Type == CoreModels.AccountType.BrokerAccount)
                    .Where(a => OptionModule.IsSome(a.Broker) && a.Broker.Value.AccountNumber == accountNumber)
                    .FirstOrDefault();

                if (testAccount == null || testAccount.Type == CoreModels.AccountType.EmptyAccount)
                {
                    CoreLogger.logError("DepositsTest", $"❌ Failed to find account. Total accounts: {Collections.Accounts.Items.Count}");
                    results.Add("❌ Failed to create test broker account");
                    return (false, string.Join("\n", results), null);
                }

                var testBrokerAccountId = testAccount.Broker.Value.Id;
                CoreLogger.logInfo("DepositsTest", $"✅ Found account with ID: {testBrokerAccountId}");

                // Set up signal monitoring for import
                CoreLogger.logInfo("DepositsTest", "Setting up signal monitoring...");
                ReactiveTestVerifications.ExpectSignals("Movements_Updated", "Snapshots_Updated");

                // Execute import
                CoreLogger.logInfo("DepositsTest", $"Starting import from: {tempCsvPath}");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                ImportResult importResult;
                try
                {
                    var importTask = ImportManager.importFile(tastytradeId, testBrokerAccountId, tempCsvPath);
                    importResult = await importTask.WaitAsync(cts.Token);
                    CoreLogger.logInfo("DepositsTest", $"✅ Import completed. Success: {importResult.Success}");
                }
                catch (Exception ex)
                {
                    CoreLogger.logError("DepositsTest", $"❌ Import operation exception: {ex.Message}");
                    results.Add("❌ Import operation timed out");
                    return (false, string.Join("\n", results), "Import timeout");
                }

                // Wait for import signals
                CoreLogger.logInfo("DepositsTest", "Waiting for reactive signals...");
                var importSignalsReceived = await ReactiveTestVerifications.WaitForAllSignalsAsync(TimeSpan.FromSeconds(15));
                if (!importSignalsReceived)
                {
                    var (expected, received, missing) = ReactiveTestVerifications.GetSignalStatus();
                    CoreLogger.logWarning("DepositsTest", $"⚠️ Missing signals: [{string.Join(", ", missing)}]");
                    CoreLogger.logWarning("DepositsTest", $"Expected: [{string.Join(", ", expected)}]");
                    CoreLogger.logWarning("DepositsTest", $"Received: [{string.Join(", ", received)}]");
                    results.Add($"⚠️ Missing signals: [{string.Join(", ", missing)}]");
                }
                else
                {
                    CoreLogger.logInfo("DepositsTest", "✅ All signals received");
                }

                // Validation
                CoreLogger.logInfo("DepositsTest", "Starting validation...");
                results.Add("=== Validation Results ===");

                // Validate BrokerAccounts.GetSnapshots retrieves all snapshots from database
                CoreLogger.logInfo("DepositsTest", "Calling BrokerAccounts.GetSnapshots...");
                var retrievedSnapshots = await BrokerAccounts.GetSnapshots(testBrokerAccountId);
                var retrievedSnapshotCount = ListModule.Length(retrievedSnapshots);

                // Expected: 21 snapshots total (1 initial + 20 from movements)
                const int EXPECTED_RETRIEVED_SNAPSHOTS = 21;
                bool retrievedSnapshotsValid = retrievedSnapshotCount == EXPECTED_RETRIEVED_SNAPSHOTS;
                results.Add($"BrokerAccounts.GetSnapshots: Expected {EXPECTED_RETRIEVED_SNAPSHOTS}, Got {retrievedSnapshotCount} - {(retrievedSnapshotsValid ? "✅ PASS" : "❌ FAIL")}");
                CoreLogger.logInfo("DepositsTest", $"Retrieved snapshots: {retrievedSnapshotCount} (expected {EXPECTED_RETRIEVED_SNAPSHOTS})");

                var movementCount = Collections.Movements.Items.Count;
                var snapshotCount = Collections.Snapshots.Items.Count;

                // Validate individual snapshot amounts (date-by-date)
                var individualSnapshotValidation = ValidateIndividualSnapshots(retrievedSnapshots, results);
                retrievedSnapshotsValid = retrievedSnapshotsValid && individualSnapshotValidation;

                CoreLogger.logInfo("DepositsTest", $"Movements count: {movementCount}");
                CoreLogger.logInfo("DepositsTest", $"Snapshots count: {snapshotCount}");

                // Expected values based on TastytradeDeposits.csv analysis:
                // 20 money movement records total:
                // - 19 deposits totaling $19,388.40
                // - 1 withdrawal of $25.00
                // - Net deposited: $19,363.40
                const int EXPECTED_MOVEMENTS = 20;
                const int EXPECTED_MIN_SNAPSHOTS = 1;

                // Financial expectations:
                // Total deposits: $10 + $24.23 + $844.56 + $2,799.61 + $750 + $900 + $910 + $900 + 
                //                 $50 + $850 + $1,200 + $50 + $1,200 + $1,200 + $1,700 + $1,500 + 
                //                 $1,000 + $2,200 + $1,300 = $19,388.40
                // Total withdrawals: $25.00
                // NOTE: Database stores GROSS amounts separately (Deposited and Withdrawn fields)
                const decimal EXPECTED_DEPOSITED = 19388.40m;  // Gross deposits (not net)
                const decimal EXPECTED_WITHDRAWN = 25.00m;
                const decimal EXPECTED_OPTIONS_INCOME = 0.00m;  // No options trades
                const decimal EXPECTED_REALIZED_GAINS = 0.00m;  // No closed positions
                const decimal EXPECTED_UNREALIZED_GAINS = 0.00m; // No open positions

                // Validate exact movement count
                bool movementCountValid = movementCount == EXPECTED_MOVEMENTS;
                results.Add($"Movements: Expected {EXPECTED_MOVEMENTS}, Got {movementCount} - {(movementCountValid ? "✅ PASS" : "❌ FAIL")}");

                // Validate minimum snapshot count
                bool snapshotCountValid = snapshotCount >= EXPECTED_MIN_SNAPSHOTS;
                results.Add($"Snapshots: Expected >= {EXPECTED_MIN_SNAPSHOTS}, Got {snapshotCount} - {(snapshotCountValid ? "✅ PASS" : "❌ FAIL")}");

                // Enhanced validation: Check broker account snapshot financial data
                try
                {
                    CoreLogger.logInfo("DepositsTest", "Validating broker account snapshot...");

                    var brokerAccountSnapshot = Collections.Snapshots.Items
                        .FirstOrDefault(s => s.Type == Binnaculum.Core.Models.OverviewSnapshotType.BrokerAccount);

                    if (brokerAccountSnapshot?.BrokerAccount?.Value?.Financial != null)
                    {
                        CoreLogger.logInfo("DepositsTest", "✅ Found broker account snapshot");

                        var movementCounter = brokerAccountSnapshot.BrokerAccount.Value.Financial.MovementCounter;
                        bool movementCounterValid = movementCounter == EXPECTED_MOVEMENTS;
                        results.Add($"Database MovementCounter: Expected {EXPECTED_MOVEMENTS}, Got {movementCounter} - {(movementCounterValid ? "✅ PASS" : "❌ FAIL")}");
                        CoreLogger.logInfo("DepositsTest", $"MovementCounter: {movementCounter} (expected {EXPECTED_MOVEMENTS})");

                        // Financial data validation - Deposited amount
                        var deposited = brokerAccountSnapshot.BrokerAccount.Value.Financial.Deposited;
                        bool depositedValid = deposited == EXPECTED_DEPOSITED;
                        results.Add($"Deposited: Expected ${EXPECTED_DEPOSITED:F2}, Got ${deposited:F2} - {(depositedValid ? "✅ PASS" : "❌ FAIL")}");
                        CoreLogger.logInfo("DepositsTest", $"Deposited: ${deposited:F2} (expected ${EXPECTED_DEPOSITED:F2})");

                        // Financial data validation - Withdrawn amount
                        var withdrawn = brokerAccountSnapshot.BrokerAccount.Value.Financial.Withdrawn;
                        bool withdrawnValid = withdrawn == EXPECTED_WITHDRAWN;
                        results.Add($"Withdrawn: Expected ${EXPECTED_WITHDRAWN:F2}, Got ${withdrawn:F2} - {(withdrawnValid ? "✅ PASS" : "❌ FAIL")}");
                        CoreLogger.logInfo("DepositsTest", $"Withdrawn: ${withdrawn:F2} (expected ${EXPECTED_WITHDRAWN:F2})");

                        // Financial data validation - Options Income (should be 0)
                        var optionsIncome = brokerAccountSnapshot.BrokerAccount.Value.Financial.OptionsIncome;
                        bool optionsIncomeValid = optionsIncome == EXPECTED_OPTIONS_INCOME;
                        results.Add($"OptionsIncome: Expected ${EXPECTED_OPTIONS_INCOME:F2}, Got ${optionsIncome:F2} - {(optionsIncomeValid ? "✅ PASS" : "❌ FAIL")}");
                        CoreLogger.logInfo("DepositsTest", $"OptionsIncome: ${optionsIncome:F2} (expected ${EXPECTED_OPTIONS_INCOME:F2})");

                        // Financial data validation - Realized Gains (should be 0)
                        var realizedGains = brokerAccountSnapshot.BrokerAccount.Value.Financial.RealizedGains;
                        bool realizedGainsValid = realizedGains == EXPECTED_REALIZED_GAINS;
                        results.Add($"RealizedGains: Expected ${EXPECTED_REALIZED_GAINS:F2}, Got ${realizedGains:F2} - {(realizedGainsValid ? "✅ PASS" : "❌ FAIL")}");
                        CoreLogger.logInfo("DepositsTest", $"RealizedGains: ${realizedGains:F2} (expected ${EXPECTED_REALIZED_GAINS:F2})");

                        // Financial data validation - Unrealized Gains (should be 0)
                        var unrealizedGains = brokerAccountSnapshot.BrokerAccount.Value.Financial.UnrealizedGains;
                        bool unrealizedGainsValid = unrealizedGains == EXPECTED_UNREALIZED_GAINS;
                        results.Add($"UnrealizedGains: Expected ${EXPECTED_UNREALIZED_GAINS:F2}, Got ${unrealizedGains:F2} - {(unrealizedGainsValid ? "✅ PASS" : "❌ FAIL")}");
                        CoreLogger.logInfo("DepositsTest", $"UnrealizedGains: ${unrealizedGains:F2} (expected ${EXPECTED_UNREALIZED_GAINS:F2})");

                        snapshotCountValid = snapshotCountValid && movementCounterValid && depositedValid &&
                                           withdrawnValid && optionsIncomeValid && realizedGainsValid && unrealizedGainsValid;
                    }
                    else
                    {
                        CoreLogger.logError("DepositsTest", "❌ BrokerAccount snapshot not found");
                        results.Add("❌ BrokerAccount snapshot not found");
                        snapshotCountValid = false;
                    }
                }
                catch (Exception snapValidationEx)
                {
                    CoreLogger.logError("DepositsTest", $"❌ Snapshot validation error: {snapValidationEx.Message}");
                    results.Add($"❌ Snapshot validation error: {snapValidationEx.Message}");
                    snapshotCountValid = false;
                }

                var success = importResult.Success &&
                             movementCountValid &&
                             snapshotCountValid &&
                             retrievedSnapshotsValid &&
                             importSignalsReceived;

                CoreLogger.logInfo("DepositsTest", $"Final result - Success: {success}");

                // Cleanup
                if (!string.IsNullOrEmpty(tempCsvPath) && File.Exists(tempCsvPath))
                {
                    CoreLogger.logDebug("DepositsTest", $"Cleaning up temp file: {tempCsvPath}");
                    File.Delete(tempCsvPath);
                }

                results.Add(success ? "\n✅ TEST PASSED" : "\n❌ TEST FAILED");
                CoreLogger.logInfo("DepositsTest", success ? "✅ TEST PASSED" : "❌ TEST FAILED");

                return (success, string.Join("\n", results), null);
            }
            catch (Exception ex)
            {
                CoreLogger.logError("DepositsTest", $"❌ UNHANDLED EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                CoreLogger.logError("DepositsTest", $"Stack trace: {ex.StackTrace}");
                results.Add($"❌ EXCEPTION: {ex.Message}");
                return (false, string.Join("\n", results), ex.ToString());
            }
            finally
            {
                CoreLogger.logDebug("DepositsTest", "Stopping reactive observations...");
                ReactiveTestVerifications.StopObserving();
                CoreLogger.logInfo("DepositsTest", "Test execution completed");
            }
        }

        /// <summary>
        /// Validate individual snapshots against expected cumulative values from CSV
        /// </summary>
        private bool ValidateIndividualSnapshots(FSharpList<CoreModels.OverviewSnapshot> snapshots, List<string> results)
        {
            const decimal TOLERANCE = 0.1m; // ±$0.10 tolerance for decimal precision
            CoreLogger.logInfo("DepositsTest", "=== Individual Snapshot Validation ===");
            results.Add("\n=== Individual Snapshot Validation (Date-by-Date) ===");

            // Expected snapshots in chronological order (oldest to newest)
            // CSV data is in reverse chronological order, so we reverse it here
            // Amounts are CUMULATIVE (running totals)
            var expectedSnapshots = new List<(DateOnly date, decimal deposited, decimal withdrawn)>
            {
                (new DateOnly(2024, 4, 22), 10.00m, 0.00m),           // First deposit: $10
                (new DateOnly(2024, 4, 23), 34.23m, 0.00m),           // Cumulative: $10 + $24.23 = $34.23
                (new DateOnly(2024, 4, 24), 878.79m, 0.00m),          // Cumulative: $34.23 + $844.56 = $878.79
                (new DateOnly(2024, 5, 1), 3678.40m, 0.00m),          // Cumulative: $878.79 + $2,799.61 = $3,678.40
                (new DateOnly(2024, 5, 28), 4428.40m, 0.00m),         // Cumulative: $3,678.40 + $750 = $4,428.40
                (new DateOnly(2024, 6, 24), 5328.40m, 0.00m),         // Cumulative: $4,428.40 + $900 = $5,328.40
                (new DateOnly(2024, 7, 31), 6238.40m, 0.00m),         // Cumulative: $5,328.40 + $910 = $6,238.40
                (new DateOnly(2024, 8, 27), 7138.40m, 0.00m),         // Cumulative: $6,238.40 + $900 = $7,138.40
                (new DateOnly(2024, 10, 3), 7188.40m, 0.00m),         // Cumulative: $7,138.40 + $50 = $7,188.40
                (new DateOnly(2024, 10, 9), 8038.40m, 0.00m),         // Cumulative: $7,188.40 + $850 = $8,038.40
                (new DateOnly(2025, 1, 31), 9238.40m, 0.00m),         // Cumulative: $8,038.40 + $1,200 = $9,238.40
                (new DateOnly(2025, 2, 21), 9288.40m, 0.00m),         // Cumulative: $9,238.40 + $50 = $9,288.40
                (new DateOnly(2025, 2, 28), 10488.40m, 0.00m),        // Cumulative: $9,288.40 + $1,200 = $10,488.40
                (new DateOnly(2025, 3, 28), 11688.40m, 0.00m),        // Cumulative: $10,488.40 + $1,200 = $11,688.40
                (new DateOnly(2025, 4, 30), 13388.40m, 0.00m),        // Cumulative: $11,688.40 + $1,700 = $13,388.40
                (new DateOnly(2025, 5, 30), 14888.40m, 0.00m),        // Cumulative: $13,388.40 + $1,500 = $14,888.40
                (new DateOnly(2025, 6, 30), 15888.40m, 0.00m),        // Cumulative: $14,888.40 + $1,000 = $15,888.40
                (new DateOnly(2025, 7, 14), 15888.40m, 25.00m),       // First withdrawal: Deposited stays, Withdrawn = $25
                (new DateOnly(2025, 8, 29), 18088.40m, 25.00m),       // Cumulative: $15,888.40 + $2,200 = $18,088.40
                (new DateOnly(2025, 9, 30), 19388.40m, 25.00m)        // Cumulative: $18,088.40 + $1,300 = $19,388.40
            };

            // Convert F# list to C# list, extract BrokerAccountSnapshots, and sort by date
            var brokerAccountSnapshots = ListModule.ToArray(snapshots)
                .Where(s => s.Type == CoreModels.OverviewSnapshotType.BrokerAccount && OptionModule.IsSome(s.BrokerAccount))
                .Select(s => s.BrokerAccount.Value)
                .OrderBy(s => s.Date)
                .ToList();

            // Log all snapshot dates and amounts for debugging
            CoreLogger.logDebug("DepositsTest", $"Total BrokerAccount snapshots: {brokerAccountSnapshots.Count}");
            foreach (var snap in brokerAccountSnapshots)
            {
                CoreLogger.logDebug("DepositsTest", $"  Snapshot: Date={snap.Date:yyyy-MM-dd}, Deposited={snap.Financial.Deposited:F2}, Withdrawn={snap.Financial.Withdrawn:F2}");
            }

            // Filter to get only snapshots that match the expected dates from CSV
            // Exclude any snapshots that might be "current day" snapshots or duplicates
            var dataSnapshots = brokerAccountSnapshots
                .Where(s => expectedSnapshots.Any(e => e.date == s.Date))
                .OrderBy(s => s.Date)
                .ToList();

            CoreLogger.logInfo("DepositsTest", $"Found {dataSnapshots.Count} data snapshots matching expected dates");

            // Log any extra snapshots (not matching expected dates)
            var extraSnapshots = brokerAccountSnapshots
                .Where(s => !expectedSnapshots.Any(e => e.date == s.Date))
                .ToList();

            if (extraSnapshots.Any())
            {
                CoreLogger.logWarning("DepositsTest", $"Found {extraSnapshots.Count} extra snapshot(s) not in expected dates:");
                foreach (var extra in extraSnapshots)
                {
                    CoreLogger.logWarning("DepositsTest", $"  Extra: Date={extra.Date:yyyy-MM-dd}, Deposited={extra.Financial.Deposited:F2}, Withdrawn={extra.Financial.Withdrawn:F2}");
                }
            }

            if (dataSnapshots.Count != expectedSnapshots.Count)
            {
                var msg = $"❌ Snapshot count mismatch: Expected {expectedSnapshots.Count}, Got {dataSnapshots.Count}";
                CoreLogger.logError("DepositsTest", msg);
                results.Add(msg);
                return false;
            }

            var allValid = true;
            var passedCount = 0;
            var failedCount = 0;

            for (int i = 0; i < expectedSnapshots.Count; i++)
            {
                var expected = expectedSnapshots[i];
                var actual = dataSnapshots[i];
                var financial = actual.Financial;

                var dateMatch = actual.Date == expected.date;
                var depositedMatch = Math.Abs(financial.Deposited - expected.deposited) <= TOLERANCE;
                var withdrawnMatch = Math.Abs(financial.Withdrawn - expected.withdrawn) <= TOLERANCE;

                var snapshotValid = dateMatch && depositedMatch && withdrawnMatch;

                if (snapshotValid)
                {
                    passedCount++;
                    var msg = $"✅ [{i + 1}/20] {expected.date:yyyy-MM-dd}: Deposited=${financial.Deposited:F2}, Withdrawn=${financial.Withdrawn:F2}";
                    CoreLogger.logInfo("DepositsTest", msg);
                    results.Add(msg);
                }
                else
                {
                    failedCount++;
                    allValid = false;

                    var issues = new List<string>();
                    if (!dateMatch)
                        issues.Add($"Date: Expected {expected.date:yyyy-MM-dd}, Got {actual.Date:yyyy-MM-dd}");
                    if (!depositedMatch)
                        issues.Add($"Deposited: Expected ${expected.deposited:F2}, Got ${financial.Deposited:F2} (Δ ${Math.Abs(financial.Deposited - expected.deposited):F2})");
                    if (!withdrawnMatch)
                        issues.Add($"Withdrawn: Expected ${expected.withdrawn:F2}, Got ${financial.Withdrawn:F2} (Δ ${Math.Abs(financial.Withdrawn - expected.withdrawn):F2})");

                    var msg = $"❌ [{i + 1}/20] {expected.date:yyyy-MM-dd}: {string.Join(" | ", issues)}";
                    CoreLogger.logError("DepositsTest", msg);
                    results.Add(msg);
                }
            }

            var summary = $"\nSnapshot Validation Summary: {passedCount} passed, {failedCount} failed out of {expectedSnapshots.Count} total";
            CoreLogger.logInfo("DepositsTest", summary);
            results.Add(summary);

            // Additional validation: Verify each BrokerAccountSnapshot has exactly 1 BrokerFinancialSnapshot
            // Since all transactions are in a single currency (USD), each snapshot should have only one financial snapshot
            CoreLogger.logInfo("DepositsTest", "\n=== Validating Financial Snapshots per Account Snapshot ===");
            results.Add("\n=== Financial Snapshots per Account Snapshot Validation ===");

            var financialSnapshotCountPassed = 0;
            var financialSnapshotCountFailed = 0;

            // We need to get the database broker account snapshot ID for each snapshot
            // Since the model doesn't expose it directly, we'll validate using the BrokerAccount.Financial list
            // which should contain exactly 1 main financial snapshot + potentially other currencies (but we expect only 1 total)
            for (int i = 0; i < dataSnapshots.Count; i++)
            {
                var snapshot = dataSnapshots[i];

                try
                {
                    // The snapshot.Financial is the main currency financial snapshot
                    // The snapshot.FinancialOtherCurrencies should be empty for single-currency accounts
                    var mainFinancialSnapshot = snapshot.Financial;
                    var otherCurrencySnapshots = snapshot.FinancialOtherCurrencies;

                    var totalFinancialSnapshots = 1 + otherCurrencySnapshots.Length; // Main + Others

                    // Expected: Exactly 1 financial snapshot per account snapshot (single currency - USD only)
                    const int EXPECTED_FINANCIAL_SNAPSHOTS = 1;
                    var isValid = totalFinancialSnapshots == EXPECTED_FINANCIAL_SNAPSHOTS && otherCurrencySnapshots.Length == 0;

                    if (isValid)
                    {
                        financialSnapshotCountPassed++;
                        var msg = $"✅ [{i + 1}/20] {snapshot.Date:yyyy-MM-dd}: {totalFinancialSnapshots} financial snapshot(s) [Main: 1, Other currencies: 0] - PASS";
                        CoreLogger.logInfo("DepositsTest", msg);
                        results.Add(msg);

                        // Validate the currency is USD
                        if (mainFinancialSnapshot.Currency.Code != "USD")
                        {
                            var currencyWarning = $"⚠️  [{i + 1}/20] Warning: Expected USD currency, got {mainFinancialSnapshot.Currency.Code}";
                            CoreLogger.logWarning("DepositsTest", currencyWarning);
                            results.Add(currencyWarning);
                        }
                    }
                    else
                    {
                        financialSnapshotCountFailed++;
                        allValid = false;
                        var msg = $"❌ [{i + 1}/20] {snapshot.Date:yyyy-MM-dd}: Expected {EXPECTED_FINANCIAL_SNAPSHOTS} financial snapshot(s), Got {totalFinancialSnapshots} [Main: 1, Other currencies: {otherCurrencySnapshots.Length}] - FAIL";
                        CoreLogger.logError("DepositsTest", msg);
                        results.Add(msg);
                    }
                }
                catch (Exception ex)
                {
                    financialSnapshotCountFailed++;
                    allValid = false;
                    var msg = $"❌ [{i + 1}/20] {snapshot.Date:yyyy-MM-dd}: Exception validating financial snapshots - {ex.Message}";
                    CoreLogger.logError("DepositsTest", msg);
                    results.Add(msg);
                }
            }

            var financialSnapshotSummary = $"\nFinancial Snapshot Count Validation: {financialSnapshotCountPassed} passed, {financialSnapshotCountFailed} failed out of {dataSnapshots.Count} total";
            CoreLogger.logInfo("DepositsTest", financialSnapshotSummary);
            results.Add(financialSnapshotSummary);

            return allValid;
        }

        /// <summary>
        /// Extract embedded CSV test file for deposits/withdrawals
        /// </summary>
        private async Task<string> ExtractTestCsvFile()
        {
            CoreLogger.logDebug("DepositsTest", "ExtractTestCsvFile called");

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Core.Platform.MauiTester.Resources.TestData.TastytradeDeposits.csv";

            CoreLogger.logDebug("DepositsTest", $"Looking for resource: {resourceName}");

            var tempPath = Path.Combine(Path.GetTempPath(), $"tastytrade_deposits_{Guid.NewGuid()}.csv");
            CoreLogger.logDebug("DepositsTest", $"Temp path: {tempPath}");

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                CoreLogger.logError("DepositsTest", "❌ Resource not found!");

                // List all available resources for debugging
                var allResources = assembly.GetManifestResourceNames();
                CoreLogger.logError("DepositsTest", $"Available resources ({allResources.Length}):");
                foreach (var res in allResources)
                {
                    CoreLogger.logError("DepositsTest", $"  - {res}");
                }

                throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
            }

            CoreLogger.logDebug("DepositsTest", $"✅ Resource stream found, size: {stream.Length} bytes");

            using var fileStream = File.Create(tempPath);
            await stream.CopyToAsync(fileStream);

            CoreLogger.logDebug("DepositsTest", "✅ File created successfully");

            return tempPath;
        }
    }
}
