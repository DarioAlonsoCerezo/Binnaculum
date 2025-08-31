# BrokerAccountTemplate Component Tests

These examples demonstrate comprehensive testing of the `BrokerAccountTemplate` component, which displays investment account information with interactive elements.

## Basic Functionality Tests

### Testing Profitable Investment Display

```csharp
using Xunit;
using Binnaculum.Tests.TestUtils.UI.DeviceTests;
using Binnaculum.UI.Controls;
using System.Reactive.Disposables;

namespace Binnaculum.Tests.Examples.ComponentTests
{
    public class BrokerAccountTemplateTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        
        [Fact]
        public async Task BrokerAccountTemplate_ProfitableInvestment_DisplaysGreenIndicators()
        {
            // Arrange - Create profitable investment scenario
            var brokerAccount = new BrokerAccountBuilder()
                .WithName("Interactive Brokers Main")
                .WithBroker(BrokerBuilder.AsInteractiveBrokers())
                .WithCurrency(CurrencyBuilder.AsUSD())
                .WithBalance(125750.50m)
                .WithProfitLoss(15750.25m, 14.35m)
                .WithTransactionCount(45)
                .AsVerifiedAccount()
                .Build();

            var template = new BrokerAccountTemplate();
            
            // Act - Load the profitable account data
            await template.LoadAccountDataAsync(brokerAccount);
            
            // Assert - Verify profitable display characteristics  
            template.AssertAccountName("Interactive Brokers Main");
            template.AssertCurrencyFormat("$125,750.50", brokerAccount.Balance);
            template.AssertPercentageValue(14.35m);
            template.AssertProfitColorIndicator(AppColors.Profit);
            template.AssertPercentageColor(AppColors.Profit);
            
            // Verify interactive elements are enabled
            template.AssertNavigationEnabled(typeof(BrokerAccountPage));
            template.AssertMovementCreationEnabled();
        }

        [Fact] 
        public async Task BrokerAccountTemplate_LossInvestment_DisplaysRedIndicators()
        {
            // Arrange - Create loss-making investment scenario
            var brokerAccount = new BrokerAccountBuilder()
                .WithName("Charles Schwab Portfolio")
                .WithBroker(BrokerBuilder.AsCharlesSchwab())
                .WithCurrency(CurrencyBuilder.AsUSD())
                .WithBalance(87200.75m)
                .WithProfitLoss(-8950.50m, -9.31m)
                .WithTransactionCount(32)
                .AsLossScenario()
                .Build();

            var template = new BrokerAccountTemplate();
            
            // Act
            await template.LoadAccountDataAsync(brokerAccount);
            
            // Assert - Verify loss display characteristics
            template.AssertAccountName("Charles Schwab Portfolio");
            template.AssertCurrencyFormat("$87,200.75", brokerAccount.Balance);
            template.AssertPercentageValue(-9.31m);
            template.AssertLossColorIndicator(AppColors.Loss);
            template.AssertPercentageColor(AppColors.Loss);
            template.AssertPercentagePrefix("-");
        }

        [Fact]
        public async Task BrokerAccountTemplate_NoMovements_DisablesInteractiveElements()
        {
            // Arrange - Account without movements/transactions
            var brokerAccount = new BrokerAccountBuilder()
                .WithName("New Fidelity Account")
                .WithBroker(BrokerBuilder.AsFidelity())
                .WithCurrency(CurrencyBuilder.AsUSD())
                .WithBalance(10000.00m)
                .WithTransactionCount(0)  // No transactions
                .AsNewAccount()
                .Build();

            var template = new BrokerAccountTemplate();
            template._hasMovements = false; // Test internal state
            
            // Act
            await template.LoadAccountDataAsync(brokerAccount);
            
            // Assert - Interactive elements should be disabled/hidden
            template.AssertNavigationDisabled();
            template.AssertMovementCreationButtonHidden();
            template.AssertEmptyStateDisplayed("No movements yet");
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}
```

## Multi-Currency Testing

### Testing European Investment Accounts

