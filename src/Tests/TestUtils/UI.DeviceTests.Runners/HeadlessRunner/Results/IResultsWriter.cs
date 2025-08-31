using Binnaculum.UI.DeviceTests.Runners.VisualRunner.Services;

namespace Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.Results;

/// <summary>
/// Interface for writing test results in different formats.
/// </summary>
public interface IResultsWriter
{
    /// <summary>
    /// Writes test execution results to the specified output.
    /// </summary>
    /// <param name="results">The test execution results to write</param>
    /// <param name="outputPath">Optional path to write to file, if null writes to console</param>
    Task WriteResultsAsync(TestExecutionResults results, string? outputPath = null);
}