using Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.CLI;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace Binnaculum.UI.DeviceTests.Runners.HeadlessRunner.Platform;

/// <summary>
/// Android-specific headless test runner with ADB integration.
/// </summary>
public class AndroidHeadlessRunner
{
    private readonly ILogger<AndroidHeadlessRunner>? _logger;

    public AndroidHeadlessRunner(ILogger<AndroidHeadlessRunner>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes tests on Android platform in headless mode.
    /// </summary>
    /// <param name="options">Command line options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exit code</returns>
    public async Task<int> RunAsync(CommandLineOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Starting Android headless test runner");

            // Validate Android environment
            await ValidateAndroidEnvironmentAsync(cancellationToken);

            // Check for connected devices/emulators
            await EnsureAndroidDeviceAvailableAsync(cancellationToken);

            // Run the core headless runner
            var runner = new HeadlessTestRunner(CreateCompatibleLogger(_logger));
            var exitCode = await runner.RunAsync(options, cancellationToken);

            // Collect Android-specific artifacts if requested
            if (options.CollectArtifacts)
            {
                await CollectAndroidArtifactsAsync(options.ArtifactPath, cancellationToken);
            }

            return exitCode;
        }
        catch (AndroidEnvironmentException ex)
        {
            _logger?.LogError(ex, "Android environment validation failed");
            Console.Error.WriteLine($"Android Error: {ex.Message}");
            return 10; // Android-specific error code
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in Android headless test runner");
            return 1;
        }
    }

    private async Task ValidateAndroidEnvironmentAsync(CancellationToken cancellationToken)
    {
        _logger?.LogDebug("Validating Android environment");

        // Check if ADB is available
        try
        {
            var adbResult = await RunCommandAsync("adb", "version", cancellationToken);
            if (adbResult.ExitCode != 0)
            {
                throw new AndroidEnvironmentException("ADB is not available or not working properly");
            }
            
            _logger?.LogDebug($"ADB validation successful: {adbResult.Output}");
        }
        catch (Exception ex) when (!(ex is AndroidEnvironmentException))
        {
            throw new AndroidEnvironmentException("ADB command not found. Please ensure Android SDK is installed and ADB is in PATH", ex);
        }
    }

    private async Task EnsureAndroidDeviceAvailableAsync(CancellationToken cancellationToken)
    {
        _logger?.LogDebug("Checking for available Android devices");

        var devicesResult = await RunCommandAsync("adb", "devices", cancellationToken);
        if (devicesResult.ExitCode != 0)
        {
            throw new AndroidEnvironmentException("Failed to list Android devices");
        }

        // Parse devices output
        var lines = devicesResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var deviceCount = lines.Skip(1).Count(line => line.Contains("device") && !line.Contains("offline"));

        if (deviceCount == 0)
        {
            _logger?.LogWarning("No Android devices found. Attempting to start emulator...");
            // In a full implementation, we could try to start an emulator here
            throw new AndroidEnvironmentException("No Android devices or emulators are available. Please connect a device or start an emulator.");
        }

        _logger?.LogInformation($"Found {deviceCount} available Android device(s)");
    }

    private async Task CollectAndroidArtifactsAsync(string? artifactPath, CancellationToken cancellationToken)
    {
        try
        {
            _logger?.LogInformation("Collecting Android-specific artifacts");

            var artifactDir = artifactPath ?? Path.Combine(Path.GetTempPath(), "BinnaculumAndroidArtifacts");
            Directory.CreateDirectory(artifactDir);

            // Collect logcat logs
            await CollectLogcatAsync(artifactDir, cancellationToken);

            // Collect system info
            await CollectSystemInfoAsync(artifactDir, cancellationToken);

            _logger?.LogInformation($"Android artifacts collected to: {artifactDir}");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to collect some Android artifacts");
        }
    }

    private async Task CollectLogcatAsync(string artifactDir, CancellationToken cancellationToken)
    {
        try
        {
            var logcatPath = Path.Combine(artifactDir, "logcat.txt");
            var logcatResult = await RunCommandAsync("adb", "logcat -d", cancellationToken, timeoutSeconds: 30);
            
            if (logcatResult.ExitCode == 0)
            {
                await File.WriteAllTextAsync(logcatPath, logcatResult.Output, cancellationToken);
                _logger?.LogDebug($"Logcat saved to: {logcatPath}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to collect logcat");
        }
    }

    private async Task CollectSystemInfoAsync(string artifactDir, CancellationToken cancellationToken)
    {
        try
        {
            var infoPath = Path.Combine(artifactDir, "system-info.txt");
            var commands = new[]
            {
                ("Device Properties", "adb shell getprop"),
                ("Memory Info", "adb shell cat /proc/meminfo"),
                ("CPU Info", "adb shell cat /proc/cpuinfo"),
                ("Disk Usage", "adb shell df")
            };

            using var writer = new StreamWriter(infoPath);
            
            foreach (var (title, command) in commands)
            {
                try
                {
                    await writer.WriteLineAsync($"=== {title} ===");
                    var parts = command.Split(' ', 2);
                    var result = await RunCommandAsync(parts[0], parts.Length > 1 ? parts[1] : "", cancellationToken, timeoutSeconds: 15);
                    
                    if (result.ExitCode == 0)
                    {
                        await writer.WriteLineAsync(result.Output);
                    }
                    else
                    {
                        await writer.WriteLineAsync($"Command failed with exit code: {result.ExitCode}");
                    }
                }
                catch (Exception ex)
                {
                    await writer.WriteLineAsync($"Error collecting {title}: {ex.Message}");
                }
                
                await writer.WriteLineAsync();
            }

            _logger?.LogDebug($"System info saved to: {infoPath}");
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to collect system info");
        }
    }

    private static async Task<CommandResult> RunCommandAsync(string fileName, string arguments, CancellationToken cancellationToken, int timeoutSeconds = 60)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);

        try
        {
            await process.WaitForExitAsync(combinedTokenSource.Token);
            
            var output = outputBuilder.ToString().Trim();
            var error = errorBuilder.ToString().Trim();
            
            return new CommandResult(process.ExitCode, output, error);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }
    }

    private static ILogger<HeadlessTestRunner>? CreateCompatibleLogger(ILogger<AndroidHeadlessRunner>? logger)
    {
        // Return null - the HeadlessTestRunner handles null loggers gracefully
        return null;
    }
}

/// <summary>
/// Exception thrown when Android environment validation fails.
/// </summary>
public class AndroidEnvironmentException : Exception
{
    public AndroidEnvironmentException(string message) : base(message) { }
    public AndroidEnvironmentException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Result of running a command.
/// </summary>
public record CommandResult(int ExitCode, string Output, string Error);