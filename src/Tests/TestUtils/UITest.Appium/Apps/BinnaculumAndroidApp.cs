using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium;
using Binnaculum.UITest.Core;

namespace Binnaculum.UITest.Appium.Apps;

/// <summary>
/// Android-specific implementation of Binnaculum app for E2E testing.
/// Provides Android-specific capabilities and behaviors.
/// </summary>
public class BinnaculumAndroidApp : AppiumApp
{
    public BinnaculumAndroidApp(AndroidDriver driver, IConfig config) 
        : base(driver, config)
    {
    }

    public override void Back()
    {
        if (_driver is AndroidDriver androidDriver)
        {
            androidDriver.PressKeyCode(AndroidKeyCode.Back);
        }
        else
        {
            throw new InvalidOperationException("Back navigation requires AndroidDriver");
        }
    }

    public override void RestartApp()
    {
        CloseApp();
        
        // For Android, we can activate the app by package name
        if (_driver is AndroidDriver androidDriver && !string.IsNullOrEmpty(_config.AppPackage))
        {
            androidDriver.ActivateApp(_config.AppPackage);
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
    /// Android-specific helper to press home button.
    /// </summary>
    public void PressHome()
    {
        if (_driver is AndroidDriver androidDriver)
        {
            androidDriver.PressKeyCode(AndroidKeyCode.Home);
        }
    }

    /// <summary>
    /// Android-specific helper to press menu button.
    /// </summary>
    public void PressMenu()
    {
        if (_driver is AndroidDriver androidDriver)
        {
            androidDriver.PressKeyCode(AndroidKeyCode.Menu);
        }
    }

    /// <summary>
    /// Android-specific helper to open notifications.
    /// </summary>
    public void OpenNotifications()
    {
        if (_driver is AndroidDriver androidDriver)
        {
            androidDriver.OpenNotifications();
        }
    }

    /// <summary>
    /// Android-specific helper to rotate screen.
    /// </summary>
    public void RotateScreen(ScreenOrientation orientation)
    {
        if (_driver is AndroidDriver androidDriver)
        {
            androidDriver.Orientation = orientation;
        }
    }
}