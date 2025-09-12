using Binnaculum.Core.UI;
using Core.Platform.MauiTester.Models;

namespace Core.Platform.MauiTester.Services
{
    /// <summary>
    /// Service for executing Core Platform validation tests that recreate the logic from PublicApiIntegrationTests.fs
    /// This class has been modernized with fluent API support, enhanced reporting, and test discovery capabilities
    /// </summary>
    public class TestRunner
    {
        #region Private Fields and Dependencies
        
        private readonly LogService _logService;
        private readonly TestDiscoveryService _discoveryService;

        // BrokerAccount creation test specific state
        private int _tastytradeId = 0;
        private int _brokerAccountId = 0;
        private int _usdCurrencyId = 0;

        #endregion

        #region Step Name Constants
        private const string STEP_WIPE_DATA = "Wipe All Data for Testing";
        private const string STEP_INIT_PLATFORM = "Initialize MAUI Platform Services";
        private const string STEP_INIT_DATABASE = "Overview.InitDatabase()";
        private const string STEP_LOAD_DATA = "Overview.LoadData()";
        private const string STEP_VERIFY_DB_INIT = "Verify Database Initialized";
        private const string STEP_VERIFY_DATA_LOADED = "Verify Data Loaded";
        private const string STEP_VERIFY_CURRENCIES = "Verify Currencies Collection";
        private const string STEP_VERIFY_USD = "Verify USD Currency Exists";
        private const string STEP_VERIFY_BROKERS = "Verify Brokers Collection";
        private const string STEP_VERIFY_IBKR = "Verify IBKR Broker Exists";
        private const string STEP_VERIFY_TASTYTRADE = "Verify Tastytrade Broker Exists";
        private const string STEP_VERIFY_SIGMATRADE = "Verify SigmaTrade Broker Exists";
        private const string STEP_VERIFY_SPY = "Verify SPY Ticker Exists";
        private const string STEP_VERIFY_SNAPSHOTS = "Verify Snapshots Collection";
        private const string STEP_FIND_TASTYTRADE = "Find Tastytrade Broker";
        private const string STEP_FIND_USD = "Find USD Currency";
        private const string STEP_CREATE_BROKER_ACCOUNT = "Create BrokerAccount";
        private const string STEP_FIND_BROKER_ACCOUNT = "Find Created BrokerAccount";
        private const string STEP_VERIFY_SINGLE_SNAPSHOT = "Verify Single Snapshot Created";
        private const string STEP_VERIFY_SNAPSHOT_TYPE = "Verify Snapshot is BrokerAccount Type";
        private const string STEP_CREATE_DEPOSIT = "Create Historical Deposit Movement";
        private const string STEP_VERIFY_FINANCIAL_DATA = "Verify Snapshot Financial Data";
        #endregion

        #region Constructors and Initialization

        public TestRunner(LogService logService)
        {
            _logService = logService;
            _discoveryService = new TestDiscoveryService();
            RegisterBuiltInTests();
        }

