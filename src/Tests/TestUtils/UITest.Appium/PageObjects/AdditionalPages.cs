using Binnaculum.UITest.Core;

namespace Binnaculum.UITest.Appium.PageObjects;

/// <summary>
/// Page Object Model for additional pages referenced by MainDashboardPage.
/// These are placeholder implementations that would need to be fleshed out.
/// </summary>

public class AccountCreatorPage : BasePage
{
    public AccountCreatorPage(IApp app) : base(app) { }

    public override void WaitForPageToLoad()
    {
        // Wait for account creator page to load
    }

    public override bool IsCurrentPage()
    {
        // Check if we're on the account creator page
        return false;
    }
}

public class SettingsPage : BasePage
{
    public SettingsPage(IApp app) : base(app) { }

    public override void WaitForPageToLoad()
    {
        // Wait for settings page to load
    }

    public override bool IsCurrentPage()
    {
        // Check if we're on the settings page
        return false;
    }
}

public class CalendarPage : BasePage
{
    public CalendarPage(IApp app) : base(app) { }

    public override void WaitForPageToLoad()
    {
        // Wait for calendar page to load
    }

    public override bool IsCurrentPage()
    {
        // Check if we're on the calendar page
        return false;
    }
}

public class PortfolioPage : BasePage
{
    private readonly IQuery _portfolioValue = null!;
    private readonly IQuery _gainLossPercentage = null!;
    private readonly IQuery _holdingsList = null!;

    public PortfolioPage(IApp app) : base(app)
    {
        _portfolioValue = _app.Query().ById("PortfolioValue");
        _gainLossPercentage = _app.Query().ById("GainLossPercentage");
        _holdingsList = _app.Query().ById("HoldingsList");
    }

    public override void WaitForPageToLoad()
    {
        WaitForElement(_portfolioValue);
    }

    public override bool IsCurrentPage()
    {
        try
        {
            var element = _app.WaitForElement(_portfolioValue, TimeSpan.FromSeconds(3));
            return element.IsDisplayed;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get the total portfolio value.
    /// </summary>
    public string GetPortfolioValue()
    {
        var element = WaitForElement(_portfolioValue);
        return element.Text ?? "0";
    }

    /// <summary>
    /// Get the gain/loss percentage.
    /// </summary>
    public string GetGainLossPercentage()
    {
        var element = WaitForElement(_gainLossPercentage);
        return element.Text ?? "0%";
    }

    /// <summary>
    /// Check if the portfolio shows gains.
    /// </summary>
    public bool ShowsGains()
    {
        var percentage = GetGainLossPercentage();
        return percentage.Contains("+") || (!percentage.Contains("-") && !percentage.Equals("0%"));
    }

    /// <summary>
    /// Check if the portfolio shows losses.
    /// </summary>
    public bool ShowsLosses()
    {
        var percentage = GetGainLossPercentage();
        return percentage.Contains("-") && !percentage.Equals("0%");
    }
}