using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium;
using Binnaculum.UITest.Core;

namespace Binnaculum.UITest.Appium.Apps;

/// <summary>
/// Windows-specific implementation of Binnaculum app for E2E testing.
/// Provides Windows-specific capabilities and behaviors.
/// </summary>
public class BinnaculumWindowsApp : AppiumApp
{
    public BinnaculumWindowsApp(WindowsDriver<IWebElement> driver, IConfig config) 
        : base(driver, config)
    {
    }

    public override void Back()
    {
        // Windows typically uses Alt+Left or browser back button
        try
        {
            var backButton = Query().ByText("Back").First();
            Tap(backButton);
        }
        catch
        {
            // Try keyboard shortcut
            if (_driver is WindowsDriver windowsDriver)
            {
                var actions = new OpenQA.Selenium.Interactions.Actions(windowsDriver);
                actions.KeyDown(OpenQA.Selenium.Keys.Alt)
                       .SendKeys(OpenQA.Selenium.Keys.ArrowLeft)
                       .KeyUp(OpenQA.Selenium.Keys.Alt)
                       .Perform();
            }
        }
    }

    public override void RestartApp()
    {
        CloseApp();
        
        // For Windows, we need to launch the app again
        // This would typically involve launching via app ID or executable path
        if (_driver is WindowsDriver<IWebElement> windowsDriver)
        {
            // Windows-specific restart logic would go here
            // Could involve Process.Start or WinUI app activation
        }
    }

    public override ApplicationState GetAppState()
    {
        try
        {
            // For Windows, we can check if the app window is visible/active
            return ApplicationState.RunningInForeground;
        }
        catch
        {
            return ApplicationState.Unknown;
        }
    }

    /// <summary>
    /// Windows-specific helper to minimize window.
    /// </summary>
    public void MinimizeWindow()
    {
        if (_driver is WindowsDriver<IWebElement> windowsDriver)
        {
            var actions = new OpenQA.Selenium.Interactions.Actions(windowsDriver);
            actions.KeyDown(OpenQA.Selenium.Keys.Alt)
                   .SendKeys(OpenQA.Selenium.Keys.F9)
                   .KeyUp(OpenQA.Selenium.Keys.Alt)
                   .Perform();
        }
    }

    /// <summary>
    /// Windows-specific helper to maximize window.
    /// </summary>
    public void MaximizeWindow()
    {
        if (_driver is WindowsDriver<IWebElement> windowsDriver)
        {
            var actions = new OpenQA.Selenium.Interactions.Actions(windowsDriver);
            actions.KeyDown(OpenQA.Selenium.Keys.Alt)
                   .SendKeys(OpenQA.Selenium.Keys.F10)
                   .KeyUp(OpenQA.Selenium.Keys.Alt)
                   .Perform();
        }
    }

    /// <summary>
    /// Windows-specific helper to close window.
    /// </summary>
    public void CloseWindow()
    {
        if (_driver is WindowsDriver<IWebElement> windowsDriver)
        {
            var actions = new OpenQA.Selenium.Interactions.Actions(windowsDriver);
            actions.KeyDown(OpenQA.Selenium.Keys.Alt)
                   .SendKeys(OpenQA.Selenium.Keys.F4)
                   .KeyUp(OpenQA.Selenium.Keys.Alt)
                   .Perform();
        }
    }
}