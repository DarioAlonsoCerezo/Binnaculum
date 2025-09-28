using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Binnaculum.Core.Database;
using Binnaculum.Core.Import;
using Binnaculum.Core.UI;
using Core.Platform.MauiTester.Services;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using CoreModels = Binnaculum.Core.Models;

namespace Core.Platform.MauiTester.TestCases
{
    /// <summary>
    /// End-to-end integration test for options trading import from Tastytrade CSV data.
    /// This test validates the complete pipeline from CSV file creation through import processing 
    /// to snapshot calculation and UI updates using real trading data.
    /// </summary>
    public class OptionsImportIntegrationTest : TestStep
    {
        private readonly TestExecutionContext _context;
        private string? _tempCsvPath;
        private int _testBrokerAccountId;
        private int _testBrokerId;
        private decimal _initialBalance;
        private int _initialMovementCount;

        public OptionsImportIntegrationTest(TestExecutionContext context)
            : base("Execute Options Import Integration Test")
        {
            _context = context;
        }

        /// <summary>
        /// Execute the complete integration test workflow
        /// </summary>
        public override async Task<(bool success, string details, string? error)> ExecuteAsync()
        {
            var startTime = DateTime.Now;
            var results = new List<string>();

            try
            {
                // Phase 1: Setup
                results.Add("=== Phase 1: Setup ===");
                await SetupTestEnvironment();
                var setupTime = DateTime.Now;
                results.Add($"Setup completed in {(setupTime - startTime).TotalSeconds:F2}s");

                // Phase 2: Import Execution
                results.Add("=== Phase 2: Import Execution ===");
                var importResult = await ExecuteImport(results);
                var importTime = DateTime.Now;
                var importDuration = importTime - setupTime;
                results.Add($"Import completed in {importDuration.TotalSeconds:F2}s");
                results.Add($"Movements imported: {importResult.ImportedData.Trades + importResult.ImportedData.BrokerMovements + importResult.ImportedData.OptionTrades}");

                // Phase 3: Validation and Results
                results.Add("=== Phase 3: Validation ===");
                var validationResult = ValidateResults(importResult);
                var endTime = DateTime.Now;
                var totalDuration = endTime - startTime;

                // Enhanced validation reporting with realized vs unrealized breakdown
                results.Add($"Financial validation: {(validationResult.Success ? "✅ PASSED" : "❌ FAILED")}");
                results.Add($"Import success: {importResult.Success}");
                results.Add($"Import errors: {importResult.Errors.Length}");
                if (importResult.Errors.Length > 0)
                {
                    results.Add("Import error details:");
                    foreach (var error in importResult.Errors.Take(3)) // Show first 3 errors
                    {
                        results.Add($"  - {error.ErrorMessage}");
                    }
                }

                // Cash flow validation
                results.Add($"Deposits: ${validationResult.ActualDeposited:F2} (expected: ${validationResult.ExpectedDeposited:F2}) {(validationResult.DepositMatch ? "✅" : "❌")}");

                // Realized performance validation
                results.Add($"Realized gains: ${validationResult.ActualRealizedGains:F2} (expected: ${validationResult.ExpectedRealizedGains:F2}) {(validationResult.RealizedGainsMatch ? "✅" : "❌")}");
                results.Add($"Realized %: {validationResult.ActualRealizedPercentage:F2}% (expected: {validationResult.ExpectedRealizedPercentage:F2}%) {(validationResult.RealizedPercentageMatch ? "✅" : "❌")}");

                // Unrealized performance validation
                results.Add($"Unrealized gains: ${validationResult.ActualUnrealizedGains:F2} (expected: ${validationResult.ExpectedUnrealizedGains:F2}) {(validationResult.UnrealizedGainsMatch ? "✅" : "❌")}");
                results.Add($"Unrealized %: {validationResult.ActualUnrealizedPercentage:F2}% (expected: {validationResult.ExpectedUnrealizedPercentage:F2}%) {(validationResult.UnrealizedPercentageMatch ? "✅" : "❌")}");

                // Total performance validation
                results.Add($"Total performance: {validationResult.ActualTotalPerformance:F2}% (expected: {validationResult.ExpectedTotalPerformance:F2}%) {(validationResult.TotalPerformanceMatch ? "✅" : "❌")}");

                // Movement validation
                results.Add($"Total movements: {validationResult.ActualMovements} (expected: {validationResult.ExpectedMovements}) {(validationResult.MovementMatch ? "✅" : "❌")}");
                results.Add($"Option trades: {validationResult.ActualOptionTrades} (expected: {validationResult.ExpectedOptionTrades}) {(validationResult.OptionTradeMatch ? "✅" : "❌")}");
                results.Add($"Broker movements: {importResult.ImportedData.BrokerMovements}");
                results.Add($"Total test duration: {totalDuration.TotalSeconds:F2}s");

                // Build success summary
                var success = validationResult.Success;
                var details = string.Join("\n", results);

                return success
                    ? (true, details, null)
                    : (false, details, "Financial validation failed - check expected vs actual values");
            }
            catch (Exception ex)
            {
                results.Add($"❌ EXCEPTION: {ex.Message}");
                return (false, string.Join("\n", results), ex.ToString());
            }
            finally
            {
                await Cleanup();
            }
        }

