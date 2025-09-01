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
                ["appActivity"] = "crc6460ce1c2ed4fc81a9.MainActivity", // Updated to actual activity name from ADB
                ["noReset"] = false,
                ["newCommandTimeout"] = 300
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
}