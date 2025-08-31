using System.Text.RegularExpressions;
using Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.CLI;
using Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.Results;
using Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.Services;
using Binnaculum.UI.DeviceTests.Runners.VisualRunner.Services;
using Binnaculum.UI.DeviceTests.Runners.VisualRunner.ViewModels;
using Microsoft.Extensions.Logging;

namespace Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.CLI;

/// <summary>
/// Main entry point for headless test runner execution.
/// </summary>
public class HeadlessTestRunner
{
    private readonly ILogger<HeadlessTestRunner>? _logger;
    private readonly TestDiscoveryService _discoveryService;
    private readonly TestExecutionService _executionService;

    public HeadlessTestRunner(ILogger<HeadlessTestRunner>? logger = null)
    {
        _logger = logger;
        _discoveryService = new TestDiscoveryService(logger);
        _executionService = new TestExecutionService(logger);
    }

    /// <summary>
    /// Main execution method for the headless test runner.
    /// </summary>
    /// <param name="options">Command line options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exit code (0 for success, non-zero for failure)</returns>
    public async Task<int> RunAsync(CommandLineOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            LogMessage($"Starting Binnaculum Headless Test Runner", VerbosityLevel.Minimal);
            LogMessage($"Platform: {options.Platform}", VerbosityLevel.Normal);
            LogMessage($"Output Format: {options.OutputFormat}", VerbosityLevel.Normal);
            
            if (!string.IsNullOrWhiteSpace(options.Filter))
            {
                LogMessage($"Filter: {options.Filter}", VerbosityLevel.Normal);
            }

            // Discover tests
            LogMessage("Discovering tests...", VerbosityLevel.Normal);
            var allTests = await _discoveryService.DiscoverTestsAsync(cancellationToken);
            
            if (allTests.Count == 0)
            {
                LogMessage("No tests found.", VerbosityLevel.Minimal);
                return 0; // No tests is not an error
            }

            // Apply filtering
            var filteredTests = ApplyFiltering(allTests, options.Filter);
            LogMessage($"Found {filteredTests.Count} tests to execute", VerbosityLevel.Minimal);

            if (filteredTests.Count == 0)
            {
                LogMessage("No tests match the specified filter.", VerbosityLevel.Minimal);
                return 0;
            }

            // Execute tests
            LogMessage("Executing tests...", VerbosityLevel.Minimal);
            var results = await _executionService.ExecuteTestsAsync(
                filteredTests, 
                options, 
                CreateProgressReporter(options.Verbosity), 
                cancellationToken);

            // Write results
            await WriteResults(results, options);

            // Return appropriate exit code
            return CalculateExitCode(results);
        }
        catch (OperationCanceledException)
        {
            LogMessage("Test execution was cancelled.", VerbosityLevel.Minimal);
            return 130; // Conventionally used for SIGINT (Ctrl+C)
        }
        catch (Exception ex)
        {
            LogMessage($"Error during test execution: {ex.Message}", VerbosityLevel.Minimal);
            if (options.Verbosity >= VerbosityLevel.Detailed)
            {
                LogMessage($"Stack trace: {ex.StackTrace}", VerbosityLevel.Detailed);
            }
            return 1; // General error
        }
    }

    private List<TestCaseViewModel> ApplyFiltering(List<TestCaseViewModel> tests, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return tests;
        }

        // Support wildcard filtering
        var pattern = ConvertWildcardToRegex(filter);
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

        return tests.Where(test => 
            regex.IsMatch(test.Name) || 
            regex.IsMatch(test.FullName) || 
            regex.IsMatch(test.DisplayName)).ToList();
    }

    private static string ConvertWildcardToRegex(string wildcard)
    {
        // Escape regex special characters except * and ?
        var escaped = Regex.Escape(wildcard);
        
        // Replace escaped wildcards with regex equivalents
        escaped = escaped.Replace(@"\*", ".*");
        escaped = escaped.Replace(@"\?", ".");
        
        return $"^{escaped}$";
    }

    private IProgress<TestExecutionProgress> CreateProgressReporter(VerbosityLevel verbosity)
    {
        return new Progress<TestExecutionProgress>(progress =>
        {
            if (progress.IsCompleted)
            {
                LogMessage("Test execution completed.", VerbosityLevel.Normal);
            }
            else if (progress.CurrentTest != null && verbosity >= VerbosityLevel.Detailed)
            {
                LogMessage($"Running: {progress.CurrentTest.DisplayName} ({progress.CompletedTests + 1}/{progress.TotalTests})", VerbosityLevel.Detailed);
            }
            else if (verbosity >= VerbosityLevel.Normal)
            {
                LogMessage($"Progress: {progress.CompletedTests}/{progress.TotalTests} tests completed", VerbosityLevel.Normal);
            }
        });
    }

    private async Task WriteResults(TestExecutionResults results, CommandLineOptions options)
    {
        IResultsWriter writer = options.OutputFormat switch
        {
            OutputFormat.Xml => new XmlResultsWriter(),
            OutputFormat.Json => new JsonResultsWriter(),
            _ => new ConsoleResultsWriter(options.Verbosity)
        };

        await writer.WriteResultsAsync(results, options.OutputPath);
    }

    private static int CalculateExitCode(TestExecutionResults results)
    {
        if (results.FailedCount > 0)
        {
            return 1; // Tests failed
        }
        
        if (results.TotalCount == 0)
        {
            return 2; // No tests executed (could be a problem)
        }
        
        return 0; // Success
    }

    private void LogMessage(string message, VerbosityLevel level)
    {
        _logger?.LogInformation(message);
        
        // Also write to console based on verbosity
        if (level <= VerbosityLevel.Minimal)
        {
            Console.WriteLine(message);
        }
    }
}