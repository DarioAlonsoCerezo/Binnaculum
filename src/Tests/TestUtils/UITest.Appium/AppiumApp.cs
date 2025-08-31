using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using Binnaculum.UITest.Core;

namespace Binnaculum.UITest.Appium;

/// <summary>
/// Base Appium implementation of the IApp interface.
/// Based on Microsoft MAUI UITest.Appium AppiumApp pattern.
/// </summary>
public abstract class AppiumApp : IApp
{
    protected readonly AppiumDriver<IWebElement> _driver;
    protected readonly IConfig _config;
    private readonly ICommandExecution _commandExecutor;

    protected AppiumApp(AppiumDriver<IWebElement> driver, IConfig config)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _commandExecutor = new DefaultCommandExecution();
    }

    // IApp required properties
    public IConfig Config => _config;
    
    public ApplicationState AppState => GetAppState();
    
    public ICommandExecution CommandExecutor => _commandExecutor;

    // IUIElementQueryable implementation
    public string ElementTree => GetElementTree();

    public IUIElement? FindElement(IQuery query)
    {
        var appiumQuery = query as AppiumQuery ?? new AppiumQuery(_driver);
        var element = appiumQuery.FindElement();
        return element != null ? new AppiumDriverElement(element, _driver) : null;
    }

    public IReadOnlyCollection<IUIElement> FindElements(IQuery query)
    {
        var appiumQuery = query as AppiumQuery ?? new AppiumQuery(_driver);
        var elements = appiumQuery.FindElements();
        return elements.Select(e => new AppiumDriverElement(e, _driver)).ToList();
    }

    public bool ElementExists(IQuery query)
    {
        return FindElement(query) != null;
    }

    // IApp FindElement by string ID
    public IUIElement FindElement(string id)
    {
        var element = FindElement(Binnaculum.UITest.Core.By.Id(id));
        if (element == null)
            throw new ElementNotFoundException(id);
        return element;
    }

    // IScreenshotSupportedApp implementation
    public void SaveScreenshot(string filePath)
    {
        var screenshot = Screenshot();
        File.WriteAllBytes(filePath, screenshot);
    }

    private string GetElementTree()
    {
        try
        {
            return _driver.PageSource;
        }
        catch
        {
            return "Unable to retrieve element tree";
        }
    }

    public virtual IQuery Query(string? query = null)
    {
        var appiumQuery = new AppiumQuery(_driver);
        
        if (!string.IsNullOrEmpty(query))
        {
            // Try to determine query type and apply it
            if (query.StartsWith("//") || query.StartsWith("/"))
                return appiumQuery.ByXPath(query);
            
            // Try by ID first, then by text
            return appiumQuery.ById(query);
        }

        return appiumQuery;
    }

    public virtual void Tap(IQuery query)
    {
        var element = WaitForElement(query);
        element.Tap();
    }

    public virtual void Tap(int x, int y)
    {
        var actions = new OpenQA.Selenium.Interactions.Actions(_driver);
        actions.MoveByOffset(x, y).Click().Perform();
    }

    public virtual void EnterText(IQuery query, string text)
    {
        var element = WaitForElement(query);
        element.EnterText(text);
    }

    public virtual void EnterText(string text)
    {
        // Send text to the currently focused element
        _driver.SwitchTo().ActiveElement().SendKeys(text);
    }

    public virtual void ClearText(IQuery query)
    {
        var element = WaitForElement(query);
        element.ClearText();
    }

    public virtual void Swipe(int startX, int startY, int endX, int endY, int duration = 1000)
    {
        var actions = new OpenQA.Selenium.Interactions.Actions(_driver);
        actions.MoveByOffset(startX, startY)
               .ClickAndHold()
               .MoveByOffset(endX - startX, endY - startY)
               .Release()
               .Perform();
    }

    public virtual void ScrollTo(IQuery query, ScrollDirection direction = ScrollDirection.Down)
    {
        // Try to find element first
        if (TryFindElement(query) != null)
            return; // Already visible

        // Scroll in the specified direction until we find it
        var attempts = 0;
        const int maxAttempts = 10;
        var screenSize = _driver.Manage().Window.Size;

        while (attempts < maxAttempts)
        {
            if (TryFindElement(query) != null)
                break;

            // Perform scroll based on direction
            switch (direction)
            {
                case ScrollDirection.Down:
                    Swipe(screenSize.Width / 2, screenSize.Height * 3 / 4, 
                          screenSize.Width / 2, screenSize.Height / 4);
                    break;
                case ScrollDirection.Up:
                    Swipe(screenSize.Width / 2, screenSize.Height / 4, 
                          screenSize.Width / 2, screenSize.Height * 3 / 4);
                    break;
                case ScrollDirection.Left:
                    Swipe(screenSize.Width * 3 / 4, screenSize.Height / 2, 
                          screenSize.Width / 4, screenSize.Height / 2);
                    break;
                case ScrollDirection.Right:
                    Swipe(screenSize.Width / 4, screenSize.Height / 2, 
                          screenSize.Width * 3 / 4, screenSize.Height / 2);
                    break;
            }

            attempts++;
            Thread.Sleep(500);
        }
    }

    public virtual IUIElement WaitForElement(IQuery query, TimeSpan? timeout = null)
    {
        var wait = timeout ?? _config.DefaultTimeout;
        var endTime = DateTime.Now.Add(wait);

        while (DateTime.Now < endTime)
        {
            var element = TryFindElement(query);
            if (element != null)
                return element;

            Thread.Sleep(100);
        }

        throw new Binnaculum.UITest.Core.TimeoutException($"Element not found within {wait} using query: {query.GetQueryString()}");
    }

    public virtual void WaitForNoElement(IQuery query, TimeSpan? timeout = null)
    {
        var wait = timeout ?? _config.DefaultTimeout;
        var endTime = DateTime.Now.Add(wait);

        while (DateTime.Now < endTime)
        {
            var element = TryFindElement(query);
            if (element == null)
                return;

            Thread.Sleep(100);
        }

        throw new Binnaculum.UITest.Core.TimeoutException($"Element still present after {wait} using query: {query.GetQueryString()}");
    }

    protected virtual IUIElement? TryFindElement(IQuery query)
    {
        if (query is AppiumQuery appiumQuery)
        {
            var webElement = appiumQuery.FindElement();
            return webElement != null ? new AppiumDriverElement(webElement, _driver) : null;
        }

        return null;
    }

    public virtual byte[] Screenshot()
    {
        var screenshot = ((OpenQA.Selenium.ITakesScreenshot)_driver).GetScreenshot();
        return screenshot.AsByteArray;
    }

    public abstract void Back();

    public virtual void DismissKeyboard()
    {
        try
        {
            _driver.HideKeyboard();
        }
        catch
        {
            // Keyboard might not be visible
        }
    }

    public virtual ApplicationState GetAppState()
    {
        try
        {
            // Basic implementation - could be enhanced per platform
            return ApplicationState.RunningInForeground;
        }
        catch
        {
            return ApplicationState.Unknown;
        }
    }

    public virtual void RestartApp()
    {
        CloseApp();
        // Platform-specific restart logic should be implemented in derived classes
    }

    public virtual void CloseApp()
    {
        _driver.CloseApp();
    }

    public virtual void Dispose()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }
}