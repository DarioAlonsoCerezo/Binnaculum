using Binnaculum.UITest.Core;

namespace Binnaculum.UITest.Appium.Components;

/// <summary>
/// Component Page Object for BrokerAccountTemplate control testing.
/// Tests specific touch interactions and percentage displays within the component.
/// </summary>
public class BrokerAccountTemplateComponent
{
    private readonly IApp _app;
    private readonly IQuery _componentRoot;
    private readonly IQuery _percentageDisplay;
    private readonly IQuery _accountNameLabel;
    private readonly IQuery _balanceLabel;

    public BrokerAccountTemplateComponent(IApp app, string accountName)
    {
        _app = app ?? throw new ArgumentNullException(nameof(app));
        
        // Find the component root by account name or other identifier
        _componentRoot = _app.Query().ByText(accountName).First();
        _percentageDisplay = _componentRoot.Child().ById("PercentageDisplay");
        _accountNameLabel = _componentRoot.Child().ById("AccountNameLabel");
        _balanceLabel = _componentRoot.Child().ById("BalanceLabel");
    }

    /// <summary>
    /// Tap the component to navigate to details.
    /// </summary>
    public void TapComponent()
    {
        _app.Tap(_componentRoot);
    }

    /// <summary>
    /// Long press the component for context actions.
    /// </summary>
    public void LongPressComponent()
    {
        var element = _app.WaitForElement(_componentRoot);
        element.LongPress();
    }

    /// <summary>
    /// Get the percentage display value.
    /// </summary>
    public string GetPercentageDisplay()
    {
        var element = _app.WaitForElement(_percentageDisplay);
        return element.Text ?? "0%";
    }

    /// <summary>
    /// Get the account name displayed.
    /// </summary>
    public string GetAccountName()
    {
        var element = _app.WaitForElement(_accountNameLabel);
        return element.Text ?? "";
    }

    /// <summary>
    /// Get the balance displayed.
    /// </summary>
    public string GetBalance()
    {
        var element = _app.WaitForElement(_balanceLabel);
        return element.Text ?? "0";
    }

    /// <summary>
    /// Check if the component shows positive percentage (gains).
    /// </summary>
    public bool ShowsGains()
    {
        var percentage = GetPercentageDisplay();
        return percentage.Contains("+") || (!percentage.Contains("-") && !percentage.Equals("0%"));
    }

    /// <summary>
    /// Check if the component shows negative percentage (losses).
    /// </summary>
    public bool ShowsLosses()
    {
        var percentage = GetPercentageDisplay();
        return percentage.Contains("-") && !percentage.Equals("0%");
    }

    /// <summary>
    /// Verify that touch gestures work correctly on the component.
    /// </summary>
    public void AssertTouchResponsive()
    {
        // Verify the component responds to tap
        var element = _app.WaitForElement(_componentRoot);
        if (!element.IsDisplayed || !element.IsEnabled)
        {
            throw new ComponentTestException("BrokerAccountTemplate component is not responsive to touch");
        }
    }

    /// <summary>
    /// Test swipe gestures on the component.
    /// </summary>
    public void SwipeLeft()
    {
        var element = _app.WaitForElement(_componentRoot);
        var location = element.Location;
        var size = element.Size;
        
        // Swipe from right to left within the component bounds
        _app.Swipe(location.X + size.Width - 10, location.Y + size.Height / 2, 
                   location.X + 10, location.Y + size.Height / 2);
    }

    /// <summary>
    /// Test swipe gestures on the component.
    /// </summary>
    public void SwipeRight()
    {
        var element = _app.WaitForElement(_componentRoot);
        var location = element.Location;
        var size = element.Size;
        
        // Swipe from left to right within the component bounds
        _app.Swipe(location.X + 10, location.Y + size.Height / 2, 
                   location.X + size.Width - 10, location.Y + size.Height / 2);
    }
}

/// <summary>
/// Exception for component testing failures.
/// </summary>
public class ComponentTestException : Exception
{
    public ComponentTestException(string message) : base(message) { }
}