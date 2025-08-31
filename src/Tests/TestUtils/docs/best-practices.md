# Investment App Testing Best Practices

This guide outlines best practices for testing investment tracking applications using Binnaculum's TestUtils infrastructure.

## Test Naming and Organization

### Naming Conventions

Follow the pattern: `ComponentName_Scenario_ExpectedResult()`

**✅ Good Examples:**
```csharp
[Fact]
public async Task BrokerAccountTemplate_ProfitableInvestment_ShowsGreenPercentage()

[Fact] 
public async Task PercentageControl_LossScenario_DisplaysRedNegativeValue()

[Fact]
public async Task CurrencyFormatter_EuropeanCulture_UsesCommaSeparator()
```

**❌ Bad Examples:**
```csharp
[Fact]
public void Test1() // Not descriptive

[Fact] 
public void BrokerTest() // Too vague

[Fact]
public void TestProfitCalculation() // Doesn't indicate expected result
```

### Test Class Organization

Group related tests by component or feature:

```csharp
namespace Binnaculum.Tests.Investment.Controls
{
    public class BrokerAccountTemplateTests : IDisposable
    {
        // Profitable scenarios
        [Fact] public async Task ShowsGreenProfitIndicator() { }
        [Fact] public async Task FormatsPositivePercentage() { }
        
        // Loss scenarios  
        [Fact] public async Task ShowsRedLossIndicator() { }
        [Fact] public async Task FormatsNegativePercentage() { }
        
        // Edge cases
        [Fact] public async Task HandlesZeroBalance() { }
        [Fact] public async Task HandlesVeryLargeNumbers() { }
    }
}
```

## Investment Test Data Management

### Use Realistic Financial Scenarios

Always use realistic investment data that reflects real-world trading patterns:

**✅ Realistic Data:**
```csharp
var brokerAccount = new BrokerAccountBuilder()
    .WithName("Interactive Brokers Main Portfolio")
    .WithBalance(125750.50m)           // Realistic balance
    .WithProfitLoss(8250.25m, 7.03m)  // Reasonable 7% gain
    .WithTransactionCount(42)          // Moderate activity
    .WithDiversification(sectors: 8)   // Diversified portfolio
    .Build();
```

**❌ Unrealistic Data:**
```csharp
var account = new BrokerAccount
{
    Balance = 999999999999.99m,    // Unrealistic amount
    ProfitPercent = 500.0m,        // Impossible returns
    TransactionCount = 1000000     // Unrealistic activity
};
```

### Multi-Currency Testing Strategy

Test all supported currencies with appropriate cultural formatting:

```csharp
[Theory]
[InlineData("USD", "en-US", "$125,750.50")]
[InlineData("EUR", "de-DE", "125.750,50 €")]
[InlineData("GBP", "en-GB", "£125,750.50")]
[InlineData("JPY", "ja-JP", "¥125,751")]
public async Task CurrencyFormatting_VariousCultures_FormatsCorrectly(
    string currency, string culture, string expected)
{
    var account = new BrokerAccountBuilder()
        .WithCurrency(currency)
        .WithBalance(125750.50m)
        .Build();
        
    using (new CultureScope(culture))
    {
        var formatted = account.FormatCurrency();
        formatted.AssertCurrencyFormat(expected);
    }
}
```

### Investment Scenario Builders

Use builder patterns for complex investment scenarios:

```csharp
// Profitable long-term investor
var longTermInvestor = InvestmentTestData
    .CreatePortfolio()
    .WithTimeHorizon(TimeSpan.FromDays(2555)) // ~7 years
    .WithAnnualReturn(8.5m)
    .WithDividendYield(2.1m)
    .WithLowVolatility()
    .Build();

// Day trader scenario
var dayTrader = InvestmentTestData
    .CreatePortfolio() 
    .WithHighFrequencyTrading(avgTransactionsPerDay: 25)
    .WithHighVolatility(volatility: 15.0m)
    .WithShortTermHolds(avgHoldTime: TimeSpan.FromHours(4))
    .Build();

// Conservative retiree portfolio
var retireePortfolio = InvestmentTestData
    .CreatePortfolio()
    .WithConservativeAllocation(bonds: 60, stocks: 35, cash: 5)
    .WithIncomeOriented(dividendYield: 4.2m)
    .WithLowRiskTolerance()
    .Build();
```

## Financial Calculation Testing

### Precision and Rounding

Always use `decimal` for financial calculations, never `float` or `double`:

**✅ Correct:**
```csharp
[Fact]
public void PercentageCalculation_DecimalPrecision_ExactResults()
{
    // Arrange
    var principal = 125750.50m;
    var currentValue = 142380.75m;
    
    // Act
    var percentage = FinancialCalculations.CalculateReturns(principal, currentValue);
    
    // Assert - Exact decimal comparison
    percentage.AssertDecimalEquals(13.22m, precision: 2);
}
```

**❌ Incorrect:**
```csharp
[Fact]
public void BadTest_FloatingPoint_InaccurateResults() 
{
    var principal = 125750.50; // double - loses precision!
    var percentage = (currentValue - principal) / principal * 100;
    // Floating point errors make this unreliable for financial data
}
```

