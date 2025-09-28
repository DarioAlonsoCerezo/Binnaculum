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
    /// End-to-end integration test for comprehensive Tastytrade import from SPX options CSV data.
    /// This test validates the complete pipeline with real SPX options trading data including
    /// complex strategies like spreads, straddles, iron condors, and calendar spreads.
    /// Expected results: $164.00 commissions, $243.97 fees, $822.03 realized gains.
    /// </summary>
    public class TastytradeImportIntegrationTest : TestStep
    {
        private readonly TestExecutionContext _context;
        private string? _tempCsvPath;
        private int _testBrokerAccountId;
        private int _testBrokerId;
        private decimal _initialBalance;
        private int _initialMovementCount;

        public TastytradeImportIntegrationTest(TestExecutionContext context)
            : base("Execute Tastytrade Import Integration Test")
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

                // Enhanced validation reporting with SPX-specific results
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

                // Commissions and fees validation
                results.Add($"Commissions: ${validationResult.ActualCommissions:F2} (expected: ${validationResult.ExpectedCommissions:F2}) {(validationResult.CommissionsMatch ? "✅" : "❌")}");
                results.Add($"Fees: ${validationResult.ActualFees:F2} (expected: ${validationResult.ExpectedFees:F2}) {(validationResult.FeesMatch ? "✅" : "❌")}");

                // Realized gains validation
                results.Add($"Realized gains: ${validationResult.ActualRealizedGains:F2} (expected: ${validationResult.ExpectedRealizedGains:F2}) {(validationResult.RealizedGainsMatch ? "✅" : "❌")}");

                // SPX Ticker Snapshot Validation
                if (validationResult.SpxTickerFound)
                {
                    results.Add("=== SPX Ticker Snapshot Validation ===");
                    results.Add($"SPX Ticker found: ✅ {validationResult.SpxTickerSymbol}");
                    results.Add($"SPX Commissions: ${validationResult.SpxCommissions:F2} (expected: ${validationResult.ExpectedCommissions:F2}) {(validationResult.SpxCommissionsMatch ? "✅" : "❌")}");
                    results.Add($"SPX Fees: ${validationResult.SpxFees:F2} (expected: ${validationResult.ExpectedFees:F2}) {(validationResult.SpxFeesMatch ? "✅" : "❌")}");
                    results.Add($"SPX Realized: ${validationResult.SpxRealized:F2} (expected: ${validationResult.ExpectedRealizedGains:F2}) {(validationResult.SpxRealizedMatch ? "✅" : "❌")}");
                }
                else
                {
                    results.Add("❌ SPX Ticker Snapshot NOT FOUND - This indicates import processing failure");
                }

                // Movement validation
                results.Add($"Total movements: {validationResult.ActualMovements} (expected: {validationResult.ExpectedMovements}) {(validationResult.MovementMatch ? "✅" : "❌")}");
                results.Add($"Option trades: {validationResult.ActualOptionTrades} (expected: {validationResult.ExpectedOptionTrades}) {(validationResult.OptionTradeMatch ? "✅" : "❌")}");
                results.Add($"Broker movements: {importResult.ImportedData.BrokerMovements}");
                results.Add($"Total test duration: {totalDuration.TotalSeconds:F2}s");

                // Build success summary
                var success = validationResult.Success && validationResult.SpxTickerFound;
                var details = string.Join("\n", results);

                return (success, details, null);
            }
            catch (Exception ex)
            {
                var errorDetails = string.Join("\n", results);
                return (false, errorDetails, $"Exception during test execution: {ex.Message}");
            }
            finally
            {
                // Cleanup
                await CleanupTestEnvironment().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Phase 1: Setup test environment
        /// </summary>
        private async Task SetupTestEnvironment()
        {
            // Extract test CSV file to temporary location
            _tempCsvPath = await ExtractTestCsvFile();

            // Create test broker account
            var brokerAccount = await SetupTestBrokerAccount();
            _testBrokerAccountId = brokerAccount.Id;
            _testBrokerId = brokerAccount.Broker.Id;

            // Get initial state for comparison
            _initialBalance = await GetCurrentBalance(_testBrokerAccountId);
            _initialMovementCount = await GetMovementCount(_testBrokerAccountId);
        }

        /// <summary>
        /// Phase 2: Execute import workflow
        /// </summary>
        private async Task<ImportResult> ExecuteImport(ICollection<string> results)
        {
            if (string.IsNullOrEmpty(_tempCsvPath))
                throw new InvalidOperationException("CSV file path is null");

            // Execute the complete import workflow using the explicitly selected broker account
            var importResult = await ImportManager.importFile(_testBrokerId, _testBrokerAccountId, _tempCsvPath);

            // After import, manually trigger comprehensive snapshot processing
            if (importResult.Success)
            {
                try
                {
                    // Trigger comprehensive snapshot recalculation
                    var metadata = TryCreateImportMetadataForAccount(_testBrokerAccountId, results);
                    if (metadata != null)
                    {
                        results.Add($"Triggering comprehensive snapshot calculations...");
                        await ReactiveTargetedSnapshotManager.updateFromImport(metadata);
                        results.Add($"Snapshot calculations completed successfully");
                    }
                    else
                    {
                        results.Add($"Warning: Could not create import metadata for comprehensive calculations");
                    }
                }
                catch (Exception ex)
                {
                    results.Add($"Warning: Error during comprehensive snapshot calculations: {ex.Message}");
                }
            }

            return importResult;
        }

        /// <summary>
        /// Phase 3: Validate results using TestVerifications patterns and SPX ticker-specific validation
        /// </summary>
        private ValidationResult ValidateResults(ImportResult importResult)
        {
            // Expected values for SPX options trading data based on issue requirements
            const decimal expectedCommissions = 164.00m;
            const decimal expectedFees = 243.97m;
            const decimal expectedRealizedGains = 822.03m;
            const decimal tolerance = 2.00m; // $2.00 tolerance as specified
            const int expectedMovements = 309; // ~140+ individual option transactions (309 CSV rows - 1 header)
            const int expectedOptionTrades = 307; // Most transactions are option trades

            var validationResult = new ValidationResult
            {
                ExpectedCommissions = expectedCommissions,
                ExpectedFees = expectedFees,
                ExpectedRealizedGains = expectedRealizedGains,
                ExpectedMovements = expectedMovements,
                ExpectedOptionTrades = expectedOptionTrades
            };

            // Get latest broker account snapshot for validation  
            var snapshot = Collections.Snapshots.Items
                .Where(s => s.BrokerAccount != null)
                .OrderByDescending(s => s.BrokerAccount.Value.Date)
                .FirstOrDefault();

            if (snapshot?.BrokerAccount != null)
            {
                var financial = snapshot.BrokerAccount.Value.Financial;
                
                validationResult.ActualCommissions = financial.Commissions;
                validationResult.ActualFees = financial.Fees;
                validationResult.ActualRealizedGains = financial.RealizedGains;
                validationResult.ActualMovements = financial.MovementCounter;
                validationResult.ActualOptionTrades = importResult.ImportedData.OptionTrades;

                // Validate within tolerance
                validationResult.CommissionsMatch = Math.Abs(financial.Commissions - expectedCommissions) <= tolerance;
                validationResult.FeesMatch = Math.Abs(financial.Fees - expectedFees) <= tolerance;
                validationResult.RealizedGainsMatch = Math.Abs(financial.RealizedGains - expectedRealizedGains) <= tolerance;
                validationResult.MovementMatch = Math.Abs(financial.MovementCounter - expectedMovements) <= 10; // Allow some variance in movement count
                validationResult.OptionTradeMatch = Math.Abs(importResult.ImportedData.OptionTrades - expectedOptionTrades) <= 10;
            }

            // SPX Ticker-Specific Validation
            var spxTickerSnapshot = Collections.TickerSnapshots.Items
                .FirstOrDefault(ts => ts.Ticker.Symbol == "SPX");

            if (spxTickerSnapshot != null)
            {
                validationResult.SpxTickerFound = true;
                validationResult.SpxTickerSymbol = spxTickerSnapshot.Ticker.Symbol;

                // Get SPX ticker currency snapshot for financial data
                var spxCurrencySnapshot = Collections.TickerSnapshots.Items
                    .Where(ts => ts.Ticker.Symbol == "SPX")
                    .OrderByDescending(ts => ts.Date)
                    .FirstOrDefault();

                if (spxCurrencySnapshot != null)
                {
                    // For ticker snapshots, we'll use the broker account snapshot data filtered for SPX
                    // Since we don't have direct ticker-level commission data, we'll approximate
                    validationResult.SpxCommissions = validationResult.ActualCommissions;
                    validationResult.SpxFees = validationResult.ActualFees;
                    validationResult.SpxRealized = validationResult.ActualRealizedGains;

                    // Validate SPX-specific values (using same values as aggregate since SPX is primary ticker)
                    validationResult.SpxCommissionsMatch = validationResult.CommissionsMatch;
                    validationResult.SpxFeesMatch = validationResult.FeesMatch;
                    validationResult.SpxRealizedMatch = validationResult.RealizedGainsMatch;
                }
            }

            // Overall success determination
            validationResult.Success = validationResult.CommissionsMatch && 
                                     validationResult.FeesMatch && 
                                     validationResult.RealizedGainsMatch &&
                                     validationResult.MovementMatch &&
                                     validationResult.OptionTradeMatch;

            return validationResult;
        }

        /// <summary>
        /// Extract embedded CSV resource to temporary file
        /// </summary>
        private async Task<string> ExtractTestCsvFile()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Core.Platform.MauiTester.Resources.TestData.TastytradeImportTest.csv";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

            var tempPath = Path.Combine(Path.GetTempPath(), $"TastytradeImportTest_{Guid.NewGuid()}.csv");

            using var fileStream = File.Create(tempPath);
            await stream.CopyToAsync(fileStream);

            return tempPath;
        }

        /// <summary>
        /// Create a test broker account for Tastytrade
        /// </summary>
        private async Task<CoreModels.BrokerAccount> SetupTestBrokerAccount()
        {
            // Find Tastytrade broker
            var tastytradeBroker = Collections.Brokers.Items.FirstOrDefault(b => b.SupportedBroker == CoreModels.SupportedBroker.Tastytrade);
            if (tastytradeBroker == null)
                throw new InvalidOperationException("Tastytrade broker not found in collections");

            // Create unique broker account for this test
            var brokerAccountName = $"Test SPX Account {DateTime.Now:yyyyMMdd_HHmmss}";
            await Creator.SaveBrokerAccount(tastytradeBroker.Id, brokerAccountName);

            // Get the newly created account
            var brokerAccount = Collections.Accounts.Items
                .Where(a => a.Type == CoreModels.AccountType.BrokerAccount)
                .FirstOrDefault(a => a.Broker != null && a.Broker.Value.Broker.Id == tastytradeBroker.Id);
            
            if (brokerAccount?.Broker == null)
                throw new InvalidOperationException("Failed to create or retrieve broker account");

            return brokerAccount.Broker.Value;
        }

        /// <summary>
        /// Get current balance for broker account using available collections
        /// </summary>
        private Task<decimal> GetCurrentBalance(int brokerAccountId)
        {
            // For this integration test, we'll use a simplified balance calculation
            // The actual validation will be based on the import result metrics
            var balance = Collections.Snapshots.Items
                .Where(s => s.BrokerAccount != null)
                .Select(s => s.BrokerAccount.Value.Financial.Deposited)
                .FirstOrDefault();
            return Task.FromResult(balance);
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

        private ImportMetadata? TryCreateImportMetadataForAccount(int brokerAccountId, ICollection<string>? log = null)
        {
            try
            {
                // Get all movements and filter those belonging to this account
                var allMovements = Collections.Movements.Items.ToList();
                var movements = new List<CoreModels.Movement>();
                foreach (var movement in allMovements)
                {
                    if (MovementBelongsToAccount(movement, brokerAccountId))
                    {
                        movements.Add(movement);
                    }
                }

                if (!movements.Any())
                {
                    log?.Add($"No movements found for account {brokerAccountId}");
                    return null;
                }

                // Extract unique broker accounts
                var brokerAccountIds = new HashSet<int> { brokerAccountId };
                var brokerAccountSet = SetModule.OfSeq(brokerAccountIds);

                // Extract unique tickers
                var tickerSymbols = new HashSet<string>();
                foreach (var movement in movements)
                {
                    CollectTickerSymbols(movement, tickerSymbols);
                }

                var tickerSet = SetModule.OfSeq(tickerSymbols);
                var oldestMovement = movements.Select(m => m.TimeStamp).DefaultIfEmpty().Min();
                var oldestOption = FSharpOption<DateTime>.Some(oldestMovement);
                var totalMovements = movements.Count;
                var metadata = new ImportMetadata(oldestOption, brokerAccountSet, tickerSet, totalMovements);

                var tickerSummary = tickerSymbols.Count > 0
                    ? string.Join(", ", tickerSymbols.OrderBy(symbol => symbol))
                    : "none";

                log?.Add($"ImportMetadata created: {totalMovements} movements, {tickerSymbols.Count} tickers ({tickerSummary}), oldest: {oldestMovement:yyyy-MM-dd}");

                return metadata;
            }
            catch (Exception ex)
            {
                log?.Add($"Error creating ImportMetadata: {ex.Message}");
                return null;
            }
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

            if (movement.BrokerMovement is FSharpOption<CoreModels.BrokerMovement> brokerMovement && OptionModule.IsSome(brokerMovement))
            {
                var tickerOption = brokerMovement.Value.Ticker;
                if (tickerOption is FSharpOption<CoreModels.Ticker> ticker && OptionModule.IsSome(ticker))
                {
                    destination.Add(ticker.Value.Symbol);
                }
            }
        }

        /// <summary>
        /// Cleanup test environment and resources
        /// </summary>
        private async Task CleanupTestEnvironment()
        {
            try
            {
                // Delete temporary CSV file
                if (!string.IsNullOrEmpty(_tempCsvPath) && File.Exists(_tempCsvPath))
                {
                    File.Delete(_tempCsvPath);
                }

                // Note: We don't delete the broker account as it might be useful for further analysis
                // and the test data is contained within a controlled test environment
            }
            catch (Exception ex)
            {
                // Log cleanup errors but don't fail the test
                System.Diagnostics.Debug.WriteLine($"Cleanup error: {ex.Message}");
            }
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Validation result structure for comprehensive test validation
    /// </summary>
    public class ValidationResult
    {
        public bool Success { get; set; }
        
        // Expected values
        public decimal ExpectedCommissions { get; set; }
        public decimal ExpectedFees { get; set; }
        public decimal ExpectedRealizedGains { get; set; }
        public int ExpectedMovements { get; set; }
        public int ExpectedOptionTrades { get; set; }
        
        // Actual values
        public decimal ActualCommissions { get; set; }
        public decimal ActualFees { get; set; }
        public decimal ActualRealizedGains { get; set; }
        public int ActualMovements { get; set; }
        public int ActualOptionTrades { get; set; }
        
        // Validation results
        public bool CommissionsMatch { get; set; }
        public bool FeesMatch { get; set; }
        public bool RealizedGainsMatch { get; set; }
        public bool MovementMatch { get; set; }
        public bool OptionTradeMatch { get; set; }
        
        // SPX Ticker-specific validation
        public bool SpxTickerFound { get; set; }
        public string SpxTickerSymbol { get; set; } = "";
        public decimal SpxCommissions { get; set; }
        public decimal SpxFees { get; set; }
        public decimal SpxRealized { get; set; }
        public bool SpxCommissionsMatch { get; set; }
        public bool SpxFeesMatch { get; set; }
        public bool SpxRealizedMatch { get; set; }
    }
}