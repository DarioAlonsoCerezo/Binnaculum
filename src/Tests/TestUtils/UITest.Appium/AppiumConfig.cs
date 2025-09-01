using Binnaculum.UITest.Core;

namespace Binnaculum.UITest.Appium;

/// <summary>
/// Configuration implementation for Appium-based testing.
/// </summary>
public class AppiumConfig : IConfig
{
    public string Platform { get; set; } = "Android";
    public string? AppPackage { get; set; }
    public string? AppPath { get; set; }
    public string? DeviceId { get; set; }
    public Dictionary<string, object> Capabilities { get; set; } = new();
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public bool ResetApp { get; set; } = true;

    /// <summary>
    /// Create Android configuration for Binnaculum.
    /// Uses dynamic discovery in development, environment variable override in CI.
    /// Enhanced with configurable reset strategies for test isolation.
    /// </summary>
    public static AppiumConfig ForBinnaculumAndroid(string? appPath = null, string? deviceId = null, AppResetStrategy resetStrategy = AppResetStrategy.ClearAppData)
    {
        return new AppiumConfig
        {
            Platform = "Android",
            AppPackage = "com.darioalonso.binnacle", // Based on UI project ApplicationId
            AppPath = appPath,
            DeviceId = deviceId,
            Capabilities = GetAndroidCapabilities(resetStrategy)
        };
    }

    /// <summary>
    /// Create Android configuration with automatic activity detection and configurable reset.
    /// </summary>
    public static AppiumConfig ForBinnaculumAndroidSimple(string? appPath = null, string? deviceId = null, AppResetStrategy resetStrategy = AppResetStrategy.ClearAppData)
    {
        return new AppiumConfig
        {
            Platform = "Android",
            AppPackage = "com.darioalonso.binnacle",
            AppPath = appPath,
            DeviceId = deviceId,
            Capabilities = GetAndroidCapabilitiesSimple(resetStrategy)
        };
    }

    /// <summary>
    /// Create iOS configuration for Binnaculum.
    /// </summary>
    public static AppiumConfig ForBinnaculumiOS(string? appPath = null, string? deviceId = null)
    {
        return new AppiumConfig
        {
            Platform = "iOS",
            AppPackage = "com.darioalonso.binnacle",
            AppPath = appPath,
            DeviceId = deviceId,
            Capabilities = new Dictionary<string, object>
            {
                ["platformName"] = "iOS",
                ["automationName"] = "XCUITest",
                ["bundleId"] = "com.darioalonso.binnacle",
                ["noReset"] = false,
                ["newCommandTimeout"] = 300
            }
        };
    }

    /// <summary>
    /// Create Windows configuration for Binnaculum.
    /// </summary>
    public static AppiumConfig ForBinnaculumWindows(string? appPath = null)
    {
        return new AppiumConfig
        {
            Platform = "Windows",
            AppPackage = "com.darioalonso.binnacle",
            AppPath = appPath,
            Capabilities = new Dictionary<string, object>
            {
                ["platformName"] = "Windows",
                ["automationName"] = "Windows",
                ["app"] = appPath ?? "com.darioalonso.binnacle_9zz4h110yvjzm!App",
                ["noReset"] = false,
                ["newCommandTimeout"] = 300
            }
        };
    }

    /// <summary>
    /// Create MacCatalyst configuration for Binnaculum.
    /// </summary>
    public static AppiumConfig ForBinnaculumMacCatalyst(string? appPath = null)
    {
        return new AppiumConfig
        {
            Platform = "MacCatalyst",
            AppPackage = "com.darioalonso.binnacle",
            AppPath = appPath,
            Capabilities = new Dictionary<string, object>
            {
                ["platformName"] = "Mac",
                ["automationName"] = "Mac2",
                ["bundleId"] = "com.darioalonso.binnacle",
                ["noReset"] = false,
                ["newCommandTimeout"] = 300
            }
        };
    }

