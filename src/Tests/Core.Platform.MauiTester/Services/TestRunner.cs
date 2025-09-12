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

        public TestRunner(LogService logService)
        {
            _logService = logService;
        }

        #region Common Setup Steps
        private static readonly (string stepName, Func<TestRunner, Task<(bool success, string details)>> action)[] CommonSetupSteps = new (string, Func<TestRunner, Task<(bool success, string details)>>)[]
        {
            (STEP_WIPE_DATA, (runner) => runner.WipeDataForTestingAsync()),
            (STEP_INIT_PLATFORM, (runner) => runner.InitializePlatformServicesAsync()),
            (STEP_INIT_DATABASE, (runner) => runner.InitializeDatabaseAsync()),
            (STEP_LOAD_DATA, (runner) => runner.LoadDataAsync())
        };
        #endregion

        #region Generalized Step Execution
        /// <summary>
        /// Executes a step with the given action and handles common success/failure logic
        /// </summary>
        private async Task<bool> ExecuteStepAsync<T>(List<TestStepResult> steps, OverallTestResult result, string stepName, 
            Action<string> progressCallback, Func<T> stepAction) where T : struct
        {
            // Handle both (bool, string) and (bool, string, string) return types
            var step = new TestStepResult { StepName = stepName };
            steps.Add(step);
            result.Steps = new List<TestStepResult>(steps);

            try
            {
                progressCallback($"Executing {stepName}...");
                _logService.Log($"Executing {stepName}...");

                var actionResult = stepAction();
                bool success;
                string details;
                string? error = null;

                // Handle different return types
                if (actionResult is ValueTuple<bool, string> basicResult)
                {
                    (success, details) = basicResult;
                }
                else if (actionResult is ValueTuple<bool, string, string> verificationResult)
                {
                    (success, details, error) = verificationResult;
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported return type: {typeof(T)}");
                }

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
                    step.ErrorMessage = error ?? details;
                    _logService.LogError($"{stepName} failed: {error ?? details}");
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

        /// <summary>
        /// Executes an async step with the given action
        /// </summary>
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

        /// <summary>
        /// Executes a verification step
        /// </summary>
        private Task<bool> ExecuteVerificationStepAsync(List<TestStepResult> steps, OverallTestResult result, string stepName,
            Action<string> progressCallback, Func<(bool success, string details, string error)> verification)
        {
            return ExecuteStepAsync(steps, result, stepName, progressCallback, () => verification());
        }

        /// <summary>
        /// Executes common setup steps for all tests
        /// </summary>
        private async Task<bool> ExecuteCommonSetupAsync(List<TestStepResult> steps, OverallTestResult result, Action<string> progressCallback)
        {
            foreach (var (stepName, action) in CommonSetupSteps)
            {
                if (!await ExecuteStepAsync(steps, result, stepName, progressCallback, () => action(this)))
                    return false;
            }
            return true;
        }
        #endregion

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

                // Execute common setup steps
                if (!await ExecuteCommonSetupAsync(steps, result, progressCallback))
                    return result;

                // Step 4: Verify database initialization state
                if (!await ExecuteVerificationStepAsync(steps, result, STEP_VERIFY_DB_INIT, progressCallback, VerifyDatabaseInitialized))
                    return result;

                // Step 5: Verify data loading state
                if (!await ExecuteVerificationStepAsync(steps, result, STEP_VERIFY_DATA_LOADED, progressCallback, VerifyDataLoaded))
                    return result;

                // Step 6: Wait for reactive collections to populate
                progressCallback("Waiting for reactive collections to populate...");
                _logService.Log("Allowing time for reactive collections to populate...");
                await Task.Delay(300); // Same delay as in original test

                // Step 7-14: Collection and data verifications
                var verificationSteps = new[]
                {
                    (STEP_VERIFY_CURRENCIES, new Func<(bool success, string details, string error)>(VerifyCurrenciesCollection)),
                    (STEP_VERIFY_USD, new Func<(bool success, string details, string error)>(VerifyUsdCurrency)),
                    (STEP_VERIFY_BROKERS, new Func<(bool success, string details, string error)>(VerifyBrokersCollection)),
                    (STEP_VERIFY_IBKR, new Func<(bool success, string details, string error)>(VerifyIbkrBroker)),
                    (STEP_VERIFY_TASTYTRADE, new Func<(bool success, string details, string error)>(VerifyTastytradeBroker)),
                    (STEP_VERIFY_SIGMATRADE, new Func<(bool success, string details, string error)>(VerifySigmaTradeBroker)),
                    (STEP_VERIFY_SPY, new Func<(bool success, string details, string error)>(VerifySpyTicker)),
                    (STEP_VERIFY_SNAPSHOTS, new Func<(bool success, string details, string error)>(VerifySnapshotsCollection))
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

                // Execute common setup steps
                if (!await ExecuteCommonSetupAsync(steps, result, progressCallback))
                    return result;

                // Step 4: Wait for reactive collections to populate
                progressCallback("Waiting for reactive collections to populate...");
                _logService.Log("Allowing time for reactive collections to populate...");
                await Task.Delay(300); // Same delay as in original test

                // Step 5: Find Tastytrade Broker
                if (!await ExecuteVerificationStepAsync(steps, result, STEP_FIND_TASTYTRADE, progressCallback, FindTastytradeBroker))
                    return result;

                // Step 6: Create BrokerAccount
                if (!await ExecuteStepAsync(steps, result, STEP_CREATE_BROKER_ACCOUNT, progressCallback, () => CreateBrokerAccountAsync("Trading")))
                    return result;

                // Step 7: Verify single snapshot exists
                if (!await ExecuteVerificationStepAsync(steps, result, STEP_VERIFY_SINGLE_SNAPSHOT, progressCallback, VerifySingleSnapshotExists))
                    return result;

                // Step 8: Verify snapshot is BrokerAccount type
                if (!await ExecuteVerificationStepAsync(steps, result, STEP_VERIFY_SNAPSHOT_TYPE, progressCallback, VerifySnapshotIsBrokerAccountType))
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

                // Execute common setup steps
                if (!await ExecuteCommonSetupAsync(steps, result, progressCallback))
                    return result;

                // Step 4: Wait for reactive collections to populate
                progressCallback("Waiting for reactive collections to populate...");
                _logService.Log("Allowing time for reactive collections to populate...");
                await Task.Delay(300);

                // Step 5: Find Tastytrade Broker
                if (!await ExecuteVerificationStepAsync(steps, result, STEP_FIND_TASTYTRADE, progressCallback, FindTastytradeBroker))
                    return result;

                // Step 6: Find USD Currency
                if (!await ExecuteVerificationStepAsync(steps, result, STEP_FIND_USD, progressCallback, FindUsdCurrency))
                    return result;

                // Step 7: Create BrokerAccount
                if (!await ExecuteStepAsync(steps, result, STEP_CREATE_BROKER_ACCOUNT, progressCallback, () => CreateBrokerAccountAsync("Trading")))
                    return result;

                // Step 8: Find Created BrokerAccount
                if (!await ExecuteVerificationStepAsync(steps, result, STEP_FIND_BROKER_ACCOUNT, progressCallback, FindCreatedBrokerAccount))
                    return result;

                // Step 9: Create Historical Deposit Movement
                if (!await ExecuteStepAsync(steps, result, STEP_CREATE_DEPOSIT, progressCallback, () => CreateMovementAsync(1200m, Binnaculum.Core.Models.BrokerMovementType.Deposit, -60, "Historical deposit test")))
                    return result;

                // Step 10: Wait for snapshots to update
                progressCallback("Waiting for snapshots to update...");
                _logService.Log("Allowing time for snapshots to update after movement creation...");
                await Task.Delay(500);

                // Step 11: Verify single snapshot exists
                if (!await ExecuteVerificationStepAsync(steps, result, STEP_VERIFY_SINGLE_SNAPSHOT, progressCallback, VerifySingleSnapshotExists))
                    return result;

                // Step 12: Verify snapshot is BrokerAccount type
                if (!await ExecuteVerificationStepAsync(steps, result, STEP_VERIFY_SNAPSHOT_TYPE, progressCallback, VerifySnapshotIsBrokerAccountType))
                    return result;

                // Step 13: Verify snapshot financial data
                if (!await ExecuteVerificationStepAsync(steps, result, STEP_VERIFY_FINANCIAL_DATA, progressCallback, VerifySnapshotFinancialData))
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
                if (!await ExecuteVerificationStepAsync(steps, result, STEP_FIND_TASTYTRADE, progressCallback, FindTastytradeBroker))
                    return result;

                // Step 6: Find USD Currency
                if (!await ExecuteVerificationStepAsync(steps, result, STEP_FIND_USD, progressCallback, FindUsdCurrency))
                    return result;

                // Step 7: Create BrokerAccount for Tastytrade broker named 'Testing'
                if (!await ExecuteStepAsync(steps, result, "Create BrokerAccount for Tastytrade", progressCallback, () => CreateBrokerAccountAsync("Testing")))
                    return result;

                // Step 8: Find Created BrokerAccount
                if (!await ExecuteVerificationStepAsync(steps, result, STEP_FIND_BROKER_ACCOUNT, progressCallback, FindCreatedBrokerAccount))
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
                    if (!await ExecuteStepAsync(steps, result, stepName, progressCallback, () => CreateMovementAsync(amount, movementType, daysOffset)))
                        return result;
                }

                // Step 13: Verify single snapshot exists
                if (!await ExecuteVerificationStepAsync(steps, result, STEP_VERIFY_SINGLE_SNAPSHOT, progressCallback, VerifySingleSnapshotExists))
                    return result;

                // Step 14: Verify snapshot is BrokerAccount type
                if (!await ExecuteVerificationStepAsync(steps, result, STEP_VERIFY_SNAPSHOT_TYPE, progressCallback, VerifySnapshotIsBrokerAccountType))
                    return result;

                // Step 15: Verify snapshot financial data - MovementCounter=4, Deposited according to requirements, Currency=USD
                if (!await ExecuteVerificationStepAsync(steps, result, "Verify Snapshot Financial Data (Multiple Movements)", progressCallback, VerifyMultipleMovementsFinancialData))
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

        #region Step Action Methods
        private async Task<(bool success, string details)> WipeDataForTestingAsync()
        {
            // 🚨 TEST-ONLY: Wipe all data to ensure fresh test environment
            // This prevents data leakage between test runs and ensures consistent, reliable results
            await Overview.WipeAllDataForTesting();
            return (true, "All data wiped for fresh test environment");
        }

        private Task<(bool success, string details)> InitializePlatformServicesAsync() =>
            Task.FromResult((true, $"Platform services available. AppData: {Microsoft.Maui.Storage.FileSystem.AppDataDirectory}"));

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