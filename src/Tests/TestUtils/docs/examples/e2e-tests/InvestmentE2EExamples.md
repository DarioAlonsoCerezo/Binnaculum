# End-to-End Testing Examples

Comprehensive examples of end-to-end testing for Binnaculum's investment tracking workflows using the TestUtils infrastructure.

## Complete User Journey Tests

### New Investor Onboarding Flow

```csharp
using Xunit;
using Binnaculum.Tests.TestUtils.UITest.Appium;

namespace Binnaculum.Tests.Examples.E2ETests
{
    public class InvestmentOnboardingE2ETests : IDisposable
    {
        private readonly IApp _app;
        
        public InvestmentOnboardingE2ETests()
        {
            _app = BinnaculumAppFactory.CreateApp(TestPlatform.Android);
        }
        
        [Fact]
        public async Task NewUser_CompleteOnboarding_CreatesFirstBrokerAccount()
        {
            // Arrange - Fresh app installation
            await _app.RestartAppAsync();
            
            // Act - Complete onboarding workflow
            
            // Step 1: Welcome and app introduction
            var welcomePage = _app.FindElement("WelcomePage");
            welcomePage.AssertDisplayed();
            
            await _app.TapAsync("GetStartedButton");
            
            // Step 2: Investment goals selection
            var goalsPage = _app.FindElement("InvestmentGoalsPage");
            await _app.TapAsync("LongTermGrowthOption");
            await _app.TapAsync("RetirementPlanningOption");
            await _app.TapAsync("ContinueButton");
            
            // Step 3: Risk tolerance assessment
            var riskPage = _app.FindElement("RiskAssessmentPage");
            await _app.TapAsync("ModerateRiskOption"); // 6/10 on risk scale
            await _app.TapAsync("ContinueButton");
            
            // Step 4: Create first broker account
            var accountPage = _app.FindElement("BrokerAccountCreatorPage");
            
            await _app.EnterTextAsync("BrokerNameField", "Interactive Brokers");
            await _app.TapAsync("BrokerDropdown");
            await _app.TapAsync("InteractiveBrokersOption");
            await _app.TapAsync("CurrencyDropdown"); 
            await _app.TapAsync("USDOption");
            await _app.EnterTextAsync("InitialBalanceField", "50000.00");
            
            await _app.TapAsync("CreateAccountButton");
            
            // Step 5: Verify account creation and navigation to overview
            var overviewPage = _app.FindElement("OverviewPage");
            overviewPage.AssertDisplayed();
            
            // Assert - Verify complete onboarding
            var brokerAccountCard = _app.FindElement("BrokerAccountTemplate");
            brokerAccountCard.AssertDisplayed();
            brokerAccountCard.AssertText("Interactive Brokers");
            brokerAccountCard.AssertText("$50,000.00");
            
            // Verify user preferences were saved
            var settingsTab = _app.FindElement("SettingsTab");
            await _app.TapAsync(settingsTab);
            
            var settingsPage = _app.FindElement("SettingsPage");
            settingsPage.AssertText("Long-term Growth");
            settingsPage.AssertText("Retirement Planning");
            settingsPage.AssertText("Moderate Risk");
        }
        
        public void Dispose()
        {
            _app?.Dispose();
        }
    }
}
```

### Investment Portfolio Management Workflow

