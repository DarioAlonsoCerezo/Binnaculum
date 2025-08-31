using Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.CLI;
using Binnaculum.UI.DeviceTests.Runners.VisualRunner.Services;
using Binnaculum.UI.DeviceTests.Runners.VisualRunner.ViewModels;
using Microsoft.Extensions.Logging;

namespace Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.Services;

/// <summary>
/// Service for executing tests in headless mode by wrapping the existing VisualDeviceRunner.
/// </summary>
public class TestExecutionService
{
    private readonly ILogger? _logger;
    private readonly VisualDeviceRunner _deviceRunner;

    public TestExecutionService(ILogger? logger = null)
    {
        _logger = logger;
        _deviceRunner = new VisualDeviceRunner(CreateCompatibleLogger(logger));
    }

    /// <summary>
    /// Executes the specified tests with the given options.
    /// </summary>
    /// <param name="tests">Tests to execute</param>
    /// <param name="options">Execution options</param>
    /// <param name="progress">Progress reporter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test execution results</returns>
    public async Task<TestExecutionResults> ExecuteTestsAsync(
        IEnumerable<TestCaseViewModel> tests,
        CommandLineOptions options,
        IProgress<TestExecutionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation($"Starting execution of {tests.Count()} tests in headless mode");

            // Apply timeout if specified
            using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(options.Timeout));
            using var combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutTokenSource.Token);

            var results = await ExecuteWithRetryLogic(
                tests, 
                options, 
                progress, 
                combinedTokenSource.Token);

            _logger?.LogInformation($"Test execution completed. {results.PassedCount} passed, {results.FailedCount} failed");
            return results;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.LogWarning("Test execution was cancelled by user");
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning($"Test execution timed out after {options.Timeout} seconds");
            throw new TimeoutException($"Test execution timed out after {options.Timeout} seconds");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during test execution");
            throw;
        }
    }

    private async Task<TestExecutionResults> ExecuteWithRetryLogic(
        IEnumerable<TestCaseViewModel> tests,
        CommandLineOptions options,
        IProgress<TestExecutionProgress>? progress,
        CancellationToken cancellationToken)
    {
        var testList = tests.ToList();
        
        // First execution attempt
        var results = await _deviceRunner.ExecuteTestsAsync(testList, progress, cancellationToken);

        // Retry failed tests if requested
        if (options.RetryCount > 0 && results.FailedCount > 0)
        {
            _logger?.LogInformation($"Retrying {results.FailedCount} failed tests up to {options.RetryCount} times");
            
            for (int retry = 1; retry <= options.RetryCount; retry++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var failedTests = testList.Where(t => t.Status == TestCaseStatus.Failed).ToList();
                if (failedTests.Count == 0)
                    break;

                _logger?.LogInformation($"Retry attempt {retry}/{options.RetryCount} for {failedTests.Count} failed tests");

                // Reset failed test statuses
                foreach (var test in failedTests)
                {
                    test.Status = TestCaseStatus.Pending;
                    test.ErrorMessage = string.Empty;
                    test.StackTrace = string.Empty;
                }

                // Execute retry
                var retryResults = await _deviceRunner.ExecuteTestsAsync(failedTests, progress, cancellationToken);
                
                // Update the main results with retry outcomes
                UpdateResultsWithRetry(results, retryResults);
            }
        }

        return results;
    }

    private static void UpdateResultsWithRetry(TestExecutionResults originalResults, TestExecutionResults retryResults)
    {
        // This is a simplified approach - in a full implementation we'd need to
        // properly merge the results, but for now we just update the counts
        // The VisualDeviceRunner already updates the TestCaseViewModel status, 
        // which is reflected in the results
    }

    private static ILogger<VisualDeviceRunner>? CreateCompatibleLogger(ILogger? logger)
    {
        if (logger is ILogger<VisualDeviceRunner> compatibleLogger)
        {
            return compatibleLogger;
        }
        
        // Return null if we can't create a compatible logger
        // The VisualDeviceRunner handles null loggers gracefully
        return null;
    }
}