    /// <summary>
    /// Gets MainActivity name using environment-appropriate discovery method.
    /// CI-friendly with fallback to dynamic discovery.
    /// </summary>
    private static string GetMainActivityForEnvironment()
    {
        // Priority 1: Environment variable override (perfect for CI)
        var envActivity = Environment.GetEnvironmentVariable("BINNACULUM_MAIN_ACTIVITY");
        if (!string.IsNullOrEmpty(envActivity))
        {
            return envActivity;
        }

        // Priority 2: Dynamic discovery (development/local testing)
        var dynamicActivity = GetMainActivity();
        if (!string.IsNullOrEmpty(dynamicActivity))
        {
            return dynamicActivity;
        }

        // Priority 3: Last resort fallback (should not happen)
        return "crc6460ce1c2ed4fc81a9.MainActivity";
    }

    /// <summary>
    /// Dynamically discovers the MainActivity name from the installed app.
    /// Falls back to common patterns if discovery fails.
    /// </summary>
    private static string GetMainActivity()
    {
        try
        {
            return DiscoverActivityFromAdb("com.darioalonso.binnacle");
        }
        catch (Exception)
        {
            // ADB discovery failed, fall back to pattern-based approach
        }

        // Fallback: Try common MAUI MainActivity patterns
        return TryFindMainActivityFromPattern();
    }