### Test Edge Cases in Calculations

```csharp
[Theory]
[InlineData(0.00, 100.00, double.PositiveInfinity)]  // Division by zero case
[InlineData(100.00, 0.00, -100.00)]                 // Complete loss
[InlineData(100.00, 100.00, 0.00)]                  // Break-even
[InlineData(100.00, 200.00, 100.00)]                // Perfect double
public void PercentageCalculation_EdgeCases_HandlesCorrectly(
    decimal original, decimal current, decimal expectedPercent)
{
    var result = FinancialCalculations.CalculatePercentageChange(original, current);
    
    if (double.IsInfinity((double)expectedPercent))
    {
        Assert.Throws<DivideByZeroException>(() => result);
    }
    else
    {
        result.AssertDecimalEquals(expectedPercent);
    }
}
```

## Memory and Performance Guidelines

### Observable Chain Testing

Always test Observable chains for memory leaks:

```csharp
[Fact]
public async Task InvestmentViewModel_ObservableChain_NoMemoryLeaks()
{
    // Arrange
    var viewModel = new InvestmentPortfolioViewModel();
    var subscription = new CompositeDisposable();
    
    // Act - Set up reactive chain with proper disposal
    viewModel.WhenAnyValue(x => x.TotalBalance)
        .Throttle(TimeSpan.FromMilliseconds(100))
        .ObserveOnMainThread()
        .Subscribe(balance => UpdateUI(balance))
        .DisposeWith(subscription);
    
    // Simulate heavy usage
    for (int i = 0; i < 1000; i++)
    {
        await viewModel.RefreshPortfolioAsync();
    }
    
    // Assert - Proper cleanup
    subscription.Dispose();
    viewModel.Dispose();
    
    // Verify no memory leaks
    GC.Collect();
    await TestHelpers.WaitForGCAsync();
    subscription.AssertObservableMemoryLeak();
}
```

### Performance Constraints for Mobile

Test performance with mobile device constraints:

```csharp
[Fact]
public async Task PortfolioCalculations_LargeDataset_MobilePerformance()
{
    // Arrange - Large portfolio (stress test)
    var portfolio = InvestmentTestData.CreateLargePortfolio(
        accounts: 10, 
        transactionsPerAccount: 500);
    
    var calculator = new PortfolioSnapshotCalculator();
    
    // Act - Measure performance
    var stopwatch = Stopwatch.StartNew();
    var snapshot = await calculator.CalculateSnapshotAsync(portfolio);
    stopwatch.Stop();
    
    // Assert - Mobile performance requirements
    Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
        "Portfolio calculations should complete within 1 second on mobile");
    
    // Memory usage should be reasonable for mobile devices
    var memoryUsage = GC.GetTotalMemory(forceFullCollection: true);
    Assert.True(memoryUsage < 100 * 1024 * 1024, 
        "Memory usage should stay under 100MB");
    
    snapshot.AssertCalculationAccuracy();
}
```

## Platform-Specific Testing

### Android Testing Guidelines

```csharp
[Fact]
public async Task BrokerAccountControl_Android_MaterialDesignCompliance()
{
    // Skip on non-Android platforms
    if (DeviceInfo.Platform != DevicePlatform.Android)
        return;
    
    var control = new BrokerAccountControl();
    await control.LoadAsync(InvestmentTestData.CreateProfitableAccount());
    
    // Test Android-specific behavior
    await control.SimulateTouchAsync();
    
    // Assert Material Design compliance
    control.AssertMaterialRippleEffect();
    control.AssertElevationShadow(expectedElevation: 4);
    control.AssertCornerRadius(MaterialDesign.CardCornerRadius);
}
```

### iOS Testing Guidelines

```csharp
[Fact] 
public async Task InvestmentChart_iOS_HapticFeedback()
{
    if (DeviceInfo.Platform != DevicePlatform.iOS)
        return;
    
    var chart = new InvestmentPerformanceChart();
    await chart.LoadDataAsync(InvestmentTestData.CreateHistoricalData());
    
    // Act - Tap on data point
    await chart.TapDataPointAsync(pointIndex: 5);
    
    // Assert - iOS haptic feedback
    chart.AssertHapticFeedbackTriggered(HapticFeedbackType.SelectionChanged);
    chart.AssertDataPointHighlighted(pointIndex: 5);
}
```

## Error Handling and Validation

### Investment Data Validation

```csharp
[Theory]
[InlineData(-100.00, "Balance cannot be negative")]
[InlineData(double.MaxValue, "Balance exceeds maximum allowed")]
public void BrokerAccount_InvalidBalance_ThrowsValidationError(
    decimal invalidBalance, string expectedError)
{
    var exception = Assert.Throws<ValidationException>(() => 
        new BrokerAccountBuilder()
            .WithBalance(invalidBalance)
            .Build());
            
    Assert.Contains(expectedError, exception.Message);
}
```

### Network and API Error Testing