```csharp
[Fact]
public async Task ExistingUser_ManagePortfolio_AddTransactionsAndViewPerformance()
{
    // Arrange - User with existing portfolio
    await _app.RestartAppAsync();
    await SetupUserWithExistingPortfolio();
    
    var overviewPage = _app.FindElement("OverviewPage");
    overviewPage.AssertDisplayed();
    
    // Act - Add new investment transaction
    
    // Step 1: Navigate to broker account
    var brokerAccount = _app.FindElement("BrokerAccountTemplate");
    await _app.TapAsync(brokerAccount);
    
    var accountDetailPage = _app.FindElement("BrokerAccountPage");
    accountDetailPage.AssertDisplayed();
    
    // Step 2: Add new buy transaction
    await _app.TapAsync("AddMovementButton");
    
    var movementCreator = _app.FindElement("BrokerMovementCreatorPage");
    movementCreator.AssertDisplayed();
    
    await _app.TapAsync("BuyTransactionType");
    await _app.EnterTextAsync("TickerField", "AAPL");
    await _app.EnterTextAsync("SharesField", "25");
    await _app.EnterTextAsync("PriceField", "175.50");
    await _app.TapAsync("TodayDateButton"); // Use today's date
    await _app.EnterTextAsync("NotesField", "Adding to tech allocation");
    
    await _app.TapAsync("SaveTransactionButton");
    
    // Step 3: Verify transaction appears in account
    accountDetailPage.AssertDisplayed();
    
    var transactionsList = _app.FindElement("TransactionsList");
    var newTransaction = transactionsList.FindElement("AAPL Buy 25 shares");
    newTransaction.AssertDisplayed();
    newTransaction.AssertText("$4,387.50"); // 25 * 175.50
    
    // Step 4: Navigate back to overview and verify updated balance
    await _app.TapAsync("BackButton");
    
    overviewPage.AssertDisplayed();
    var updatedAccount = _app.FindElement("BrokerAccountTemplate");
    
    // Balance should reflect new purchase
    var expectedNewBalance = "$54,387.50"; // Original $50,000 + $4,387.50
    updatedAccount.AssertText(expectedNewBalance);
    
    // Step 5: View performance charts
    await _app.TapAsync("PerformanceTab");
    
    var performancePage = _app.FindElement("PerformanceChartsPage");
    performancePage.AssertDisplayed();
    
    var portfolioChart = _app.FindElement("PortfolioPerformanceChart");
    portfolioChart.AssertDisplayed();
    
    // Verify chart shows updated data
    await _app.TapAsync(portfolioChart); // Tap to see details
    var chartTooltip = _app.FindElement("ChartTooltip");
    chartTooltip.AssertText(expectedNewBalance);
    
    // Assert - Complete workflow verification
    await _app.TapAsync("OverviewTab");
    
    // Verify portfolio overview reflects changes
    var portfolioSummary = _app.FindElement("PortfolioSummary");
    portfolioSummary.AssertText("Total Value: $54,387.50");
    portfolioSummary.AssertText("Today: +$4,387.50");
    
    // Verify diversification updated
    var diversificationSection = _app.FindElement("PortfolioDiversification");
    diversificationSection.AssertText("Technology: 8.1%"); // AAPL addition
}
```

## Multi-Platform E2E Testing

### Cross-Platform Investment Data Sync

```csharp
[Theory]
[InlineData(TestPlatform.Android)]
[InlineData(TestPlatform.iOS)]  
[InlineData(TestPlatform.Windows)]
[InlineData(TestPlatform.MacCatalyst)]
public async Task InvestmentData_CrossPlatform_ConsistentBehavior(TestPlatform platform)
{
    // Arrange - Platform-specific app instance
    using var app = BinnaculumAppFactory.CreateApp(platform);
    await app.RestartAppAsync();
    
    // Setup common test data
    var testPortfolio = InvestmentTestData.CreateStandardTestPortfolio();
    await SetupUserDataAsync(app, testPortfolio);
    
    // Act - Perform standard investment operations
    await NavigateToOverviewAsync(app);
    
    // Step 1: Verify portfolio display
    var portfolioValue = await GetPortfolioValueAsync(app);
    
    // Step 2: Add investment transaction
    await AddInvestmentTransactionAsync(app, "MSFT", shares: 50, price: 380.00m);
    
    // Step 3: Verify updated portfolio
    var updatedValue = await GetPortfolioValueAsync(app);
    
    // Assert - Consistent behavior across platforms
    var expectedIncrease = 50 * 380.00m; // $19,000
    var actualIncrease = updatedValue - portfolioValue;
    
    Assert.Equal(expectedIncrease, actualIncrease);
    
    // Verify platform-specific UI elements
    await VerifyPlatformSpecificUI(app, platform);
}

private async Task VerifyPlatformSpecificUI(IApp app, TestPlatform platform)
{
    switch (platform)
    {
        case TestPlatform.Android:
            // Verify Material Design elements
            var fab = app.FindElement("FloatingActionButton");
            fab.AssertDisplayed();
            await app.TapAsync(fab);
            fab.AssertMaterialRippleEffect();
            break;
            
        case TestPlatform.iOS:
            // Verify iOS navigation patterns
            var backButton = app.FindElement("NavigationBackButton");
            backButton.AssertText("< Back"); // iOS style
            break;
            
        case TestPlatform.Windows:
            // Verify Windows desktop UI
            var menuBar = app.FindElement("MenuBar");
            menuBar.AssertDisplayed();
            break;
            
        case TestPlatform.MacCatalyst:
            // Verify macOS menu integration
            var touchBar = app.FindElement("TouchBarControls");
            if (touchBar.Exists())
            {
                touchBar.AssertDisplayed();
            }
            break;
    }
}
```