        /// <summary>
        /// Register the built-in test scenarios with the discovery service
        /// </summary>
        private void RegisterBuiltInTests()
        {
            // Overview Test - Basic platform validation
            _discoveryService.RegisterTest(() => TestScenarioBuilder.Create()
                .Named("Overview Platform Validation")
                .WithDescription("Validates Overview.InitDatabase() and Overview.LoadData() work in MAUI environment")
                .WithTags(TestTags.Overview, TestTags.Database, TestTags.Collection, TestTags.Smoke)
                .AddCommonSetup(this)
                .AddDelay("Wait for reactive collections", TimeSpan.FromMilliseconds(300))
                .AddVerificationStep("Verify Database Initialized", TestVerifications.VerifyDatabaseInitialized)
                .AddVerificationStep("Verify Data Loaded", TestVerifications.VerifyDataLoaded)
                .AddVerificationStep("Verify Currencies Collection", TestVerifications.VerifyCurrenciesCollection)
                .AddVerificationStep("Verify USD Currency", TestVerifications.VerifyUsdCurrency)
                .AddVerificationStep("Verify Brokers Collection", TestVerifications.VerifyBrokersCollection)
                .AddVerificationStep("Verify IBKR Broker", TestVerifications.VerifyIbkrBroker)
                .AddVerificationStep("Verify Tastytrade Broker", TestVerifications.VerifyTastytradeBroker)
                .AddVerificationStep("Verify Sigma Trade Broker", TestVerifications.VerifySigmaTradeBroker)
                .AddVerificationStep("Verify SPY Ticker", TestVerifications.VerifySpyTicker)
                .AddVerificationStep("Verify Snapshots Collection", TestVerifications.VerifySnapshotsCollection));

            // BrokerAccount Creation Test
            _discoveryService.RegisterTest(() => TestScenarioBuilder.Create()
                .Named("BrokerAccount Creation")
                .WithDescription("Validates creating a new broker account and verifying snapshot generation")
                .WithTags(TestTags.BrokerAccount, TestTags.Financial, TestTags.Integration)
                .AddCommonSetup(this)
                .AddDelay("Wait for reactive collections", TimeSpan.FromMilliseconds(300))
                .AddVerificationStep("Find Tastytrade Broker", () => {
                    var (success, details, error, id) = TestVerifications.FindTastytradeBroker();
                    if (success) _tastytradeId = id;
                    return (success, details, error);
                })
                .AddAsyncStep("Create BrokerAccount", () => CreateBrokerAccountAsync("Trading"))
                .AddVerificationStep("Verify Single Snapshot", TestVerifications.VerifySingleSnapshotExists)
                .AddVerificationStep("Verify Snapshot Type", TestVerifications.VerifySnapshotIsBrokerAccountType));
        }

        /// <summary>
        /// Get the discovery service for accessing registered tests
        /// </summary>
        public TestDiscoveryService Discovery => _discoveryService;

        #endregion

        #region Common Setup Steps
        private static readonly (string stepName, Func<TestRunner, Task<(bool success, string details)>> action)[] CommonSetupSteps = new (string, Func<TestRunner, Task<(bool success, string details)>>)[]
        {
            (STEP_WIPE_DATA, (runner) => runner.WipeDataForTestingAsync()),
            (STEP_INIT_PLATFORM, (runner) => runner.InitializePlatformServicesAsync()),
            (STEP_INIT_DATABASE, (runner) => runner.InitializeDatabaseAsync()),
            (STEP_LOAD_DATA, (runner) => runner.LoadDataAsync())
        };
        #endregion

        #region Unified Step Execution and Scenario Runner

