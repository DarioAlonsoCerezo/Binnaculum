using System.Text.Json;
using System.Text.Json.Serialization;
using Binnaculum.UI.DeviceTests.Runners.VisualRunner.Services;
using Binnaculum.UI.DeviceTests.Runners.VisualRunner.ViewModels;

namespace Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.Results;

/// <summary>
/// Writes test results in JSON format for modern tooling and analysis.
/// </summary>
public class JsonResultsWriter : IResultsWriter
{
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonResultsWriter()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task WriteResultsAsync(TestExecutionResults results, string? outputPath = null)
    {
        var jsonResult = CreateJsonResult(results);
        var jsonString = JsonSerializer.Serialize(jsonResult, _jsonOptions);
        
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            await File.WriteAllTextAsync(outputPath, jsonString);
        }
        else
        {
            Console.WriteLine(jsonString);
        }
    }

    private static JsonTestResults CreateJsonResult(TestExecutionResults results)
    {
        return new JsonTestResults
        {
            Timestamp = DateTime.UtcNow,
            Summary = new TestSummary
            {
                Total = results.TotalCount,
                Passed = results.PassedCount,
                Failed = results.FailedCount,
                Skipped = results.SkippedCount,
                Duration = results.Results.Sum(r => r.Duration.TotalMilliseconds)
            },
            Tests = results.Results.Select(r => new JsonTestResult
            {
                Name = r.TestName,
                Status = r.Status.ToString().ToLowerInvariant(),
                Duration = r.Duration.TotalMilliseconds,
                ErrorMessage = r.ErrorMessage,
                StackTrace = r.StackTrace
            }).ToList()
        };
    }
}

/// <summary>
/// JSON structure for test results output.
/// </summary>
public class JsonTestResults
{
    public DateTime Timestamp { get; set; }
    public TestSummary Summary { get; set; } = new();
    public List<JsonTestResult> Tests { get; set; } = new();
}

/// <summary>
/// Summary statistics for test execution.
/// </summary>
public class TestSummary
{
    public int Total { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public double Duration { get; set; } // in milliseconds
}

/// <summary>
/// Individual test result in JSON format.
/// </summary>
public class JsonTestResult
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double Duration { get; set; } // in milliseconds
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
}