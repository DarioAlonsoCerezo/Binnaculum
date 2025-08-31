using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Mac;
using OpenQA.Selenium;
using Binnaculum.UITest.Core;

namespace Binnaculum.UITest.Appium.Apps;

/// <summary>
/// MacCatalyst-specific implementation of Binnaculum app for E2E testing.
/// Provides MacCatalyst-specific capabilities and behaviors.
/// </summary>
public class BinnaculumMacCatalystApp : AppiumApp
{
    public BinnaculumMacCatalystApp(MacDriver<IWebElement> driver, IConfig config) 
        : base(driver, config)
    {
    }

    public override void Back()
    {
        // MacCatalyst typically uses navigation bar back button or CMD+Left
        try
        {
            var backButton = Query().ByText("Back").First();
            Tap(backButton);
        }
        catch
        {
            // Try keyboard shortcut
            if (_driver is MacDriver macDriver)
            {
                var actions = new OpenQA.Selenium.Interactions.Actions(macDriver);
                actions.KeyDown(OpenQA.Selenium.Keys.Command)
                       .SendKeys(OpenQA.Selenium.Keys.ArrowLeft)
                       .KeyUp(OpenQA.Selenium.Keys.Command)
                       .Perform();
            }
        }
    }

    public override void RestartApp()
    {
        CloseApp();
        
        // For MacCatalyst, we can activate the app by bundle ID
        if (_driver is MacDriver<IWebElement> macDriver && !string.IsNullOrEmpty(_config.AppPackage))
        {
            macDriver.ActivateApp(_config.AppPackage);
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
    /// MacCatalyst-specific helper to minimize window.
    /// </summary>
    public void MinimizeWindow()
    {
        if (_driver is MacDriver<IWebElement> macDriver)
        {
            var actions = new OpenQA.Selenium.Interactions.Actions(macDriver);
            actions.KeyDown(OpenQA.Selenium.Keys.Command)
                   .SendKeys("m")
                   .KeyUp(OpenQA.Selenium.Keys.Command)
                   .Perform();
        }
    }

    /// <summary>
    /// MacCatalyst-specific helper to close window.
    /// </summary>
    public void CloseWindow()
    {
        if (_driver is MacDriver<IWebElement> macDriver)
        {
            var actions = new OpenQA.Selenium.Interactions.Actions(macDriver);
            actions.KeyDown(OpenQA.Selenium.Keys.Command)
                   .SendKeys("w")
                   .KeyUp(OpenQA.Selenium.Keys.Command)
                   .Perform();
        }
    }

    /// <summary>
    /// MacCatalyst-specific helper for menu navigation.
    /// </summary>
    public void AccessMenu(string menuName)
    {
        if (_driver is MacDriver<IWebElement> macDriver)
        {
            // Click on menu bar
            var menuBar = Query().ByClass("MenuBar").First();
            Tap(menuBar);
            
            // Click on specific menu
            var menu = Query().ByText(menuName).First();
            Tap(menu);
        }
    }
}