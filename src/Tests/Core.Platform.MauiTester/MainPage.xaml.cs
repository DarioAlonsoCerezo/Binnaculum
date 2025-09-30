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

        private async void OnRunTestClicked(object? sender, EventArgs e)
        {
            await ExecuteTestAsync((callback) => _testRunner.ExecuteScenarioByNameAsync("Overview Platform Validation", callback), "Run Overview Test", "Overview");
        }

        private async void OnRunOverviewReactiveTestClicked(object? sender, EventArgs e)
        {
            await ExecuteTestAsync((callback) => _testRunner.ExecuteScenarioByNameAsync("Overview Reactive Validation", callback), "Run Overview Reactive Test", "Overview Reactive");
        }

        private async void OnRunBrokerAccountTestClicked(object? sender, EventArgs e)
        {
            await ExecuteTestAsync((callback) => _testRunner.ExecuteScenarioByNameAsync("BrokerAccount Creation", callback), "Run BrokerAccount Creation Test", "BrokerAccount Creation");
        }

        private async void OnRunBrokerAccountReactiveTestClicked(object? sender, EventArgs e)
        {
            await ExecuteTestAsync((callback) => _testRunner.ExecuteScenarioByNameAsync("BrokerAccount Creation Reactive Validation", callback), "Run BrokerAccount Creation Reactive Test", "BrokerAccount Creation Reactive");
        }

        private async void OnRunBrokerAccountDepositTestClicked(object? sender, EventArgs e)
        {
            await ExecuteTestAsync((callback) => _testRunner.ExecuteScenarioByNameAsync("BrokerAccount + Deposit", callback), "Run BrokerAccount + Deposit Test", "BrokerAccount + Deposit");
        }

        private async void OnRunBrokerAccountMultipleMovementsTestClicked(object? sender, EventArgs e)
        {
            await ExecuteTestAsync((callback) => _testRunner.ExecuteScenarioByNameAsync("BrokerAccount Multiple Movements", callback), "Run BrokerAccount Multiple Movements Test", "BrokerAccount Multiple Movements");
        }

        private async void OnRunOptionsImportIntegrationTestClicked(object? sender, EventArgs e)
        {
            await ExecuteTestAsync((callback) => _testRunner.ExecuteScenarioByNameAsync("Options Import Integration Test", callback), "Run Options Import Integration Test", "Options Import Integration Test");
        }

        private async void OnRunTastytradeImportIntegrationTestClicked(object? sender, EventArgs e)
        {
            await ExecuteTestAsync((callback) => _testRunner.ExecuteScenarioByNameAsync("Tastytrade Import Integration Test", callback), "Execute Tastytrade Import Test", "Tastytrade Import Integration Test");
        }

        private async void OnRunTsllImportIntegrationTestClicked(object? sender, EventArgs e)
        {
            await ExecuteTestAsync((callback) => _testRunner.ExecuteScenarioByNameAsync("TSLL Import Integration Test", callback), "Execute TSLL Import Test", "TSLL Import Integration Test");
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
                RunTestButton.Text = "Running Test...";
                RunOverviewReactiveTestButton.Text = "Running Test...";
                RunBrokerAccountTestButton.Text = "Running Test...";
                RunBrokerAccountReactiveTestButton.Text = "Running Test...";
                RunBrokerAccountDepositTestButton.Text = "Running Test...";
                RunBrokerAccountMultipleMovementsTestButton.Text = "Running Test...";
                RunOptionsImportIntegrationTestButton.Text = "Running Test...";
                RunTastytradeImportIntegrationTestButton.Text = "Running Test...";
                RunTsllImportIntegrationTestButton.Text = "Running Test...";

                await Task.Delay(100); // Allow UI to update

                // Execute the test with progress updates
                var result = await testMethod(UpdateProgressStatus);

                // Update the UI with final results
                _currentResult = result;
                BindingContext = _currentResult;

                // Reset button text
                RunTestButton.Text = "Run Overview Test";
                RunOverviewReactiveTestButton.Text = "Run Overview Reactive Test";
                RunBrokerAccountTestButton.Text = "Run BrokerAccount Creation Test";
                RunBrokerAccountReactiveTestButton.Text = "Run BrokerAccount Creation Reactive Test";
                RunBrokerAccountDepositTestButton.Text = "Run BrokerAccount + Deposit Test";
                RunBrokerAccountMultipleMovementsTestButton.Text = "Run BrokerAccount Multiple Movements Test";
                RunOptionsImportIntegrationTestButton.Text = "Run Options Import Integration Test";
                RunTastytradeImportIntegrationTestButton.Text = "Execute Tastytrade Import Test";
                RunTsllImportIntegrationTestButton.Text = "Execute TSLL Import Test";

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
                RunTestButton.Text = "Run Overview Test";
                RunOverviewReactiveTestButton.Text = "Run Overview Reactive Test";
                RunBrokerAccountTestButton.Text = "Run BrokerAccount Creation Test";
                RunBrokerAccountReactiveTestButton.Text = "Run BrokerAccount Creation Reactive Test";
                RunBrokerAccountDepositTestButton.Text = "Run BrokerAccount + Deposit Test";
                RunBrokerAccountMultipleMovementsTestButton.Text = "Run BrokerAccount Multiple Movements Test";
                RunOptionsImportIntegrationTestButton.Text = "Run Options Import Integration Test";
                RunTastytradeImportIntegrationTestButton.Text = "Execute Tastytrade Import Test";
                RunTsllImportIntegrationTestButton.Text = "Execute TSLL Import Test";

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
            RunTestButton.Text = "Run Overview Test";
            RunOverviewReactiveTestButton.Text = "Run Overview Reactive Test";
            RunBrokerAccountTestButton.Text = "Run BrokerAccount Creation Test";
            RunBrokerAccountReactiveTestButton.Text = "Run BrokerAccount Creation Reactive Test";
            RunBrokerAccountDepositTestButton.Text = "Run BrokerAccount + Deposit Test";
            RunBrokerAccountMultipleMovementsTestButton.Text = "Run BrokerAccount Multiple Movements Test";
            RunOptionsImportIntegrationTestButton.Text = "Run Options Import Integration Test";
            RunTastytradeImportIntegrationTestButton.Text = "Execute Tastytrade Import Test";
            RunTsllImportIntegrationTestButton.Text = "Execute TSLL Import Test";
        }
    }
}