## Performance and Stress Testing E2E

### Large Portfolio End-to-End Performance

```csharp
[Fact]
public async Task LargePortfolio_E2EUserJourney_MaintainsPerformance()
{
    // Arrange - Create large realistic portfolio
    var largePortfolio = InvestmentTestData.CreateLargePortfolio(
        brokerAccounts: 8,
        transactionsPerAccount: 200
    );
    
    await SetupUserDataAsync(_app, largePortfolio);
    
    // Act - Perform complete user journey with large dataset
    var journeyStopwatch = Stopwatch.StartNew();
    
    // Step 1: App launch and overview load
    var launchTime = Stopwatch.StartNew();
    await _app.RestartAppAsync();
    var overviewPage = _app.FindElement("OverviewPage");
    await overviewPage.WaitForDisplayedAsync(timeout: TimeSpan.FromSeconds(10));
    launchTime.Stop();
    
    // Step 2: Navigate through all broker accounts
    var navigationTime = Stopwatch.StartNew();
    for (int i = 0; i < 8; i++)
    {
        var accountCard = _app.FindElement($"BrokerAccountTemplate_{i}");
        await _app.TapAsync(accountCard);
        
        var accountPage = _app.FindElement("BrokerAccountPage");
        await accountPage.WaitForDisplayedAsync();
        
        // Verify transactions load properly
        var transactionsList = _app.FindElement("TransactionsList");
        await transactionsList.WaitForDisplayedAsync();
        
        await _app.TapAsync("BackButton");
        await overviewPage.WaitForDisplayedAsync();
    }
    navigationTime.Stop();
    
    // Step 3: Performance charts with large dataset
    var chartsTime = Stopwatch.StartNew();
    await _app.TapAsync("PerformanceTab");
    
    var performancePage = _app.FindElement("PerformanceChartsPage");
    await performancePage.WaitForDisplayedAsync();
    
    var portfolioChart = _app.FindElement("PortfolioPerformanceChart");
    await portfolioChart.WaitForDisplayedAsync(timeout: TimeSpan.FromSeconds(15));
    chartsTime.Stop();
    
    // Step 4: Search and filter functionality
    var searchTime = Stopwatch.StartNew();
    await _app.TapAsync("SearchTab");
    
    await _app.EnterTextAsync("SearchField", "AAPL");
    var searchResults = _app.FindElement("SearchResults");
    await searchResults.WaitForDisplayedAsync();
    
    // Filter by date range
    await _app.TapAsync("FilterButton");
    await _app.TapAsync("LastYearFilter");
    await searchResults.WaitForUpdatedAsync();
    searchTime.Stop();
    
    journeyStopwatch.Stop();
    
    // Assert - Performance requirements for large portfolios
    Assert.True(launchTime.ElapsedMilliseconds < 5000,
        $"App launch with large portfolio took {launchTime.ElapsedMilliseconds}ms, " +
        $"should be < 5000ms");
    
    Assert.True(navigationTime.ElapsedMilliseconds < 10000,
        $"Account navigation took {navigationTime.ElapsedMilliseconds}ms, " +
        $"should be < 10000ms for 8 accounts");
    
    Assert.True(chartsTime.ElapsedMilliseconds < 8000,
        $"Chart rendering took {chartsTime.ElapsedMilliseconds}ms, " +
        $"should be < 8000ms for large dataset");
    
    Assert.True(searchTime.ElapsedMilliseconds < 3000,
        $"Search functionality took {searchTime.ElapsedMilliseconds}ms, " +
        $"should be < 3000ms");
    
    Console.WriteLine($"E2E Journey Performance - Launch: {launchTime.ElapsedMilliseconds}ms, " +
                      $"Navigation: {navigationTime.ElapsedMilliseconds}ms, " +
                      $"Charts: {chartsTime.ElapsedMilliseconds}ms, " +
                      $"Search: {searchTime.ElapsedMilliseconds}ms, " +
                      $"Total: {journeyStopwatch.ElapsedMilliseconds}ms");
}
```

