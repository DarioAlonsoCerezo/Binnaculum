using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;
using OpenQA.Selenium.Appium.Mac;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium;
using Binnaculum.UITest.Core;
using Binnaculum.UITest.Appium.Apps;

namespace Binnaculum.UITest.Appium;

/// <summary>
/// Factory for creating platform-specific Binnaculum app instances.
/// </summary>
public static class BinnaculumAppFactory
{
    /// <summary>
    /// Create a Binnaculum app instance for the specified platform.
    /// </summary>
    /// <param name="config">App configuration</param>
    /// <param name="appiumServerUri">Optional server URI. If null, attempts to detect from fixture.</param>
    /// <param name="serverFixture">Optional server fixture for automatic server URL detection.</param>
    public static IApp CreateApp(IConfig config, Uri? appiumServerUri = null, AppiumServerFixture? serverFixture = null)
    {
        // Priority: explicit URI > fixture URI > default URI
        var serverUri = appiumServerUri 
                       ?? serverFixture?.ServerUrl 
                       ?? new Uri("http://127.0.0.1:4723/");
        
        return config.Platform.ToLowerInvariant() switch
        {
            "android" => CreateAndroidApp(config, serverUri),
            "ios" => CreateiOSApp(config, serverUri),
            "windows" => CreateWindowsApp(config, serverUri),
            "maccatalyst" or "mac" => CreateMacCatalystApp(config, serverUri),
            _ => throw new NotSupportedException($"Platform '{config.Platform}' is not supported")
        };
    }

    /// <summary>
    /// Create a Binnaculum app instance with automatic server detection.
    /// Throws meaningful exception if server is not available.
    /// </summary>
    public static IApp CreateAppWithAutoServer(IConfig config, AppiumServerFixture serverFixture)
    {
        if (!serverFixture.IsServerAvailable)
        {
            throw new InvalidOperationException(
                "Appium server is not available. Ensure the AppiumServerFixture is properly initialized.");
        }

        return CreateApp(config, serverFixture.ServerUrl, serverFixture);
    }

    private static BinnaculumAndroidApp CreateAndroidApp(IConfig config, Uri serverUri)
    {
        var options = new AppiumOptions();
        
        // Set required capabilities using AddAdditionalOption
        options.AddAdditionalOption("platformName", "Android");
        options.AddAdditionalOption("automationName", "UiAutomator2");
        
        // Add device-specific capabilities
        if (!string.IsNullOrEmpty(config.DeviceId))
            options.AddAdditionalOption("udid", config.DeviceId);
        
        if (!string.IsNullOrEmpty(config.AppPackage))
            options.AddAdditionalOption("appPackage", config.AppPackage);
        
        if (!string.IsNullOrEmpty(config.AppPath))
            options.AddAdditionalOption("app", config.AppPath);
        
        // Add any additional capabilities
        foreach (var capability in config.Capabilities)
        {
            options.AddAdditionalOption(capability.Key, capability.Value);
        }

        var driver = new AndroidDriver(serverUri, options, config.DefaultTimeout);
        return new BinnaculumAndroidApp(driver, config);
    }

    private static BinnaculumiOSApp CreateiOSApp(IConfig config, Uri serverUri)
    {
        var options = new AppiumOptions();
        
        // Set required capabilities using AddAdditionalOption
        options.AddAdditionalOption("platformName", "iOS");
        options.AddAdditionalOption("automationName", "XCUITest");
        
        // Add device-specific capabilities
        if (!string.IsNullOrEmpty(config.DeviceId))
            options.AddAdditionalOption("udid", config.DeviceId);
        
        if (!string.IsNullOrEmpty(config.AppPackage))
            options.AddAdditionalOption("bundleId", config.AppPackage);
        
        if (!string.IsNullOrEmpty(config.AppPath))
            options.AddAdditionalOption("app", config.AppPath);
        
        // Add any additional capabilities
        foreach (var capability in config.Capabilities)
        {
            options.AddAdditionalOption(capability.Key, capability.Value);
        }

        var driver = new IOSDriver(serverUri, options, config.DefaultTimeout);
        return new BinnaculumiOSApp(driver, config);
    }

    private static BinnaculumWindowsApp CreateWindowsApp(IConfig config, Uri serverUri)
    {
        var options = new AppiumOptions();
        
        // Set required capabilities using AddAdditionalOption
        options.AddAdditionalOption("platformName", "Windows");
        options.AddAdditionalOption("automationName", "Windows");
        
        if (!string.IsNullOrEmpty(config.AppPath))
            options.AddAdditionalOption("app", config.AppPath);
        else if (!string.IsNullOrEmpty(config.AppPackage))
            options.AddAdditionalOption("app", config.AppPackage);
        
        // Add any additional capabilities
        foreach (var capability in config.Capabilities)
        {
            options.AddAdditionalOption(capability.Key, capability.Value);
        }

        var driver = new WindowsDriver(serverUri, options, config.DefaultTimeout);
        return new BinnaculumWindowsApp(driver, config);
    }

    private static BinnaculumMacCatalystApp CreateMacCatalystApp(IConfig config, Uri serverUri)
    {
        var options = new AppiumOptions();
        
        // Set required capabilities using AddAdditionalOption
        options.AddAdditionalOption("platformName", "Mac");
        options.AddAdditionalOption("automationName", "Mac2");
        
        if (!string.IsNullOrEmpty(config.AppPackage))
            options.AddAdditionalOption("bundleId", config.AppPackage);
        
        if (!string.IsNullOrEmpty(config.AppPath))
            options.AddAdditionalOption("app", config.AppPath);
        
        // Add any additional capabilities
        foreach (var capability in config.Capabilities)
        {
            options.AddAdditionalOption(capability.Key, capability.Value);
        }

        var driver = new MacDriver(serverUri, options, config.DefaultTimeout);
        return new BinnaculumMacCatalystApp(driver, config);
    }
}