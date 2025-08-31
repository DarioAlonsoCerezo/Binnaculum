using OpenQA.Selenium.Appium;
using OpenQA.Selenium;
using Binnaculum.UITest.Core;

namespace Binnaculum.UITest.Appium;

/// <summary>
/// Appium implementation of IUIElement for element interactions.
/// Based on Microsoft MAUI UITest.Appium AppiumDriverElement pattern.
/// </summary>
public class AppiumDriverElement : IUIElement
{
    private readonly IWebElement _element;
    private readonly AppiumDriver _driver;

    public AppiumDriverElement(IWebElement element, AppiumDriver driver)
    {
        _element = element ?? throw new ArgumentNullException(nameof(element));
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    public string? Id => GetAttribute("resource-id") ?? GetAttribute("name") ?? GetAttribute("id");

    public string? Text => _element.Text;

    public string? Class => _element.TagName;

    public bool IsEnabled => _element.Enabled;

    public bool IsDisplayed => _element.Displayed;

    public Rectangle Location
    {
        get
        {
            var location = _element.Location;
            var size = _element.Size;
            return new Rectangle(location.X, location.Y, size.Width, size.Height);
        }
    }

    public Size Size
    {
        get
        {
            var size = _element.Size;
            return new Size(size.Width, size.Height);
        }
    }

    public void Tap()
    {
        _element.Click();
    }

    public void LongPress()
    {
        // For Appium, we use Actions to perform long press
        var actions = new OpenQA.Selenium.Interactions.Actions(_driver);
        actions.ClickAndHold(_element).Perform();
        Thread.Sleep(1000); // Hold for 1 second
        actions.Release(_element).Perform();
    }

    public void DoubleTap()
    {
        var actions = new OpenQA.Selenium.Interactions.Actions(_driver);
        actions.DoubleClick(_element).Perform();
    }

    public void EnterText(string text)
    {
        _element.Clear();
        _element.SendKeys(text);
    }

    public void ClearText()
    {
        _element.Clear();
    }

    public string? GetAttribute(string attributeName)
    {
        try
        {
            return _element.GetAttribute(attributeName);
        }
        catch
        {
            return null;
        }
    }

    public bool WaitForElement(TimeSpan? timeout = null)
    {
        var wait = timeout ?? TimeSpan.FromSeconds(10);
        var endTime = DateTime.Now.Add(wait);

        while (DateTime.Now < endTime)
        {
            try
            {
                if (IsDisplayed)
                    return true;
            }
            catch
            {
                // Element might not be available yet
            }

            Thread.Sleep(100);
        }

        return false;
    }
}