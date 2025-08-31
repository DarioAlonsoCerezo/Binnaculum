# Writing Your First Device Test

This tutorial will guide you through creating your first device test for Binnaculum's investment tracking functionality.

## Prerequisites

Before starting, ensure you have:
- .NET 9 SDK installed
- MAUI workloads installed (`dotnet workload install maui-android`)
- Understanding of xUnit testing patterns
- Basic knowledge of Binnaculum's investment domain models

## Step 1: Understanding Device Tests

Device tests in Binnaculum run on actual devices or emulators, testing the complete UI and business logic integration. They're different from unit tests because they:

- Execute on the target platform (Android, iOS, Windows, MacCatalyst)
- Test real UI components with platform-specific behavior
- Validate investment calculations with actual data formatting
- Include memory leak detection for Observable chains
- Test F# business logic integration from C# UI tests

## Step 2: Creating a Simple Investment Test

Let's create a test that validates currency formatting for investment portfolios:

```csharp
using Xunit;
using Binnaculum.Tests.TestUtils.UI.DeviceTests;

namespace YourProject.Tests
{
    public class InvestmentFormattingTests
    {
        [Fact]
        public async Task CurrencyFormatting_USDInvestment_DisplaysCorrectFormat()
        {
            // Arrange - Create realistic investment test data
            var investmentData = InvestmentTestData.CreateSingleBrokerAccount()
                .WithCurrency("USD")
                .WithBalance(15750.25m)
                .WithProfitLoss(1250.75m, 8.61m);

            // Act - Test currency formatting
            var formattedBalance = investmentData.FormatCurrency();
            var formattedProfitLoss = investmentData.FormatPercentage();

            // Assert - Validate Binnaculum-specific formatting
            formattedBalance.AssertCurrencyFormat("$15,750.25");
            formattedProfitLoss.AssertPercentageFormat("+8.61%");
        }
    }
}
```

## Step 3: Using Investment Test Builders

Binnaculum provides fluent test builders for realistic investment scenarios:

```csharp
[Fact]
public async Task BrokerAccountTemplate_ProfitableInvestment_ShowsGreenIndicators()
{
    // Arrange - Use builder pattern for complex scenarios
    var brokerAccount = new BrokerAccountBuilder()
        .WithBroker(BrokerBuilder.AsInteractiveBrokers())
        .WithCurrency(CurrencyBuilder.AsUSD())
        .AsProfitableScenario(returnPercentage: 12.5m)
        .WithTransactionCount(25)
        .Build();

    var template = new BrokerAccountTemplate();
    
    // Act - Load the account data
    await template.LoadAccountDataAsync(brokerAccount);
    
    // Assert - Verify UI displays profit correctly
    template.AssertProfitColorIndicator(AppColors.Profit);
    template.AssertPercentageValue(12.5m);
    template.AssertCurrencyFormat(brokerAccount.Balance);
}
```

## Step 4: Testing Across Platforms

Device tests should validate platform-specific behavior:

```csharp
[Fact]
public async Task PercentageControl_TouchInteraction_BehavesCorrectlyOnPlatform()
{
    // Arrange
    var control = new PercentageControl();
    var testData = InvestmentTestData.CreateMixedPortfolio();
    
    await control.LoadAsync(testData);
    
    // Act & Assert - Platform-specific behavior
    if (DeviceInfo.Platform == DevicePlatform.Android)
    {
        // Android: Test material design touch ripple
        await control.SimulateTouchAsync();
        control.AssertMaterialRippleEffect();
    }
    else if (DeviceInfo.Platform == DevicePlatform.iOS)
    {
        // iOS: Test haptic feedback
        await control.SimulateTapAsync();
        control.AssertHapticFeedbackTriggered();
    }
}
```

## Step 5: Memory Leak Testing

Always test Observable chains for memory leaks in investment UI:

```csharp
[Fact]
public async Task InvestmentObservables_Subscription_NoMemoryLeaks()
{
    // Arrange
    var viewModel = new BrokerAccountViewModel();
    var testData = InvestmentTestData.CreateLargePortfolio(1000);
    
    // Act - Subscribe to observables with disposal
    var subscription = viewModel.WhenAnyValue(x => x.TotalBalance)
        .Subscribe(balance => { /* handle balance changes */ })
        .DisposeWith(viewModel.Disposables);
    
    await viewModel.LoadDataAsync(testData);
    
    // Assert - No memory leaks
    subscription.AssertObservableMemoryLeak();
    
    // Cleanup
    viewModel.Dispose();
    GC.Collect();
    await TestHelpers.WaitForGCAsync();
}
```

## Step 6: Running Your Test

### On Android
```bash
# Build for Android
dotnet build -f net9.0-android

# Run on emulator or device  
dotnet test -f net9.0-android --filter "InvestmentFormattingTests"
```

### On Windows
```bash
# Build for Windows
dotnet build -f net9.0-windows10.0.19041.0

# Run on local machine
dotnet test -f net9.0-windows10.0.19041.0 --filter "InvestmentFormattingTests"
```

### Using Visual Test Runner
```csharp
// For interactive testing and debugging
var runner = VisualTestRunnerLauncher.LaunchVisualRunner();
// Select your test in the UI and run interactively
```

## Step 7: Best Practices

1. **Use Realistic Data**: Always use `InvestmentTestData` builders with realistic financial scenarios
2. **Test Multiple Currencies**: Include USD, EUR, GBP scenarios
3. **Validate Platform Differences**: Test touch vs mouse interactions  
4. **Check Memory Usage**: Include Observable disposal tests
5. **Test Edge Cases**: Zero values, very large numbers, negative percentages
6. **Follow Naming Conventions**: `ComponentName_Scenario_ExpectedResult()`

## Next Steps

- Read [Platform-Specific Setup Guides](platform-setup/) for detailed environment configuration
- Explore [Example Test Suites](../examples/) for more complex scenarios
- Review [Best Practices](../best-practices.md) for investment app testing guidelines
- Check [Troubleshooting Guide](../troubleshooting.md) for common issues

## Common Issues

**Test Discovery Problems**: Ensure your test class is public and methods are marked with `[Fact]`

**Platform Dependencies**: Some features require platform-specific setup - see platform guides

**Memory Leaks**: Always dispose subscriptions with `.DisposeWith(Disposables)` pattern

**Build Errors**: Verify MAUI workloads are installed for your target platforms

For more detailed troubleshooting, see the [complete troubleshooting guide](../troubleshooting.md).