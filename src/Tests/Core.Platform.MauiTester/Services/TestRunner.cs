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