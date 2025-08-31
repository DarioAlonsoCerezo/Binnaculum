using System.CommandLine;
using System.CommandLine.Binding;

namespace Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.CLI;

/// <summary>
/// Parses command line arguments for the headless test runner.
/// </summary>
public class ArgumentParser
{
    public static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("Binnaculum Headless Test Runner - Execute device tests without UI")
        {
            CreatePlatformOption(),
            CreateFilterOption(),
            CreateOutputFormatOption(),
            CreateOutputPathOption(),
            CreateHeadlessOption(),
            CreateParallelOption(),
            CreateTimeoutOption(),
            CreateRetryOption(),
            CreateVerbosityOption(),
            CreateArtifactsOption(),
            CreateArtifactPathOption()
        };

        rootCommand.SetHandler(async (context) =>
        {
            var options = new CommandLineOptions
            {
                Platform = context.ParseResult.GetValueForOption(CreatePlatformOption())!,
                Filter = context.ParseResult.GetValueForOption(CreateFilterOption()),
                OutputFormat = context.ParseResult.GetValueForOption(CreateOutputFormatOption()),
                OutputPath = context.ParseResult.GetValueForOption(CreateOutputPathOption()),
                Headless = context.ParseResult.GetValueForOption(CreateHeadlessOption()),
                Parallel = context.ParseResult.GetValueForOption(CreateParallelOption()),
                Timeout = context.ParseResult.GetValueForOption(CreateTimeoutOption()),
                RetryCount = context.ParseResult.GetValueForOption(CreateRetryOption()),
                Verbosity = context.ParseResult.GetValueForOption(CreateVerbosityOption()),
                CollectArtifacts = context.ParseResult.GetValueForOption(CreateArtifactsOption()),
                ArtifactPath = context.ParseResult.GetValueForOption(CreateArtifactPathOption())
            };

            var runner = new HeadlessTestRunner();
            var exitCode = await runner.RunAsync(options, context.GetCancellationToken());
            context.ExitCode = exitCode;
        });

        return rootCommand;
    }

    private static Option<string> CreatePlatformOption() =>
        new(new[] { "--platform", "-p" }, () => "android", "Target platform (android, ios, windows, maccatalyst)");

    private static Option<string?> CreateFilterOption() =>
        new(new[] { "--filter", "-f" }, "Filter tests by name, category, or assembly (supports wildcards)");

    private static Option<OutputFormat> CreateOutputFormatOption() =>
        new(new[] { "--output-format", "-o" }, () => OutputFormat.Console, "Output format (console, xml, json)");

    private static Option<string?> CreateOutputPathOption() =>
        new(new[] { "--output-path", "--out" }, "Path to write output file (if not console)");

    private static Option<bool> CreateHeadlessOption() =>
        new(new[] { "--headless" }, () => true, "Run in headless mode (no UI)");

    private static Option<bool> CreateParallelOption() =>
        new(new[] { "--parallel" }, () => false, "Execute tests in parallel");

    private static Option<int> CreateTimeoutOption() =>
        new(new[] { "--timeout", "-t" }, () => 300, "Timeout in seconds for test execution");

    private static Option<int> CreateRetryOption() =>
        new(new[] { "--retry-failed", "-r" }, () => 0, "Number of times to retry failed tests");

    private static Option<VerbosityLevel> CreateVerbosityOption() =>
        new(new[] { "--verbosity", "-v" }, () => VerbosityLevel.Normal, "Verbosity level (quiet, minimal, normal, detailed, diagnostic)");

    private static Option<bool> CreateArtifactsOption() =>
        new(new[] { "--collect-artifacts" }, () => false, "Collect artifacts (screenshots, logs) for failed tests");

    private static Option<string?> CreateArtifactPathOption() =>
        new(new[] { "--artifact-path" }, "Path to store collected artifacts");
}