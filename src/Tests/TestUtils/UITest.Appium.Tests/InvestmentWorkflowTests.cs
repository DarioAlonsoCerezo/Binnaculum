using Xunit;
using Microsoft.Extensions.Logging;
using Binnaculum.UITest.Core;
using Binnaculum.UITest.Appium;
using Binnaculum.UITest.Appium.PageObjects;
using Binnaculum.UITest.Appium.TestData;

namespace Binnaculum.UITest.Appium.Tests;

/// <summary>
/// End-to-end tests for investment portfolio workflows in Binnaculum.
/// These tests validate complete user journeys from dashboard to movement creation.
/// </summary>
[Collection("AppiumServer")]  // NEW: Use the collection fixture
public class InvestmentWorkflowTests : IDisposable
{
    private readonly IConfig _config;
    private readonly Binnaculum.UITest.Appium.AppiumServerFixture _serverFixture;
    private IApp? _app;

    public InvestmentWorkflowTests(Binnaculum.UITest.Appium.AppiumServerFixture serverFixture)  // NEW: Inject fixture
    {
        _serverFixture = serverFixture;
        
        // Configure for Android testing - would need to be made configurable for CI/CD
        _config = AppiumConfig.ForBinnaculumAndroid();
        
        // Don't create app in constructor - let each test handle it
    }

    [SkippableFact]
    public void AddInvestmentMovement_WithValidData_CreatesMovementSuccessfully()
    {
        // Skip if Appium server is not available
        Skip.IfNot(_serverFixture.IsServerAvailable, "Appium server is not available");
        
        // Create app lazily
        _app = BinnaculumAppFactory.CreateAppWithAutoServer(_config, _serverFixture);
        
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

    [SkippableFact]
    public void Dashboard_OnAppStart_DisplaysPortfolioData()
    {
        // Skip if Appium server is not available
        Skip.IfNot(_serverFixture.IsServerAvailable, "Appium server is not available");
        
        // Create app lazily
        _app = BinnaculumAppFactory.CreateAppWithAutoServer(_config, _serverFixture);
        
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

    [SkippableFact]
    public void BrokerAccount_Navigation_WorksCorrectly()
    {
        // Skip if Appium server is not available
        Skip.IfNot(_serverFixture.IsServerAvailable, "Appium server is not available");
        
        // Create app lazily
        _app = BinnaculumAppFactory.CreateAppWithAutoServer(_config, _serverFixture);
        
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

    [SkippableFact]
    public void MovementCreator_WithDividendData_CreatesSuccessfully()
    {
        // Skip if Appium server is not available
        Skip.IfNot(_serverFixture.IsServerAvailable, "Appium server is not available");
        
        // Create app lazily
        _app = BinnaculumAppFactory.CreateAppWithAutoServer(_config, _serverFixture);
        
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

    [SkippableFact]
    public void Portfolio_MultipleMovements_ShowsCorrectBalance()
    {
        // Skip if Appium server is not available
        Skip.IfNot(_serverFixture.IsServerAvailable, "Appium server is not available");
        
        // Create app lazily
        _app = BinnaculumAppFactory.CreateAppWithAutoServer(_config, _serverFixture);
        
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

    [SkippableFact]
    public void CrossScreenNavigation_BackAndForth_MaintainsState()
    {
        // Skip if Appium server is not available
        Skip.IfNot(_serverFixture.IsServerAvailable, "Appium server is not available");
        
        // Create app lazily
        _app = BinnaculumAppFactory.CreateAppWithAutoServer(_config, _serverFixture);
        
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
/// xUnit collection definition for Appium server tests.
/// All test classes that use [Collection("AppiumServer")] will share the same server instance.
/// </summary>
[CollectionDefinition("AppiumServer")]
public class AppiumServerCollection : ICollectionFixture<Binnaculum.UITest.Appium.AppiumServerFixture>
{
    // This class has no code, it exists only to define the collection
}