using Core.Platform.MauiTester.Models;
using Core.Platform.MauiTester.Services;

namespace Core.Platform.MauiTester
{
    public partial class MainPage : ContentPage
    {
        private readonly TestRunner _testRunner;
        private readonly LogService _logService;
        private OverallTestResult? _currentResult;

        public MainPage(TestRunner testRunner, LogService logService)
        {
            InitializeComponent();
            _testRunner = testRunner;
            _logService = logService;

            // Initialize with a default result
            _currentResult = new OverallTestResult();
            BindingContext = _currentResult;
        }

        private async void OnRunOverviewReactiveTestClicked(object? sender, EventArgs e)
        {
            await ExecuteTestAsync((callback) => _testRunner.ExecuteScenarioByNameAsync("Overview Reactive Validation", callback), "Run Overview Reactive Test", "Overview Reactive");
        }

        private async void OnRunBrokerAccountReactiveTestClicked(object? sender, EventArgs e)
        {
            await ExecuteTestAsync((callback) => _testRunner.ExecuteScenarioByNameAsync("BrokerAccount Creation Reactive Validation", callback), "Run BrokerAccount Creation Reactive Test", "BrokerAccount Creation Reactive");
        }

        private async void OnRunBrokerAccountDepositReactiveTestClicked(object? sender, EventArgs e)
        {
            await ExecuteTestAsync((callback) => _testRunner.ExecuteScenarioByNameAsync("BrokerAccount + Deposit Reactive Validation", callback), "Run BrokerAccount + Deposit Reactive Test", "BrokerAccount + Deposit Reactive");
        }

        private async void OnRunBrokerAccountMultipleMovementsSignalBasedTestClicked(object? sender, EventArgs e)
        {
            await ExecuteTestAsync((callback) => _testRunner.ExecuteScenarioByNameAsync("BrokerAccount Multiple Movements Signal-Based Validation", callback), "Run BrokerAccount Multiple Movements Signal-Based Test", "BrokerAccount Multiple Movements Signal-Based");
        }

        private async void OnRunOptionsImportIntegrationSignalBasedTestClicked(object? sender, EventArgs e)
        {
            await ExecuteTestAsync((callback) => _testRunner.ExecuteScenarioByNameAsync("Options Import Integration Signal-Based Validation", callback), "Run Options Import Integration Signal-Based Test", "Options Import Integration Signal-Based Validation");
        }

        private async void OnRunDepositsWithdrawalsIntegrationTestClicked(object? sender, EventArgs e)
        {
            await ExecuteTestAsync((callback) => _testRunner.ExecuteScenarioByNameAsync("Deposits & Withdrawals Integration Test", callback), "Run Deposits & Withdrawals Integration Test", "Deposits & Withdrawals Integration Test");
        }

        private async void OnTastytradeImportIntegrationTestClicked(object? sender, EventArgs e)
        {
            await ExecuteTestAsync((callback) => _testRunner.ExecuteScenarioByNameAsync("Tastytrade Import Integration Test", callback), "Execute Tastytrade Import Test", "Tastytrade Import Integration Test");
        }

        private async void OnTsllImportIntegrationTestClicked(object? sender, EventArgs e)
        {
            await ExecuteTestAsync((callback) => _testRunner.ExecuteScenarioByNameAsync("Reactive TSLL Multi-Asset Import Integration Test", callback), "Execute Reactive TSLL Import Test", "Reactive TSLL Multi-Asset Import Integration Test");
        }

        private async void OnRunPfizerImportIntegrationTestClicked(object? sender, EventArgs e)
        {
            await ExecuteTestAsync((callback) => _testRunner.ExecuteScenarioByNameAsync("Pfizer Import Integration Test", callback), "Execute Pfizer Import Test", "Pfizer Import Integration Test");
        }