## Error Handling and Recovery E2E

### Network Connectivity Issues

```csharp
[Fact]
public async Task InvestmentApp_NetworkConnectivityIssues_GracefulDegradation()
{
    // Arrange - App with network dependency for market data
    await _app.RestartAppAsync();
    await SetupUserWithPortfolioAsync();
    
    var overviewPage = _app.FindElement("OverviewPage");
    overviewPage.AssertDisplayed();
    
    // Act - Simulate network connectivity issues
    
    // Step 1: Simulate network disconnection
    await SimulateNetworkDisconnectionAsync();
    
    // Step 2: Try to refresh market data
    await _app.TapAsync("RefreshButton");
    
    // Step 3: Verify graceful degradation
    var networkStatus = _app.FindElement("NetworkStatusIndicator");
    networkStatus.AssertDisplayed();
    networkStatus.AssertText("Offline Mode");
    
    // Cached data should still be displayed
    var brokerAccount = _app.FindElement("BrokerAccountTemplate");
    brokerAccount.AssertDisplayed();
    brokerAccount.AssertText("$125,750.50"); // Cached balance
    
    // User should be notified about offline status
    var offlineNotification = _app.FindElement("OfflineNotification");
    offlineNotification.AssertDisplayed();
    offlineNotification.AssertText("Market data may not be current");
    
    // Step 4: Simulate network reconnection
    await SimulateNetworkReconnectionAsync();
    
    // Step 5: Verify automatic data refresh
    await _app.TapAsync("RefreshButton");
    
    // Network status should update
    await networkStatus.WaitForTextAsync("Online", timeout: TimeSpan.FromSeconds(10));
    
    // Fresh data should load
    var updatedAccount = _app.FindElement("BrokerAccountTemplate");
    await updatedAccount.WaitForUpdatedAsync();
    
    // Offline notification should disappear
    offlineNotification.AssertNotDisplayed();
    
    // Assert - Complete recovery verification
    networkStatus.AssertText("Online");
    updatedAccount.AssertDisplayed();
    
    // Verify user can perform normal operations after recovery
    await _app.TapAsync(updatedAccount);
    var accountPage = _app.FindElement("BrokerAccountPage");
    accountPage.AssertDisplayed();
    
    await _app.TapAsync("AddMovementButton");
    var movementCreator = _app.FindElement("BrokerMovementCreatorPage");
    movementCreator.AssertDisplayed(); // Should work normally after reconnection
}
```

## Data Import/Export E2E Scenarios

### Portfolio Data Migration