        /// <summary>
        /// Phase 1: Setup test environment
        /// </summary>
        private async Task SetupTestEnvironment()
        {
            // Extract embedded CSV to temporary file
            _tempCsvPath = await ExtractTestCsvFile();

            // Create test broker account
            var brokerAccount = await SetupTestBrokerAccount();
            _testBrokerAccountId = brokerAccount.Id;
            _testBrokerId = brokerAccount.Broker.Id;

            // Record initial state
            _initialBalance = await GetCurrentBalance(brokerAccount.Id);
            _initialMovementCount = await GetMovementCount(brokerAccount.Id);
        }

        /// <summary>
        /// Phase 2: Execute import workflow
        /// </summary>
        private async Task<ImportResult> ExecuteImport(ICollection<string> results)
        {
            if (string.IsNullOrEmpty(_tempCsvPath))
                throw new InvalidOperationException("CSV file path is null");

            // Execute the complete import workflow using the broker ID (not broker account ID)
            var importResult = await ImportManager.importFile(_testBrokerId, _tempCsvPath);

            // After import, manually trigger comprehensive snapshot processing
            if (importResult.Success)
            {
                try
                {
                    var importMetadata = TryCreateImportMetadataForAccount(_testBrokerAccountId, results);
                    if (importMetadata != null)
                    {
                        var oldestDateText = OptionModule.IsSome(importMetadata.OldestMovementDate)
                            ? importMetadata.OldestMovementDate.Value.ToString("yyyy-MM-dd")
                            : "n/a";
                        results.Add($"🔁 Triggering targeted snapshot refresh from {oldestDateText} covering {importMetadata.TotalMovementsImported} movements...");
                        await ReactiveTargetedSnapshotManager.updateFromImport(importMetadata);
                        results.Add("✅ Targeted snapshot refresh completed");
                    }
                    else
                    {
                        results.Add($"⚠️ Unable to build import metadata for broker account {_testBrokerAccountId}; skipping targeted refresh");
                    }

                    // Also force a general data refresh to ensure everything is loaded
                    results.Add("🔄 Starting Overview.LoadData()...");
                    await Overview.LoadData();
                    results.Add($"✅ Overview.LoadData() completed. Collections.Snapshots.Items.Count = {Collections.Snapshots.Items.Count}");

                    // Force reactive refresh to ensure snapshots are created
                    results.Add("🔄 Forcing ReactiveSnapshotManager.refresh()...");
                    ReactiveSnapshotManager.refresh();
                    results.Add("✅ ReactiveSnapshotManager.refresh() called");

                    await Task.Delay(3000); // Allow time for all processing to complete
                    results.Add($"📊 Final snapshot count: {Collections.Snapshots.Items.Count}");
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail the import - this is supplementary processing
                    System.Diagnostics.Debug.WriteLine($"Failed to trigger targeted snapshot updates: {ex.Message}");

                    // Fallback to just doing a general refresh
                    try
                    {
                        results.Add("🔄 Fallback: Starting Overview.LoadData()...");
                        await Overview.LoadData();
                        results.Add($"✅ Fallback Overview.LoadData() completed. Collections.Snapshots.Items.Count = {Collections.Snapshots.Items.Count}");
                        await Task.Delay(1000);
                    }
                    catch (Exception fallbackEx)
                    {
                        results.Add($"❌ Fallback refresh also failed: {fallbackEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"Fallback refresh also failed: {fallbackEx.Message}");
                    }
                }
            }

            return importResult;
        }

        /// <summary>
        /// Phase 3: Validate results using TestVerifications patterns
        /// </summary>
        private ValidationResult ValidateResults(ImportResult importResult)
        {
            // Add detailed logging to understand what data we're getting
            System.Diagnostics.Debug.WriteLine("=== DETAILED VALIDATION ANALYSIS ===");
            System.Diagnostics.Debug.WriteLine($"Collections.Snapshots.Items.Count: {Collections.Snapshots.Items.Count}");

            // Check if we have any broker accounts created
            System.Diagnostics.Debug.WriteLine($"Collections.Accounts.Items.Count: {Collections.Accounts.Items.Count}");
            foreach (var account in Collections.Accounts.Items.Take(3))
            {
                System.Diagnostics.Debug.WriteLine($"Account: {DescribeAccount(account)}");
            }

            // Manually trigger snapshot creation if none exist
            if (Collections.Snapshots.Items.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ No snapshots found - attempting manual trigger");

                try
                {
                    var metadata = TryCreateImportMetadataForAccount(_testBrokerAccountId);
                    if (metadata != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Triggering targeted snapshot refresh for account {_testBrokerAccountId}...");
                        ReactiveTargetedSnapshotManager.updateFromImport(metadata).GetAwaiter().GetResult();
                        System.Diagnostics.Debug.WriteLine($"✅ Targeted snapshot refresh completed. Snapshot count: {Collections.Snapshots.Items.Count}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Unable to build import metadata for account {_testBrokerAccountId}; targeted refresh skipped");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Manual snapshot creation failed: {ex.Message}");
                }
            }

            // Use TestVerifications for consistent snapshot validation
            var (success, details, error) = TestVerifications.VerifyOptionsFinancialData();
            System.Diagnostics.Debug.WriteLine($"TestVerifications result: Success={success}, Details='{details}', Error='{error}'");

            // Extract actual values from Collections.Snapshots for detailed reporting
            decimal actualDeposited = 0;
            decimal actualRealizedGains = 0;
            decimal actualUnrealizedGains = 0;
            int actualMovements = 0;
            decimal actualRealizedPercentage = 0;
            decimal actualUnrealizedPercentage = 0;
            decimal actualTotalPerformance = 0;

            if (Collections.Snapshots.Items.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine("=== SNAPSHOT ANALYSIS ===");
                for (int i = 0; i < Math.Min(3, Collections.Snapshots.Items.Count); i++)
                {
                    var snapshot = Collections.Snapshots.Items[i];
                    System.Diagnostics.Debug.WriteLine($"Snapshot {i}: Type={snapshot.GetType().Name}");

                    if (snapshot.BrokerAccount != null)
                    {
                        var brokerSnapshot = snapshot.BrokerAccount.Value;
                        System.Diagnostics.Debug.WriteLine($"  BrokerAccount Found");

                        var financial = brokerSnapshot.Financial;
                        System.Diagnostics.Debug.WriteLine($"  Financial Data:");
                        System.Diagnostics.Debug.WriteLine($"    Deposited: ${financial.Deposited:F2}");
                        System.Diagnostics.Debug.WriteLine($"    RealizedGains: ${financial.RealizedGains:F2}");
                        System.Diagnostics.Debug.WriteLine($"    UnrealizedGains: ${financial.UnrealizedGains:F2}");
                        System.Diagnostics.Debug.WriteLine($"    MovementCounter: {financial.MovementCounter}");
                        System.Diagnostics.Debug.WriteLine($"    Invested: ${financial.Invested:F2}");
                        System.Diagnostics.Debug.WriteLine($"    Withdrawn: ${financial.Withdrawn:F2}");
                        System.Diagnostics.Debug.WriteLine($"    DividendsReceived: ${financial.DividendsReceived:F2}");
                        System.Diagnostics.Debug.WriteLine($"    OptionsIncome: ${financial.OptionsIncome:F2}");
                        System.Diagnostics.Debug.WriteLine($"    Commissions: ${financial.Commissions:F2}");
                        System.Diagnostics.Debug.WriteLine($"    Fees: ${financial.Fees:F2}");

                        // Use the first (latest) snapshot for validation
                        if (i == 0)
                        {
                            actualDeposited = financial.Deposited;
                            actualRealizedGains = financial.RealizedGains;
                            actualUnrealizedGains = financial.UnrealizedGains;
                            actualMovements = financial.MovementCounter;

                            // Calculate performance percentages
                            if (actualDeposited > 0)
                            {
                                actualRealizedPercentage = (actualRealizedGains / actualDeposited) * 100;
                                actualUnrealizedPercentage = (actualUnrealizedGains / actualDeposited) * 100;
                                actualTotalPerformance = actualRealizedPercentage + actualUnrealizedPercentage;
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"  BrokerAccount: NULL");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No snapshots found in Collections.Snapshots.Items");
            }

            // Log other relevant collections
            System.Diagnostics.Debug.WriteLine("=== OTHER COLLECTIONS ANALYSIS ===");
            System.Diagnostics.Debug.WriteLine($"Collections.Accounts.Items.Count: {Collections.Accounts.Items.Count}");
            System.Diagnostics.Debug.WriteLine($"Collections.Movements.Items.Count: {Collections.Movements.Items.Count}");
            System.Diagnostics.Debug.WriteLine($"Collections.Tickers.Items.Count: {Collections.Tickers.Items.Count}");

            // Analyze test broker account specifically
            var testBrokerAccount = Collections.Accounts.Items
                .Where(a => a.Type == Binnaculum.Core.Models.AccountType.BrokerAccount)
                .FirstOrDefault(a => a.Broker?.Value.Id == _testBrokerAccountId);

            if (testBrokerAccount != null)
            {
                System.Diagnostics.Debug.WriteLine($"Test Broker Account Found: ID={testBrokerAccount.Broker?.Value.Id}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Test Broker Account NOT FOUND (looking for ID={_testBrokerAccountId})");
            }

            System.Diagnostics.Debug.WriteLine("=== END DETAILED VALIDATION ANALYSIS ===");

            // Count actual option trades from import result
            int actualOptionTrades = importResult.ImportedData.OptionTrades;

            // Log import result details
            System.Diagnostics.Debug.WriteLine("=== IMPORT RESULT ANALYSIS ===");
            System.Diagnostics.Debug.WriteLine($"Import Success: {importResult.Success}");
            System.Diagnostics.Debug.WriteLine($"Import Errors Count: {importResult.Errors.Length}");
            System.Diagnostics.Debug.WriteLine($"Imported Data:");
            System.Diagnostics.Debug.WriteLine($"  Trades: {importResult.ImportedData.Trades}");
            System.Diagnostics.Debug.WriteLine($"  BrokerMovements: {importResult.ImportedData.BrokerMovements}");
            System.Diagnostics.Debug.WriteLine($"  Dividends: {importResult.ImportedData.Dividends}");
            System.Diagnostics.Debug.WriteLine($"  OptionTrades: {importResult.ImportedData.OptionTrades}");
            System.Diagnostics.Debug.WriteLine($"  NewTickers: {importResult.ImportedData.NewTickers}");
            System.Diagnostics.Debug.WriteLine($"Processing Time: {importResult.ProcessingTimeMs}ms");

            if (importResult.Errors.Length > 0)
            {
                System.Diagnostics.Debug.WriteLine("Import Errors:");
                foreach (var importError in importResult.Errors.Take(5))
                {
                    System.Diagnostics.Debug.WriteLine($"  - Row {importError.RowNumber}: {importError.ErrorMessage}");
                }
            }

            const decimal tolerance = 2.00m; // Allow tolerance for calculation differences

            return new ValidationResult
            {
                Success = success,
                ErrorMessage = error,

                // Cash flow validation
                ActualDeposited = actualDeposited,
                ExpectedDeposited = 878.79m,
                DepositMatch = Math.Abs(actualDeposited - 878.79m) <= tolerance,

                // Realized performance validation - updated for traditional option accounting (no auto-expiration)
                ActualRealizedGains = actualRealizedGains,
                ExpectedRealizedGains = 23.65m, // Only explicitly closed option strategies
                RealizedGainsMatch = Math.Abs(actualRealizedGains - 23.65m) <= tolerance,
                ActualRealizedPercentage = actualRealizedPercentage,
                ExpectedRealizedPercentage = 2.70m, // Updated percentage: 23.65 / 878.79 * 100
                RealizedPercentageMatch = Math.Abs(actualRealizedPercentage - 2.70m) <= (tolerance / 10), // Tighter tolerance for percentages

                // Unrealized performance validation
                ActualUnrealizedGains = actualUnrealizedGains,
                ExpectedUnrealizedGains = 14.86m,
                UnrealizedGainsMatch = Math.Abs(actualUnrealizedGains - 14.86m) <= tolerance,
                ActualUnrealizedPercentage = actualUnrealizedPercentage,
                ExpectedUnrealizedPercentage = 1.69m,
                UnrealizedPercentageMatch = Math.Abs(actualUnrealizedPercentage - 1.69m) <= (tolerance / 10),

                // Total performance validation
                ActualTotalPerformance = actualTotalPerformance,
                ExpectedTotalPerformance = 4.39m, // 2.70% + 1.69%
                TotalPerformanceMatch = Math.Abs(actualTotalPerformance - 4.39m) <= (tolerance / 10),

                // Movement validation
                ActualMovements = actualMovements,
                ExpectedMovements = 16,
                MovementMatch = actualMovements == 16,
                ActualOptionTrades = actualOptionTrades,
                ExpectedOptionTrades = 12,
                OptionTradeMatch = actualOptionTrades == 12,

                ImportResult = importResult
            };
        }

        /// <summary>
        /// Extract embedded CSV resource to temporary file
        /// </summary>
        private async Task<string> ExtractTestCsvFile()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Core.Platform.MauiTester.Resources.TestData.TastytradeOptionsTest.csv";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

            var tempPath = Path.Combine(Path.GetTempPath(), $"TastytradeOptionsTest_{Guid.NewGuid()}.csv");

            using var fileStream = File.Create(tempPath);
            await stream.CopyToAsync(fileStream);

            return tempPath;
        }

        /// <summary>
        /// Create a test broker account for Tastytrade
        /// </summary>
        private async Task<Binnaculum.Core.Models.BrokerAccount> SetupTestBrokerAccount()
        {
            // Find Tastytrade broker
            var tastytradeBroker = Collections.Brokers.Items.FirstOrDefault(b => b.SupportedBroker == Binnaculum.Core.Models.SupportedBroker.Tastytrade);
            if (tastytradeBroker == null)
                throw new InvalidOperationException("Tastytrade broker not found in collections");

            // Create test broker account
            var accountName = $"Integration_Test_{DateTime.Now:yyyyMMdd_HHmmss}";
            await Creator.SaveBrokerAccount(tastytradeBroker.Id, accountName);

            // Find the created account
            var createdAccount = Collections.Accounts.Items
                .Where(a => a.Type == Binnaculum.Core.Models.AccountType.BrokerAccount)
                .FirstOrDefault(a => a.Broker?.Value.Broker.SupportedBroker == Binnaculum.Core.Models.SupportedBroker.Tastytrade);

            if (createdAccount?.Broker?.Value == null)
                throw new InvalidOperationException("Failed to create test broker account");

            return createdAccount.Broker.Value;
        }

        /// <summary>
        /// Get current balance for broker account using available collections
        /// </summary>
        private Task<decimal> GetCurrentBalance(int brokerAccountId)
        {
            // For this integration test, we'll use a simplified approach
            // and rely on the import result's impact rather than exact balance tracking
            // This avoids complex F# Option interop issues
            return Task.FromResult(0m);
        }

        /// <summary>
        /// Get movement count for broker account using available collections
        /// </summary>
        private Task<int> GetMovementCount(int brokerAccountId)
        {
            // For this integration test, we'll use a simplified count
            // The actual validation will be based on the import result metrics
            var movementCount = Collections.Movements.Items.Count;
            return Task.FromResult(movementCount);
        }



        /// <summary>
        /// Cleanup test resources
        /// </summary>
        private async Task Cleanup()
        {
            // Remove temporary CSV file
            if (!string.IsNullOrEmpty(_tempCsvPath) && File.Exists(_tempCsvPath))
            {
                try
                {
                    File.Delete(_tempCsvPath);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the test for cleanup issues
                    System.Diagnostics.Debug.WriteLine($"Failed to cleanup temp file: {ex.Message}");
                }
            }

            // Clean up test data from database if needed
            if (_testBrokerAccountId > 0)
            {
                try
                {
                    await CleanupTestData(_testBrokerAccountId);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the test for cleanup issues
                    System.Diagnostics.Debug.WriteLine($"Failed to cleanup test data: {ex.Message}");
                }
            }

            // Note: Snapshot management is now automatic via import system
            // No manual reactive manager refresh needed
        }

        /// <summary>
        /// Clean up test data from database
        /// </summary>
        private async Task CleanupTestData(int brokerAccountId)
        {
            // This is a placeholder - in a real implementation, you might want to
            // remove test movements and snapshots to keep the database clean
            // For now, we'll just refresh the reactive managers
            await Task.Delay(1); // Placeholder async operation
        }

        private ImportMetadata? TryCreateImportMetadataForAccount(int brokerAccountId, ICollection<string>? log = null)
        {
            var tickerSymbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var brokerAccountSet = SetModule.OfSeq(new[] { brokerAccountId });
            int totalMovements = 0;
            DateTime? oldestMovement = null;

            foreach (var movement in Collections.Movements.Items)
            {
                if (!MovementBelongsToAccount(movement, brokerAccountId))
                {
                    continue;
                }

                totalMovements++;
                if (oldestMovement == null || movement.TimeStamp < oldestMovement.Value)
                {
                    oldestMovement = movement.TimeStamp;
                }

                CollectTickerSymbols(movement, tickerSymbols);
            }

            if (totalMovements == 0 || oldestMovement == null)
            {
                log?.Add($"⚠️ No movements found for broker account {brokerAccountId} when building import metadata");
                System.Diagnostics.Debug.WriteLine($"[SnapshotRefresh] Skipping metadata build; no movements for account {brokerAccountId}");
                return null;
            }

            var tickerSet = SetModule.OfSeq(tickerSymbols);
            var oldestOption = FSharpOption<DateTime>.Some(oldestMovement.Value);
            var metadata = new ImportMetadata(oldestOption, brokerAccountSet, tickerSet, totalMovements);

            var tickerSummary = tickerSymbols.Count > 0
                ? string.Join(", ", tickerSymbols.OrderBy(symbol => symbol))
                : "none";

            log?.Add($"📅 Snapshot metadata built from {oldestMovement.Value:yyyy-MM-dd} covering {totalMovements} movements (tickers: {tickerSummary})");
            System.Diagnostics.Debug.WriteLine($"[SnapshotRefresh] Metadata for account {brokerAccountId}: oldest={oldestMovement.Value:yyyy-MM-dd}, movements={totalMovements}, tickers={tickerSummary}");

            return metadata;
        }

        private static bool MovementBelongsToAccount(CoreModels.Movement movement, int brokerAccountId)
        {
            var brokerAccount = GetAssociatedBrokerAccount(movement);
            return brokerAccount != null && brokerAccount.Id == brokerAccountId;
        }

        private static CoreModels.BrokerAccount? GetAssociatedBrokerAccount(CoreModels.Movement movement)
        {
            if (movement.BrokerMovement is FSharpOption<CoreModels.BrokerMovement> brokerMovement && OptionModule.IsSome(brokerMovement))
            {
                return brokerMovement.Value.BrokerAccount;
            }

            if (movement.Trade is FSharpOption<CoreModels.Trade> trade && OptionModule.IsSome(trade))
            {
                return trade.Value.BrokerAccount;
            }

            if (movement.OptionTrade is FSharpOption<CoreModels.OptionTrade> optionTrade && OptionModule.IsSome(optionTrade))
            {
                return optionTrade.Value.BrokerAccount;
            }

            if (movement.Dividend is FSharpOption<CoreModels.Dividend> dividend && OptionModule.IsSome(dividend))
            {
                return dividend.Value.BrokerAccount;
            }

            if (movement.DividendTax is FSharpOption<CoreModels.DividendTax> dividendTax && OptionModule.IsSome(dividendTax))
            {
                return dividendTax.Value.BrokerAccount;
            }

            if (movement.DividendDate is FSharpOption<CoreModels.DividendDate> dividendDate && OptionModule.IsSome(dividendDate))
            {
                return dividendDate.Value.BrokerAccount;
            }

            return null;
        }

        private static void CollectTickerSymbols(CoreModels.Movement movement, ISet<string> destination)
        {
            if (movement.Trade is FSharpOption<CoreModels.Trade> trade && OptionModule.IsSome(trade))
            {
                destination.Add(trade.Value.Ticker.Symbol);
            }

            if (movement.OptionTrade is FSharpOption<CoreModels.OptionTrade> optionTrade && OptionModule.IsSome(optionTrade))
            {
                destination.Add(optionTrade.Value.Ticker.Symbol);
            }

            if (movement.Dividend is FSharpOption<CoreModels.Dividend> dividend && OptionModule.IsSome(dividend))
            {
                destination.Add(dividend.Value.Ticker.Symbol);
            }

            if (movement.DividendTax is FSharpOption<CoreModels.DividendTax> dividendTax && OptionModule.IsSome(dividendTax))
            {
                destination.Add(dividendTax.Value.Ticker.Symbol);
            }

            if (movement.DividendDate is FSharpOption<CoreModels.DividendDate> dividendDate && OptionModule.IsSome(dividendDate))
            {
                destination.Add(dividendDate.Value.Ticker.Symbol);
            }

            if (movement.BrokerMovement is FSharpOption<CoreModels.BrokerMovement> brokerMovement && OptionModule.IsSome(brokerMovement))
            {
                var tickerOption = brokerMovement.Value.Ticker;
                if (tickerOption is FSharpOption<CoreModels.Ticker> ticker && OptionModule.IsSome(ticker))
                {
                    destination.Add(ticker.Value.Symbol);
                }
            }
        }

        private static string DescribeAccount(Binnaculum.Core.Models.Account account)
        {
            if (account.Broker != null)
            {
                var brokerAccount = account.Broker.Value;
                return $"BrokerAccount Id={brokerAccount.Id}, Broker={brokerAccount.Broker.Name}, AccountNumber={brokerAccount.AccountNumber}";
            }

            if (account.Bank != null)
            {
                var bankAccount = account.Bank.Value;
                return $"BankAccount Id={bankAccount.Id}, Name={bankAccount.Name}";
            }

            return $"Account Type={account.Type}";
        }

        /// <summary>
        /// Enhanced validation result container with separate realized/unrealized validation
        /// </summary>
        private class ValidationResult
        {
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }

            // Cash flow validation
            public decimal ActualDeposited { get; set; }
            public decimal ExpectedDeposited { get; set; } = 878.79m;
            public bool DepositMatch { get; set; }

            // Realized performance validation
            public decimal ActualRealizedGains { get; set; }
            public decimal ExpectedRealizedGains { get; set; } = 23.65m; // Only explicitly closed option strategies
            public bool RealizedGainsMatch { get; set; }
            public decimal ActualRealizedPercentage { get; set; }
            public decimal ExpectedRealizedPercentage { get; set; } = 2.70m; // 23.65 / 878.79 * 100
            public bool RealizedPercentageMatch { get; set; }

            // Unrealized performance validation
            public decimal ActualUnrealizedGains { get; set; }
            public decimal ExpectedUnrealizedGains { get; set; } = 14.86m;
            public bool UnrealizedGainsMatch { get; set; }
            public decimal ActualUnrealizedPercentage { get; set; }
            public decimal ExpectedUnrealizedPercentage { get; set; } = 1.69m;
            public bool UnrealizedPercentageMatch { get; set; }

            // Total performance validation
            public decimal ActualTotalPerformance { get; set; }
            public decimal ExpectedTotalPerformance { get; set; } = 4.39m; // 2.70% + 1.69%
            public bool TotalPerformanceMatch { get; set; }

            // Movement validation
            public int ActualMovements { get; set; }
            public int ExpectedMovements { get; set; } = 16;
            public bool MovementMatch { get; set; }
            public int ActualOptionTrades { get; set; }
            public int ExpectedOptionTrades { get; set; } = 12;
            public bool OptionTradeMatch { get; set; }

            public ImportResult ImportResult { get; set; } = null!;
        }
    }
}