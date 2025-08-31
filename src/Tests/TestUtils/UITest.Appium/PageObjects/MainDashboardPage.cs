using Binnaculum.UITest.Core;

namespace Binnaculum.UITest.Appium.PageObjects;

/// <summary>
/// Page Object Model for the main dashboard/overview page of Binnaculum.
/// Represents the entry point with portfolio overview and navigation to other screens.
/// </summary>
public class MainDashboardPage : BasePage
{
    // Element selectors based on expected MAUI AutomationId or text
    private readonly IQuery _totalPortfolioValue = null!;
    private readonly IQuery _addBrokerAccountButton = null!;
    private readonly IQuery _brokerAccountsList = null!;
    private readonly IQuery _settingsButton = null!;
    private readonly IQuery _calendarButton = null!;

    public MainDashboardPage(IApp app) : base(app)
    {
        // Initialize queries - these would need to be updated based on actual MAUI page structure
        _totalPortfolioValue = _app.Query().ById("TotalPortfolioValue");
        _addBrokerAccountButton = _app.Query().ById("AddBrokerAccountButton");
        _brokerAccountsList = _app.Query().ById("BrokerAccountsList");
        _settingsButton = _app.Query().ById("SettingsButton");
        _calendarButton = _app.Query().ById("CalendarButton");
    }

    public override void WaitForPageToLoad()
    {
        WaitForElement(_totalPortfolioValue);
    }

    public override bool IsCurrentPage()
    {
        try
        {
            var element = _app.WaitForElement(_totalPortfolioValue, TimeSpan.FromSeconds(3));
            return element.IsDisplayed;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get the total portfolio value displayed on the dashboard.
    /// </summary>
    public string GetTotalPortfolioValue()
    {
        var element = WaitForElement(_totalPortfolioValue);
        return element.Text ?? "0";
    }

    /// <summary>
    /// Navigate to the Add Broker Account page.
    /// </summary>
    public AccountCreatorPage NavigateToAddBrokerAccount()
    {
        Tap(_addBrokerAccountButton);
        return new AccountCreatorPage(_app);
    }

    /// <summary>
    /// Navigate to a specific broker account details page.
    /// </summary>
    public BrokerAccountDetailsPage NavigateToBrokerAccount(string brokerName)
    {
        // Look for broker account by text/name
        var brokerAccountQuery = _app.Query().ByText(brokerName);
        ScrollToElement(brokerAccountQuery);
        Tap(brokerAccountQuery);
        
        return new BrokerAccountDetailsPage(_app);
    }

    /// <summary>
    /// Navigate to the settings page.
    /// </summary>
    public SettingsPage NavigateToSettings()
    {
        Tap(_settingsButton);
        return new SettingsPage(_app);
    }

    /// <summary>
    /// Navigate to the calendar page.
    /// </summary>
    public CalendarPage NavigateToCalendar()
    {
        Tap(_calendarButton);
        return new CalendarPage(_app);
    }

    /// <summary>
    /// Get the list of visible broker accounts.
    /// </summary>
    public List<string> GetVisibleBrokerAccounts()
    {
        var accounts = new List<string>();
        
        // This would need to be implemented based on the actual UI structure
        // For now, return empty list as placeholder
        return accounts;
    }

    /// <summary>
    /// Check if the dashboard shows any portfolio data.
    /// </summary>
    public bool HasPortfolioData()
    {
        try
        {
            var portfolioValue = GetTotalPortfolioValue();
            return !string.IsNullOrEmpty(portfolioValue) && portfolioValue != "0";
        }
        catch
        {
            return false;
        }
    }
}