```csharp
[Fact]
public async Task BrokerAccountTemplate_EuropeanAccount_DisplaysEURFormatting()
{
    // Arrange - European broker with EUR currency
    var brokerAccount = new BrokerAccountBuilder()
        .WithName("European Investment Portfolio")
        .WithBroker(BrokerBuilder.AsEuropeanBroker())
        .WithCurrency(CurrencyBuilder.AsEUR())
        .WithBalance(95750.80m)
        .WithProfitLoss(7250.30m, 8.21m)
        .WithTransactionCount(28)
        .Build();

    var template = new BrokerAccountTemplate();
    
    // Act - Test with European formatting culture
    using (new CultureScope("de-DE")) // German locale
    {
        await template.LoadAccountDataAsync(brokerAccount);
        
        // Assert - European currency formatting
        template.AssertCurrencyFormat("95.750,80 €", brokerAccount.Balance);
        template.AssertPercentageFormat("8,21%", brokerAccount.ProfitLossPercentage);
        template.AssertDecimalSeparator(",");
        template.AssertThousandsSeparator(".");
    }
}

[Fact]
public async Task BrokerAccountTemplate_MultiCurrency_ConsistentFormatting()
{
    // Arrange - Test multiple currencies in same test
    var currencies = new[]
    {
        (CurrencyBuilder.AsUSD(), "en-US", "$125,750.50"),
        (CurrencyBuilder.AsEUR(), "de-DE", "125.750,50 €"),  
        (CurrencyBuilder.AsGBP(), "en-GB", "£125,750.50")
    };
    
    var template = new BrokerAccountTemplate();
    
    foreach (var (currency, culture, expectedFormat) in currencies)
    {
        // Arrange
        var account = new BrokerAccountBuilder()
            .WithCurrency(currency)
            .WithBalance(125750.50m)
            .Build();
        
        // Act
        using (new CultureScope(culture))
        {
            await template.LoadAccountDataAsync(account);
            
            // Assert
            template.AssertCurrencyFormat(expectedFormat, account.Balance);
        }
    }
}
```

## Observable Chain and Memory Testing

### Testing Reactive UI Updates

```csharp
[Fact]
public async Task BrokerAccountTemplate_BalanceUpdates_ReactiveUIUpdates()
{
    // Arrange
    var viewModel = new BrokerAccountViewModel();
    var template = new BrokerAccountTemplate { BindingContext = viewModel };
    
    var initialAccount = new BrokerAccountBuilder()
        .WithBalance(100000.00m)
        .WithProfitLoss(5000.00m, 5.26m)
        .Build();
    
    // Act - Subscribe to balance changes with proper disposal
    var balanceUpdates = new List<decimal>();
    var subscription = viewModel.WhenAnyValue(x => x.Balance)
        .Subscribe(balance => balanceUpdates.Add(balance))
        .DisposeWith(_disposables);
    
    await template.LoadAccountDataAsync(initialAccount);
    
    // Simulate balance updates from real-time market data
    await viewModel.UpdateBalanceAsync(102500.75m); // Market gain
    await viewModel.UpdateBalanceAsync(98750.25m);  // Market loss
    
    // Assert - UI reacts to changes
    Assert.Equal(3, balanceUpdates.Count);
    Assert.Equal(100000.00m, balanceUpdates[0]);
    Assert.Equal(102500.75m, balanceUpdates[1]); 
    Assert.Equal(98750.25m, balanceUpdates[2]);
    
    template.AssertCurrencyFormat("$98,750.25");
    
    // Assert - No memory leaks
    subscription.AssertObservableMemoryLeak();
}

[Fact]
public async Task BrokerAccountTemplate_DisposedProperly_NoMemoryLeaks()
{
    // Arrange
    var template = new BrokerAccountTemplate();
    var largeDataSet = InvestmentTestData.CreateLargePortfolio(1000); // Many transactions
    
    // Act - Load large dataset and subscribe to updates
    var subscription = template.WhenAnyValue(x => x.BindingContext)
        .Subscribe(context => { /* process updates */ })
        .DisposeWith(_disposables);
    
    await template.LoadAccountDataAsync(largeDataSet);
    
    // Simulate heavy usage
    for (int i = 0; i < 100; i++)
    {
        await template.RefreshDataAsync();
    }
    
    // Assert - Proper cleanup
    template.Dispose();
    subscription.AssertObservableMemoryLeak();
    
    // Force garbage collection and verify cleanup
    GC.Collect();
    await TestHelpers.WaitForGCAsync(TimeSpan.FromSeconds(2));
    
    // Verify memory usage is reasonable
    var memoryAfterGC = GC.GetTotalMemory(forceFullCollection: true);
    Assert.True(memoryAfterGC < 50 * 1024 * 1024, "Memory usage should be under 50MB after cleanup");
}
```

## Navigation and Interaction Testing

### Testing User Interactions

```csharp
[Fact]
public async Task BrokerAccountTemplate_TapNavigation_NavigatesToBrokerAccountPage()
{
    // Arrange
    var brokerAccount = new BrokerAccountBuilder()
        .WithName("Interactive Brokers")
        .AsProfitableScenario()
        .Build();
        
    var template = new BrokerAccountTemplate();
    var navigationService = new MockNavigationService();
    template.SetNavigationService(navigationService);
    
    await template.LoadAccountDataAsync(brokerAccount);
    
    // Act - Simulate user tap
    await template.SimulateTapAsync();
    
    // Assert - Navigation occurred
    navigationService.AssertNavigatedTo<BrokerAccountPage>();
    navigationService.AssertNavigationParameterPassed("BrokerAccountId", brokerAccount.Id);
}

[Fact]
public async Task BrokerAccountTemplate_MovementCreation_NavigatesToCreator()
{
    // Arrange
    var template = new BrokerAccountTemplate();
    var navigationService = new MockNavigationService();
    template.SetNavigationService(navigationService);
    
    var account = new BrokerAccountBuilder()
        .WithTransactionCount(10) // Has existing movements
        .Build();
    await template.LoadAccountDataAsync(account);
    
    // Act - Tap movement creation button
    await template.TapMovementCreationButtonAsync();
    
    // Assert 
    navigationService.AssertNavigatedTo<BrokerMovementCreatorPage>();
    navigationService.AssertModalPresentation();
}
```

