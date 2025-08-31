using Binnaculum.UI.DeviceTests.Runners.VisualRunner.Services;
using Binnaculum.UI.DeviceTests.Runners.VisualRunner.ViewModels;
using Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.CLI;

namespace Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.Results;

/// <summary>
/// Writes test results to console output in a human-readable format.
/// </summary>
public class ConsoleResultsWriter : IResultsWriter
{
    private readonly VerbosityLevel _verbosity;
    private readonly TextWriter _output;

    public ConsoleResultsWriter(VerbosityLevel verbosity = VerbosityLevel.Normal, TextWriter? output = null)
    {
        _verbosity = verbosity;
        _output = output ?? Console.Out;
    }

    public async Task WriteResultsAsync(TestExecutionResults results, string? outputPath = null)
    {
        // Write header
        await _output.WriteLineAsync("=== Binnaculum Headless Test Runner Results ===");
        await _output.WriteLineAsync($"Execution completed at: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
        await _output.WriteLineAsync();

        // Write summary
        await WriteSummaryAsync(results);

        // Write detailed results if verbosity allows
        if (_verbosity >= VerbosityLevel.Normal)
        {
            await WriteDetailedResultsAsync(results);
        }

        // Write failed tests details if any
        if (results.FailedCount > 0 && _verbosity >= VerbosityLevel.Minimal)
        {
            await WriteFailedTestsAsync(results);
        }
    }

    private async Task WriteSummaryAsync(TestExecutionResults results)
    {
        await _output.WriteLineAsync("=== Test Summary ===");
        await _output.WriteLineAsync($"Total: {results.TotalCount}");
        
        // Use colors for console output if supported
        if (results.PassedCount > 0)
        {
            await _output.WriteLineAsync($"✅ Passed: {results.PassedCount}");
        }
        
        if (results.FailedCount > 0)
        {
            await _output.WriteLineAsync($"❌ Failed: {results.FailedCount}");
        }
        
        if (results.SkippedCount > 0)
        {
            await _output.WriteLineAsync($"⏭️  Skipped: {results.SkippedCount}");
        }

        // Calculate total duration
        var totalDuration = results.Results.Sum(r => r.Duration.TotalMilliseconds);
        await _output.WriteLineAsync($"⏱️  Total Duration: {totalDuration:F2}ms ({totalDuration/1000:F2}s)");
        
        await _output.WriteLineAsync();
    }

    private async Task WriteDetailedResultsAsync(TestExecutionResults results)
    {
        if (_verbosity < VerbosityLevel.Detailed) return;

        await _output.WriteLineAsync("=== Detailed Results ===");
        
        foreach (var result in results.Results.OrderBy(r => r.TestName))
        {
            var statusIcon = GetStatusIcon(result.Status);
            var duration = $"({result.Duration.TotalMilliseconds:F2}ms)";
            
            await _output.WriteLineAsync($"{statusIcon} {result.TestName} {duration}");
            
            if (_verbosity >= VerbosityLevel.Diagnostic && !string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                await _output.WriteLineAsync($"  Error: {result.ErrorMessage}");
            }
        }
        
        await _output.WriteLineAsync();
    }

    private async Task WriteFailedTestsAsync(TestExecutionResults results)
    {
        var failedTests = results.Results.Where(r => r.Status == TestCaseStatus.Failed).ToList();
        if (!failedTests.Any()) return;

        await _output.WriteLineAsync("=== Failed Tests Details ===");
        
        for (int i = 0; i < failedTests.Count; i++)
        {
            var test = failedTests[i];
            await _output.WriteLineAsync($"{i + 1}. {test.TestName}");
            
            if (!string.IsNullOrWhiteSpace(test.ErrorMessage))
            {
                await _output.WriteLineAsync($"   Error: {test.ErrorMessage}");
            }
            
            if (_verbosity >= VerbosityLevel.Detailed && !string.IsNullOrWhiteSpace(test.StackTrace))
            {
                await _output.WriteLineAsync("   Stack Trace:");
                foreach (var line in test.StackTrace.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    await _output.WriteLineAsync($"     {line.Trim()}");
                }
            }
            
            await _output.WriteLineAsync();
        }
    }

    private static string GetStatusIcon(TestCaseStatus status) => status switch
    {
        TestCaseStatus.Passed => "✅",
        TestCaseStatus.Failed => "❌",
        TestCaseStatus.Skipped => "⏭️ ",
        TestCaseStatus.Running => "🏃",
        _ => "⏸️ "
    };
}