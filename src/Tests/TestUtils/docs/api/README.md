# TestUtils API Documentation

This directory contains API documentation for Binnaculum's TestUtils infrastructure components.

## Core API Components

### Assertion Extensions (`BinnaculumAssertionExtensions`)

Investment-specific assertions for comprehensive testing:

#### Currency and Financial Assertions
- `AssertCurrencyFormat(expectedFormat)` - Validates currency formatting across cultures
- `AssertBinnaculumCurrencyFormat(value)` - Tests custom investment app formatting
- `AssertPercentageCalculation(expected, tolerance)` - Verifies percentage calculations
- `AssertPortfolioBalance(expectedBalance, tolerance)` - Tests portfolio balance calculations  
- `AssertFinancialSnapshot(snapshot)` - Comprehensive financial validation

#### Memory and Observable Testing
- `AssertObservableMemoryLeak<T>(observable)` - Tests Observable chains for memory leaks
- `WaitForGCAsync(timeout)` - Garbage collection helper for memory testing

#### Platform-Specific Assertions
- `AssertMaterialRippleEffect()` - Android Material Design validation (Android only)
- `AssertHapticFeedbackTriggered()` - iOS haptic feedback validation (iOS only)

### Test Data Builders

Fluent builders for realistic investment scenarios:

#### `BrokerAccountBuilder`
```csharp
var account = new BrokerAccountBuilder()
    .WithName("Interactive Brokers Main")
    .WithBroker(BrokerBuilder.AsInteractiveBrokers())
    .WithCurrency(CurrencyBuilder.AsUSD())
    .WithBalance(125750.50m)
    .AsProfitableScenario(returnPercentage: 12.5m)
    .WithTransactionCount(45)
    .Build();
```

#### `InvestmentTestData`
Static factory methods for common test scenarios:
- `CreateSingleBrokerAccount()` - Simple single-account scenario
- `CreateLargePortfolio(accountCount, transactionsPerAccount)` - Performance testing data
- `CreateMixedPortfolio()` - Mixed profit/loss scenarios
- `CreateHistoricalPriceData(days, dataPointsPerDay)` - Chart testing data

### Test Runners

#### Visual Test Runner (`VisualTestRunnerLauncher`)
Interactive XAML-based test execution:

```csharp
// Launch visual runner for development/debugging
var app = VisualTestRunnerLauncher.LaunchVisualRunner();

// Custom configuration
var builder = VisualTestRunnerLauncher.CreateMauiApp();
// Add configuration
var app = builder.Build();
```

#### Headless Test Runner (`HeadlessTestRunner`)  
Command-line execution for CI/CD:

```bash
# Command-line usage
./scripts/run-headless-tests.sh --platform android --collect-artifacts

# Programmatic usage
var runner = new HeadlessTestRunner(config);
var results = await runner.ExecuteTestsAsync(testSelection);
```

### Platform Abstractions

#### Device Testing (`TestDevice`)
Platform-specific device management:

```csharp
var device = TestDevice.GetCurrentDevice();
if (device.Platform == TestPlatform.Android)
{
    // Android-specific testing
}
```

#### App Interface (`IApp`)
Cross-platform app interaction:

```csharp
IApp app = AppFactory.CreateApp(TestPlatform.Android);
var element = app.FindElement("BrokerAccountTemplate");
await app.TapAsync(element);
```

## Detailed API Reference

> **Note**: Complete API documentation with method signatures, parameters, and examples will be generated automatically using XML documentation comments and DocFX or similar tools.

### Generating Full API Documentation

To generate complete API documentation:

```bash
# Install documentation generation tools
dotnet tool install -g docfx

# Generate API documentation
docfx build docfx.json

# Serve documentation locally
docfx serve _site/
```

### Integration with IDE

Most IDEs will show inline documentation from XML comments:

```csharp
/// <summary>
/// Validates currency formatting for investment values across different cultures.
/// </summary>
/// <param name="expectedFormat">The expected formatted currency string</param>
/// <param name="culture">Optional culture for format validation</param>
/// <example>
/// <code>
/// var account = CreateTestAccount(balance: 125750.50m, currency: "USD");
/// account.FormatCurrency().AssertCurrencyFormat("$125,750.50");
/// </code>
/// </example>
public static void AssertCurrencyFormat(this string actualFormat, string expectedFormat, CultureInfo culture = null)
```

## Usage Patterns

### Basic Component Testing
```csharp
[Fact]
public async Task ComponentTest_Scenario_ExpectedResult()
{
    // Arrange - Use builders for realistic data
    var testData = new BrokerAccountBuilder()
        .AsProfitableScenario()
        .Build();
    
    // Act - Test component behavior
    var component = new BrokerAccountTemplate();
    await component.LoadAsync(testData);
    
    // Assert - Use investment-specific assertions
    component.AssertProfitColorIndicator(AppColors.Profit);
    component.AssertCurrencyFormat(testData.FormattedBalance);
}
```

### Performance Testing
```csharp
[Fact] 
public async Task PerformanceTest_LargeDataset_MeetsRequirements()
{
    var portfolio = InvestmentTestData.CreateLargePortfolio(1000);
    
    var metrics = await PerformanceTestHelpers.MeasureAsync(
        () => calculator.CalculateAsync(portfolio),
        "Large Portfolio Calculation"
    );
    
    metrics.AssertPerformanceRequirements(
        maxTimeMs: 2000,
        maxMemoryMB: 100
    );
}
```

### Platform-Specific Testing
```csharp
[Fact]
public async Task PlatformTest_AndroidSpecific_MaterialDesign()
{
    if (DeviceInfo.Platform != DevicePlatform.Android)
        return;
    
    var control = new InvestmentControl();
    await control.SimulateTouchAsync();
    
    control.AssertMaterialRippleEffect();
    control.AssertElevationShadow();
}
```

## Extension Points

The TestUtils API is designed for extensibility:

### Custom Assertions
```csharp
public static class CustomAssertions
{
    public static void AssertPortfolioDiversification(
        this BrokerAccount account,
        int expectedSectors)
    {
        // Custom assertion implementation
    }
}
```

### Custom Test Data
```csharp
public static class CustomTestData  
{
    public static BrokerAccount CreateRetirementPortfolio()
    {
        return new BrokerAccountBuilder()
            .WithConservativeAllocation()
            .WithIncomeOriented()
            .Build();
    }
}
```

## Contributing to API Documentation

When adding new API components:

1. **Add XML Documentation**: Include comprehensive XML comments
2. **Provide Examples**: Include realistic usage examples in XML comments  
3. **Update API Overview**: Add new components to this overview
4. **Test Documentation**: Ensure examples compile and run successfully

For detailed contributing guidelines, see the main [TestUtils documentation](../README.md).