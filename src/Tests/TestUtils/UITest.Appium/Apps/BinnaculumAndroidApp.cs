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
    public BinnaculumAndroidApp(AndroidDriver<IWebElement> driver, IConfig config) 
        : base(driver, config)
    {
    }

    public override void Back()
    {
        if (_driver is AndroidDriver<IWebElement> androidDriver)
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
        if (_driver is AndroidDriver<IWebElement> androidDriver && !string.IsNullOrEmpty(_config.AppPackage))
        {
            androidDriver.ActivateApp(_config.AppPackage);
        }
    }

    public override AppState GetAppState()
    {
        try
        {
            if (_driver is AndroidDriver<IWebElement> androidDriver && !string.IsNullOrEmpty(_config.AppPackage))
            {
                var state = androidDriver.QueryAppState(_config.AppPackage);
                return state switch
                {
                    ApplicationState.NotInstalled => AppState.NotInstalled,
                    ApplicationState.NotRunning => AppState.NotRunning,
                    ApplicationState.RunningInBackground => AppState.RunningInBackground,
                    ApplicationState.RunningInForeground => AppState.RunningInForeground,
                    _ => AppState.Unknown
                };
            }
            
            return AppState.Unknown;
        }
        catch
        {
            return AppState.Unknown;
        }
    }

    /// <summary>
    /// Android-specific helper to press home button.
    /// </summary>
    public void PressHome()
    {
        if (_driver is AndroidDriver<IWebElement> androidDriver)
        {
            androidDriver.PressKeyCode(AndroidKeyCode.Home);
        }
    }

    /// <summary>
    /// Android-specific helper to press menu button.
    /// </summary>
    public void PressMenu()
    {
        if (_driver is AndroidDriver<IWebElement> androidDriver)
        {
            androidDriver.PressKeyCode(AndroidKeyCode.Menu);
        }
    }

    /// <summary>
    /// Android-specific helper to open notifications.
    /// </summary>
    public void OpenNotifications()
    {
        if (_driver is AndroidDriver<IWebElement> androidDriver)
        {
            androidDriver.OpenNotifications();
        }
    }

    /// <summary>
    /// Android-specific helper to rotate screen.
    /// </summary>
    public void RotateScreen(ScreenOrientation orientation)
    {
        if (_driver is AndroidDriver<IWebElement> androidDriver)
        {
            androidDriver.Orientation = orientation;
        }
    }
}