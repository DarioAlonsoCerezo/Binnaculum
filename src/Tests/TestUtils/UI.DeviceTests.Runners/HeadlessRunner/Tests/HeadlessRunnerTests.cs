using Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.CLI;
using Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.Results;
using Binnaculum.UI.DeviceTests.Runners.VisualRunner.Services;
using Binnaculum.UI.DeviceTests.Runners.VisualRunner.ViewModels;
using Xunit;

namespace Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.Tests;

/// <summary>
/// Tests for the headless test runner components.
/// </summary>
public class HeadlessRunnerTests
{
    [Fact]
    public void CommandLineOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new CommandLineOptions();

        // Assert
        Assert.Equal("android", options.Platform);
        Assert.Equal(OutputFormat.Console, options.OutputFormat);
        Assert.True(options.Headless);
        Assert.False(options.Parallel);
        Assert.Equal(300, options.Timeout);
        Assert.Equal(0, options.RetryCount);
        Assert.Equal(VerbosityLevel.Normal, options.Verbosity);
        Assert.False(options.CollectArtifacts);
    }

    [Fact]
    public void ArgumentParser_CreateRootCommand_ShouldNotReturnNull()
    {
        // Act
        var command = ArgumentParser.CreateRootCommand();

        // Assert
        Assert.NotNull(command);
        Assert.Contains("Binnaculum Headless Test Runner", command.Description);
    }

    [Fact]
    public async Task ConsoleResultsWriter_WriteResults_ShouldGenerateOutput()
    {
        // Arrange
        var results = CreateSampleResults();
        using var output = new StringWriter();
        var writer = new ConsoleResultsWriter(VerbosityLevel.Normal, output);

        // Act
        await writer.WriteResultsAsync(results);

        // Assert
        var content = output.ToString();
        Assert.Contains("Binnaculum Headless Test Runner Results", content);
        Assert.Contains("Total: 3", content);
        Assert.Contains("Passed: 2", content);
        Assert.Contains("Failed: 1", content);
    }

    [Fact]
    public async Task XmlResultsWriter_WriteResults_ShouldGenerateValidXml()
    {
        // Arrange
        var results = CreateSampleResults();
        var writer = new XmlResultsWriter();

        // Act & Assert - Should not throw
        using var output = new StringWriter();
        Console.SetOut(output);
        await writer.WriteResultsAsync(results);
        
        var xmlContent = output.ToString();
        Assert.Contains("<?xml", xmlContent);
        Assert.Contains("<testsuites", xmlContent);
        Assert.Contains("<testsuite", xmlContent);
        Assert.Contains("<testcase", xmlContent);
    }

    [Fact]
    public async Task JsonResultsWriter_WriteResults_ShouldGenerateValidJson()
    {
        // Arrange
        var results = CreateSampleResults();
        var writer = new JsonResultsWriter();

        // Act & Assert - Should not throw
        using var output = new StringWriter();
        Console.SetOut(output);
        await writer.WriteResultsAsync(results);
        
        var jsonContent = output.ToString();
        Assert.Contains("\"timestamp\"", jsonContent);
        Assert.Contains("\"summary\"", jsonContent);
        Assert.Contains("\"tests\"", jsonContent);
    }

    private static TestExecutionResults CreateSampleResults()
    {
        var results = new TestExecutionResults();
        
        results.AddResult(new TestExecutionResult
        {
            TestName = "TestAssembly.TestClass1.Test1",
            Status = TestCaseStatus.Passed,
            Duration = TimeSpan.FromMilliseconds(100),
            ErrorMessage = null,
            StackTrace = null
        });
        
        results.AddResult(new TestExecutionResult
        {
            TestName = "TestAssembly.TestClass1.Test2",
            Status = TestCaseStatus.Passed,
            Duration = TimeSpan.FromMilliseconds(150),
            ErrorMessage = null,
            StackTrace = null
        });
        
        results.AddResult(new TestExecutionResult
        {
            TestName = "TestAssembly.TestClass2.FailedTest",
            Status = TestCaseStatus.Failed,
            Duration = TimeSpan.FromMilliseconds(50),
            ErrorMessage = "Test assertion failed",
            StackTrace = "   at TestClass2.FailedTest() line 42"
        });

        return results;
    }
}