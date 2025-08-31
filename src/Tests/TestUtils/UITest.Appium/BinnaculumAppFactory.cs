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
    public static IApp CreateApp(IConfig config, Uri? appiumServerUri = null)
    {
        var serverUri = appiumServerUri ?? new Uri("http://127.0.0.1:4723/");
        
        return config.Platform.ToLowerInvariant() switch
        {
            "android" => CreateAndroidApp(config, serverUri),
            "ios" => CreateiOSApp(config, serverUri),
            "windows" => CreateWindowsApp(config, serverUri),
            "maccatalyst" or "mac" => CreateMacCatalystApp(config, serverUri),
            _ => throw new NotSupportedException($"Platform '{config.Platform}' is not supported")
        };
    }

    private static BinnaculumAndroidApp CreateAndroidApp(IConfig config, Uri serverUri)
    {
        var options = new AppiumOptions();
        
        // Set required capabilities
        options.AddAdditionalCapability("platformName", "Android");
        options.AddAdditionalCapability("automationName", "UiAutomator2");
        
        // Add device-specific capabilities
        if (!string.IsNullOrEmpty(config.DeviceId))
            options.AddAdditionalCapability("udid", config.DeviceId);
        
        if (!string.IsNullOrEmpty(config.AppPackage))
            options.AddAdditionalCapability("appPackage", config.AppPackage);
        
        if (!string.IsNullOrEmpty(config.AppPath))
            options.AddAdditionalCapability("app", config.AppPath);
        
        // Add any additional capabilities
        foreach (var capability in config.Capabilities)
        {
            options.AddAdditionalCapability(capability.Key, capability.Value);
        }

        var driver = new AndroidDriver<IWebElement>(serverUri, options, config.DefaultTimeout);
        return new BinnaculumAndroidApp(driver, config);
    }

    private static BinnaculumiOSApp CreateiOSApp(IConfig config, Uri serverUri)
    {
        var options = new AppiumOptions();
        
        // Set required capabilities
        options.AddAdditionalCapability("platformName", "iOS");
        options.AddAdditionalCapability("automationName", "XCUITest");
        
        // Add device-specific capabilities
        if (!string.IsNullOrEmpty(config.DeviceId))
            options.AddAdditionalCapability("udid", config.DeviceId);
        
        if (!string.IsNullOrEmpty(config.AppPackage))
            options.AddAdditionalCapability("bundleId", config.AppPackage);
        
        if (!string.IsNullOrEmpty(config.AppPath))
            options.AddAdditionalCapability("app", config.AppPath);
        
        // Add any additional capabilities
        foreach (var capability in config.Capabilities)
        {
            options.AddAdditionalCapability(capability.Key, capability.Value);
        }

        var driver = new IOSDriver<IWebElement>(serverUri, options, config.DefaultTimeout);
        return new BinnaculumiOSApp(driver, config);
    }

    private static BinnaculumWindowsApp CreateWindowsApp(IConfig config, Uri serverUri)
    {
        var options = new AppiumOptions();
        
        // Set required capabilities
        options.AddAdditionalCapability("platformName", "Windows");
        options.AddAdditionalCapability("automationName", "Windows");
        
        if (!string.IsNullOrEmpty(config.AppPath))
            options.AddAdditionalCapability("app", config.AppPath);
        else if (!string.IsNullOrEmpty(config.AppPackage))
            options.AddAdditionalCapability("app", config.AppPackage);
        
        // Add any additional capabilities
        foreach (var capability in config.Capabilities)
        {
            options.AddAdditionalCapability(capability.Key, capability.Value);
        }

        var driver = new WindowsDriver<IWebElement>(serverUri, options, config.DefaultTimeout);
        return new BinnaculumWindowsApp(driver, config);
    }

    private static BinnaculumMacCatalystApp CreateMacCatalystApp(IConfig config, Uri serverUri)
    {
        var options = new AppiumOptions();
        
        // Set required capabilities
        options.AddAdditionalCapability("platformName", "Mac");
        options.AddAdditionalCapability("automationName", "Mac2");
        
        if (!string.IsNullOrEmpty(config.AppPackage))
            options.AddAdditionalCapability("bundleId", config.AppPackage);
        
        if (!string.IsNullOrEmpty(config.AppPath))
            options.AddAdditionalCapability("app", config.AppPath);
        
        // Add any additional capabilities
        foreach (var capability in config.Capabilities)
        {
            options.AddAdditionalCapability(capability.Key, capability.Value);
        }

        var driver = new MacDriver<IWebElement>(serverUri, options, config.DefaultTimeout);
        return new BinnaculumMacCatalystApp(driver, config);
    }
}