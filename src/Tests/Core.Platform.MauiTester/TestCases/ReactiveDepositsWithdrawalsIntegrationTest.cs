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
                System.Diagnostics.Debug.WriteLine("[DepositsTest] Test started");
                Console.WriteLine("[DepositsTest] Test started");

                // Extract embedded CSV file
                System.Diagnostics.Debug.WriteLine("[DepositsTest] Extracting CSV file...");
                Console.WriteLine("[DepositsTest] Extracting CSV file...");
                var tempCsvPath = await ExtractTestCsvFile();
                if (!File.Exists(tempCsvPath))
                {
                    System.Diagnostics.Debug.WriteLine("[DepositsTest] ❌ CSV file extraction failed");
                    Console.WriteLine("[DepositsTest] ❌ CSV file extraction failed");
                    results.Add("❌ CSV file extraction failed");
                    return (false, string.Join("\n", results), "CSV extraction failed");
                }
                System.Diagnostics.Debug.WriteLine($"[DepositsTest] ✅ CSV extracted to: {tempCsvPath}");
                Console.WriteLine($"[DepositsTest] ✅ CSV extracted to: {tempCsvPath}");

                // Use the existing Tastytrade broker from test context
                var tastytradeId = _context.TastytradeId;
                var accountNumber = $"DEPOSITS-TEST-{DateTime.Now:yyyyMMdd-HHmmss}";
                System.Diagnostics.Debug.WriteLine($"[DepositsTest] Creating broker account: {accountNumber}");
                Console.WriteLine($"[DepositsTest] Creating broker account: {accountNumber}");

                // Add timeout for account creation to prevent hanging
                using var accountCreationCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                try
                {
                    System.Diagnostics.Debug.WriteLine("[DepositsTest] Calling SaveBrokerAccount...");
                    Console.WriteLine("[DepositsTest] Calling SaveBrokerAccount...");
                    await Creator.SaveBrokerAccount(tastytradeId, accountNumber).WaitAsync(accountCreationCts.Token);
                    System.Diagnostics.Debug.WriteLine("[DepositsTest] ✅ SaveBrokerAccount completed");
                    Console.WriteLine("[DepositsTest] ✅ SaveBrokerAccount completed");
                }
                catch (OperationCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine("[DepositsTest] ❌ Account creation timed out");
                    Console.WriteLine("[DepositsTest] ❌ Account creation timed out");
                    results.Add("❌ Account creation timed out");
                    return (false, string.Join("\n", results), "Account creation timeout");
                }

                // Find the created broker account
                System.Diagnostics.Debug.WriteLine("[DepositsTest] Looking for created account...");
                Console.WriteLine("[DepositsTest] Looking for created account...");
                var testAccount = Collections.Accounts.Items
                    .Where(a => a.Type == CoreModels.AccountType.BrokerAccount)
                    .Where(a => OptionModule.IsSome(a.Broker) && a.Broker.Value.AccountNumber == accountNumber)
                    .FirstOrDefault();

                if (testAccount == null || testAccount.Type == CoreModels.AccountType.EmptyAccount)
                {
                    System.Diagnostics.Debug.WriteLine($"[DepositsTest] ❌ Failed to find account. Total accounts: {Collections.Accounts.Items.Count}");
                    Console.WriteLine($"[DepositsTest] ❌ Failed to find account. Total accounts: {Collections.Accounts.Items.Count}");
                    results.Add("❌ Failed to create test broker account");
                    return (false, string.Join("\n", results), null);
                }

                var testBrokerAccountId = testAccount.Broker.Value.Id;
                System.Diagnostics.Debug.WriteLine($"[DepositsTest] ✅ Found account with ID: {testBrokerAccountId}");
                Console.WriteLine($"[DepositsTest] ✅ Found account with ID: {testBrokerAccountId}");

                // Set up signal monitoring for import
                System.Diagnostics.Debug.WriteLine("[DepositsTest] Setting up signal monitoring...");
                Console.WriteLine("[DepositsTest] Setting up signal monitoring...");
                ReactiveTestVerifications.ExpectSignals("Movements_Updated", "Snapshots_Updated");

                // Execute import
                System.Diagnostics.Debug.WriteLine($"[DepositsTest] Starting import from: {tempCsvPath}");
                Console.WriteLine($"[DepositsTest] Starting import from: {tempCsvPath}");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                ImportResult importResult;
                try
                {
                    var importTask = ImportManager.importFile(tastytradeId, testBrokerAccountId, tempCsvPath);
                    importResult = await importTask.WaitAsync(cts.Token);
                    System.Diagnostics.Debug.WriteLine($"[DepositsTest] ✅ Import completed. Success: {importResult.Success}");
                    Console.WriteLine($"[DepositsTest] ✅ Import completed. Success: {importResult.Success}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DepositsTest] ❌ Import operation exception: {ex.Message}");
                    Console.WriteLine($"[DepositsTest] ❌ Import operation exception: {ex.Message}");
                    results.Add("❌ Import operation timed out");
                    return (false, string.Join("\n", results), "Import timeout");
                }

                // Wait for import signals
                System.Diagnostics.Debug.WriteLine("[DepositsTest] Waiting for reactive signals...");
                Console.WriteLine("[DepositsTest] Waiting for reactive signals...");
                var importSignalsReceived = await ReactiveTestVerifications.WaitForAllSignalsAsync(TimeSpan.FromSeconds(15));
                if (!importSignalsReceived)
                {
                    var (expected, received, missing) = ReactiveTestVerifications.GetSignalStatus();
                    System.Diagnostics.Debug.WriteLine($"[DepositsTest] ⚠️ Missing signals: [{string.Join(", ", missing)}]");
                    System.Diagnostics.Debug.WriteLine($"[DepositsTest] Expected: [{string.Join(", ", expected)}]");
                    System.Diagnostics.Debug.WriteLine($"[DepositsTest] Received: [{string.Join(", ", received)}]");
                    Console.WriteLine($"[DepositsTest] ⚠️ Missing signals: [{string.Join(", ", missing)}]");
                    Console.WriteLine($"[DepositsTest] Expected: [{string.Join(", ", expected)}]");
                    Console.WriteLine($"[DepositsTest] Received: [{string.Join(", ", received)}]");
                    results.Add($"⚠️ Missing signals: [{string.Join(", ", missing)}]");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DepositsTest] ✅ All signals received");
                    Console.WriteLine("[DepositsTest] ✅ All signals received");
                }

                // Validation
                System.Diagnostics.Debug.WriteLine("[DepositsTest] Starting validation...");
                Console.WriteLine("[DepositsTest] Starting validation...");
                results.Add("=== Validation Results ===");

                var movementCount = Collections.Movements.Items.Count;
                var snapshotCount = Collections.Snapshots.Items.Count;

                System.Diagnostics.Debug.WriteLine($"[DepositsTest] Movements count: {movementCount}");
                System.Diagnostics.Debug.WriteLine($"[DepositsTest] Snapshots count: {snapshotCount}");
                Console.WriteLine($"[DepositsTest] Movements count: {movementCount}");
                Console.WriteLine($"[DepositsTest] Snapshots count: {snapshotCount}");

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
                    System.Diagnostics.Debug.WriteLine("[DepositsTest] Validating broker account snapshot...");
                    Console.WriteLine("[DepositsTest] Validating broker account snapshot...");

                    var brokerAccountSnapshot = Collections.Snapshots.Items
                        .FirstOrDefault(s => s.Type == Binnaculum.Core.Models.OverviewSnapshotType.BrokerAccount);

                    if (brokerAccountSnapshot?.BrokerAccount?.Value?.Financial != null)
                    {
                        System.Diagnostics.Debug.WriteLine("[DepositsTest] ✅ Found broker account snapshot");
                        Console.WriteLine("[DepositsTest] ✅ Found broker account snapshot");

                        var movementCounter = brokerAccountSnapshot.BrokerAccount.Value.Financial.MovementCounter;
                        bool movementCounterValid = movementCounter == EXPECTED_MOVEMENTS;
                        results.Add($"Database MovementCounter: Expected {EXPECTED_MOVEMENTS}, Got {movementCounter} - {(movementCounterValid ? "✅ PASS" : "❌ FAIL")}");
                        System.Diagnostics.Debug.WriteLine($"[DepositsTest] MovementCounter: {movementCounter} (expected {EXPECTED_MOVEMENTS})");
                        Console.WriteLine($"[DepositsTest] MovementCounter: {movementCounter} (expected {EXPECTED_MOVEMENTS})");

                        // Financial data validation - Deposited amount
                        var deposited = brokerAccountSnapshot.BrokerAccount.Value.Financial.Deposited;
                        bool depositedValid = deposited == EXPECTED_DEPOSITED;
                        results.Add($"Deposited: Expected ${EXPECTED_DEPOSITED:F2}, Got ${deposited:F2} - {(depositedValid ? "✅ PASS" : "❌ FAIL")}");
                        System.Diagnostics.Debug.WriteLine($"[DepositsTest] Deposited: ${deposited:F2} (expected ${EXPECTED_DEPOSITED:F2})");
                        Console.WriteLine($"[DepositsTest] Deposited: ${deposited:F2} (expected ${EXPECTED_DEPOSITED:F2})");

                        // Financial data validation - Withdrawn amount
                        var withdrawn = brokerAccountSnapshot.BrokerAccount.Value.Financial.Withdrawn;
                        bool withdrawnValid = withdrawn == EXPECTED_WITHDRAWN;
                        results.Add($"Withdrawn: Expected ${EXPECTED_WITHDRAWN:F2}, Got ${withdrawn:F2} - {(withdrawnValid ? "✅ PASS" : "❌ FAIL")}");
                        System.Diagnostics.Debug.WriteLine($"[DepositsTest] Withdrawn: ${withdrawn:F2} (expected ${EXPECTED_WITHDRAWN:F2})");
                        Console.WriteLine($"[DepositsTest] Withdrawn: ${withdrawn:F2} (expected ${EXPECTED_WITHDRAWN:F2})");

                        // Financial data validation - Options Income (should be 0)
                        var optionsIncome = brokerAccountSnapshot.BrokerAccount.Value.Financial.OptionsIncome;
                        bool optionsIncomeValid = optionsIncome == EXPECTED_OPTIONS_INCOME;
                        results.Add($"OptionsIncome: Expected ${EXPECTED_OPTIONS_INCOME:F2}, Got ${optionsIncome:F2} - {(optionsIncomeValid ? "✅ PASS" : "❌ FAIL")}");
                        System.Diagnostics.Debug.WriteLine($"[DepositsTest] OptionsIncome: ${optionsIncome:F2} (expected ${EXPECTED_OPTIONS_INCOME:F2})");
                        Console.WriteLine($"[DepositsTest] OptionsIncome: ${optionsIncome:F2} (expected ${EXPECTED_OPTIONS_INCOME:F2})");

                        // Financial data validation - Realized Gains (should be 0)
                        var realizedGains = brokerAccountSnapshot.BrokerAccount.Value.Financial.RealizedGains;
                        bool realizedGainsValid = realizedGains == EXPECTED_REALIZED_GAINS;
                        results.Add($"RealizedGains: Expected ${EXPECTED_REALIZED_GAINS:F2}, Got ${realizedGains:F2} - {(realizedGainsValid ? "✅ PASS" : "❌ FAIL")}");
                        System.Diagnostics.Debug.WriteLine($"[DepositsTest] RealizedGains: ${realizedGains:F2} (expected ${EXPECTED_REALIZED_GAINS:F2})");
                        Console.WriteLine($"[DepositsTest] RealizedGains: ${realizedGains:F2} (expected ${EXPECTED_REALIZED_GAINS:F2})");

                        // Financial data validation - Unrealized Gains (should be 0)
                        var unrealizedGains = brokerAccountSnapshot.BrokerAccount.Value.Financial.UnrealizedGains;
                        bool unrealizedGainsValid = unrealizedGains == EXPECTED_UNREALIZED_GAINS;
                        results.Add($"UnrealizedGains: Expected ${EXPECTED_UNREALIZED_GAINS:F2}, Got ${unrealizedGains:F2} - {(unrealizedGainsValid ? "✅ PASS" : "❌ FAIL")}");
                        System.Diagnostics.Debug.WriteLine($"[DepositsTest] UnrealizedGains: ${unrealizedGains:F2} (expected ${EXPECTED_UNREALIZED_GAINS:F2})");
                        Console.WriteLine($"[DepositsTest] UnrealizedGains: ${unrealizedGains:F2} (expected ${EXPECTED_UNREALIZED_GAINS:F2})");

                        snapshotCountValid = snapshotCountValid && movementCounterValid && depositedValid &&
                                           withdrawnValid && optionsIncomeValid && realizedGainsValid && unrealizedGainsValid;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[DepositsTest] ❌ BrokerAccount snapshot not found");
                        Console.WriteLine("[DepositsTest] ❌ BrokerAccount snapshot not found");
                        results.Add("❌ BrokerAccount snapshot not found");
                        snapshotCountValid = false;
                    }
                }
                catch (Exception snapValidationEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[DepositsTest] ❌ Snapshot validation error: {snapValidationEx.Message}");
                    Console.WriteLine($"[DepositsTest] ❌ Snapshot validation error: {snapValidationEx.Message}");
                    results.Add($"❌ Snapshot validation error: {snapValidationEx.Message}");
                    snapshotCountValid = false;
                }

                var success = importResult.Success &&
                             movementCountValid &&
                             snapshotCountValid &&
                             importSignalsReceived;

                System.Diagnostics.Debug.WriteLine($"[DepositsTest] Final result - Success: {success}");
                Console.WriteLine($"[DepositsTest] Final result - Success: {success}");

                // Cleanup
                if (!string.IsNullOrEmpty(tempCsvPath) && File.Exists(tempCsvPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[DepositsTest] Cleaning up temp file: {tempCsvPath}");
                    Console.WriteLine($"[DepositsTest] Cleaning up temp file: {tempCsvPath}");
                    File.Delete(tempCsvPath);
                }

                results.Add(success ? "\n✅ TEST PASSED" : "\n❌ TEST FAILED");
                System.Diagnostics.Debug.WriteLine(success ? "[DepositsTest] ✅ TEST PASSED" : "[DepositsTest] ❌ TEST FAILED");
                Console.WriteLine(success ? "[DepositsTest] ✅ TEST PASSED" : "[DepositsTest] ❌ TEST FAILED");

                return (success, string.Join("\n", results), null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DepositsTest] ❌ UNHANDLED EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DepositsTest] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[DepositsTest] ❌ UNHANDLED EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                Console.WriteLine($"[DepositsTest] Stack trace: {ex.StackTrace}");
                results.Add($"❌ EXCEPTION: {ex.Message}");
                return (false, string.Join("\n", results), ex.ToString());
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine("[DepositsTest] Stopping reactive observations...");
                Console.WriteLine("[DepositsTest] Stopping reactive observations...");
                ReactiveTestVerifications.StopObserving();
                System.Diagnostics.Debug.WriteLine("[DepositsTest] Test execution completed");
                Console.WriteLine("[DepositsTest] Test execution completed");
            }
        }

        /// <summary>
        /// Extract embedded CSV test file for deposits/withdrawals
        /// </summary>
        private async Task<string> ExtractTestCsvFile()
        {
            System.Diagnostics.Debug.WriteLine("[DepositsTest] ExtractTestCsvFile called");
            Console.WriteLine("[DepositsTest] ExtractTestCsvFile called");

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Core.Platform.MauiTester.Resources.TestData.TastytradeDeposits.csv";

            System.Diagnostics.Debug.WriteLine($"[DepositsTest] Looking for resource: {resourceName}");
            Console.WriteLine($"[DepositsTest] Looking for resource: {resourceName}");

            var tempPath = Path.Combine(Path.GetTempPath(), $"tastytrade_deposits_{Guid.NewGuid()}.csv");
            System.Diagnostics.Debug.WriteLine($"[DepositsTest] Temp path: {tempPath}");
            Console.WriteLine($"[DepositsTest] Temp path: {tempPath}");

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                System.Diagnostics.Debug.WriteLine($"[DepositsTest] ❌ Resource not found!");
                Console.WriteLine($"[DepositsTest] ❌ Resource not found!");

                // List all available resources for debugging
                var allResources = assembly.GetManifestResourceNames();
                System.Diagnostics.Debug.WriteLine($"[DepositsTest] Available resources ({allResources.Length}):");
                Console.WriteLine($"[DepositsTest] Available resources ({allResources.Length}):");
                foreach (var res in allResources)
                {
                    System.Diagnostics.Debug.WriteLine($"[DepositsTest]   - {res}");
                    Console.WriteLine($"[DepositsTest]   - {res}");
                }

                throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
            }

            System.Diagnostics.Debug.WriteLine($"[DepositsTest] ✅ Resource stream found, size: {stream.Length} bytes");
            Console.WriteLine($"[DepositsTest] ✅ Resource stream found, size: {stream.Length} bytes");

            using var fileStream = File.Create(tempPath);
            await stream.CopyToAsync(fileStream);

            System.Diagnostics.Debug.WriteLine($"[DepositsTest] ✅ File created successfully");
            Console.WriteLine($"[DepositsTest] ✅ File created successfully");

            return tempPath;
        }
    }
}
