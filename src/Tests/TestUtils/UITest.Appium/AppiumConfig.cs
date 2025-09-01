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
    /// </summary>
    public static AppiumConfig ForBinnaculumAndroid(string? appPath = null, string? deviceId = null)
    {
        return new AppiumConfig
        {
            Platform = "Android",
            AppPackage = "com.darioalonso.binnacle", // Based on UI project ApplicationId
            AppPath = appPath,
            DeviceId = deviceId,
            Capabilities = new Dictionary<string, object>
            {
                ["platformName"] = "Android",
                ["automationName"] = "UiAutomator2",
                ["appPackage"] = "com.darioalonso.binnacle",
                ["appActivity"] = GetMainActivityForEnvironment(), // Smart discovery based on environment
                ["noReset"] = false,
                ["newCommandTimeout"] = 300
            }
        };
    }

    /// <summary>
    /// Create Android configuration for Binnaculum with automatic activity detection.
    /// </summary>
    public static AppiumConfig ForBinnaculumAndroidSimple(string? appPath = null, string? deviceId = null)
    {
        return new AppiumConfig
        {
            Platform = "Android",
            AppPackage = "com.darioalonso.binnacle",
            AppPath = appPath,
            DeviceId = deviceId,
            Capabilities = new Dictionary<string, object>
            {
                ["platformName"] = "Android",
                ["automationName"] = "UiAutomator2",
                ["appPackage"] = "com.darioalonso.binnacle",
                // Omit appActivity to let Appium discover the launch activity automatically
                ["noReset"] = false,
                ["newCommandTimeout"] = 300,
                ["autoLaunch"] = true  // Ensure Appium launches the app
            }
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
            // Try to discover the actual MainActivity using ADB
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = "shell \"dumpsys package com.darioalonso.binnacle | grep -A 50 'Activity Resolver Table' | grep MainActivity\"",
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
                    if (!string.IsNullOrEmpty(className))
                    {
                        return className;
                    }
                }
            }
        }
        catch (Exception)
        {
            // ADB discovery failed, fall back to pattern-based approach
        }

        // Fallback: Try common MAUI MainActivity patterns
        return TryFindMainActivityFromPattern();
    }

    /// <summary>
    /// Attempts to find MainActivity using common MAUI naming patterns.
    /// </summary>
    private static string TryFindMainActivityFromPattern()
    {
        try
        {
            // Get list of all activities for the package
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = "shell \"dumpsys package com.darioalonso.binnacle | grep 'Activity'\"",
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
                    if (line.Contains("MainActivity") && line.Contains("com.darioalonso.binnacle"))
                    {
                        // Extract the activity name pattern
                        var match = System.Text.RegularExpressions.Regex.Match(line, @"(crc[a-f0-9]+\.MainActivity)");
                        if (match.Success)
                        {
                            return match.Groups[1].Value;
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // Pattern discovery also failed
        }

        // Final fallback: Use the last known working value
        // This should be updated when the pattern changes
        return "crc6460ce1c2ed4fc81a9.MainActivity";
    }
}