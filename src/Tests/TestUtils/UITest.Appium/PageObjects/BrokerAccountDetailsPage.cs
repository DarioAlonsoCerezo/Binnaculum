using Binnaculum.UITest.Core;

namespace Binnaculum.UITest.Appium.PageObjects;

/// <summary>
/// Page Object Model for the broker account details page.
/// Handles interactions with individual broker account screens showing movements and balances.
/// </summary>
public class BrokerAccountDetailsPage : BasePage
{
    // Element selectors based on expected MAUI AutomationId or text
    private readonly IQuery _accountNameLabel = null!;
    private readonly IQuery _accountBalanceLabel = null!;
    private readonly IQuery _addMovementButton = null!;
    private readonly IQuery _movementsList = null!;
    private readonly IQuery _percentageControl = null!;
    private readonly IQuery _backButton = null!;

    public BrokerAccountDetailsPage(IApp app) : base(app)
    {
        // Initialize queries - these would need to be updated based on actual MAUI page structure
        _accountNameLabel = _app.Query().ById("AccountNameLabel");
        _accountBalanceLabel = _app.Query().ById("AccountBalanceLabel");
        _addMovementButton = _app.Query().ById("AddMovementButton");
        _movementsList = _app.Query().ById("MovementsList");
        _percentageControl = _app.Query().ById("PercentageControl");
        _backButton = _app.Query().ById("BackButton").First();
    }

    public override void WaitForPageToLoad()
    {
        WaitForElement(_accountNameLabel);
        WaitForElement(_accountBalanceLabel);
    }

    public override bool IsCurrentPage()
    {
        try
        {
            var nameElement = _app.WaitForElement(_accountNameLabel, TimeSpan.FromSeconds(3));
            var balanceElement = _app.WaitForElement(_accountBalanceLabel, TimeSpan.FromSeconds(3));
            return nameElement.IsDisplayed && balanceElement.IsDisplayed;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get the account name displayed on the page.
    /// </summary>
    public string GetAccountName()
    {
        var element = WaitForElement(_accountNameLabel);
        return element.Text ?? "";
    }

    /// <summary>
    /// Get the current account balance.
    /// </summary>
    public string GetAccountBalance()
    {
        var element = WaitForElement(_accountBalanceLabel);
        return element.Text ?? "0";
    }

    /// <summary>
    /// Get the percentage gain/loss displayed.
    /// </summary>
    public string GetPercentageDisplay()
    {
        var element = WaitForElement(_percentageControl);
        return element.Text ?? "0%";
    }

    /// <summary>
    /// Navigate to add a new movement.
    /// </summary>
    public MovementCreatorPage NavigateToAddMovement()
    {
        Tap(_addMovementButton);
        return new MovementCreatorPage(_app);
    }

    /// <summary>
    /// Check if a movement with the specified description exists.
    /// </summary>
    public bool HasMovementWithDescription(string description)
    {
        try
        {
            var movementQuery = _app.Query().ByText(description);
            var element = _app.WaitForElement(movementQuery, TimeSpan.FromSeconds(3));
            return element.IsDisplayed;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Assert that the percentage display has been updated.
    /// This checks that the percentage control shows a non-zero value.
    /// </summary>
    public void AssertPercentageUpdated()
    {
        var percentage = GetPercentageDisplay();
        if (percentage == "0%" || percentage == "0.00%" || string.IsNullOrEmpty(percentage))
        {
            throw new AssertionException($"Expected percentage to be updated, but got: {percentage}");
        }
    }

    /// <summary>
    /// Assert that a movement exists with the specified description.
    /// </summary>
    public void AssertMovementExists(string description)
    {
        if (!HasMovementWithDescription(description))
        {
            throw new AssertionException($"Expected movement with description '{description}' to exist, but it was not found.");
        }
    }

    /// <summary>
    /// Get the count of visible movements.
    /// </summary>
    public int GetMovementsCount()
    {
        // This would need to be implemented based on the actual UI structure
        // For now, return 0 as placeholder
        return 0;
    }

    /// <summary>
    /// Scroll down to load more movements if available.
    /// </summary>
    public void ScrollToLoadMoreMovements()
    {
        ScrollToElement(_movementsList, ScrollDirection.Down);
    }
}

/// <summary>
/// Simple assertion exception for test failures.
/// </summary>
public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}