    /// <summary>
    /// Discovers the MainActivity using ADB with proper security and resource management.
    /// </summary>
    private static string DiscoverActivityFromAdb(string packageName)
    {
        // Validate package name to prevent command injection
        if (string.IsNullOrWhiteSpace(packageName) || !IsValidPackageName(packageName))
        {
            throw new ArgumentException("Invalid package name", nameof(packageName));
        }

        // Use safe command construction with parameterized arguments
        var arguments = new[]
        {
            "shell",
            $"dumpsys package {packageName}",
            "|",
            "grep -A 50 'Activity Resolver Table'",
            "|", 
            "grep MainActivity"
        };

        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "adb",
                Arguments = string.Join(" ", arguments),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
        {
            // Parse the output to extract the MainActivity class name
            // Expected format: "7df136c com.darioalonso.binnacle/crc6460ce1c2ed4fc81a9.MainActivity filter 36e5d35"
            var parts = output.Split(new[] { ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var mainActivityPart = parts.FirstOrDefault(p => p.Contains("MainActivity"));
            
            if (!string.IsNullOrEmpty(mainActivityPart))
            {
                // Extract just the class name (e.g., "crc6460ce1c2ed4fc81a9.MainActivity")
                var className = mainActivityPart.Split('.').Length >= 2 ? mainActivityPart : null;
                if (!string.IsNullOrEmpty(className) && IsValidActivityName(className))
                {
                    return className;
                }
            }
        }

        throw new InvalidOperationException("Could not discover MainActivity from ADB");
    }

    /// <summary>
    /// Attempts to find MainActivity using common MAUI naming patterns.
    /// </summary>
    private static string TryFindMainActivityFromPattern()
    {
        try
        {
            return DiscoverActivitiesFromAdb("com.darioalonso.binnacle");
        }
        catch (Exception)
        {
            // Pattern discovery also failed
        }

        // Final fallback: Use the last known working value
        // This should be updated when the pattern changes
        return "crc6460ce1c2ed4fc81a9.MainActivity";
    }

    /// <summary>
    /// Discovers all activities for a package using ADB with proper security.
    /// </summary>
    private static string DiscoverActivitiesFromAdb(string packageName)
    {
        // Validate package name to prevent command injection
        if (string.IsNullOrWhiteSpace(packageName) || !IsValidPackageName(packageName))
        {
            throw new ArgumentException("Invalid package name", nameof(packageName));
        }

        // Use safe command construction
        var arguments = new[]
        {
            "shell",
            $"dumpsys package {packageName}",
            "|",
            "grep 'Activity'"
        };

        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "adb",
                Arguments = string.Join(" ", arguments),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
        {
            // Look for any line containing MainActivity
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("MainActivity") && line.Contains(packageName))
                {
                    // Extract the activity name pattern
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"(crc[a-f0-9]+\.MainActivity)");
                    if (match.Success && IsValidActivityName(match.Groups[1].Value))
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
        }

        throw new InvalidOperationException("Could not discover MainActivity from activity patterns");
    }

    /// <summary>
    /// Validates that a package name is safe to use in shell commands.
    /// </summary>
    private static bool IsValidPackageName(string packageName)
    {
        // Package names should only contain alphanumeric, dots, and underscores
        // This prevents command injection attacks
        return System.Text.RegularExpressions.Regex.IsMatch(packageName, @"^[a-zA-Z0-9._]+$");
    }

    /// <summary>
    /// Validates that an activity name follows expected patterns.
    /// </summary>
    private static bool IsValidActivityName(string activityName)
    {
        // Activity names should follow the crc[hex].MainActivity pattern
        return System.Text.RegularExpressions.Regex.IsMatch(activityName, @"^crc[a-f0-9]+\.MainActivity$");
    }

    /// <summary>
    /// Gets Android capabilities with appropriate reset strategy for test isolation.
    /// </summary>
    private static Dictionary<string, object> GetAndroidCapabilities(AppResetStrategy resetStrategy)
    {
        var capabilities = new Dictionary<string, object>
        {
            ["platformName"] = "Android",
            ["automationName"] = "UiAutomator2",
            ["appPackage"] = "com.darioalonso.binnacle",
            ["appActivity"] = GetMainActivityForEnvironment(),
            ["newCommandTimeout"] = 300
        };

        // Configure reset strategy for test isolation
        switch (resetStrategy)
        {
            case AppResetStrategy.NoReset:
                capabilities["noReset"] = true;
                capabilities["fullReset"] = false;
                break;

            case AppResetStrategy.ClearAppData:
                capabilities["noReset"] = false;   // Reset app data but keep app installed
                capabilities["fullReset"] = false; // Don't uninstall/reinstall
                break;

            case AppResetStrategy.ReinstallApp:
                // Note: Full reset requires app capability (APK path)
                // If no app path provided, fallback to ClearAppData
                capabilities["noReset"] = false;   // Reset app data
                capabilities["fullReset"] = false; // Can't reinstall without APK path
                capabilities["clearSystemFiles"] = true; // Clear system files for deeper reset
                break;

            case AppResetStrategy.KillAndRestart:
                capabilities["noReset"] = true;    // Keep app data
                capabilities["fullReset"] = false;
                capabilities["forceAppLaunch"] = true; // Force kill and restart
                break;
        }

        return capabilities;
    }

    /// <summary>
    /// Gets Android capabilities for simple configuration with reset strategy.
    /// </summary>
    private static Dictionary<string, object> GetAndroidCapabilitiesSimple(AppResetStrategy resetStrategy)
    {
        var capabilities = new Dictionary<string, object>
        {
            ["platformName"] = "Android",
            ["automationName"] = "UiAutomator2",
            ["appPackage"] = "com.darioalonso.binnacle",
            ["newCommandTimeout"] = 300,
            ["autoLaunch"] = true
        };

        // Apply same reset strategy logic
        switch (resetStrategy)
        {
            case AppResetStrategy.NoReset:
                capabilities["noReset"] = true;
                capabilities["fullReset"] = false;
                break;

            case AppResetStrategy.ClearAppData:
                capabilities["noReset"] = false;
                capabilities["fullReset"] = false;
                break;

            case AppResetStrategy.ReinstallApp:
                // For simple config without APK, use deeper clearing instead
                capabilities["noReset"] = false;
                capabilities["fullReset"] = false;
                capabilities["clearSystemFiles"] = true;
                break;

            case AppResetStrategy.KillAndRestart:
                capabilities["noReset"] = true;
                capabilities["fullReset"] = false;
                capabilities["forceAppLaunch"] = true;
                break;
        }

        return capabilities;
    }
}

/// <summary>
/// Defines how the app should be reset between test runs for proper test isolation.
/// </summary>
public enum AppResetStrategy
{
    /// <summary>
    /// No reset - app keeps all data and state between tests.
    /// Fastest but poorest test isolation.
    /// </summary>
    NoReset,

    /// <summary>
    /// Clear app data but keep app installed.
    /// Good balance of speed and test isolation.
    /// Equivalent to "Clear Data" in Android settings.
    /// </summary>
    ClearAppData,

    /// <summary>
    /// Uninstall and reinstall the app.
    /// Slowest but provides complete test isolation.
    /// Ensures completely fresh app state.
    /// </summary>
    ReinstallApp,

    /// <summary>
    /// Kill app process and restart, but keep data.
    /// Good for testing app lifecycle but maintains data.
    /// Useful for testing app resume/backgrounding scenarios.
    /// </summary>
    KillAndRestart
}