```csharp
[Fact]
public async Task StockPriceService_NetworkError_FallsBackToCache()
{
    // Arrange
    var mockService = new MockStockPriceService();
    mockService.SetupNetworkFailure();
    
    var portfolio = new InvestmentPortfolioViewModel(mockService);
    
    // Act
    await portfolio.RefreshPricesAsync();
    
    // Assert - Graceful degradation
    portfolio.AssertUsingCachedPrices();
    portfolio.AssertUserNotifiedOfOfflineMode();
    portfolio.AssertDataStillDisplayed();
}
```

## Test Data and Fixtures

### Shared Test Data

Create reusable test fixtures for common scenarios:

```csharp
public static class InvestmentFixtures
{
    public static readonly BrokerAccount ProfitableAccount = new BrokerAccountBuilder()
        .WithName("Profitable Test Account")
        .WithBalance(100000.00m)
        .WithProfitLoss(15000.00m, 15.0m)
        .Build();
        
    public static readonly BrokerAccount LossAccount = new BrokerAccountBuilder()
        .WithName("Loss Test Account") 
        .WithBalance(85000.00m)
        .WithProfitLoss(-15000.00m, -15.0m)
        .Build();
        
    public static IEnumerable<BrokerAccount> GetMultiCurrencyPortfolio()
    {
        yield return CreateAccount(CurrencyBuilder.AsUSD(), 125000.00m);
        yield return CreateAccount(CurrencyBuilder.AsEUR(), 98000.50m);
        yield return CreateAccount(CurrencyBuilder.AsGBP(), 76500.25m);
    }
}
```

## Documentation and Maintenance

### Test Documentation

Document complex test scenarios:

```csharp
/// <summary>
/// Tests the portfolio rebalancing algorithm with realistic investment constraints.
/// 
/// Scenario: 60/40 stock/bond portfolio with $500K total value
/// Target allocation: 60% stocks ($300K), 40% bonds ($200K)
/// Current allocation: 65% stocks ($325K), 35% bonds ($175K)
/// 
/// Expected rebalancing actions:
/// - Sell $25K of stocks  
/// - Buy $25K of bonds
/// - Maintain target allocation within 1% tolerance
/// </summary>
[Fact]
public async Task PortfolioRebalancer_StandardScenario_MaintainsTargetAllocation()
{
    // Implementation follows documented scenario
}
```

### Test Maintenance

Keep tests maintainable and reliable:

1. **Regular Updates**: Update test data when business rules change
2. **Flaky Test Monitoring**: Identify and fix non-deterministic tests
3. **Performance Monitoring**: Track test execution time trends
4. **Coverage Analysis**: Ensure critical investment logic is well-tested

## Common Anti-Patterns to Avoid

### ❌ Don't Test UI Implementation Details

```csharp
// Bad - Testing internal implementation
[Fact]
public void BrokerControl_InternalMethod_DoesWork()
{
    var control = new BrokerAccountControl();
    var result = control.InternalCalculationMethod(); // Testing private method
    Assert.NotNull(result);
}
```

### ✅ Do Test Observable Behavior

```csharp
// Good - Testing observable behavior and outcomes
[Fact] 
public async Task BrokerControl_BalanceUpdate_UIReflectsChange()
{
    var control = new BrokerAccountControl();
    await control.LoadAsync(CreateTestAccount(balance: 100000.00m));
    
    // Act - Observable behavior
    await control.UpdateBalanceAsync(125000.00m);
    
    // Assert - Observable outcome
    control.AssertDisplayedBalance("$125,000.00");
    control.AssertColorIndicator(AppColors.Profit);
}
```

### ❌ Don't Use Magic Numbers

```csharp
// Bad - Magic numbers without context
[Fact]
public void ProfitCalculation_Returns_CorrectValue()
{
    var result = Calculator.CalculateProfit(100000, 115000);
    Assert.Equal(15.0, result); // What does 15.0 represent?
}
```

### ✅ Do Use Named Constants and Clear Context

```csharp
// Good - Clear constants and context
[Fact]
public void ProfitCalculation_FifteenPercentGain_ReturnsCorrectPercentage()
{
    // Arrange - Clear scenario
    const decimal InitialInvestment = 100000.00m;
    const decimal CurrentValue = 115000.00m;
    const decimal ExpectedProfitPercentage = 15.0m;
    
    // Act
    var actualProfit = Calculator.CalculateProfit(InitialInvestment, CurrentValue);
    
    // Assert
    actualProfit.AssertDecimalEquals(ExpectedProfitPercentage, precision: 2);
}
```

## Summary

Following these best practices ensures:

1. **Reliable Tests**: Consistent, deterministic results across platforms
2. **Realistic Scenarios**: Tests reflect actual investment use cases  
3. **Maintainable Code**: Clear structure and naming conventions
4. **Performance Awareness**: Tests validate mobile device constraints
5. **Platform Coverage**: Appropriate testing across Android, iOS, Windows, and MacCatalyst
6. **Financial Accuracy**: Proper handling of decimal precision and edge cases

These practices help create a robust testing foundation for investment tracking applications that provides confidence in production deployments and catches regressions early in development.