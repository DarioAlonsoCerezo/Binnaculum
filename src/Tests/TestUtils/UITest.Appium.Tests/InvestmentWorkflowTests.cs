using Xunit;
using Binnaculum.UITest.Core;
using Binnaculum.UITest.Appium;
using Binnaculum.UITest.Appium.PageObjects;
using Binnaculum.UITest.Appium.TestData;

namespace Binnaculum.UITest.Appium.Tests;

/// <summary>
/// End-to-end tests for investment portfolio workflows in Binnaculum.
/// These tests validate complete user journeys from dashboard to movement creation.
/// </summary>
public class InvestmentWorkflowTests : IDisposable
{
    private readonly IApp _app;
    private readonly IConfig _config;

    public InvestmentWorkflowTests()
    {
        // Configure for Android testing - would need to be made configurable for CI/CD
        _config = AppiumConfig.ForBinnaculumAndroid();
        
        // Note: This would require Appium server to be running
        // In practice, this would be set up in test infrastructure
        try
        {
            _app = BinnaculumAppFactory.CreateApp(_config);
        }
        catch
        {
            // If Appium server is not available, skip tests
            Skip.If(true, "Appium server not available - skipping E2E tests");
            _app = null!;
        }
    }

    [Fact]
    public async Task AddInvestmentMovement_WithValidData_CreatesMovementSuccessfully()
    {
        // Skip if app not initialized (Appium not available)
        Skip.If(_app == null, "Appium not available");

        // Arrange
        var mainPage = new MainDashboardPage(_app);
        var testData = InvestmentTestData.CreateProfitableMovement();
        
        // Act
        mainPage.WaitForPageToLoad();
        var brokerPage = mainPage.NavigateToBrokerAccount("Test Broker");
        var movementPage = brokerPage.NavigateToAddMovement();
        movementPage.EnterMovementData(testData);
        brokerPage = movementPage.SaveMovement();
        
        // Assert  
        brokerPage.AssertMovementExists(testData.Description);
        brokerPage.AssertPercentageUpdated();
    }

    [Fact]
    public void Dashboard_OnAppStart_DisplaysPortfolioData()
    {
        // Skip if app not initialized
        Skip.If(_app == null, "Appium not available");

        // Arrange
        var mainPage = new MainDashboardPage(_app);
        
        // Act
        mainPage.WaitForPageToLoad();
        
        // Assert
        Assert.True(mainPage.IsCurrentPage(), "Should be on the main dashboard page");
        
        var portfolioValue = mainPage.GetTotalPortfolioValue();
        Assert.NotNull(portfolioValue);
        Assert.NotEqual("", portfolioValue);
    }

    [Fact]
    public void BrokerAccount_Navigation_WorksCorrectly()
    {
        // Skip if app not initialized
        Skip.If(_app == null, "Appium not available");

        // Arrange
        var mainPage = new MainDashboardPage(_app);
        mainPage.WaitForPageToLoad();
        
        // Act
        var brokerPage = mainPage.NavigateToBrokerAccount("Test Broker");
        
        // Assert
        Assert.True(brokerPage.IsCurrentPage(), "Should be on broker account details page");
        
        var accountName = brokerPage.GetAccountName();
        Assert.Contains("Test Broker", accountName);
    }

    [Fact]
    public void MovementCreator_WithDividendData_CreatesSuccessfully()
    {
        // Skip if app not initialized
        Skip.If(_app == null, "Appium not available");

        // Arrange
        var mainPage = new MainDashboardPage(_app);
        var testData = InvestmentTestData.CreateDividendMovement();
        
        // Act
        mainPage.WaitForPageToLoad();
        var brokerPage = mainPage.NavigateToBrokerAccount("Test Broker");
        var movementPage = brokerPage.NavigateToAddMovement();
        movementPage.EnterMovementData(testData);
        
        // Assert
        Assert.True(movementPage.IsSaveButtonEnabled(), "Save button should be enabled with valid dividend data");
        
        brokerPage = movementPage.SaveMovement();
        brokerPage.AssertMovementExists(testData.Description);
    }

    [Fact]
    public void Portfolio_MultipleMovements_ShowsCorrectBalance()
    {
        // Skip if app not initialized
        Skip.If(_app == null, "Appium not available");

        // Arrange
        var mainPage = new MainDashboardPage(_app);
        var movements = InvestmentTestData.CreateCompletePortfolioScenario();
        
        // Act
        mainPage.WaitForPageToLoad();
        var brokerPage = mainPage.NavigateToBrokerAccount("Test Broker");
        
        foreach (var movement in movements.Take(3)) // Add first 3 movements
        {
            var movementPage = brokerPage.NavigateToAddMovement();
            movementPage.EnterMovementData(movement);
            brokerPage = movementPage.SaveMovement();
        }
        
        // Assert
        var balance = brokerPage.GetAccountBalance();
        Assert.NotEqual("0", balance);
        
        // Navigate back to main dashboard to check total portfolio
        brokerPage.NavigateBack();
        Assert.True(mainPage.HasPortfolioData(), "Portfolio should show data after adding movements");
    }

    [Fact]
    public void CrossScreenNavigation_BackAndForth_MaintainsState()
    {
        // Skip if app not initialized
        Skip.If(_app == null, "Appium not available");

        // Arrange
        var mainPage = new MainDashboardPage(_app);
        
        // Act & Assert
        mainPage.WaitForPageToLoad();
        Assert.True(mainPage.IsCurrentPage());
        
        // Navigate to broker account
        var brokerPage = mainPage.NavigateToBrokerAccount("Test Broker");
        Assert.True(brokerPage.IsCurrentPage());
        
        // Navigate to movement creator
        var movementPage = brokerPage.NavigateToAddMovement();
        Assert.True(movementPage.IsCurrentPage());
        
        // Navigate back to broker account
        brokerPage = movementPage.CancelMovement();
        Assert.True(brokerPage.IsCurrentPage());
        
        // Navigate back to main dashboard
        brokerPage.NavigateBack();
        Assert.True(mainPage.IsCurrentPage());
    }

    public void Dispose()
    {
        _app?.Dispose();
    }
}

/// <summary>
/// Simple Skip utility for conditional test skipping.
/// </summary>
public static class Skip
{
    public static void If(bool condition, string reason)
    {
        if (condition)
        {
            throw new SkipException(reason);
        }
    }
}

/// <summary>
/// Exception thrown to skip a test.
/// </summary>
public class SkipException : Exception
{
    public SkipException(string reason) : base(reason) { }
}