        private async Task ExecuteTestAsync(Func<Action<string>, Task<OverallTestResult>> testMethod, string buttonText, string testName)
        {
            try
            {
                _logService.Clear();
                _logService.Log($"Starting {testName} validation test...");

                // Create new test result and bind to UI
                _currentResult = new OverallTestResult();
                BindingContext = _currentResult;

                // Set test as running to disable all buttons
                _currentResult.IsRunning = true;

                // Update UI to show test is running
                SetAllButtonsToRunningText();

                await Task.Delay(100); // Allow UI to update

                // Execute the test with progress updates
                var result = await testMethod(UpdateProgressStatus);

                // Update the UI with final results
                _currentResult = result;
                BindingContext = _currentResult;

                // Reset button text
                ResetAllButtonsToOriginalText();

                _logService.Log($"{testName} test execution completed. Overall result: {result.AllTestsPassed}");
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error running {testName} test: {ex.Message}");

                // Show error in UI
                if (_currentResult != null)
                {
                    _currentResult.IsRunning = false;
                    _currentResult.IsCompleted = true;
                    _currentResult.AllTestsPassed = false;
                    _currentResult.OverallStatus = $"Error: {ex.Message}";
                }

                // Reset button text
                ResetAllButtonsToOriginalText();

                await DisplayAlert("Test Error", $"An error occurred while running the {testName} test:\n\n{ex.Message}", "OK");
            }
        }

        private void UpdateProgressStatus(string status)
        {
            // Update the status label with current progress
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_currentResult != null)
                {
                    _currentResult.OverallStatus = status;
                }
            });
        }

        private async void OnViewDetailsClicked(object? sender, EventArgs e)
        {
            if (_currentResult?.Steps == null)
                return;

            var details = string.Join("\n\n", _currentResult.Steps.Select(step =>
                $"{step.StatusIcon} {step.StepName}\n" +
                $"Details: {step.Details}\n" +
                (string.IsNullOrEmpty(step.ErrorMessage) ? "" : $"Error: {step.ErrorMessage}\n")));

            details += "\n\n=== Full Log ===\n" + _logService.GetFullLog();

            await DisplayAlert("Test Details", details, "OK");
        }

        private void OnClearResultsClicked(object? sender, EventArgs e)
        {
            _logService.Clear();
            _currentResult = new OverallTestResult();
            BindingContext = _currentResult;
            ResetAllButtonsToOriginalText();
        }

        private void SetAllButtonsToRunningText()
        {
            RunOverviewReactiveTestButton.Text = "Running Test...";
            RunBrokerAccountReactiveTestButton.Text = "Running Test...";
            RunBrokerAccountDepositReactiveTestButton.Text = "Running Test...";
            RunBrokerAccountMultipleMovementsSignalBasedTestButton.Text = "Running Test...";
            RunOptionsImportIntegrationSignalBasedTestButton.Text = "Running Test...";
            RunDepositsWithdrawalsIntegrationTestButton.Text = "Running Test...";
            RunPfizerImportIntegrationTestButton.Text = "Running Test...";
            RunTsllImportIntegrationTestButton.Text = "Running Test...";
        }

        private void ResetAllButtonsToOriginalText()
        {
            RunOverviewReactiveTestButton.Text = "Run Overview Test";
            RunBrokerAccountReactiveTestButton.Text = "Run BrokerAccount Creation Test";
            RunBrokerAccountDepositReactiveTestButton.Text = "Run BrokerAccount + Deposit Test";
            RunBrokerAccountMultipleMovementsSignalBasedTestButton.Text = "Run BrokerAccount Multiple Movements Test";
            RunOptionsImportIntegrationSignalBasedTestButton.Text = "Run Options Import Test";
            RunDepositsWithdrawalsIntegrationTestButton.Text = "Run Money Movements Test";
            RunPfizerImportIntegrationTestButton.Text = "Run Pfizer Import Test";
            RunTsllImportIntegrationTestButton.Text = "Run TSLL Import Test";
        }
    }
}
