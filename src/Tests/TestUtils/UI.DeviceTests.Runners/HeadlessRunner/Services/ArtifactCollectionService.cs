using Binnaculum.UI.DeviceTests.Runners.VisualRunner.Services;
using Microsoft.Extensions.Logging;

namespace Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.Services;

/// <summary>
/// Service for collecting artifacts (screenshots, logs, memory dumps) during test execution.
/// </summary>
public class ArtifactCollectionService
{
    private readonly ILogger? _logger;
    private readonly string _artifactPath;

    public ArtifactCollectionService(string artifactPath, ILogger? logger = null)
    {
        _logger = logger;
        _artifactPath = artifactPath ?? Path.Combine(Path.GetTempPath(), "BinnaculumTestArtifacts", DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
        
        // Ensure artifact directory exists
        Directory.CreateDirectory(_artifactPath);
    }

    /// <summary>
    /// Collects artifacts for failed tests.
    /// </summary>
    /// <param name="results">Test execution results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task CollectArtifactsAsync(TestExecutionResults results, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation($"Collecting artifacts to: {_artifactPath}");

            var failedTests = results.Results.Where(r => r.Status == VisualRunner.ViewModels.TestCaseStatus.Failed).ToList();
            
            if (failedTests.Count == 0)
            {
                _logger?.LogInformation("No failed tests - skipping artifact collection");
                return;
            }

            _logger?.LogInformation($"Collecting artifacts for {failedTests.Count} failed tests");

            // Create artifacts summary
            await CreateArtifactsSummaryAsync(failedTests, cancellationToken);

            // Collect screenshots (placeholder - would integrate with platform-specific screenshot APIs)
            await CollectScreenshotsAsync(failedTests, cancellationToken);

            // Collect logs (placeholder - would integrate with logging system)
            await CollectLogsAsync(failedTests, cancellationToken);

            // Collect memory dumps (placeholder - would integrate with diagnostic tools)
            await CollectMemoryDumpsAsync(failedTests, cancellationToken);

            _logger?.LogInformation($"Artifact collection completed: {_artifactPath}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during artifact collection");
            throw;
        }
    }

    private async Task CreateArtifactsSummaryAsync(List<TestExecutionResult> failedTests, CancellationToken cancellationToken)
    {
        var summaryPath = Path.Combine(_artifactPath, "artifacts-summary.txt");
        
        using var writer = new StreamWriter(summaryPath);
        
        await writer.WriteLineAsync($"Binnaculum Test Artifacts Summary");
        await writer.WriteLineAsync($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        await writer.WriteLineAsync($"Failed Tests: {failedTests.Count}");
        await writer.WriteLineAsync();

        foreach (var test in failedTests)
        {
            await writer.WriteLineAsync($"Test: {test.TestName}");
            await writer.WriteLineAsync($"Duration: {test.Duration.TotalMilliseconds:F2}ms");
            
            if (!string.IsNullOrWhiteSpace(test.ErrorMessage))
            {
                await writer.WriteLineAsync($"Error: {test.ErrorMessage}");
            }
            
            if (!string.IsNullOrWhiteSpace(test.StackTrace))
            {
                await writer.WriteLineAsync("Stack Trace:");
                await writer.WriteLineAsync(test.StackTrace);
            }
            
            await writer.WriteLineAsync(new string('-', 50));
            await writer.WriteLineAsync();
        }

        _logger?.LogDebug($"Created artifacts summary: {summaryPath}");
    }

    private async Task CollectScreenshotsAsync(List<TestExecutionResult> failedTests, CancellationToken cancellationToken)
    {
        // Placeholder for screenshot collection
        // In a real implementation, this would:
        // 1. Integrate with platform-specific screenshot APIs
        // 2. Capture screenshots for failed UI tests
        // 3. Save with meaningful names based on test names
        
        var screenshotDir = Path.Combine(_artifactPath, "screenshots");
        Directory.CreateDirectory(screenshotDir);

        // Create a placeholder file indicating screenshots would be collected here
        var placeholderPath = Path.Combine(screenshotDir, "screenshot-collection-placeholder.txt");
        await File.WriteAllTextAsync(placeholderPath, 
            $"Screenshots for {failedTests.Count} failed tests would be collected here.\n" +
            "Integration with platform-specific screenshot APIs required.", cancellationToken);

        _logger?.LogDebug($"Screenshot collection prepared: {screenshotDir}");
    }

    private async Task CollectLogsAsync(List<TestExecutionResult> failedTests, CancellationToken cancellationToken)
    {
        // Placeholder for log collection
        // In a real implementation, this would:
        // 1. Collect application logs
        // 2. Collect system logs relevant to test execution
        // 3. Filter logs by test execution timeframe
        
        var logDir = Path.Combine(_artifactPath, "logs");
        Directory.CreateDirectory(logDir);

        var placeholderPath = Path.Combine(logDir, "log-collection-placeholder.txt");
        await File.WriteAllTextAsync(placeholderPath, 
            $"Logs for {failedTests.Count} failed tests would be collected here.\n" +
            "Integration with application and system logging required.", cancellationToken);

        _logger?.LogDebug($"Log collection prepared: {logDir}");
    }

    private async Task CollectMemoryDumpsAsync(List<TestExecutionResult> failedTests, CancellationToken cancellationToken)
    {
        // Placeholder for memory dump collection
        // In a real implementation, this would:
        // 1. Capture memory dumps for crashed tests
        // 2. Collect GC statistics
        // 3. Generate memory usage reports
        
        var dumpDir = Path.Combine(_artifactPath, "memory-dumps");
        Directory.CreateDirectory(dumpDir);

        var placeholderPath = Path.Combine(dumpDir, "memory-dump-placeholder.txt");
        await File.WriteAllTextAsync(placeholderPath, 
            $"Memory dumps for {failedTests.Count} failed tests would be collected here.\n" +
            "Integration with diagnostic tooling required.", cancellationToken);

        _logger?.LogDebug($"Memory dump collection prepared: {dumpDir}");
    }

    /// <summary>
    /// Gets the path where artifacts are being collected.
    /// </summary>
    public string ArtifactPath => _artifactPath;
}