```csharp
[Fact]
public async Task PortfolioData_ImportExport_PreservesInvestmentAccuracy()
{
    // Arrange - User with complex multi-account portfolio
    var originalPortfolio = InvestmentTestData.CreateComplexPortfolio(
        accounts: 5,
        currencies: new[] { "USD", "EUR", "GBP" },
        transactionTypes: new[] { "Buy", "Sell", "Dividend", "Split" },
        timeSpan: TimeSpan.FromDays(365 * 3) // 3 years of history
    );
    
    await SetupUserDataAsync(_app, originalPortfolio);
    
    // Act - Export portfolio data
    
    // Step 1: Navigate to settings and export
    await _app.TapAsync("SettingsTab");
    var settingsPage = _app.FindElement("SettingsPage");
    await _app.TapAsync("DataManagementSection");
    await _app.TapAsync("ExportPortfolioButton");
    
    // Step 2: Configure export options
    var exportDialog = _app.FindElement("ExportOptionsDialog");
    await _app.TapAsync("IncludeAllDataOption");
    await _app.TapAsync("JSONFormatOption");
    await _app.TapAsync("ExportButton");
    
    // Step 3: Verify export success
    var exportSuccess = _app.FindElement("ExportSuccessNotification");
    exportSuccess.AssertDisplayed();
    var exportPath = exportSuccess.GetText("FilePath");
    
    // Step 4: Clear existing data (simulate new device)
    await _app.TapAsync("ClearAllDataButton");
    await _app.TapAsync("ConfirmClearButton");
    
    // Verify data is cleared
    await _app.TapAsync("OverviewTab");
    var emptyState = _app.FindElement("EmptyPortfolioState");
    emptyState.AssertDisplayed();
    
    // Step 5: Import data back
    await _app.TapAsync("SettingsTab");
    await _app.TapAsync("DataManagementSection");
    await _app.TapAsync("ImportPortfolioButton");
    
    var importDialog = _app.FindElement("ImportOptionsDialog");
    await _app.TapAsync("SelectFileButton");
    await SelectFileAsync(exportPath);
    await _app.TapAsync("ImportButton");
    
    // Step 6: Verify import success and data integrity
    var importSuccess = _app.FindElement("ImportSuccessNotification");
    importSuccess.AssertDisplayed();
    
    await _app.TapAsync("OverviewTab");
    var restoredOverview = _app.FindElement("OverviewPage");
    restoredOverview.AssertDisplayed();
    
    // Assert - Verify complete data restoration
    
    // Check account count
    var brokerAccounts = _app.FindElements("BrokerAccountTemplate");
    Assert.Equal(5, brokerAccounts.Count);
    
    // Verify financial totals match
    var totalValue = _app.FindElement("TotalPortfolioValue");
    totalValue.AssertText(originalPortfolio.TotalValue.FormatCurrency());
    
    // Check individual account balances
    for (int i = 0; i < 5; i++)
    {
        var account = brokerAccounts[i];
        var expectedBalance = originalPortfolio.Accounts[i].Balance;
        account.AssertText(expectedBalance.FormatCurrency());
    }
    
    // Verify transaction history preserved
    await _app.TapAsync(brokerAccounts[0]);
    var accountPage = _app.FindElement("BrokerAccountPage");
    var transactionCount = _app.FindElement("TransactionCount");
    transactionCount.AssertText($"{originalPortfolio.Accounts[0].TransactionCount} transactions");
    
    // Verify performance calculations are accurate
    await _app.TapAsync("BackButton");
    await _app.TapAsync("PerformanceTab");
    var performancePage = _app.FindElement("PerformanceChartsPage");
    
    var totalReturn = _app.FindElement("TotalReturnValue");
    totalReturn.AssertText(originalPortfolio.TotalReturnPercentage.FormatPercentage());
}
```

## E2E Test Utilities

### Helper Methods for Complex Workflows

```csharp
public static class E2ETestHelpers
{
    public static async Task SetupUserWithExistingPortfolio(IApp app)
    {
        var portfolio = InvestmentTestData.CreateStandardTestPortfolio();
        await SetupUserDataAsync(app, portfolio);
    }
    
    public static async Task<decimal> GetPortfolioValueAsync(IApp app)
    {
        var valueElement = app.FindElement("TotalPortfolioValue");
        var valueText = valueElement.GetText();
        return ParseCurrencyValue(valueText);
    }
    
    public static async Task AddInvestmentTransactionAsync(
        IApp app, 
        string ticker, 
        int shares, 
        decimal price)
    {
        await app.TapAsync("AddTransactionButton");
        
        var creator = app.FindElement("BrokerMovementCreatorPage");
        await app.EnterTextAsync("TickerField", ticker);
        await app.EnterTextAsync("SharesField", shares.ToString());
        await app.EnterTextAsync("PriceField", price.ToString("F2"));
        await app.TapAsync("SaveTransactionButton");
    }
    
    public static async Task SimulateNetworkDisconnectionAsync()
    {
        // Platform-specific network simulation
        // Implementation varies by test platform
    }
    
    private static decimal ParseCurrencyValue(string currencyText)
    {
        // Remove currency symbols and parse
        var cleanText = Regex.Replace(currencyText, @"[^\d.,]", "");
        return decimal.Parse(cleanText);
    }
}
```

These end-to-end testing examples demonstrate comprehensive user journey validation for Binnaculum's investment tracking functionality. The tests cover:

1. **Complete User Workflows**: Onboarding, portfolio management, and data operations
2. **Cross-Platform Consistency**: Ensuring identical behavior across all supported platforms
3. **Performance Under Load**: Large dataset handling and user experience validation
4. **Error Recovery**: Network issues and graceful degradation scenarios
5. **Data Integrity**: Import/export functionality with financial accuracy preservation

All tests are designed to validate real user scenarios while ensuring the investment app meets quality and performance standards across all target platforms.