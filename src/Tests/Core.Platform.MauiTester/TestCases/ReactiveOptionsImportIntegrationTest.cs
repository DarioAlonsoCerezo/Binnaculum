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
                System.Diagnostics.Debug.WriteLine("[ReactiveTest] Test started");
                Console.WriteLine("[ReactiveTest] Test started");

                // CRITICAL: Start observing reactive streams BEFORE any operations
                System.Diagnostics.Debug.WriteLine("[ReactiveTest] Starting reactive stream observation...");
                Console.WriteLine("[ReactiveTest] Starting reactive stream observation...");
                ReactiveTestVerifications.StartObserving();
                System.Diagnostics.Debug.WriteLine("[ReactiveTest] ✅ Reactive stream observation started");
                Console.WriteLine("[ReactiveTest] ✅ Reactive stream observation started");

                // Extract embedded CSV file
                System.Diagnostics.Debug.WriteLine("[ReactiveTest] Extracting CSV file...");
                Console.WriteLine("[ReactiveTest] Extracting CSV file...");
                Console.WriteLine("[ReactiveTest] Extracting CSV file...");
                var tempCsvPath = await ExtractTestCsvFile();
                if (!File.Exists(tempCsvPath))
                {
                    System.Diagnostics.Debug.WriteLine("[ReactiveTest] ❌ CSV file extraction failed");
                    Console.WriteLine("[ReactiveTest] ❌ CSV file extraction failed");
                    results.Add("❌ CSV file extraction failed");
                    return (false, string.Join("\n", results), "CSV extraction failed");
                }
                System.Diagnostics.Debug.WriteLine($"[ReactiveTest] ✅ CSV extracted to: {tempCsvPath}");
                Console.WriteLine($"[ReactiveTest] ✅ CSV extracted to: {tempCsvPath}");

                // Use the existing Tastytrade broker from test context
                var tastytradeId = _context.TastytradeId;
                var accountNumber = $"REACTIVE-TEST-{DateTime.Now:yyyyMMdd-HHmmss}";
                System.Diagnostics.Debug.WriteLine($"[ReactiveTest] Creating broker account: {accountNumber}");
                Console.WriteLine($"[ReactiveTest] Creating broker account: {accountNumber}");

                // Add timeout for account creation to prevent hanging
                using var accountCreationCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                try
                {
                    System.Diagnostics.Debug.WriteLine("[ReactiveTest] Calling SaveBrokerAccount...");
                    Console.WriteLine("[ReactiveTest] Calling SaveBrokerAccount...");
                    await Creator.SaveBrokerAccount(tastytradeId, accountNumber).WaitAsync(accountCreationCts.Token);
                    System.Diagnostics.Debug.WriteLine("[ReactiveTest] ✅ SaveBrokerAccount completed");
                    Console.WriteLine("[ReactiveTest] ✅ SaveBrokerAccount completed");
                }
                catch (OperationCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine("[ReactiveTest] ❌ Account creation timed out");
                    Console.WriteLine("[ReactiveTest] ❌ Account creation timed out");
                    results.Add("❌ Account creation timed out");
                    return (false, string.Join("\n", results), "Account creation timeout");
                }

                // Find the created broker account
                System.Diagnostics.Debug.WriteLine("[ReactiveTest] Looking for created account...");
                Console.WriteLine("[ReactiveTest] Looking for created account...");
                var testAccount = Collections.Accounts.Items
                    .Where(a => a.Type == CoreModels.AccountType.BrokerAccount)
                    .Where(a => OptionModule.IsSome(a.Broker) && a.Broker.Value.AccountNumber == accountNumber)
                    .FirstOrDefault();

                if (testAccount == null || testAccount.Type == CoreModels.AccountType.EmptyAccount)
                {
                    System.Diagnostics.Debug.WriteLine($"[ReactiveTest] ❌ Failed to find account. Total accounts: {Collections.Accounts.Items.Count}");
                    Console.WriteLine($"[ReactiveTest] ❌ Failed to find account. Total accounts: {Collections.Accounts.Items.Count}");
                    results.Add("❌ Failed to create test broker account");
                    return (false, string.Join("\n", results), null);
                }

                var testBrokerAccountId = testAccount.Broker.Value.Id;
                System.Diagnostics.Debug.WriteLine($"[ReactiveTest] ✅ Found account with ID: {testBrokerAccountId}");
                Console.WriteLine($"[ReactiveTest] ✅ Found account with ID: {testBrokerAccountId}");

                // Set up signal monitoring BEFORE import to avoid race conditions
                // CRITICAL: Must call ExpectSignals() BEFORE the import operation starts!
                System.Diagnostics.Debug.WriteLine("[ReactiveTest] Setting up signal monitoring BEFORE import...");
                Console.WriteLine("[ReactiveTest] Setting up signal monitoring BEFORE import...");
                // Always expect these signals from successful import
                ReactiveTestVerifications.ExpectSignals("Movements_Updated", "Tickers_Updated", "Snapshots_Updated");

                // Execute import
                System.Diagnostics.Debug.WriteLine($"[ReactiveTest] Starting import from: {tempCsvPath}");
                Console.WriteLine($"[ReactiveTest] Starting import from: {tempCsvPath}");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                ImportResult importResult;
                try
                {
                    var importTask = ImportManager.importFile(tastytradeId, testBrokerAccountId, tempCsvPath);
                    importResult = await importTask.WaitAsync(cts.Token);
                    System.Diagnostics.Debug.WriteLine($"[ReactiveTest] ✅ Import completed. Success: {importResult.Success}");
                    Console.WriteLine($"[ReactiveTest] ✅ Import completed. Success: {importResult.Success}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ReactiveTest] ❌ Import operation exception: {ex.Message}");
                    Console.WriteLine($"[ReactiveTest] ❌ Import operation exception: {ex.Message}");
                    results.Add("❌ Import operation timed out");
                    return (false, string.Join("\n", results), "Import timeout");
                }

                // Wait for signals that should have been emitted by import
                bool importSignalsReceived = true;
                if (importResult.Success)
                {
                    System.Diagnostics.Debug.WriteLine("[ReactiveTest] Import succeeded, waiting for reactive signals...");
                    Console.WriteLine("[ReactiveTest] Import succeeded, waiting for reactive signals...");

                    System.Diagnostics.Debug.WriteLine("[ReactiveTest] Waiting for reactive signals...");
                    Console.WriteLine("[ReactiveTest] Waiting for reactive signals...");
                    importSignalsReceived = await ReactiveTestVerifications.WaitForAllSignalsAsync(TimeSpan.FromSeconds(15));
                    if (!importSignalsReceived)
                    {
                        var (expected, received, missing) = ReactiveTestVerifications.GetSignalStatus();
                        System.Diagnostics.Debug.WriteLine($"[ReactiveTest] ⚠️ Missing signals: [{string.Join(", ", missing)}]");
                        System.Diagnostics.Debug.WriteLine($"[ReactiveTest] Expected: [{string.Join(", ", expected)}]");
                        System.Diagnostics.Debug.WriteLine($"[ReactiveTest] Received: [{string.Join(", ", received)}]");
                        Console.WriteLine($"[ReactiveTest] ⚠️ Missing signals: [{string.Join(", ", missing)}]");
                        Console.WriteLine($"[ReactiveTest] Expected: [{string.Join(", ", expected)}]");
                        Console.WriteLine($"[ReactiveTest] Received: [{string.Join(", ", received)}]");
                        results.Add($"⚠️ Missing signals: [{string.Join(", ", missing)}]");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[ReactiveTest] ✅ All signals received");
                        Console.WriteLine("[ReactiveTest] ✅ All signals received");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[ReactiveTest] Import failed - signals may not have been emitted");
                    Console.WriteLine("[ReactiveTest] Import failed - signals may not have been emitted");
                    importSignalsReceived = false;
                }

                // Validation
                System.Diagnostics.Debug.WriteLine("[ReactiveTest] Starting validation...");
                Console.WriteLine("[ReactiveTest] Starting validation...");
                results.Add("=== Validation Results ===");

                var movementCount = Collections.Movements.Items.Count;
                var tickerCount = Collections.Tickers.Items.Count;
                var snapshotCount = Collections.Snapshots.Items.Count;

                System.Diagnostics.Debug.WriteLine($"[ReactiveTest] Movements count: {movementCount}");
                System.Diagnostics.Debug.WriteLine($"[ReactiveTest] Tickers count: {tickerCount}");
                System.Diagnostics.Debug.WriteLine($"[ReactiveTest] Snapshots count: {snapshotCount}");
                Console.WriteLine($"[ReactiveTest] Movements count: {movementCount}");
                Console.WriteLine($"[ReactiveTest] Tickers count: {tickerCount}");
                Console.WriteLine($"[ReactiveTest] Snapshots count: {snapshotCount}");

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
                const decimal EXPECTED_OPTIONS_INCOME = 54.37m;

                // Realized Gains: Net profit/loss from CLOSED positions (round-trip calculations)
                // See docs/options-realized-unrealized-gains-calculation.md for detailed breakdown
                // SOFI 7.00 PUT (closed portion): $33.86 - $17.13 = $16.73
                // MPW 4.50 PUT: $17.86 - $8.13 = $9.73
                // MPW 4.00 PUT: $0.86 - (-$5.13) = $5.99
                // PLTR 21.00 PUT: $4.86 - (-$12.13) = $16.99
                // PLTR 21.50 PUT: $17.86 - $8.13 = $8.73
                // Total Realized: $16.73 + $9.73 + $5.99 + $16.99 + $8.73 = $58.17
                const decimal EXPECTED_REALIZED_GAINS = 58.17m;

                // Unrealized Gains: Net premium from OPEN positions (not yet closed)
                // SOFI 7.00 PUT (re-opened): $15.86
                // SOFI 6.50 PUT: $14.86
                // Total Unrealized: $15.86 + $14.86 = $30.72
                const decimal EXPECTED_UNREALIZED_GAINS = 30.72m;

                // Validate exact Collections.Movements count (option trades + money movements)
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
                    System.Diagnostics.Debug.WriteLine("[ReactiveTest] Validating broker account snapshot...");
                    Console.WriteLine("[ReactiveTest] Validating broker account snapshot...");
                    // Get the broker account snapshot to validate movement counter matches expected database count
                    var brokerAccountSnapshot = Collections.Snapshots.Items
                        .FirstOrDefault(s => s.Type == Binnaculum.Core.Models.OverviewSnapshotType.BrokerAccount);

                    if (brokerAccountSnapshot?.BrokerAccount?.Value?.Financial != null)
                    {
                        System.Diagnostics.Debug.WriteLine("[ReactiveTest] ✅ Found broker account snapshot");
                        Console.WriteLine("[ReactiveTest] ✅ Found broker account snapshot");
                        var movementCounter = brokerAccountSnapshot.BrokerAccount.Value.Financial.MovementCounter;
                        bool movementCounterValid = movementCounter == EXPECTED_DATABASE_MOVEMENTS;
                        results.Add($"Database MovementCounter: Expected {EXPECTED_DATABASE_MOVEMENTS}, Got {movementCounter} - {(movementCounterValid ? "✅ PASS" : "❌ FAIL")}");
                        System.Diagnostics.Debug.WriteLine($"[ReactiveTest] MovementCounter: {movementCounter} (expected {EXPECTED_DATABASE_MOVEMENTS})");
                        Console.WriteLine($"[ReactiveTest] MovementCounter: {movementCounter} (expected {EXPECTED_DATABASE_MOVEMENTS})");

                        // Financial data validation - Deposited amount
                        var deposited = brokerAccountSnapshot.BrokerAccount.Value.Financial.Deposited;
                        bool depositedValid = deposited == EXPECTED_DEPOSITED;
                        results.Add($"Deposited: Expected ${EXPECTED_DEPOSITED:F2}, Got ${deposited:F2} - {(depositedValid ? "✅ PASS" : "❌ FAIL")}");
                        System.Diagnostics.Debug.WriteLine($"[ReactiveTest] Deposited: ${deposited:F2} (expected ${EXPECTED_DEPOSITED:F2})");
                        Console.WriteLine($"[ReactiveTest] Deposited: ${deposited:F2} (expected ${EXPECTED_DEPOSITED:F2})");

                        // Financial data validation - Options Income
                        var optionsIncome = brokerAccountSnapshot.BrokerAccount.Value.Financial.OptionsIncome;
                        bool optionsIncomeValid = optionsIncome == EXPECTED_OPTIONS_INCOME;
                        results.Add($"OptionsIncome: Expected ${EXPECTED_OPTIONS_INCOME:F2}, Got ${optionsIncome:F2} - {(optionsIncomeValid ? "✅ PASS" : "❌ FAIL")}");
                        System.Diagnostics.Debug.WriteLine($"[ReactiveTest] OptionsIncome: ${optionsIncome:F2} (expected ${EXPECTED_OPTIONS_INCOME:F2})");
                        Console.WriteLine($"[ReactiveTest] OptionsIncome: ${optionsIncome:F2} (expected ${EXPECTED_OPTIONS_INCOME:F2})");

                        // Financial data validation - Realized Gains
                        var realizedGains = brokerAccountSnapshot.BrokerAccount.Value.Financial.RealizedGains;
                        bool realizedGainsValid = Math.Abs(realizedGains - EXPECTED_REALIZED_GAINS) < 0.01m;
                        results.Add($"RealizedGains: Expected ${EXPECTED_REALIZED_GAINS:F2}, Got ${realizedGains:F2} - {(realizedGainsValid ? "✅ PASS" : "❌ FAIL")}");
                        System.Diagnostics.Debug.WriteLine($"[ReactiveTest] RealizedGains: ${realizedGains:F2} (expected ${EXPECTED_REALIZED_GAINS:F2})");
                        Console.WriteLine($"[ReactiveTest] RealizedGains: ${realizedGains:F2} (expected ${EXPECTED_REALIZED_GAINS:F2})");

                        // Financial data validation - Unrealized Gains
                        var unrealizedGains = brokerAccountSnapshot.BrokerAccount.Value.Financial.UnrealizedGains;
                        bool unrealizedGainsValid = Math.Abs(unrealizedGains - EXPECTED_UNREALIZED_GAINS) < 0.01m;
                        results.Add($"UnrealizedGains: Expected ${EXPECTED_UNREALIZED_GAINS:F2}, Got ${unrealizedGains:F2} - {(unrealizedGainsValid ? "✅ PASS" : "❌ FAIL")}");
                        System.Diagnostics.Debug.WriteLine($"[ReactiveTest] UnrealizedGains: ${unrealizedGains:F2} (expected ${EXPECTED_UNREALIZED_GAINS:F2})");
                        Console.WriteLine($"[ReactiveTest] UnrealizedGains: ${unrealizedGains:F2} (expected ${EXPECTED_UNREALIZED_GAINS:F2})");

                        snapshotCountValid = snapshotCountValid && movementCounterValid && depositedValid && optionsIncomeValid && realizedGainsValid && unrealizedGainsValid;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[ReactiveTest] ❌ BrokerAccount snapshot not found");
                        Console.WriteLine("[ReactiveTest] ❌ BrokerAccount snapshot not found");
                        results.Add("❌ BrokerAccount snapshot not found");
                        snapshotCountValid = false;
                    }
                }
                catch (Exception snapValidationEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[ReactiveTest] ❌ Snapshot validation error: {snapValidationEx.Message}");
                    Console.WriteLine($"[ReactiveTest] ❌ Snapshot validation error: {snapValidationEx.Message}");
                    results.Add($"❌ Snapshot validation error: {snapValidationEx.Message}");
                    snapshotCountValid = false;
                }
                var success = (importResult.Success ?
                             (movementCountValid && tickerCountValid) :
                             true) &&  // For failed imports, we only care about structure
                             snapshotCountValid &&
                             importSignalsReceived &&
                             importResult.Success;  // Import must ultimately succeed for test to pass

                System.Diagnostics.Debug.WriteLine($"[ReactiveTest] Final result - Success: {success}");
                Console.WriteLine($"[ReactiveTest] Final result - Success: {success}");

                // Cleanup
                if (!string.IsNullOrEmpty(tempCsvPath) && File.Exists(tempCsvPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[ReactiveTest] Cleaning up temp file: {tempCsvPath}");
                    Console.WriteLine($"[ReactiveTest] Cleaning up temp file: {tempCsvPath}");
                    File.Delete(tempCsvPath);
                }

                results.Add(success ? "\n✅ TEST PASSED" : "\n❌ TEST FAILED");
                System.Diagnostics.Debug.WriteLine(success ? "[ReactiveTest] ✅ TEST PASSED" : "[ReactiveTest] ❌ TEST FAILED");
                Console.WriteLine(success ? "[ReactiveTest] ✅ TEST PASSED" : "[ReactiveTest] ❌ TEST FAILED");

                return (success, string.Join("\n", results), null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ReactiveTest] ❌ UNHANDLED EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ReactiveTest] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[ReactiveTest] ❌ UNHANDLED EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                Console.WriteLine($"[ReactiveTest] Stack trace: {ex.StackTrace}");
                results.Add($"❌ EXCEPTION: {ex.Message}");
                return (false, string.Join("\n", results), ex.ToString());
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine("[ReactiveTest] Stopping reactive observations...");
                Console.WriteLine("[ReactiveTest] Stopping reactive observations...");
                ReactiveTestVerifications.StopObserving();
                System.Diagnostics.Debug.WriteLine("[ReactiveTest] Test execution completed");
                Console.WriteLine("[ReactiveTest] Test execution completed");
            }
        }

        /// <summary>
        /// Extract embedded CSV test file
        /// </summary>
        private async Task<string> ExtractTestCsvFile()
        {
            System.Diagnostics.Debug.WriteLine("[ReactiveTest] ExtractTestCsvFile called");
            Console.WriteLine("[ReactiveTest] ExtractTestCsvFile called");

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Core.Platform.MauiTester.Resources.TestData.TastytradeOptionsTest.csv";

            System.Diagnostics.Debug.WriteLine($"[ReactiveTest] Looking for resource: {resourceName}");
            Console.WriteLine($"[ReactiveTest] Looking for resource: {resourceName}");

            var tempPath = Path.Combine(Path.GetTempPath(), $"tastytrade_test_{Guid.NewGuid()}.csv");
            System.Diagnostics.Debug.WriteLine($"[ReactiveTest] Temp path: {tempPath}");
            Console.WriteLine($"[ReactiveTest] Temp path: {tempPath}");

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                System.Diagnostics.Debug.WriteLine($"[ReactiveTest] ❌ Resource not found!");
                Console.WriteLine($"[ReactiveTest] ❌ Resource not found!");

                // List all available resources for debugging
                var allResources = assembly.GetManifestResourceNames();
                System.Diagnostics.Debug.WriteLine($"[ReactiveTest] Available resources ({allResources.Length}):");
                Console.WriteLine($"[ReactiveTest] Available resources ({allResources.Length}):");
                foreach (var res in allResources)
                {
                    System.Diagnostics.Debug.WriteLine($"[ReactiveTest]   - {res}");
                    Console.WriteLine($"[ReactiveTest]   - {res}");
                }

                throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
            }

            System.Diagnostics.Debug.WriteLine($"[ReactiveTest] ✅ Resource stream found, size: {stream.Length} bytes");
            Console.WriteLine($"[ReactiveTest] ✅ Resource stream found, size: {stream.Length} bytes");

            using var fileStream = File.Create(tempPath);
            await stream.CopyToAsync(fileStream);

            System.Diagnostics.Debug.WriteLine($"[ReactiveTest] ✅ File created successfully");
            Console.WriteLine($"[ReactiveTest] ✅ File created successfully");

            return tempPath;
        }
    }
}