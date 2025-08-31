namespace Binnaculum.UITest.Core;

/// <summary>
/// Concrete implementation of IConfig with platform detection and default settings.
/// Provides configuration management for UITest framework.
/// </summary>
public class Config : IConfig
{
    public Config(TestPlatform platform = TestPlatform.Unknown)
    {
        // Auto-detect platform if not specified
        if (platform == TestPlatform.Unknown)
        {
            platform = TestDevice.DetectHostPlatform();
        }

        Platform = platform.ToString();
        Capabilities = new Dictionary<string, object>();
        DefaultTimeout = TimeSpan.FromSeconds(10);
        ResetApp = true;

        // Set platform-specific defaults
        ConfigureForPlatform(platform);
    }

    public string Platform { get; set; }
    public string? AppPackage { get; set; }
    public string? AppPath { get; set; }
    public string? DeviceId { get; set; }
    public Dictionary<string, object> Capabilities { get; set; }
    public TimeSpan DefaultTimeout { get; set; }
    public bool ResetApp { get; set; }

    /// <summary>
    /// Create a configuration for Android platform.
    /// </summary>
    /// <param name="appPackage">Android app package name</param>
    /// <param name="appPath">Path to APK file</param>
    /// <param name="deviceId">Android device ID</param>
    /// <returns>Android configuration</returns>
    public static Config ForAndroid(string? appPackage = null, string? appPath = null, string? deviceId = null)
    {
        var config = new Config(TestPlatform.Android)
        {
            AppPackage = appPackage ?? "com.darioalonso.binnacle",
            AppPath = appPath,
            DeviceId = deviceId
        };

        config.Capabilities["platformName"] = "Android";
        config.Capabilities["automationName"] = "UiAutomator2";
        config.Capabilities["appPackage"] = config.AppPackage;
        config.Capabilities["appActivity"] = "crc64e1fb321c08285b90.MainActivity";
        config.Capabilities["noReset"] = !config.ResetApp;
        config.Capabilities["newCommandTimeout"] = 300;

        return config;
    }

    /// <summary>
    /// Create a configuration for iOS platform.
    /// </summary>
    /// <param name="bundleId">iOS bundle identifier</param>
    /// <param name="appPath">Path to app file</param>
    /// <param name="deviceId">iOS device UDID</param>
    /// <returns>iOS configuration</returns>
    public static Config ForIOS(string? bundleId = null, string? appPath = null, string? deviceId = null)
    {
        var config = new Config(TestPlatform.iOS)
        {
            AppPackage = bundleId ?? "com.darioalonso.binnacle",
            AppPath = appPath,
            DeviceId = deviceId
        };

        config.Capabilities["platformName"] = "iOS";
        config.Capabilities["automationName"] = "XCUITest";
        config.Capabilities["bundleId"] = config.AppPackage;
        config.Capabilities["noReset"] = !config.ResetApp;
        config.Capabilities["newCommandTimeout"] = 300;

        return config;
    }

    /// <summary>
    /// Create a configuration for Windows platform.
    /// </summary>
    /// <param name="appId">Windows app ID</param>
    /// <param name="appPath">Path to app executable</param>
    /// <returns>Windows configuration</returns>
    public static Config ForWindows(string? appId = null, string? appPath = null)
    {
        var config = new Config(TestPlatform.Windows)
        {
            AppPackage = appId ?? "com.darioalonso.binnacle_9zz4h110yvjzm!App",
            AppPath = appPath
        };

        config.Capabilities["platformName"] = "Windows";
        config.Capabilities["automationName"] = "Windows";
        config.Capabilities["app"] = config.AppPackage;
        config.Capabilities["noReset"] = !config.ResetApp;
        config.Capabilities["newCommandTimeout"] = 300;

        return config;
    }

    /// <summary>
    /// Create a configuration for MacCatalyst platform.
    /// </summary>
    /// <param name="bundleId">macOS bundle identifier</param>
    /// <param name="appPath">Path to app bundle</param>
    /// <returns>MacCatalyst configuration</returns>
    public static Config ForMacCatalyst(string? bundleId = null, string? appPath = null)
    {
        var config = new Config(TestPlatform.MacCatalyst)
        {
            AppPackage = bundleId ?? "com.darioalonso.binnacle",
            AppPath = appPath
        };

        config.Capabilities["platformName"] = "Mac";
        config.Capabilities["automationName"] = "Mac2";
        config.Capabilities["bundleId"] = config.AppPackage;
        config.Capabilities["noReset"] = !config.ResetApp;
        config.Capabilities["newCommandTimeout"] = 300;

        return config;
    }

    /// <summary>
    /// Create a configuration automatically detecting the current platform.
    /// </summary>
    /// <returns>Platform-specific configuration</returns>
    public static Config ForCurrentPlatform()
    {
        var platform = TestDevice.DetectHostPlatform();
        
        return platform switch
        {
            TestPlatform.Android => ForAndroid(),
            TestPlatform.iOS => ForIOS(),
            TestPlatform.Windows => ForWindows(),
            TestPlatform.MacCatalyst => ForMacCatalyst(),
            _ => new Config(TestPlatform.Unknown)
        };
    }

    /// <summary>
    /// Configure platform-specific settings.
    /// </summary>
    /// <param name="platform">Target platform</param>
    private void ConfigureForPlatform(TestPlatform platform)
    {
        switch (platform)
        {
            case TestPlatform.Android:
                DefaultTimeout = TimeSpan.FromSeconds(15); // Android can be slower
                break;
            case TestPlatform.iOS:
                DefaultTimeout = TimeSpan.FromSeconds(12);
                break;
            case TestPlatform.Windows:
                DefaultTimeout = TimeSpan.FromSeconds(8);
                break;
            case TestPlatform.MacCatalyst:
                DefaultTimeout = TimeSpan.FromSeconds(10);
                break;
        }
    }

    /// <summary>
    /// Set a capability value.
    /// </summary>
    /// <param name="key">Capability key</param>
    /// <param name="value">Capability value</param>
    /// <returns>This config instance for method chaining</returns>
    public Config SetCapability(string key, object value)
    {
        Capabilities[key] = value;
        return this;
    }

    /// <summary>
    /// Set the default timeout.
    /// </summary>
    /// <param name="timeout">Timeout value</param>
    /// <returns>This config instance for method chaining</returns>
    public Config SetTimeout(TimeSpan timeout)
    {
        DefaultTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Set whether to reset the app between tests.
    /// </summary>
    /// <param name="reset">Whether to reset</param>
    /// <returns>This config instance for method chaining</returns>
    public Config SetResetApp(bool reset)
    {
        ResetApp = reset;
        if (Capabilities.ContainsKey("noReset"))
        {
            Capabilities["noReset"] = !reset;
        }
        return this;
    }

    public override string ToString()
    {
        return $"Config(Platform={Platform}, Package={AppPackage}, Timeout={DefaultTimeout.TotalSeconds}s)";
    }
}