using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.iOS;
using OpenQA.Selenium;
using Binnaculum.UITest.Core;

namespace Binnaculum.UITest.Appium.Apps;

/// <summary>
/// iOS-specific implementation of Binnaculum app for E2E testing.
/// Provides iOS-specific capabilities and behaviors.
/// </summary>
public class BinnaculumiOSApp : AppiumApp
{
    public BinnaculumiOSApp(IOSDriver<IWebElement> driver, IConfig config) 
        : base(driver, config)
    {
    }

    public override void Back()
    {
        // iOS typically uses navigation bar back button or swipe gesture
        // Try to find a back button first
        try
        {
            var backButton = Query().ByText("Back").First();
            Tap(backButton);
        }
        catch
        {
            // If no back button, try swipe gesture from left edge
            var screenSize = _driver.Manage().Window.Size;
            Swipe(0, screenSize.Height / 2, screenSize.Width / 4, screenSize.Height / 2);
        }
    }

    public override void RestartApp()
    {
        CloseApp();
        
        // For iOS, we can activate the app by bundle ID
        if (_driver is IOSDriver<IWebElement> iosDriver && !string.IsNullOrEmpty(_config.AppPackage))
        {
            iosDriver.ActivateApp(_config.AppPackage);
        }
    }

    public override ApplicationState GetAppState()
    {
        try
        {
            // Simplified implementation - just return running state
            // Full implementation would need proper Appium AppState query
            return ApplicationState.RunningInForeground;
        }
        catch
        {
            return ApplicationState.Unknown;
        }
    }

    /// <summary>
    /// iOS-specific helper to press home button (for devices with home button).
    /// </summary>
    public void PressHome()
    {
        if (_driver is IOSDriver<IWebElement> iosDriver)
        {
            iosDriver.ExecuteScript("mobile: pressButton", new Dictionary<string, object> { ["name"] = "home" });
        }
    }

    /// <summary>
    /// iOS-specific helper to activate Siri.
    /// </summary>
    public void ActivateSiri()
    {
        if (_driver is IOSDriver<IWebElement> iosDriver)
        {
            iosDriver.ExecuteScript("mobile: activateSiri");
        }
    }

    /// <summary>
    /// iOS-specific helper to rotate screen.
    /// </summary>
    public void RotateScreen(ScreenOrientation orientation)
    {
        if (_driver is IOSDriver<IWebElement> iosDriver)
        {
            iosDriver.Orientation = orientation;
        }
    }
}