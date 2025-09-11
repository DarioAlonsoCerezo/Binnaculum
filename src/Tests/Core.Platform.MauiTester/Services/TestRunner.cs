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
                var platformStep = new TestStepResult { StepName = "Initialize MAUI Platform Services" };
                steps.Add(platformStep);
                result.Steps = new List<TestStepResult>(steps);

                try
                {
                    progressCallback("Initializing MAUI platform services...");
                    _logService.Log("Checking MAUI platform services availability...");
                    
                    // Verify platform services are available (this is what fails in headless tests)
                    var isMainThread = Microsoft.Maui.ApplicationModel.MainThread.IsMainThread;
                    var appDataPath = Microsoft.Maui.Storage.FileSystem.AppDataDirectory;
                    
                    platformStep.IsCompleted = true;
                    platformStep.IsSuccessful = true;
                    platformStep.Details = $"Platform services available. AppData: {appDataPath}";
                    _logService.Log($"MAUI platform services initialized successfully. AppData directory: {appDataPath}");
                }
                catch (Exception ex)
                {
                    platformStep.IsCompleted = true;
                    platformStep.IsSuccessful = false;
                    platformStep.ErrorMessage = ex.Message;
                    _logService.LogError($"Platform initialization failed: {ex.Message}");
                    return await CompleteTestWithError(result, "Platform services not available");
                }

                // Step 2: Call Overview.InitDatabase() 
                var initDbStep = new TestStepResult { StepName = "Overview.InitDatabase()" };
                steps.Add(initDbStep);
                result.Steps = new List<TestStepResult>(steps);

                try
                {
                    progressCallback("Calling Overview.InitDatabase()...");
                    _logService.Log("Executing Overview.InitDatabase()...");
                    
                    await Overview.InitDatabase();
                    
                    initDbStep.IsCompleted = true;
                    initDbStep.IsSuccessful = true;
                    initDbStep.Details = "Database initialization completed";
                    _logService.Log("Overview.InitDatabase() completed successfully");
                }
                catch (Exception ex)
                {
                    initDbStep.IsCompleted = true;
                    initDbStep.IsSuccessful = false;
                    initDbStep.ErrorMessage = ex.Message;
                    _logService.LogError($"InitDatabase failed: {ex.Message}");
                    return await CompleteTestWithError(result, "Database initialization failed");
                }

                // Step 3: Call Overview.LoadData()
                var loadDataStep = new TestStepResult { StepName = "Overview.LoadData()" };
                steps.Add(loadDataStep);
                result.Steps = new List<TestStepResult>(steps);

                try
                {
                    progressCallback("Calling Overview.LoadData()...");
                    _logService.Log("Executing Overview.LoadData()...");
                    
                    await Overview.LoadData();
                    
                    loadDataStep.IsCompleted = true;
                    loadDataStep.IsSuccessful = true;
                    loadDataStep.Details = "Data loading completed";
                    _logService.Log("Overview.LoadData() completed successfully");
                }
                catch (Exception ex)
                {
                    loadDataStep.IsCompleted = true;
                    loadDataStep.IsSuccessful = false;
                    loadDataStep.ErrorMessage = ex.Message;
                    _logService.LogError($"LoadData failed: {ex.Message}");
                    return await CompleteTestWithError(result, "Data loading failed");
                }

                // Step 4: Verify database initialization state
                var verifyInitStep = new TestStepResult { StepName = "Verify Database Initialized" };
                steps.Add(verifyInitStep);
                result.Steps = new List<TestStepResult>(steps);

                try
                {
                    progressCallback("Verifying database state...");
                    _logService.Log("Checking Overview.Data.Value.IsDatabaseInitialized...");
                    
                    var isInitialized = Overview.Data.Value.IsDatabaseInitialized;
                    
                    if (isInitialized)
                    {
                        verifyInitStep.IsCompleted = true;
                        verifyInitStep.IsSuccessful = true;
                        verifyInitStep.Details = "Database initialized: True";
                        _logService.Log("Database initialization verified successfully");
                    }
                    else
                    {
                        verifyInitStep.IsCompleted = true;
                        verifyInitStep.IsSuccessful = false;
                        verifyInitStep.ErrorMessage = "Database should be initialized but state shows false";
                        _logService.LogError("Database initialization state verification failed");
                        return await CompleteTestWithError(result, "Database state verification failed");
                    }
                }
                catch (Exception ex)
                {
                    verifyInitStep.IsCompleted = true;
                    verifyInitStep.IsSuccessful = false;
                    verifyInitStep.ErrorMessage = ex.Message;
                    _logService.LogError($"Database state verification failed: {ex.Message}");
                    return await CompleteTestWithError(result, "Database state verification failed");
                }

                // Step 5: Verify data loading state
                var verifyLoadStep = new TestStepResult { StepName = "Verify Data Loaded" };
                steps.Add(verifyLoadStep);
                result.Steps = new List<TestStepResult>(steps);

                try
                {
                    progressCallback("Verifying data loading state...");
                    _logService.Log("Checking Overview.Data.Value.TransactionsLoaded...");
                    
                    var isLoaded = Overview.Data.Value.TransactionsLoaded;
                    
                    if (isLoaded)
                    {
                        verifyLoadStep.IsCompleted = true;
                        verifyLoadStep.IsSuccessful = true;
                        verifyLoadStep.Details = "Data loaded: True";
                        _logService.Log("Data loading state verified successfully");
                    }
                    else
                    {
                        verifyLoadStep.IsCompleted = true;
                        verifyLoadStep.IsSuccessful = false;
                        verifyLoadStep.ErrorMessage = "Data should be loaded but state shows false";
                        _logService.LogError("Data loading state verification failed");
                        return await CompleteTestWithError(result, "Data loading state verification failed");
                    }
                }
                catch (Exception ex)
                {
                    verifyLoadStep.IsCompleted = true;
                    verifyLoadStep.IsSuccessful = false;
                    verifyLoadStep.ErrorMessage = ex.Message;
                    _logService.LogError($"Data loading state verification failed: {ex.Message}");
                    return await CompleteTestWithError(result, "Data loading state verification failed");
                }

                // Step 6: Wait for reactive collections to populate
                progressCallback("Waiting for reactive collections to populate...");
                _logService.Log("Allowing time for reactive collections to populate...");
                await Task.Delay(300); // Same delay as in original test

                // Step 7: Verify currencies collection
                var verifyCurrenciesStep = new TestStepResult { StepName = "Verify Currencies Collection" };
                steps.Add(verifyCurrenciesStep);
                result.Steps = new List<TestStepResult>(steps);

                try
                {
                    progressCallback("Verifying currencies collection...");
                    _logService.Log("Checking Collections.Currencies.Items.Count...");
                    
                    var currencyCount = Collections.Currencies.Items.Count;
                    
                    if (currencyCount > 0)
                    {
                        verifyCurrenciesStep.IsCompleted = true;
                        verifyCurrenciesStep.IsSuccessful = true;
                        verifyCurrenciesStep.Details = $"Currencies: {currencyCount}";
                        _logService.Log($"Currencies collection verified: {currencyCount} items");
                    }
                    else
                    {
                        verifyCurrenciesStep.IsCompleted = true;
                        verifyCurrenciesStep.IsSuccessful = false;
                        verifyCurrenciesStep.ErrorMessage = "Currencies collection should not be empty after LoadData";
                        _logService.LogError("Currencies collection is empty");
                        return await CompleteTestWithError(result, "Currencies collection verification failed");
                    }
                }
                catch (Exception ex)
                {
                    verifyCurrenciesStep.IsCompleted = true;
                    verifyCurrenciesStep.IsSuccessful = false;
                    verifyCurrenciesStep.ErrorMessage = ex.Message;
                    _logService.LogError($"Currencies collection verification failed: {ex.Message}");
                    return await CompleteTestWithError(result, "Currencies collection verification failed");
                }

                // Step 8: Verify USD currency exists
                var verifyUsdStep = new TestStepResult { StepName = "Verify USD Currency Exists" };
                steps.Add(verifyUsdStep);
                result.Steps = new List<TestStepResult>(steps);

                try
                {
                    progressCallback("Verifying USD currency exists...");
                    _logService.Log("Checking for USD currency in Collections.Currencies.Items...");
                    
                    var usdExists = Collections.Currencies.Items.Any(c => c.Code == "USD");
                    
                    if (usdExists)
                    {
                        verifyUsdStep.IsCompleted = true;
                        verifyUsdStep.IsSuccessful = true;
                        verifyUsdStep.Details = "USD Found: True";
                        _logService.Log("USD currency found successfully");
                    }
                    else
                    {
                        verifyUsdStep.IsCompleted = true;
                        verifyUsdStep.IsSuccessful = false;
                        verifyUsdStep.ErrorMessage = "Should contain USD currency";
                        _logService.LogError("USD currency not found in collection");
                        return await CompleteTestWithError(result, "USD currency verification failed");
                    }
                }
                catch (Exception ex)
                {
                    verifyUsdStep.IsCompleted = true;
                    verifyUsdStep.IsSuccessful = false;
                    verifyUsdStep.ErrorMessage = ex.Message;
                    _logService.LogError($"USD currency verification failed: {ex.Message}");
                    return await CompleteTestWithError(result, "USD currency verification failed");
                }

                // Step 9: Verify brokers collection
                var verifyBrokersStep = new TestStepResult { StepName = "Verify Brokers Collection" };
                steps.Add(verifyBrokersStep);
                result.Steps = new List<TestStepResult>(steps);

                try
                {
                    progressCallback("Verifying brokers collection...");
                    _logService.Log("Checking Collections.Brokers.Items.Count...");
                    
                    var brokerCount = Collections.Brokers.Items.Count;
                    
                    if (brokerCount >= 3)
                    {
                        verifyBrokersStep.IsCompleted = true;
                        verifyBrokersStep.IsSuccessful = true;
                        verifyBrokersStep.Details = $"Brokers: {brokerCount}";
                        _logService.Log($"Brokers collection verified: {brokerCount} items");
                    }
                    else
                    {
                        verifyBrokersStep.IsCompleted = true;
                        verifyBrokersStep.IsSuccessful = false;
                        verifyBrokersStep.ErrorMessage = $"Expected at least 3 brokers but found {brokerCount}";
                        _logService.LogError($"Brokers collection has insufficient items: {brokerCount}");
                        return await CompleteTestWithError(result, "Brokers collection verification failed");
                    }
                }
                catch (Exception ex)
                {
                    verifyBrokersStep.IsCompleted = true;
                    verifyBrokersStep.IsSuccessful = false;
                    verifyBrokersStep.ErrorMessage = ex.Message;
                    _logService.LogError($"Brokers collection verification failed: {ex.Message}");
                    return await CompleteTestWithError(result, "Brokers collection verification failed");
                }

                // Step 10: Verify IBKR broker exists
                var verifyIbkrStep = new TestStepResult { StepName = "Verify IBKR Broker Exists" };
                steps.Add(verifyIbkrStep);
                result.Steps = new List<TestStepResult>(steps);

                try
                {
                    progressCallback("Verifying IBKR broker exists...");
                    _logService.Log("Checking for IBKR broker in Collections.Brokers.Items...");
                    
                    var ibkrExists = Collections.Brokers.Items.Any(b => b.Name == "Interactive Brokers");
                    
                    if (ibkrExists)
                    {
                        verifyIbkrStep.IsCompleted = true;
                        verifyIbkrStep.IsSuccessful = true;
                        verifyIbkrStep.Details = "IBKR Found: True";
                        _logService.Log("IBKR broker found successfully");
                    }
                    else
                    {
                        verifyIbkrStep.IsCompleted = true;
                        verifyIbkrStep.IsSuccessful = false;
                        verifyIbkrStep.ErrorMessage = "Should contain IBKR broker (Interactive Brokers)";
                        _logService.LogError("IBKR broker not found in collection");
                        return await CompleteTestWithError(result, "IBKR broker verification failed");
                    }
                }
                catch (Exception ex)
                {
                    verifyIbkrStep.IsCompleted = true;
                    verifyIbkrStep.IsSuccessful = false;
                    verifyIbkrStep.ErrorMessage = ex.Message;
                    _logService.LogError($"IBKR broker verification failed: {ex.Message}");
                    return await CompleteTestWithError(result, "IBKR broker verification failed");
                }

                // Step 11: Verify Tastytrade broker exists
                var verifyTastytradeStep = new TestStepResult { StepName = "Verify Tastytrade Broker Exists" };
                steps.Add(verifyTastytradeStep);
                result.Steps = new List<TestStepResult>(steps);

                try
                {
                    progressCallback("Verifying Tastytrade broker exists...");
                    _logService.Log("Checking for Tastytrade broker in Collections.Brokers.Items...");
                    
                    var tastytradeExists = Collections.Brokers.Items.Any(b => b.Name == "Tastytrade");
                    
                    if (tastytradeExists)
                    {
                        verifyTastytradeStep.IsCompleted = true;
                        verifyTastytradeStep.IsSuccessful = true;
                        verifyTastytradeStep.Details = "Tastytrade Found: True";
                        _logService.Log("Tastytrade broker found successfully");
                    }
                    else
                    {
                        verifyTastytradeStep.IsCompleted = true;
                        verifyTastytradeStep.IsSuccessful = false;
                        verifyTastytradeStep.ErrorMessage = "Should contain Tastytrade broker (Tastytrade)";
                        _logService.LogError("Tastytrade broker not found in collection");
                        return await CompleteTestWithError(result, "Tastytrade broker verification failed");
                    }
                }
                catch (Exception ex)
                {
                    verifyTastytradeStep.IsCompleted = true;
                    verifyTastytradeStep.IsSuccessful = false;
                    verifyTastytradeStep.ErrorMessage = ex.Message;
                    _logService.LogError($"Tastytrade broker verification failed: {ex.Message}");
                    return await CompleteTestWithError(result, "Tastytrade broker verification failed");
                }

                // Step 12: Verify SigmaTrade broker exists
                var verifySigmaTradeStep = new TestStepResult { StepName = "Verify SigmaTrade Broker Exists" };
                steps.Add(verifySigmaTradeStep);
                result.Steps = new List<TestStepResult>(steps);

                try
                {
                    progressCallback("Verifying SigmaTrade broker exists...");
                    _logService.Log("Checking for SigmaTrade broker in Collections.Brokers.Items...");
                    
                    var sigmaTradeExists = Collections.Brokers.Items.Any(b => b.Name == "Sigma Trade");
                    
                    if (sigmaTradeExists)
                    {
                        verifySigmaTradeStep.IsCompleted = true;
                        verifySigmaTradeStep.IsSuccessful = true;
                        verifySigmaTradeStep.Details = "SigmaTrade Found: True";
                        _logService.Log("SigmaTrade broker found successfully");
                    }
                    else
                    {
                        verifySigmaTradeStep.IsCompleted = true;
                        verifySigmaTradeStep.IsSuccessful = false;
                        verifySigmaTradeStep.ErrorMessage = "Should contain SigmaTrade broker (Sigma Trade)";
                        _logService.LogError("SigmaTrade broker not found in collection");
                        return await CompleteTestWithError(result, "SigmaTrade broker verification failed");
                    }
                }
                catch (Exception ex)
                {
                    verifySigmaTradeStep.IsCompleted = true;
                    verifySigmaTradeStep.IsSuccessful = false;
                    verifySigmaTradeStep.ErrorMessage = ex.Message;
                    _logService.LogError($"SigmaTrade broker verification failed: {ex.Message}");
                    return await CompleteTestWithError(result, "SigmaTrade broker verification failed");
                }

                // Step 13: Verify tickers collection contains SPY
                var verifySpyTickerStep = new TestStepResult { StepName = "Verify SPY Ticker Exists" };
                steps.Add(verifySpyTickerStep);
                result.Steps = new List<TestStepResult>(steps);

                try
                {
                    progressCallback("Verifying SPY ticker exists...");
                    _logService.Log("Checking for SPY ticker in Collections.Tickers.Items...");
                    
                    var spyExists = Collections.Tickers.Items.Any(t => t.Symbol == "SPY");
                    
                    if (spyExists)
                    {
                        verifySpyTickerStep.IsCompleted = true;
                        verifySpyTickerStep.IsSuccessful = true;
                        verifySpyTickerStep.Details = "SPY Ticker Found: True";
                        _logService.Log("SPY ticker found successfully");
                    }
                    else
                    {
                        verifySpyTickerStep.IsCompleted = true;
                        verifySpyTickerStep.IsSuccessful = false;
                        verifySpyTickerStep.ErrorMessage = "Should contain SPY ticker";
                        _logService.LogError("SPY ticker not found in collection");
                        return await CompleteTestWithError(result, "SPY ticker verification failed");
                    }
                }
                catch (Exception ex)
                {
                    verifySpyTickerStep.IsCompleted = true;
                    verifySpyTickerStep.IsSuccessful = false;
                    verifySpyTickerStep.ErrorMessage = ex.Message;
                    _logService.LogError($"SPY ticker verification failed: {ex.Message}");
                    return await CompleteTestWithError(result, "SPY ticker verification failed");
                }

                // Step 14: Verify snapshots collection contains exactly one Empty snapshot
                var verifySnapshotsStep = new TestStepResult { StepName = "Verify Snapshots Collection" };
                steps.Add(verifySnapshotsStep);
                result.Steps = new List<TestStepResult>(steps);

                try
                {
                    progressCallback("Verifying snapshots collection...");
                    _logService.Log("Checking Collections.Snapshots.Items for single Empty snapshot...");
                    
                    var snapshotCount = Collections.Snapshots.Items.Count;
                    var emptySnapshotCount = Collections.Snapshots.Items.Count(s => s.Type == Binnaculum.Core.Models.OverviewSnapshotType.Empty);
                    
                    if (snapshotCount == 1 && emptySnapshotCount == 1)
                    {
                        verifySnapshotsStep.IsCompleted = true;
                        verifySnapshotsStep.IsSuccessful = true;
                        verifySnapshotsStep.Details = "Single Empty Snapshot Found: True";
                        _logService.Log("Snapshots collection verified: exactly 1 Empty snapshot");
                    }
                    else
                    {
                        verifySnapshotsStep.IsCompleted = true;
                        verifySnapshotsStep.IsSuccessful = false;
                        verifySnapshotsStep.ErrorMessage = $"Expected exactly 1 Empty snapshot but found {snapshotCount} total snapshots ({emptySnapshotCount} Empty)";
                        _logService.LogError($"Snapshots collection verification failed: {snapshotCount} total, {emptySnapshotCount} Empty");
                        return await CompleteTestWithError(result, "Snapshots collection verification failed");
                    }
                }
                catch (Exception ex)
                {
                    verifySnapshotsStep.IsCompleted = true;
                    verifySnapshotsStep.IsSuccessful = false;
                    verifySnapshotsStep.ErrorMessage = ex.Message;
                    _logService.LogError($"Snapshots collection verification failed: {ex.Message}");
                    return await CompleteTestWithError(result, "Snapshots collection verification failed");
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