## Edge Cases and Error Scenarios

### Testing Extreme Values

```csharp
[Fact]
public async Task BrokerAccountTemplate_ZeroBalance_DisplaysCorrectly()
{
    // Arrange - Account with exactly zero balance
    var account = new BrokerAccountBuilder()
        .WithBalance(0.00m)
        .WithProfitLoss(0.00m, 0.00m)
        .Build();
    
    var template = new BrokerAccountTemplate();
    
    // Act
    await template.LoadAccountDataAsync(account);
    
    // Assert
    template.AssertCurrencyFormat("$0.00");
    template.AssertPercentageFormat("0.00%");
    template.AssertNeutralColorIndicator(); // Neither profit nor loss color
}

[Fact]
public async Task BrokerAccountTemplate_VeryLargeNumbers_HandlesCorrectly()
{
    // Arrange - Billionaire investor scenario
    var account = new BrokerAccountBuilder()
        .WithBalance(2_500_000_000.75m) // $2.5 billion
        .WithProfitLoss(125_000_000.25m, 5.26m)
        .Build();
    
    var template = new BrokerAccountTemplate();
    
    // Act
    await template.LoadAccountDataAsync(account);
    
    // Assert - Large number formatting
    template.AssertCurrencyFormat("$2,500,000,000.75");
    template.AssertPercentageFormat("5.26%");
    template.AssertNoOverflowErrors();
    template.AssertUIElementsVisible(); // UI should not break with large numbers
}

[Fact] 
public async Task BrokerAccountTemplate_NullAccount_HandlesGracefully()
{
    // Arrange
    var template = new BrokerAccountTemplate();
    
    // Act & Assert - Should not throw
    await Assert.ThrowsAsync<ArgumentNullException>(() => 
        template.LoadAccountDataAsync(null));
    
    // Template should remain in safe state
    template.AssertDefaultState();
}
```

## Performance Testing

### Testing with Large Datasets

```csharp
[Fact]
public async Task BrokerAccountTemplate_LargeTransactionHistory_PerformsWell()
{
    // Arrange - Account with many transactions (mobile performance test)
    var account = new BrokerAccountBuilder()
        .WithTransactionCount(5000) // Large transaction history
        .AsMixedScenario()
        .Build();
    
    var template = new BrokerAccountTemplate();
    
    // Act - Measure load time
    var stopwatch = Stopwatch.StartNew();
    await template.LoadAccountDataAsync(account);
    stopwatch.Stop();
    
    // Assert - Performance requirements for mobile devices
    Assert.True(stopwatch.ElapsedMilliseconds < 500, 
        "Loading should complete within 500ms on mobile devices");
    
    // Verify UI responsiveness
    template.AssertUIResponsive();
    
    // Memory usage should be reasonable
    var memoryUsage = GC.GetTotalMemory(forceFullCollection: false);
    Assert.True(memoryUsage < 100 * 1024 * 1024, 
        "Memory usage should stay under 100MB for large datasets");
}
```

## Platform-Specific Tests

### Android Material Design Tests

```csharp
[Fact]
public async Task BrokerAccountTemplate_AndroidMaterialDesign_FollowsDesignGuidelines()
{
    // Skip if not Android
    if (DeviceInfo.Platform != DevicePlatform.Android)
        return;
    
    // Arrange
    var template = new BrokerAccountTemplate();
    var account = new BrokerAccountBuilder().AsProfitableScenario().Build();
    
    await template.LoadAccountDataAsync(account);
    
    // Act - Test material design compliance
    await template.SimulateTouchAsync();
    
    // Assert - Material design characteristics
    template.AssertMaterialRippleEffect();
    template.AssertElevationShadow();
    template.AssertMaterialCornerRadius();
    template.AssertTouchFeedbackTiming(MaterialDesignTiming.RippleDuration);
}
```

This comprehensive test suite demonstrates:

1. **Realistic Investment Scenarios**: Profitable, loss, and neutral accounts
2. **Multi-Currency Support**: USD, EUR, GBP with proper formatting
3. **Reactive UI Testing**: Observable chains and memory leak detection
4. **Navigation Testing**: Page navigation and modal presentation
5. **Edge Case Handling**: Zero balances, large numbers, null inputs
6. **Performance Validation**: Mobile device constraints
7. **Platform-Specific Behavior**: Material design compliance

All tests follow Binnaculum's investment domain patterns and use the established TestUtils infrastructure for realistic and maintainable testing.