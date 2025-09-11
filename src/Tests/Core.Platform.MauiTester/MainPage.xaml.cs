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
            try
            {
                _logService.Clear();
                _logService.Log("Starting Core Platform validation test...");

                // Create new test result and bind to UI
                _currentResult = new OverallTestResult();
                BindingContext = _currentResult;

                // Update UI to show test is running
                RunTestButton.Text = "Running Test...";

                // Execute the test with progress updates
                var result = await _testRunner.ExecuteOverviewTestAsync(UpdateProgressStatus);

                // Update the UI with final results
                _currentResult = result;
                BindingContext = _currentResult;

                // Reset button text
                RunTestButton.Text = "Run Overview Test";

                _logService.Log($"Test execution completed. Overall result: {result.AllTestsPassed}");
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error running test: {ex.Message}");
                
                // Show error in UI
                if (_currentResult != null)
                {
                    _currentResult.IsRunning = false;
                    _currentResult.IsCompleted = true;
                    _currentResult.AllTestsPassed = false;
                    _currentResult.OverallStatus = $"Error: {ex.Message}";
                }

                RunTestButton.Text = "Run Overview Test";

                await DisplayAlert("Test Error", $"An error occurred while running the test:\n\n{ex.Message}", "OK");
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
        }
    }
}
