using Binnaculum.Core.UI;
using Core.Platform.MauiTester.Models;

namespace Core.Platform.MauiTester.Services
{
    /// <summary>
    /// Orchestrates test execution for Core Platform validation tests
    /// 
    /// This class focuses solely on test execution orchestration:
    /// - Scenario execution and step coordination
    /// - Progress reporting and result aggregation  
    /// - Access to test discovery and execution services
    /// 
    /// Test infrastructure has been extracted to dedicated classes:
    /// - TestActions: Step action methods (WipeDataForTestingAsync, etc.)
    /// - TestExecutionContext: State management (TastytradeId, BrokerAccountId, etc.)
    /// - BuiltInTestScenarios: Scenario definitions and registration
    /// - TestVerifications: Centralized verification utilities
    /// - TestScenarioBuilder: Fluent API for building test scenarios
    /// </summary>
    public class TestRunner
    {
        #region Private Fields and Dependencies
        
        private readonly LogService _logService;
        private readonly TestDiscoveryService _discoveryService;
        private readonly TestExecutionContext _context = new();
        private readonly TestActions _actions;

        #endregion

        #region Constructors and Initialization

        public TestRunner(LogService logService)
        {
            _logService = logService;
            _discoveryService = new TestDiscoveryService();
            _actions = new TestActions(_context);
            RegisterBuiltInTests();
        }

        /// <summary>
        /// Register the built-in test scenarios with the discovery service
        /// </summary>
        private void RegisterBuiltInTests()
        {
            BuiltInTestScenarios.RegisterAll(_discoveryService, this, _actions);
        }

        /// <summary>
        /// Get the discovery service for accessing registered tests
        /// </summary>
        public TestDiscoveryService Discovery => _discoveryService;

        /// <summary>
        /// Get the test actions instance for step execution
        /// </summary>
        public TestActions Actions => _actions;

        /// <summary>
        /// Set the Tastytrade broker ID for test execution (used by built-in scenarios)
        /// </summary>
        public void SetTastytradeId(int id) => _context.TastytradeId = id;

        /// <summary>
        /// Get the Tastytrade broker ID from test execution context
        /// </summary>
        public int GetTastytradeId() => _context.TastytradeId;

        /// <summary>
        /// Set the USD currency ID for test execution (used by built-in scenarios)
        /// </summary>
        public void SetUsdCurrencyId(int id) => _context.UsdCurrencyId = id;

        /// <summary>
        /// Set the broker account ID for test execution (used by built-in scenarios)
        /// </summary>
        public void SetBrokerAccountId(int id) => _context.BrokerAccountId = id;

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
                    result.MarkCompleted(false, $"{testStep.StepName} failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                step.MarkCompleted(false);
                step.ErrorMessage = ex.Message;
                _logService.LogError($"{testStep.StepName} failed in {step.DurationText}: {ex.Message}");
                result.MarkCompleted(false, $"{testStep.StepName} failed");
                return false;
            }
        }
        #endregion

    }
}