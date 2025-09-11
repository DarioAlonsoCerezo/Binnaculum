using Binnaculum.Core.UI;
using Core.Platform.MauiTester.Models;

namespace Core.Platform.MauiTester.Services
{
    /// <summary>
    /// Service for executing Core Platform validation tests that recreate the logic from PublicApiIntegrationTests.fs
    /// </summary>
    public class TestRunner
    {
        private readonly LogService _logService;

        public TestRunner(LogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// Execute the main test that validates Overview.InitDatabase() and Overview.LoadData() work in MAUI environment
        /// This recreates the exact logic from PublicApiIntegrationTests.fs
        /// </summary>
        public async Task<OverallTestResult> ExecuteOverviewTestAsync(Action<string> progressCallback)
        {
            var result = new OverallTestResult();
            var steps = new List<TestStepResult>();

            try
            {
                result.IsRunning = true;
                result.OverallStatus = "Running Core Platform validation tests...";
                progressCallback("Starting Core Platform validation tests...");

                // Step 1: Initialize MAUI platform services
                if (!await ExecuteStepAsync(steps, result, "Initialize MAUI Platform Services", progressCallback, InitializePlatformServicesAsync))
                    return result;

                // Step 2: Call Overview.InitDatabase() 
                if (!await ExecuteStepAsync(steps, result, "Overview.InitDatabase()", progressCallback, InitializeDatabaseAsync))
                    return result;

                // Step 3: Call Overview.LoadData()
                if (!await ExecuteStepAsync(steps, result, "Overview.LoadData()", progressCallback, LoadDataAsync))
                    return result;

                // Step 4: Verify database initialization state
                if (!await ExecuteVerificationStepAsync(steps, result, "Verify Database Initialized", progressCallback, VerifyDatabaseInitialized))
                    return result;

                // Step 5: Verify data loading state
                if (!await ExecuteVerificationStepAsync(steps, result, "Verify Data Loaded", progressCallback, VerifyDataLoaded))
                    return result;

                // Step 6: Wait for reactive collections to populate
                progressCallback("Waiting for reactive collections to populate...");
                _logService.Log("Allowing time for reactive collections to populate...");
                await Task.Delay(300); // Same delay as in original test

                // Step 7-14: Collection and data verifications
                var verificationSteps = new[]
                {
                    ("Verify Currencies Collection", new Func<(bool success, string details, string error)>(VerifyCurrenciesCollection)),
                    ("Verify USD Currency Exists", new Func<(bool success, string details, string error)>(VerifyUsdCurrency)),
                    ("Verify Brokers Collection", new Func<(bool success, string details, string error)>(VerifyBrokersCollection)),
                    ("Verify IBKR Broker Exists", new Func<(bool success, string details, string error)>(VerifyIbkrBroker)),
                    ("Verify Tastytrade Broker Exists", new Func<(bool success, string details, string error)>(VerifyTastytradeBroker)),
                    ("Verify SigmaTrade Broker Exists", new Func<(bool success, string details, string error)>(VerifySigmaTradeBroker)),
                    ("Verify SPY Ticker Exists", new Func<(bool success, string details, string error)>(VerifySpyTicker)),
                    ("Verify Snapshots Collection", new Func<(bool success, string details, string error)>(VerifySnapshotsCollection))
                };

                foreach (var (stepName, verification) in verificationSteps)
                {
                    if (!await ExecuteVerificationStepAsync(steps, result, stepName, progressCallback, verification))
                        return result;
                }

                // All tests passed!
                result.IsRunning = false;
                result.IsCompleted = true;
                result.AllTestsPassed = true;
                result.OverallStatus = "All tests completed successfully!";
                progressCallback("✅ All Core Platform validation tests passed!");
                _logService.Log("All Core Platform validation tests completed successfully");

                return result;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Unexpected error during test execution: {ex.Message}");
                return await CompleteTestWithError(result, $"Unexpected error: {ex.Message}");
            }
        }

        private async Task<bool> ExecuteStepAsync(List<TestStepResult> steps, OverallTestResult result, string stepName, 
            Action<string> progressCallback, Func<Task<(bool success, string details)>> stepAction)
        {
            var step = new TestStepResult { StepName = stepName };
            steps.Add(step);
            result.Steps = new List<TestStepResult>(steps);

            try
            {
                progressCallback($"Executing {stepName}...");
                _logService.Log($"Executing {stepName}...");

                var (success, details) = await stepAction();

                step.IsCompleted = true;
                step.IsSuccessful = success;
                step.Details = details;
                
                if (success)
                {
                    _logService.Log($"{stepName} completed successfully");
                    return true;
                }
                else
                {
                    step.ErrorMessage = details;
                    _logService.LogError($"{stepName} failed: {details}");
                    await CompleteTestWithError(result, $"{stepName} failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                step.IsCompleted = true;
                step.IsSuccessful = false;
                step.ErrorMessage = ex.Message;
                _logService.LogError($"{stepName} failed: {ex.Message}");
                await CompleteTestWithError(result, $"{stepName} failed");
                return false;
            }
        }

        private async Task<bool> ExecuteVerificationStepAsync(List<TestStepResult> steps, OverallTestResult result, string stepName,
            Action<string> progressCallback, Func<(bool success, string details, string error)> verification)
        {
            var step = new TestStepResult { StepName = stepName };
            steps.Add(step);
            result.Steps = new List<TestStepResult>(steps);

            try
            {
                progressCallback($"Verifying {stepName.Replace("Verify ", "").ToLower()}...");
                _logService.Log($"Executing {stepName}...");

                var (success, details, error) = verification();

                step.IsCompleted = true;
                step.IsSuccessful = success;
                step.Details = details;

                if (success)
                {
                    _logService.Log($"{stepName} verified successfully");
                    return true;
                }
                else
                {
                    step.ErrorMessage = error;
                    _logService.LogError(error);
                    await CompleteTestWithError(result, $"{stepName} failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                step.IsCompleted = true;
                step.IsSuccessful = false;
                step.ErrorMessage = ex.Message;
                _logService.LogError($"{stepName} failed: {ex.Message}");
                await CompleteTestWithError(result, $"{stepName} failed");
                return false;
            }
        }

        // Step action methods
        private Task<(bool success, string details)> InitializePlatformServicesAsync()
        {
            // Verify platform services are available (this is what fails in headless tests)
            var isMainThread = Microsoft.Maui.ApplicationModel.MainThread.IsMainThread;
            var appDataPath = Microsoft.Maui.Storage.FileSystem.AppDataDirectory;
            
            return Task.FromResult((true, $"Platform services available. AppData: {appDataPath}"));
        }

        private async Task<(bool success, string details)> InitializeDatabaseAsync()
        {
            await Overview.InitDatabase();
            return (true, "Database initialization completed");
        }

        private async Task<(bool success, string details)> LoadDataAsync()
        {
            await Overview.LoadData();
            return (true, "Data loading completed");
        }

        // Verification methods
        private (bool success, string details, string error) VerifyDatabaseInitialized()
        {
            var isInitialized = Overview.Data.Value.IsDatabaseInitialized;
            return isInitialized 
                ? (true, "Database initialized: True", "")
                : (false, "", "Database should be initialized but state shows false");
        }

        private (bool success, string details, string error) VerifyDataLoaded()
        {
            var isLoaded = Overview.Data.Value.TransactionsLoaded;
            return isLoaded
                ? (true, "Data loaded: True", "")
                : (false, "", "Data should be loaded but state shows false");
        }

        private (bool success, string details, string error) VerifyCurrenciesCollection()
        {
            var currencyCount = Collections.Currencies.Items.Count;
            return currencyCount > 0
                ? (true, $"Currencies: {currencyCount}", "")
                : (false, "", "Currencies collection should not be empty after LoadData");
        }

        private (bool success, string details, string error) VerifyUsdCurrency()
        {
            var usdExists = Collections.Currencies.Items.Any(c => c.Code == "USD");
            return usdExists
                ? (true, "USD Found: True", "")
                : (false, "", "Should contain USD currency");
        }

        private (bool success, string details, string error) VerifyBrokersCollection()
        {
            var brokerCount = Collections.Brokers.Items.Count;
            return brokerCount >= 3
                ? (true, $"Brokers: {brokerCount}", "")
                : (false, "", $"Expected at least 3 brokers but found {brokerCount}");
        }

        private (bool success, string details, string error) VerifyIbkrBroker()
        {
            var ibkrExists = Collections.Brokers.Items.Any(b => b.Name == "Interactive Brokers");
            return ibkrExists
                ? (true, "IBKR Found: True", "")
                : (false, "", "Should contain IBKR broker (Interactive Brokers)");
        }

        private (bool success, string details, string error) VerifyTastytradeBroker()
        {
            var tastytradeExists = Collections.Brokers.Items.Any(b => b.Name == "Tastytrade");
            return tastytradeExists
                ? (true, "Tastytrade Found: True", "")
                : (false, "", "Should contain Tastytrade broker (Tastytrade)");
        }

        private (bool success, string details, string error) VerifySigmaTradeBroker()
        {
            var sigmaTradeExists = Collections.Brokers.Items.Any(b => b.Name == "Sigma Trade");
            return sigmaTradeExists
                ? (true, "SigmaTrade Found: True", "")
                : (false, "", "Should contain SigmaTrade broker (Sigma Trade)");
        }

        private (bool success, string details, string error) VerifySpyTicker()
        {
            var spyExists = Collections.Tickers.Items.Any(t => t.Symbol == "SPY");
            return spyExists
                ? (true, "SPY Ticker Found: True", "")
                : (false, "", "Should contain SPY ticker");
        }

        private (bool success, string details, string error) VerifySnapshotsCollection()
        {
            var snapshotCount = Collections.Snapshots.Items.Count;
            var emptySnapshotCount = Collections.Snapshots.Items.Count(s => s.Type == Binnaculum.Core.Models.OverviewSnapshotType.Empty);
            
            return (snapshotCount == 1 && emptySnapshotCount == 1)
                ? (true, "Single Empty Snapshot Found: True", "")
                : (false, "", $"Expected exactly 1 Empty snapshot but found {snapshotCount} total snapshots ({emptySnapshotCount} Empty)");
        }

        private Task<OverallTestResult> CompleteTestWithError(OverallTestResult result, string errorMessage)
        {
            result.IsRunning = false;
            result.IsCompleted = true;
            result.AllTestsPassed = false;
            result.OverallStatus = $"Test failed: {errorMessage}";
            return Task.FromResult(result);
        }
    }
}