        /// <summary>
        /// Execute a test scenario using the modern fluent API
        /// </summary>
        public async Task<OverallTestResult> ExecuteScenarioAsync(TestScenario scenario, Action<string> progressCallback)
        {
            var result = new OverallTestResult();
            var steps = new List<TestStepResult>();

            try
            {
                result.MarkStarted(scenario.Name);
                result.Tags = new List<string>(scenario.Tags);
                result.OverallStatus = $"Running {scenario.Name}...";
                progressCallback($"Starting {scenario.Name}...");
                _logService.Log($"Starting test scenario: {scenario.Name}");

                foreach (var testStep in scenario.Steps)
                {
                    if (!await ExecuteStep(steps, result, progressCallback, testStep))
                    {
                        result.MarkCompleted(false, $"Test failed at step: {testStep.StepName}");
                        return result;
                    }
                }

                // All tests passed!
                result.MarkCompleted(true, $"All {scenario.Steps.Count} steps completed successfully");
                result.OverallStatus = $"{scenario.Name} completed successfully!";
                progressCallback($"✅ {scenario.Name} passed!");
                _logService.Log($"{scenario.Name} completed successfully");

                return result;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Unexpected error during {scenario.Name}: {ex.Message}");
                result.MarkCompleted(false, $"Unexpected error: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Execute a registered test scenario by name
        /// </summary>
        public async Task<OverallTestResult> ExecuteScenarioByNameAsync(string scenarioName, Action<string> progressCallback)
        {
            var scenario = _discoveryService.GetTestByName(scenarioName);
            if (scenario == null)
            {
                var result = new OverallTestResult();
                result.MarkCompleted(false, $"Test scenario '{scenarioName}' not found");
                return result;
            }

            return await ExecuteScenarioAsync(scenario, progressCallback);
        }
        /// <summary>
        /// Executes a test step using the TestStep abstraction and handles common success/failure logic with enhanced reporting
        /// </summary>
        private async Task<bool> ExecuteStep(List<TestStepResult> steps, OverallTestResult result, 
            Action<string> progressCallback, TestStep testStep)
        {
            var step = new TestStepResult { StepName = testStep.StepName };
            step.MarkStarted();
            steps.Add(step);
            result.Steps = new List<TestStepResult>(steps);

            try
            {
                progressCallback($"Executing {testStep.StepName}...");
                _logService.Log($"Executing {testStep.StepName}...");

                var (success, details, error) = await testStep.ExecuteAsync();

                step.MarkCompleted(success);
                step.Details = details;
                
                if (success)
                {
                    _logService.Log($"{testStep.StepName} completed successfully in {step.DurationText}");
                    return true;
                }
                else
                {
                    step.ErrorMessage = error ?? details;
                    _logService.LogError($"{testStep.StepName} failed in {step.DurationText}: {error ?? details}");
                    await CompleteTestWithError(result, $"{testStep.StepName} failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                step.MarkCompleted(false);
                step.ErrorMessage = ex.Message;
                _logService.LogError($"{testStep.StepName} failed in {step.DurationText}: {ex.Message}");
                await CompleteTestWithError(result, $"{testStep.StepName} failed");
                return false;
            }
        }

        /// <summary>
        /// Backward compatibility: Executes a step with generic return type
        /// </summary>
        [Obsolete("Use ExecuteStep with TestStep abstraction instead")]
        private Task<bool> ExecuteStepAsync<T>(List<TestStepResult> steps, OverallTestResult result, string stepName, 
            Action<string> progressCallback, Func<T> stepAction) where T : struct
        {
            return ExecuteStep(steps, result, progressCallback, new GenericSyncTestStep<T>(stepName, stepAction));
        }

        /// <summary>
        /// Backward compatibility: Executes an async step
        /// </summary>
        [Obsolete("Use ExecuteStep with TestStep abstraction instead")]
        private Task<bool> ExecuteStepAsync(List<TestStepResult> steps, OverallTestResult result, string stepName, 
            Action<string> progressCallback, Func<Task<(bool success, string details)>> stepAction)
        {
            return ExecuteStep(steps, result, progressCallback, new AsyncTestStep(stepName, stepAction));
        }

        /// <summary>
        /// Backward compatibility: Executes a verification step
        /// </summary>
        [Obsolete("Use ExecuteStep with TestStep abstraction instead")]
        private Task<bool> ExecuteVerificationStepAsync(List<TestStepResult> steps, OverallTestResult result, string stepName,
            Action<string> progressCallback, Func<(bool success, string details, string error)> verification)
        {
            return ExecuteStep(steps, result, progressCallback, new VerificationTestStep(stepName, verification));
        }

        /// <summary>
        /// Executes common setup steps for all tests using the TestStep abstraction
        /// </summary>
        private async Task<bool> ExecuteCommonSetupAsync(List<TestStepResult> steps, OverallTestResult result, Action<string> progressCallback)
        {
            foreach (var (stepName, action) in CommonSetupSteps)
            {
                var testStep = new AsyncTestStep(stepName, () => action(this));
                if (!await ExecuteStep(steps, result, progressCallback, testStep))
                    return false;
            }
            return true;
        }
        #endregion

        #region Public Helper Methods for TestScenarioBuilder

        // Note: The helper methods (WipeDataForTestingAsync, InitializePlatformServicesAsync, etc.) 
        // are defined as public in the "Step Action Methods" region below for use by TestScenarioBuilder

        #endregion

        #region Legacy Test Methods (Preserved for Backward Compatibility)

        /// <summary>
        /// Execute the main test that validates Overview.InitDatabase() and Overview.LoadData() work in MAUI environment
        /// This recreates the exact logic from PublicApiIntegrationTests.fs
        /// </summary>
        public async Task<OverallTestResult> ExecuteOverviewTestAsync(Action<string> progressCallback)
        {
            return await ExecuteScenarioByNameAsync("Overview Platform Validation", progressCallback);
        }

        /// <summary>
        /// Execute the BrokerAccount creation test that validates creating a new broker account and verifying snapshot generation
        /// </summary>
        public async Task<OverallTestResult> ExecuteBrokerAccountCreationTestAsync(Action<string> progressCallback)
        {
            return await ExecuteScenarioByNameAsync("BrokerAccount Creation", progressCallback);
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

                // Execute common setup steps
                if (!await ExecuteCommonSetupAsync(steps, result, progressCallback))
                    return result;

                // Step 4: Wait for reactive collections to populate
                progressCallback("Waiting for reactive collections to populate...");
                _logService.Log("Allowing time for reactive collections to populate...");
                await Task.Delay(300);

                // Step 5: Find Tastytrade Broker
                var findTastytradeStep = new VerificationTestStep(STEP_FIND_TASTYTRADE, FindTastytradeBroker);
                if (!await ExecuteStep(steps, result, progressCallback, findTastytradeStep))
                    return result;

                // Step 6: Find USD Currency
                var findUsdStep = new VerificationTestStep(STEP_FIND_USD, FindUsdCurrency);
                if (!await ExecuteStep(steps, result, progressCallback, findUsdStep))
                    return result;

                // Step 7: Create BrokerAccount
                var createAccountStep = new AsyncTestStep(STEP_CREATE_BROKER_ACCOUNT, () => CreateBrokerAccountAsync("Trading"));
                if (!await ExecuteStep(steps, result, progressCallback, createAccountStep))
                    return result;

                // Step 8: Find Created BrokerAccount
                var findAccountStep = new VerificationTestStep(STEP_FIND_BROKER_ACCOUNT, FindCreatedBrokerAccount);
                if (!await ExecuteStep(steps, result, progressCallback, findAccountStep))
                    return result;

                // Step 9: Create Historical Deposit Movement
                var createDepositStep = new AsyncTestStep(STEP_CREATE_DEPOSIT, () => CreateMovementAsync(1200m, Binnaculum.Core.Models.BrokerMovementType.Deposit, -60, "Historical deposit test"));
                if (!await ExecuteStep(steps, result, progressCallback, createDepositStep))
                    return result;

                // Step 10: Wait for snapshots to update
                progressCallback("Waiting for snapshots to update...");
                _logService.Log("Allowing time for snapshots to update after movement creation...");
                await Task.Delay(500);

                // Step 11: Verify single snapshot exists
                var verifySingleSnapshotStep = new VerificationTestStep(STEP_VERIFY_SINGLE_SNAPSHOT, VerifySingleSnapshotExists);
                if (!await ExecuteStep(steps, result, progressCallback, verifySingleSnapshotStep))
                    return result;

                // Step 12: Verify snapshot is BrokerAccount type
                var verifySnapshotTypeStep = new VerificationTestStep(STEP_VERIFY_SNAPSHOT_TYPE, VerifySnapshotIsBrokerAccountType);
                if (!await ExecuteStep(steps, result, progressCallback, verifySnapshotTypeStep))
                    return result;

                // Step 13: Verify snapshot financial data
                var verifyFinancialDataStep = new VerificationTestStep(STEP_VERIFY_FINANCIAL_DATA, VerifySnapshotFinancialData);
                if (!await ExecuteStep(steps, result, progressCallback, verifyFinancialDataStep))
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

        /// <summary>
        /// Execute the BrokerAccount Multiple Movements test that validates creating a BrokerAccount and adding multiple deposit/withdrawal movements
        /// This test ensures the snapshot logic and financial calculations are robust when handling a sequence of deposits and withdrawals
        /// </summary>
        public async Task<OverallTestResult> ExecuteBrokerAccountMultipleMovementsTestAsync(Action<string> progressCallback)
        {
            var result = new OverallTestResult();
            var steps = new List<TestStepResult>();

            try
            {
                result.IsRunning = true;
                result.OverallStatus = "Running BrokerAccount Multiple Movements validation test...";
                progressCallback("Starting BrokerAccount Multiple Movements validation test...");

                // Execute common setup steps
                if (!await ExecuteCommonSetupAsync(steps, result, progressCallback))
                    return result;

                // Step 4: Wait for reactive collections to populate
                progressCallback("Waiting for reactive collections to populate...");
                _logService.Log("Allowing time for reactive collections to populate...");
                await Task.Delay(300);

                // Step 5: Find Tastytrade Broker
                var findTastytradeStep = new VerificationTestStep(STEP_FIND_TASTYTRADE, FindTastytradeBroker);
                if (!await ExecuteStep(steps, result, progressCallback, findTastytradeStep))
                    return result;

                // Step 6: Find USD Currency
                var findUsdStep = new VerificationTestStep(STEP_FIND_USD, FindUsdCurrency);
                if (!await ExecuteStep(steps, result, progressCallback, findUsdStep))
                    return result;

                // Step 7: Create BrokerAccount for Tastytrade broker named 'Testing'
                var createAccountStep = new AsyncTestStep("Create BrokerAccount for Tastytrade", () => CreateBrokerAccountAsync("Testing"));
                if (!await ExecuteStep(steps, result, progressCallback, createAccountStep))
                    return result;

                // Step 8: Find Created BrokerAccount
                var findAccountStep = new VerificationTestStep(STEP_FIND_BROKER_ACCOUNT, FindCreatedBrokerAccount);
                if (!await ExecuteStep(steps, result, progressCallback, findAccountStep))
                    return result;

                // Step 9-12: Create multiple historical movements
                var movements = new[]
                {
                    (1200m, Binnaculum.Core.Models.BrokerMovementType.Deposit, -60, "Create Historical Deposit ($1200, 60 days ago)"),
                    (300m, Binnaculum.Core.Models.BrokerMovementType.Withdrawal, -55, "Create Historical Withdrawal ($300, 55 days ago)"),
                    (300m, Binnaculum.Core.Models.BrokerMovementType.Withdrawal, -50, "Create Historical Withdrawal ($300, 50 days ago)"),
                    (600m, Binnaculum.Core.Models.BrokerMovementType.Deposit, -10, "Create Historical Deposit ($600, 10 days ago)")
                };

                foreach (var (amount, movementType, daysOffset, stepName) in movements)
                {
                    var movementStep = new AsyncTestStep(stepName, () => CreateMovementAsync(amount, movementType, daysOffset));
                    if (!await ExecuteStep(steps, result, progressCallback, movementStep))
                        return result;
                }

                // Step 13: Verify single snapshot exists
                var verifySingleSnapshotStep = new VerificationTestStep(STEP_VERIFY_SINGLE_SNAPSHOT, VerifySingleSnapshotExists);
                if (!await ExecuteStep(steps, result, progressCallback, verifySingleSnapshotStep))
                    return result;

                // Step 14: Verify snapshot is BrokerAccount type
                var verifySnapshotTypeStep = new VerificationTestStep(STEP_VERIFY_SNAPSHOT_TYPE, VerifySnapshotIsBrokerAccountType);
                if (!await ExecuteStep(steps, result, progressCallback, verifySnapshotTypeStep))
                    return result;

                // Step 15: Verify snapshot financial data - MovementCounter=4, Deposited according to requirements, Currency=USD
                var verifyFinancialDataStep = new VerificationTestStep("Verify Snapshot Financial Data (Multiple Movements)", VerifyMultipleMovementsFinancialData);
                if (!await ExecuteStep(steps, result, progressCallback, verifyFinancialDataStep))
                    return result;

                // All tests passed!
                result.IsRunning = false;
                result.IsCompleted = true;
                result.AllTestsPassed = true;
                result.OverallStatus = "All BrokerAccount Multiple Movements tests completed successfully!";
                progressCallback("✅ All BrokerAccount Multiple Movements tests passed!");
                _logService.Log("All BrokerAccount Multiple Movements tests completed successfully");

                return result;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Unexpected error during BrokerAccount Multiple Movements test: {ex.Message}");
                return await CompleteTestWithError(result, $"Unexpected error: {ex.Message}");
            }
        }

        #endregion

        #region Step Action Methods (Public for TestScenarioBuilder)
        
        /// <summary>
        /// Wipe all data to ensure fresh test environment (used by TestScenarioBuilder)
        /// </summary>
        public async Task<(bool success, string details)> WipeDataForTestingAsync()
        {
            // 🚨 TEST-ONLY: Wipe all data to ensure fresh test environment
            // This prevents data leakage between test runs and ensures consistent, reliable results
            await Overview.WipeAllDataForTesting();
            return (true, "All data wiped for fresh test environment");
        }

        /// <summary>
        /// Initialize platform services (used by TestScenarioBuilder)
        /// </summary>
        public Task<(bool success, string details)> InitializePlatformServicesAsync() =>
            Task.FromResult((true, $"Platform services available. AppData: {Microsoft.Maui.Storage.FileSystem.AppDataDirectory}"));

        /// <summary>
        /// Initialize database (used by TestScenarioBuilder)
        /// </summary>
        public async Task<(bool success, string details)> InitializeDatabaseAsync()
        {
            await Overview.InitDatabase();
            return (true, "Database initialization completed");
        }

        /// <summary>
        /// Load data from database (used by TestScenarioBuilder)
        /// </summary>
        public async Task<(bool success, string details)> LoadDataAsync()
        {
            await Overview.LoadData();
            return (true, "Data loading completed");
        }

        /// <summary>
        /// Creates a BrokerAccount with the specified name
        /// </summary>
        private async Task<(bool success, string details)> CreateBrokerAccountAsync(string accountName)
        {
            if (_tastytradeId == 0)
                return (false, "Tastytrade broker ID is 0, cannot create account");

            await Creator.SaveBrokerAccount(_tastytradeId, accountName);
            return (true, $"BrokerAccount named '{accountName}' created successfully");
        }

        /// <summary>
        /// Creates a movement with specified parameters and date offset
        /// </summary>
        private async Task<(bool success, string details)> CreateMovementAsync(decimal amount, 
            Binnaculum.Core.Models.BrokerMovementType movementType, int daysOffset, string? description = null)
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

            // Create movement with specified date offset
            var movementDate = DateTime.Now.AddDays(daysOffset);
            var notes = description ?? $"Historical {movementType.ToString().ToLower()} test movement";
            
            var movement = new Binnaculum.Core.Models.BrokerMovement(
                id: 0,  // Will be assigned by database 
                timeStamp: movementDate,
                amount: amount,
                currency: usdCurrency,
                brokerAccount: brokerAccount,
                commissions: 0.0m,
                fees: 0.0m,
                movementType: movementType,
                notes: Microsoft.FSharp.Core.FSharpOption<string>.Some(notes),
                fromCurrency: Microsoft.FSharp.Core.FSharpOption<Binnaculum.Core.Models.Currency>.None,
                amountChanged: Microsoft.FSharp.Core.FSharpOption<decimal>.None,
                ticker: Microsoft.FSharp.Core.FSharpOption<Binnaculum.Core.Models.Ticker>.None,
                quantity: Microsoft.FSharp.Core.FSharpOption<decimal>.None
            );

            await Creator.SaveBrokerMovement(movement);
            
            // Wait a bit after each movement to ensure snapshot calculation
            await Task.Delay(350);
            
            return (true, $"Historical {movementType} Movement Created: ${amount} USD on {movementDate:yyyy-MM-dd}");
        }
        #endregion

        #region Database and Data Verification Methods
        private (bool success, string details, string error) VerifyDatabaseInitialized() =>
            Overview.Data.Value.IsDatabaseInitialized
                ? (true, "Database initialized: True", "")
                : (false, "", "Database should be initialized but state shows false");

        private (bool success, string details, string error) VerifyDataLoaded() =>
            Overview.Data.Value.TransactionsLoaded
                ? (true, "Data loaded: True", "")
                : (false, "", "Data should be loaded but state shows false");
        #endregion

        #region Collection Verification Methods
        private (bool success, string details, string error) VerifyCurrenciesCollection()
        {
            var currencyCount = Collections.Currencies.Items.Count;
            return currencyCount > 0
                ? (true, $"Currencies: {currencyCount}", "")
                : (false, "", "Currencies collection should not be empty after LoadData");
        }

        private (bool success, string details, string error) VerifyUsdCurrency() =>
            Collections.Currencies.Items.Any(c => c.Code == "USD")
                ? (true, "USD Found: True", "")
                : (false, "", "Should contain USD currency");

        private (bool success, string details, string error) VerifyBrokersCollection()
        {
            var brokerCount = Collections.Brokers.Items.Count;
            return brokerCount >= 3
                ? (true, $"Brokers: {brokerCount}", "")
                : (false, "", $"Expected at least 3 brokers but found {brokerCount}");
        }

        private (bool success, string details, string error) VerifyIbkrBroker() =>
            Collections.Brokers.Items.Any(b => b.Name == "Interactive Brokers")
                ? (true, "IBKR Found: True", "")
                : (false, "", "Should contain IBKR broker (Interactive Brokers)");

        private (bool success, string details, string error) VerifyTastytradeBroker() =>
            Collections.Brokers.Items.Any(b => b.Name == "Tastytrade")
                ? (true, "Tastytrade Found: True", "")
                : (false, "", "Should contain Tastytrade broker (Tastytrade)");

        private (bool success, string details, string error) VerifySigmaTradeBroker() =>
            Collections.Brokers.Items.Any(b => b.Name == "Sigma Trade")
                ? (true, "SigmaTrade Found: True", "")
                : (false, "", "Should contain SigmaTrade broker (Sigma Trade)");

        private (bool success, string details, string error) VerifySpyTicker() =>
            Collections.Tickers.Items.Any(t => t.Symbol == "SPY")
                ? (true, "SPY Ticker Found: True", "")
                : (false, "", "Should contain SPY ticker");

        private (bool success, string details, string error) VerifySnapshotsCollection()
        {
            var snapshotCount = Collections.Snapshots.Items.Count;
            var emptySnapshotCount = Collections.Snapshots.Items.Count(s => s.Type == Binnaculum.Core.Models.OverviewSnapshotType.Empty);
            
            return (snapshotCount == 1 && emptySnapshotCount == 1)
                ? (true, "Single Empty Snapshot Found: True", "")
                : (false, "", $"Expected exactly 1 Empty snapshot but found {snapshotCount} total snapshots ({emptySnapshotCount} Empty)");
        }
        #endregion

        #region BrokerAccount Test Verification Methods
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
        #endregion

        #region Financial Data Verification Methods
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

            return (true, "Financial Data: Deposited=1200, MovementCounter=1, Currency=USD", "");
        }

        private (bool success, string details, string error) VerifyMultipleMovementsFinancialData()
        {
            if (Collections.Snapshots.Items.Count == 0)
                return (false, "", "No snapshots found to verify financial data");

            var snapshot = Collections.Snapshots.Items.First();
            if (snapshot.BrokerAccount == null)
                return (false, "", "Snapshot does not contain BrokerAccount data");

            var brokerAccountSnapshot = snapshot.BrokerAccount.Value;
            var financial = brokerAccountSnapshot.Financial;
            
            // Verify the key financial data from multiple movements
            if (financial.MovementCounter != 4)
                return (false, "", $"Expected MovementCounter = 4 but found {financial.MovementCounter}");
            
            // Calculate expected values based on test requirements
            var actualDeposited = financial.Deposited;
            var actualWithdrawn = financial.Withdrawn;
            var netAmount = actualDeposited - actualWithdrawn;
            
            var expectedTotalDeposited = 1800.0m; // 1200 + 600
            var expectedTotalWithdrawn = 600.0m;  // 300 + 300
            var expectedNetDeposited = 1200.0m;   // 1800 - 600
            
            if (actualDeposited != expectedTotalDeposited)
                return (false, "", $"Expected Total Deposited = {expectedTotalDeposited} but found {actualDeposited}");
            
            if (actualWithdrawn != expectedTotalWithdrawn)
                return (false, "", $"Expected Total Withdrawn = {expectedTotalWithdrawn} but found {actualWithdrawn}");
            
            if (netAmount != expectedNetDeposited)
                return (false, "", $"Expected Net Deposited = {expectedNetDeposited} but found {netAmount}");
            
            if (financial.Currency.Code != "USD")
                return (false, "", $"Expected Currency = USD but found {financial.Currency.Code}");

            return (true, $"Financial Data: TotalDeposited={actualDeposited}, TotalWithdrawn={actualWithdrawn}, NetDeposited={netAmount}, MovementCounter={financial.MovementCounter}, Currency=USD", "");
        }
        #endregion

        #region Helper Methods
        private Task<OverallTestResult> CompleteTestWithError(OverallTestResult result, string errorMessage)
        {
            result.IsRunning = false;
            result.IsCompleted = true;
            result.AllTestsPassed = false;
            result.OverallStatus = $"Test failed: {errorMessage}";
            return Task.FromResult(result);
        }
        #endregion
    }
}