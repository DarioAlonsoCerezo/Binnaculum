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

        // BrokerAccount creation test specific methods
        private int _tastytradeId = 0;
        private int _brokerAccountId = 0;
        private int _usdCurrencyId = 0;

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

                // Step 0: Wipe all data for fresh test environment
                if (!await ExecuteStepAsync(steps, result, "Wipe All Data for Testing", progressCallback, WipeDataForTestingAsync))
                    return result;

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

        /// <summary>
        /// Execute the BrokerAccount creation test that validates creating a new broker account and verifying snapshot generation
        /// </summary>
        public async Task<OverallTestResult> ExecuteBrokerAccountCreationTestAsync(Action<string> progressCallback)
        {
            var result = new OverallTestResult();
            var steps = new List<TestStepResult>();

            try
            {
                result.IsRunning = true;
                result.OverallStatus = "Running BrokerAccount Creation validation test...";
                progressCallback("Starting BrokerAccount Creation validation test...");

                // Step 0: Wipe all data for fresh test environment
                if (!await ExecuteStepAsync(steps, result, "Wipe All Data for Testing", progressCallback, WipeDataForTestingAsync))
                    return result;

                // Step 1: Initialize MAUI platform services
                if (!await ExecuteStepAsync(steps, result, "Initialize MAUI Platform Services", progressCallback, InitializePlatformServicesAsync))
                    return result;

                // Step 2: Call Overview.InitDatabase() 
                if (!await ExecuteStepAsync(steps, result, "Overview.InitDatabase()", progressCallback, InitializeDatabaseAsync))
                    return result;

                // Step 3: Call Overview.LoadData()
                if (!await ExecuteStepAsync(steps, result, "Overview.LoadData()", progressCallback, LoadDataAsync))
                    return result;

                // Step 4: Wait for reactive collections to populate
                progressCallback("Waiting for reactive collections to populate...");
                _logService.Log("Allowing time for reactive collections to populate...");
                await Task.Delay(300); // Same delay as in original test

                // Step 5: Find Tastytrade Broker
                if (!await ExecuteVerificationStepAsync(steps, result, "Find Tastytrade Broker", progressCallback, FindTastytradeBroker))
                    return result;

                // Step 6: Create BrokerAccount
                if (!await ExecuteStepAsync(steps, result, "Create BrokerAccount", progressCallback, CreateBrokerAccountAsync))
                    return result;

                // Step 7: Verify single snapshot exists
                if (!await ExecuteVerificationStepAsync(steps, result, "Verify Single Snapshot Created", progressCallback, VerifySingleSnapshotExists))
                    return result;

                // Step 8: Verify snapshot is BrokerAccount type
                if (!await ExecuteVerificationStepAsync(steps, result, "Verify Snapshot is BrokerAccount Type", progressCallback, VerifySnapshotIsBrokerAccountType))
                    return result;

                // All tests passed!
                result.IsRunning = false;
                result.IsCompleted = true;
                result.AllTestsPassed = true;
                result.OverallStatus = "All BrokerAccount creation tests completed successfully!";
                progressCallback("✅ All BrokerAccount creation tests passed!");
                _logService.Log("All BrokerAccount creation tests completed successfully");

                return result;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Unexpected error during BrokerAccount creation test: {ex.Message}");
                return await CompleteTestWithError(result, $"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute the BrokerAccount creation + Deposit movement test that validates the complete flow:
        /// 1. Create a BrokerAccount, 2. Add a Deposit movement, 3. Verify snapshot contains correct financial data
        /// </summary>
        public async Task<OverallTestResult> ExecuteBrokerAccountDepositTestAsync(Action<string> progressCallback)
        {
            var result = new OverallTestResult();
            var steps = new List<TestStepResult>();

            try
            {
                result.IsRunning = true;
                result.OverallStatus = "Running BrokerAccount + Deposit validation test...";
                progressCallback("Starting BrokerAccount + Deposit validation test...");

                // Step 0: Wipe all data for fresh test environment
                if (!await ExecuteStepAsync(steps, result, "Wipe All Data for Testing", progressCallback, WipeDataForTestingAsync))
                    return result;

                // Step 1: Initialize MAUI platform services
                if (!await ExecuteStepAsync(steps, result, "Initialize MAUI Platform Services", progressCallback, InitializePlatformServicesAsync))
                    return result;

                // Step 2: Call Overview.InitDatabase() 
                if (!await ExecuteStepAsync(steps, result, "Overview.InitDatabase()", progressCallback, InitializeDatabaseAsync))
                    return result;

                // Step 3: Call Overview.LoadData()
                if (!await ExecuteStepAsync(steps, result, "Overview.LoadData()", progressCallback, LoadDataAsync))
                    return result;

                // Step 4: Wait for reactive collections to populate
                progressCallback("Waiting for reactive collections to populate...");
                _logService.Log("Allowing time for reactive collections to populate...");
                await Task.Delay(300); // Same delay as in original test

                // Step 5: Find Tastytrade Broker
                if (!await ExecuteVerificationStepAsync(steps, result, "Find Tastytrade Broker", progressCallback, FindTastytradeBroker))
                    return result;

                // Step 6: Find USD Currency
                if (!await ExecuteVerificationStepAsync(steps, result, "Find USD Currency", progressCallback, FindUsdCurrency))
                    return result;

                // Step 7: Create BrokerAccount
                if (!await ExecuteStepAsync(steps, result, "Create BrokerAccount", progressCallback, CreateBrokerAccountAsync))
                    return result;

                // Step 8: Find Created BrokerAccount
                if (!await ExecuteVerificationStepAsync(steps, result, "Find Created BrokerAccount", progressCallback, FindCreatedBrokerAccount))
                    return result;

                // Step 9: Create Historical Deposit Movement
                if (!await ExecuteStepAsync(steps, result, "Create Historical Deposit Movement", progressCallback, CreateHistoricalDepositMovementAsync))
                    return result;

                // Step 10: Wait for snapshots to update
                progressCallback("Waiting for snapshots to update...");
                _logService.Log("Allowing time for snapshots to update after movement creation...");
                await Task.Delay(500);

                // Step 11: Verify single snapshot exists
                if (!await ExecuteVerificationStepAsync(steps, result, "Verify Single Snapshot Created", progressCallback, VerifySingleSnapshotExists))
                    return result;

                // Step 12: Verify snapshot is BrokerAccount type
                if (!await ExecuteVerificationStepAsync(steps, result, "Verify Snapshot is BrokerAccount Type", progressCallback, VerifySnapshotIsBrokerAccountType))
                    return result;

                // Step 13: Verify snapshot financial data
                if (!await ExecuteVerificationStepAsync(steps, result, "Verify Snapshot Financial Data", progressCallback, VerifySnapshotFinancialData))
                    return result;

                // All tests passed!
                result.IsRunning = false;
                result.IsCompleted = true;
                result.AllTestsPassed = true;
                result.OverallStatus = "All BrokerAccount + Deposit tests completed successfully!";
                progressCallback("✅ All BrokerAccount + Deposit tests passed!");
                _logService.Log("All BrokerAccount + Deposit tests completed successfully");

                return result;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Unexpected error during BrokerAccount + Deposit test: {ex.Message}");
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
        private async Task<(bool success, string details)> WipeDataForTestingAsync()
        {
            // 🚨 TEST-ONLY: Wipe all data to ensure fresh test environment
            // This prevents data leakage between test runs and ensures consistent, reliable results
            await Overview.WipeAllDataForTesting();
            return (true, "All data wiped for fresh test environment");
        }

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

        private (bool success, string details, string error) FindTastytradeBroker()
        {
            var tastytradeBroker = Collections.Brokers.Items.FirstOrDefault(b => b.Name == "Tastytrade");
            if (tastytradeBroker != null)
            {
                _tastytradeId = tastytradeBroker.Id;
                return (true, $"Tastytrade Broker Found: ID = {_tastytradeId}", "");
            }
            return (false, "", "Tastytrade broker not found in Collections.Brokers.Items");
        }

        private async Task<(bool success, string details)> CreateBrokerAccountAsync()
        {
            if (_tastytradeId == 0)
                return (false, "Tastytrade broker ID is 0, cannot create account");

            await Creator.SaveBrokerAccount(_tastytradeId, "Trading");
            return (true, "BrokerAccount created successfully");
        }

        private (bool success, string details, string error) VerifySingleSnapshotExists()
        {
            var snapshotCount = Collections.Snapshots.Items.Count;
            return snapshotCount == 1
                ? (true, $"Single Snapshot Found: Count = {snapshotCount}", "")
                : (false, "", $"Expected exactly 1 snapshot but found {snapshotCount}");
        }

        private (bool success, string details, string error) VerifySnapshotIsBrokerAccountType()
        {
            if (Collections.Snapshots.Items.Count == 0)
                return (false, "", "No snapshots found to verify type");

            var snapshot = Collections.Snapshots.Items.First();
            var isBrokerAccount = snapshot.Type == Binnaculum.Core.Models.OverviewSnapshotType.BrokerAccount;
            return isBrokerAccount
                ? (true, "Snapshot Type: BrokerAccount", "")
                : (false, "", $"Expected BrokerAccount snapshot type but found {snapshot.Type}");
        }

        private (bool success, string details, string error) FindUsdCurrency()
        {
            var usdCurrency = Collections.Currencies.Items.FirstOrDefault(c => c.Code == "USD");
            if (usdCurrency != null)
            {
                _usdCurrencyId = usdCurrency.Id;
                return (true, $"USD Currency Found: ID = {_usdCurrencyId}", "");
            }
            return (false, "", "USD currency not found in Collections.Currencies.Items");
        }

        private (bool success, string details, string error) FindCreatedBrokerAccount()
        {
            var brokerAccount = Collections.Accounts.Items
                .Where(a => a.Type == Binnaculum.Core.Models.AccountType.BrokerAccount)
                .FirstOrDefault(a => a.Broker != null && a.Broker.Value.Broker.Id == _tastytradeId);
            
            if (brokerAccount?.Broker != null)
            {
                _brokerAccountId = brokerAccount.Broker.Value.Id;
                return (true, $"BrokerAccount Found: ID = {_brokerAccountId}", "");
            }
            return (false, "", "Created BrokerAccount not found in Collections.Accounts.Items");
        }

        private async Task<(bool success, string details)> CreateHistoricalDepositMovementAsync()
        {
            if (_brokerAccountId == 0)
                return (false, "BrokerAccount ID is 0, cannot create movement");
            
            if (_usdCurrencyId == 0)
                return (false, "USD Currency ID is 0, cannot create movement");

            // Get the actual BrokerAccount and Currency objects
            var brokerAccount = Collections.Accounts.Items
                .Where(a => a.Type == Binnaculum.Core.Models.AccountType.BrokerAccount)
                .FirstOrDefault(a => a.Broker != null && a.Broker.Value.Id == _brokerAccountId)?.Broker?.Value;
            
            var usdCurrency = Collections.Currencies.Items.FirstOrDefault(c => c.Id == _usdCurrencyId);
            
            if (brokerAccount == null)
                return (false, "Could not find BrokerAccount object for movement creation");
            
            if (usdCurrency == null)
                return (false, "Could not find USD Currency object for movement creation");

            // Create a historical deposit (2 months ago)
            var historicalDate = DateTime.Now.AddMonths(-2);
            
            var depositMovement = new Binnaculum.Core.Models.BrokerMovement(
                id: 0,  // Will be assigned by database 
                timeStamp: historicalDate,
                amount: 1200.0m,
                currency: usdCurrency,
                brokerAccount: brokerAccount,
                commissions: 0.0m,
                fees: 0.0m,
                movementType: Binnaculum.Core.Models.BrokerMovementType.Deposit,
                notes: Microsoft.FSharp.Core.FSharpOption<string>.Some("Historical deposit test"),
                fromCurrency: Microsoft.FSharp.Core.FSharpOption<Binnaculum.Core.Models.Currency>.None,
                amountChanged: Microsoft.FSharp.Core.FSharpOption<decimal>.None,
                ticker: Microsoft.FSharp.Core.FSharpOption<Binnaculum.Core.Models.Ticker>.None,
                quantity: Microsoft.FSharp.Core.FSharpOption<decimal>.None
            );

            await Creator.SaveBrokerMovement(depositMovement);
            return (true, $"Historical Deposit Movement Created: $1200 USD on {historicalDate:yyyy-MM-dd}");
        }

        private (bool success, string details, string error) VerifySnapshotFinancialData()
        {
            if (Collections.Snapshots.Items.Count == 0)
                return (false, "", "No snapshots found to verify financial data");

            var snapshot = Collections.Snapshots.Items.First();
            if (snapshot.BrokerAccount == null)
                return (false, "", "Snapshot does not contain BrokerAccount data");

            var brokerAccountSnapshot = snapshot.BrokerAccount.Value;
            var financial = brokerAccountSnapshot.Financial;
            
            // Verify the key financial data from the deposit
            if (financial.Deposited != 1200.0m)
                return (false, "", $"Expected Deposited = 1200 but found {financial.Deposited}");
            
            if (financial.MovementCounter != 1)
                return (false, "", $"Expected MovementCounter = 1 but found {financial.MovementCounter}");
            
            if (financial.Currency.Code != "USD")
                return (false, "", $"Expected Currency = USD but found {financial.Currency.Code}");

            // Verify portfolio value equals deposited amount (no investments)
            if (brokerAccountSnapshot.PortfolioValue != 1200.0m)
                return (false, "", $"Expected PortfolioValue = 1200 but found {brokerAccountSnapshot.PortfolioValue}");

            return (true, "Financial Data: Deposited=1200, MovementCounter=1, Currency=USD, PortfolioValue=1200", "");
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