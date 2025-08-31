using System.CommandLine;

namespace Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.CLI;

/// <summary>
/// Command line options for the headless test runner.
/// </summary>
public class CommandLineOptions
{
    public string Platform { get; set; } = "android";
    public string? Filter { get; set; }
    public OutputFormat OutputFormat { get; set; } = OutputFormat.Console;
    public string? OutputPath { get; set; }
    public bool Headless { get; set; } = true;
    public bool Parallel { get; set; } = false;
    public int Timeout { get; set; } = 300; // 5 minutes default
    public int RetryCount { get; set; } = 0;
    public VerbosityLevel Verbosity { get; set; } = VerbosityLevel.Normal;
    public bool CollectArtifacts { get; set; } = false;
    public string? ArtifactPath { get; set; }
    public bool Help { get; set; } = false;
}

/// <summary>
/// Supported output formats for test results.
/// </summary>
public enum OutputFormat
{
    Console,
    Xml,
    Json
}

/// <summary>
/// Verbosity levels for logging output.
/// </summary>
public enum VerbosityLevel
{
    Quiet,
    Minimal,
    Normal,
    Detailed,
    Diagnostic
}

/// <summary>
/// Supported platforms for headless test execution.
/// </summary>
public enum TestPlatform
{
    Android,
    iOS,
    Windows,
    MacCatalyst
}