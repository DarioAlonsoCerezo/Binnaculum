# Creating Custom Investment Assertions

Guide for extending Binnaculum's TestUtils with custom assertions specific to investment tracking functionality.

## Overview

Binnaculum's TestUtils framework is designed to be extensible with custom assertions that match your investment app's specific requirements. This guide shows how to create domain-specific assertions that integrate seamlessly with the existing infrastructure.

## Basic Custom Assertion Structure

### Extension Method Pattern

All custom assertions in Binnaculum follow the extension method pattern:

```csharp
using Xunit;
using Binnaculum.Core.Models;

namespace Binnaculum.Tests.CustomAssertions
{
    public static class InvestmentAssertions
    {
        /// <summary>
        /// Asserts that a portfolio meets diversification requirements
        /// </summary>
        /// <param name="portfolio">Portfolio to validate</param>
        /// <param name="minSectors">Minimum number of sectors required</param>
        /// <param name="maxSectorConcentration">Maximum percentage in any single sector</param>
        public static void AssertPortfolioDiversification(
            this BrokerAccount portfolio,
            int minSectors = 5,
            decimal maxSectorConcentration = 0.25m)
        {
            // Get sector breakdown from portfolio holdings
            var sectorAllocation = portfolio.GetSectorAllocation();
            
            // Assert minimum sector count
            Assert.True(sectorAllocation.Count >= minSectors,
                $"Portfolio has {sectorAllocation.Count} sectors, " +
                $"minimum {minSectors} required for diversification");
            
            // Assert no single sector is over-concentrated
            var maxConcentration = sectorAllocation.Values.Max();
            Assert.True(maxConcentration <= maxSectorConcentration,
                $"Portfolio has {maxConcentration:P1} in single sector, " +
                $"maximum {maxSectorConcentration:P1} recommended");
        }
        
        /// <summary>
        /// Asserts that investment performance falls within expected risk-adjusted returns
        /// </summary>
        /// <param name="account">Investment account to analyze</param>
        /// <param name="expectedSharpeRatio">Expected Sharpe ratio range</param>
        /// <param name="timeFrame">Analysis time frame</param>
        public static void AssertRiskAdjustedReturns(
            this BrokerAccount account,
            (decimal min, decimal max) expectedSharpeRatio,
            TimeSpan timeFrame)
        {
            var performance = account.CalculatePerformanceMetrics(timeFrame);
            
            Assert.True(performance.SharpeRatio >= expectedSharpeRatio.min,
                $"Sharpe ratio {performance.SharpeRatio:F2} below minimum {expectedSharpeRatio.min:F2}");
            
            Assert.True(performance.SharpeRatio <= expectedSharpeRatio.max,
                $"Sharpe ratio {performance.SharpeRatio:F2} above maximum {expectedSharpeRatio.max:F2} " +
                $"(may indicate excessive risk)");
        }
    }
}
```

## Advanced Financial Assertions

### Currency Conversion and Multi-Currency Testing

```csharp
public static class CurrencyAssertions
{
    /// <summary>
    /// Asserts that multi-currency portfolio values are consistent across base currency conversions
    /// </summary>
    /// <param name="portfolio">Multi-currency portfolio</param>
    /// <param name="baseCurrency">Base currency for comparison</param>
    /// <param name="tolerance">Acceptable conversion tolerance (default: 0.01)</param>
    public static void AssertCurrencyConsistency(
        this IEnumerable<BrokerAccount> portfolio,
        string baseCurrency = "USD",
        decimal tolerance = 0.01m)
    {
        var accounts = portfolio.ToList();
        var exchangeRates = CurrencyService.GetCurrentExchangeRates();
        
        // Calculate total value in base currency using different methods
        var totalValueMethod1 = accounts
            .Sum(account => account.ConvertToBaseCurrency(baseCurrency, exchangeRates));
        
        var totalValueMethod2 = accounts
            .Select(account => account.ConvertToBaseCurrency(baseCurrency, exchangeRates))
            .Sum();
        
        var difference = Math.Abs(totalValueMethod1 - totalValueMethod2);
        
        Assert.True(difference <= tolerance,
            $"Currency conversion inconsistency detected: {difference:C} " +
            $"exceeds tolerance of {tolerance:C}");
    }
    
    /// <summary>
    /// Asserts that currency formatting matches culture-specific expectations
    /// </summary>
    /// <param name="value">Currency value to format</param>
    /// <param name="currency">Currency code (ISO 4217)</param>
    /// <param name="culture">Culture for formatting</param>
    /// <param name="expectedPattern">Expected formatting pattern</param>
    public static void AssertCultureSpecificFormatting(
        this decimal value,
        string currency,
        CultureInfo culture,
        string expectedPattern = null)
    {
        using (new CultureScope(culture))
        {
            var formatted = value.FormatCurrency(currency);
            
            if (expectedPattern != null)
            {
                Assert.Matches(expectedPattern, formatted);
            }
            
            // Verify culture-specific characteristics
            switch (culture.Name)
            {
                case "en-US":
                    Assert.Contains("$", formatted);
                    Assert.Contains(",", formatted); // Thousands separator
                    Assert.Contains(".", formatted); // Decimal separator
                    break;
                    
                case "de-DE":
                    Assert.Contains("€", formatted);
                    Assert.Contains(".", formatted); // Thousands separator in German
                    Assert.Contains(",", formatted); // Decimal separator in German
                    break;
                    
                case "ja-JP":
                    Assert.Contains("¥", formatted);
                    Assert.DoesNotContain(".", formatted); // No decimal for Yen
                    break;
            }
        }
    }
}
```

### Performance and Timing Assertions

```csharp
public static class PerformanceAssertions
{
    /// <summary>
    /// Asserts that investment calculation performance meets mobile device requirements
    /// </summary>
    /// <param name="calculationFunc">Function to measure</param>
    /// <param name="maxExecutionTime">Maximum allowed execution time</param>
    /// <param name="maxMemoryUsage">Maximum allowed memory usage in bytes</param>
    /// <param name="operationName">Name for logging purposes</param>
    public static async Task AssertMobilePerformance<T>(
        this Func<Task<T>> calculationFunc,
        TimeSpan maxExecutionTime,
        long maxMemoryUsage,
        string operationName = "Operation")
    {
        // Warm up to avoid JIT overhead
        await calculationFunc();
        GC.Collect();
        
        // Measure actual performance
        var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);
        var stopwatch = Stopwatch.StartNew();
        
        var result = await calculationFunc();
        
        stopwatch.Stop();
        var memoryAfter = GC.GetTotalMemory(forceFullCollection: false);
        var memoryUsed = memoryAfter - memoryBefore;
        
        // Assert performance requirements
        Assert.True(stopwatch.Elapsed <= maxExecutionTime,
            $"{operationName} took {stopwatch.ElapsedMilliseconds}ms, " +
            $"exceeds mobile limit of {maxExecutionTime.TotalMilliseconds}ms");
        
        Assert.True(memoryUsed <= maxMemoryUsage,
            $"{operationName} used {memoryUsed / 1024 / 1024}MB, " +
            $"exceeds mobile limit of {maxMemoryUsage / 1024 / 1024}MB");
    }
    
    /// <summary>
    /// Asserts that UI updates maintain 60 FPS performance
    /// </summary>
    /// <param name="uiUpdateFunc">UI update function</param>
    /// <param name="updateCount">Number of updates to test</param>
    /// <param name="targetFPS">Target FPS (default: 60)</param>
    public static async Task AssertUIFrameRate(
        this Func<Task> uiUpdateFunc,
        int updateCount = 100,
        int targetFPS = 60)
    {
        var targetFrameTime = TimeSpan.FromMilliseconds(1000.0 / targetFPS);
        var updateTimes = new List<TimeSpan>();
        
        for (int i = 0; i < updateCount; i++)
        {
            var frameStart = Stopwatch.StartNew();
            await uiUpdateFunc();
            frameStart.Stop();
            
            updateTimes.Add(frameStart.Elapsed);
        }
        
        var averageFrameTime = TimeSpan.FromMilliseconds(updateTimes.Average(t => t.TotalMilliseconds));
        var maxFrameTime = updateTimes.Max();
        
        Assert.True(averageFrameTime <= targetFrameTime,
            $"Average frame time {averageFrameTime.TotalMilliseconds:F1}ms " +
            $"exceeds target {targetFrameTime.TotalMilliseconds:F1}ms for {targetFPS} FPS");
        
        // Allow occasional frame drops, but not too many
        var slowFrames = updateTimes.Count(t => t > targetFrameTime);
        var slowFramePercentage = (double)slowFrames / updateCount;
        
        Assert.True(slowFramePercentage <= 0.05, // Max 5% slow frames
            $"Too many slow frames: {slowFramePercentage:P1} exceeds 5% threshold");
    }
}
```

## Domain-Specific Investment Assertions

### Risk Management Assertions

```csharp
public static class RiskManagementAssertions
{
    /// <summary>
    /// Asserts that portfolio position sizing follows risk management rules
    /// </summary>
    /// <param name="portfolio">Investment portfolio</param>
    /// <param name="maxPositionSize">Maximum position size as percentage of total</param>
    /// <param name="maxSectorExposure">Maximum sector exposure</param>
    public static void AssertRiskManagementCompliance(
        this BrokerAccount portfolio,
        decimal maxPositionSize = 0.05m, // 5% max position
        decimal maxSectorExposure = 0.20m) // 20% max sector
    {
        var positions = portfolio.GetPositions();
        var totalValue = portfolio.GetTotalValue();
        
        foreach (var position in positions)
        {
            var positionPercentage = position.Value / totalValue;
            
            Assert.True(positionPercentage <= maxPositionSize,
                $"Position {position.Symbol} represents {positionPercentage:P1} of portfolio, " +
                $"exceeds {maxPositionSize:P1} risk limit");
        }
        
        var sectorExposure = portfolio.GetSectorExposure();
        foreach (var sector in sectorExposure)
        {
            Assert.True(sector.Value <= maxSectorExposure,
                $"Sector {sector.Key} represents {sector.Value:P1} of portfolio, " +
                $"exceeds {maxSectorExposure:P1} risk limit");
        }
    }
    
    /// <summary>
    /// Asserts that portfolio drawdown stays within acceptable limits
    /// </summary>
    /// <param name="account">Investment account</param>
    /// <param name="maxDrawdown">Maximum acceptable drawdown percentage</param>
    /// <param name="timeFrame">Analysis period</param>
    public static void AssertDrawdownLimits(
        this BrokerAccount account,
        decimal maxDrawdown = 0.20m, // 20% max drawdown
        TimeSpan? timeFrame = null)
    {
        var analysisStart = DateTime.Now - (timeFrame ?? TimeSpan.FromDays(365));
        var performance = account.GetPerformanceHistory(analysisStart);
        
        var peak = decimal.MinValue;
        var maxObservedDrawdown = 0m;
        
        foreach (var dailyValue in performance.DailyValues)
        {
            if (dailyValue > peak)
            {
                peak = dailyValue;
            }
            
            var currentDrawdown = (peak - dailyValue) / peak;
            if (currentDrawdown > maxObservedDrawdown)
            {
                maxObservedDrawdown = currentDrawdown;
            }
        }
        
        Assert.True(maxObservedDrawdown <= maxDrawdown,
            $"Maximum drawdown {maxObservedDrawdown:P1} exceeds limit of {maxDrawdown:P1}");
    }
}
```

### Compliance and Regulatory Assertions

```csharp
public static class ComplianceAssertions
{
    /// <summary>
    /// Asserts that wash sale rules are properly tracked
    /// </summary>
    /// <param name="transactions">Transaction history</param>
    /// <param name="symbol">Symbol to check for wash sales</param>
    public static void AssertWashSaleCompliance(
        this IEnumerable<Transaction> transactions,
        string symbol)
    {
        var symbolTransactions = transactions
            .Where(t => t.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Date)
            .ToList();
        
        var washSaleViolations = new List<(Transaction sell, Transaction buy)>();
        
        for (int i = 0; i < symbolTransactions.Count - 1; i++)
        {
            var transaction = symbolTransactions[i];
            
            if (transaction.Type == TransactionType.Sell && transaction.RealizedGainLoss < 0)
            {
                // Look for purchases within 30 days before or after
                var washSalePeriodStart = transaction.Date.AddDays(-30);
                var washSalePeriodEnd = transaction.Date.AddDays(30);
                
                var potentialWashSales = symbolTransactions
                    .Where(t => t.Type == TransactionType.Buy &&
                               t.Date >= washSalePeriodStart &&
                               t.Date <= washSalePeriodEnd &&
                               t.Date != transaction.Date)
                    .ToList();
                
                foreach (var washSaleBuy in potentialWashSales)
                {
                    washSaleViolations.Add((transaction, washSaleBuy));
                }
            }
        }
        
        if (washSaleViolations.Any())
        {
            var violationDetails = string.Join("\n", washSaleViolations
                .Select(v => $"  • Sold {symbol} at loss on {v.sell.Date:yyyy-MM-dd}, " +
                           $"bought again on {v.buy.Date:yyyy-MM-dd}"));
            
            Assert.True(false, 
                $"Wash sale violations detected for {symbol}:\n{violationDetails}\n" +
                "These transactions may not be tax-deductible losses.");
        }
    }
    
    /// <summary>
    /// Asserts that day trading buying power rules are followed
    /// </summary>
    /// <param name="account">Trading account</param>
    /// <param name="transactions">Daily transactions</param>
    /// <param name="date">Trading date to check</param>
    public static void AssertDayTradingCompliance(
        this BrokerAccount account,
        IEnumerable<Transaction> transactions,
        DateTime date)
    {
        var dayTrades = transactions
            .Where(t => t.Date.Date == date.Date)
            .GroupBy(t => t.Symbol)
            .Where(g => g.Count() > 1) // Same symbol traded multiple times
            .Where(g => g.Any(t => t.Type == TransactionType.Buy) && 
                       g.Any(t => t.Type == TransactionType.Sell))
            .ToList();
        
        if (dayTrades.Any() && account.AccountType != AccountType.PatternDayTrader)
        {
            var dayTradeCount = account.GetDayTradeCount(TimeSpan.FromDays(5)); // Rolling 5-day period
            
            Assert.True(dayTradeCount < 3,
                $"Account executed {dayTradeCount} day trades in 5-day period. " +
                "Non-PDT accounts are limited to 3 day trades per 5-day period.");
            
            // Check buying power requirements
            var requiredBuyingPower = dayTrades.Sum(g => g.Sum(t => Math.Abs(t.Value)));
            var availableBuyingPower = account.GetDayTradingBuyingPower();
            
            Assert.True(availableBuyingPower >= requiredBuyingPower,
                $"Insufficient day trading buying power: {availableBuyingPower:C} available, " +
                $"{requiredBuyingPower:C} required");
        }
    }
}
```

## Platform-Specific Custom Assertions

### Android Material Design Investment UI

```csharp
#if ANDROID
public static class AndroidInvestmentAssertions
{
    /// <summary>
    /// Asserts that investment cards follow Material Design principles
    /// </summary>
    /// <param name="investmentCard">Investment card view</param>
    public static void AssertMaterialDesignCompliance(this View investmentCard)
    {
        // Check elevation for depth perception
        var elevation = investmentCard.Elevation;
        Assert.True(elevation >= 2 && elevation <= 8,
            $"Investment card elevation {elevation}dp should be between 2-8dp for proper depth");
        
        // Check corner radius for modern appearance
        if (investmentCard is CardView cardView)
        {
            var cornerRadius = cardView.Radius;
            Assert.True(cornerRadius >= 4 && cornerRadius <= 16,
                $"Card corner radius {cornerRadius}dp should be between 4-16dp");
        }
        
        // Check touch feedback
        Assert.True(investmentCard.IsClickable && investmentCard.Focusable,
            "Investment cards should be clickable with touch feedback");
    }
    
    /// <summary>
    /// Asserts that financial colors follow Material Design color guidelines
    /// </summary>
    /// <param name="view">View displaying financial data</param>
    /// <param name="financialValue">Financial value (positive/negative)</param>
    public static void AssertFinancialColorGuidelines(this View view, decimal financialValue)
    {
        var expectedColorResource = financialValue >= 0 
            ? Android.Resource.Color.HoloProfitGreen 
            : Android.Resource.Color.HoloRedDark;
        
        if (view is TextView textView)
        {
            var currentColor = textView.CurrentTextColor;
            var expectedColor = ContextCompat.GetColor(view.Context, expectedColorResource);
            
            Assert.Equal(expectedColor, currentColor);
        }
    }
}
#endif
```

### iOS Investment UI Assertions

```csharp
#if IOS
public static class iOSInvestmentAssertions
{
    /// <summary>
    /// Asserts that investment displays follow iOS Human Interface Guidelines
    /// </summary>
    /// <param name="investmentView">Investment display view</param>
    public static void AssertHIGCompliance(this UIView investmentView)
    {
        // Check minimum touch target size (44x44 points)
        if (investmentView is UIButton button)
        {
            Assert.True(button.Frame.Width >= 44 && button.Frame.Height >= 44,
                $"Touch target {button.Frame.Width}x{button.Frame.Height} should be at least 44x44 points");
        }
        
        // Check accessibility
        Assert.True(investmentView.IsAccessibilityElement || investmentView.AccessibilityElementsHidden,
            "Investment views should be accessible or explicitly hidden from accessibility");
        
        Assert.False(string.IsNullOrEmpty(investmentView.AccessibilityLabel),
            "Investment views should have descriptive accessibility labels");
    }
    
    /// <summary>
    /// Asserts that financial number formatting follows iOS conventions
    /// </summary>
    /// <param name="label">Label displaying financial data</param>
    /// <param name="value">Financial value</param>
    /// <param name="locale">Expected locale</param>
    public static void AssertIOSFinancialFormatting(this UILabel label, decimal value, NSLocale locale)
    {
        var formatter = new NSNumberFormatter
        {
            NumberStyle = NSNumberFormatterStyle.Currency,
            Locale = locale
        };
        
        var expectedText = formatter.StringFromNumber(new NSDecimalNumber(value));
        
        Assert.Equal(expectedText, label.Text);
    }
}
#endif
```

## Integration with Existing TestUtils

### Registering Custom Assertions

Create an extension registration system:

```csharp
public static class CustomAssertionRegistry
{
    private static readonly List<Type> RegisteredAssertions = new List<Type>();
    
    /// <summary>
    /// Register custom assertion classes for discovery
    /// </summary>
    /// <typeparam name="T">Custom assertion class</typeparam>
    public static void RegisterAssertions<T>() where T : class
    {
        RegisteredAssertions.Add(typeof(T));
    }
    
    /// <summary>
    /// Get all registered assertion methods for documentation generation
    /// </summary>
    public static IEnumerable<MethodInfo> GetAllAssertionMethods()
    {
        return RegisteredAssertions
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.Name.StartsWith("Assert"))
            .Where(method => method.IsExtensionMethod());
    }
}

// In your test assembly initialization
[AssemblyInitialize]
public static void InitializeCustomAssertions(TestContext context)
{
    // Register all custom assertion classes
    CustomAssertionRegistry.RegisterAssertions<InvestmentAssertions>();
    CustomAssertionRegistry.RegisterAssertions<CurrencyAssertions>();
    CustomAssertionRegistry.RegisterAssertions<PerformanceAssertions>();
    CustomAssertionRegistry.RegisterAssertions<RiskManagementAssertions>();
    CustomAssertionRegistry.RegisterAssertions<ComplianceAssertions>();
    
    #if ANDROID
    CustomAssertionRegistry.RegisterAssertions<AndroidInvestmentAssertions>();
    #endif
    
    #if IOS
    CustomAssertionRegistry.RegisterAssertions<iOSInvestmentAssertions>();
    #endif
}
```

## Best Practices for Custom Assertions

### 1. Naming Conventions

- Use `Assert` prefix for all assertion methods
- Be descriptive and domain-specific: `AssertPortfolioDiversification` vs `AssertDiversity`
- Include expected parameters in name when helpful: `AssertRiskAdjustedReturns`

### 2. Error Messages

```csharp
// ✅ Good: Descriptive with context
Assert.True(sharpeRatio >= expectedMin,
    $"Sharpe ratio {sharpeRatio:F2} below minimum {expectedMin:F2} " +
    $"for {timeFrame.TotalDays}-day analysis of {portfolio.Name}");

// ❌ Bad: Vague without context
Assert.True(sharpeRatio >= expectedMin, "Sharpe ratio too low");
```

### 3. Tolerance and Precision

```csharp
// Always specify precision for financial calculations
public static void AssertDecimalEquals(this decimal actual, decimal expected, int decimalPlaces = 2)
{
    var tolerance = (decimal)Math.Pow(10, -decimalPlaces);
    var difference = Math.Abs(actual - expected);
    
    Assert.True(difference <= tolerance,
        $"Expected {expected:F{decimalPlaces}}, actual {actual:F{decimalPlaces}}, " +
        $"difference {difference:F{decimalPlaces + 1}} exceeds tolerance {tolerance:F{decimalPlaces + 1}}");
}
```

### 4. Async Support

```csharp
// Support async operations for database and network calls
public static async Task AssertPortfolioDataIntegrityAsync(this BrokerAccount account)
{
    var positions = await account.GetPositionsAsync();
    var calculatedTotal = positions.Sum(p => p.Value);
    var reportedTotal = account.TotalValue;
    
    calculatedTotal.AssertDecimalEquals(reportedTotal, precision: 2);
}
```

## Documentation and Discovery

### XML Documentation

Always provide comprehensive XML documentation:

```csharp
/// <summary>
/// Asserts that an investment portfolio maintains proper diversification across sectors and positions.
/// This assertion helps ensure risk management compliance for investment tracking applications.
/// </summary>
/// <param name="portfolio">The portfolio to analyze for diversification</param>
/// <param name="minSectors">Minimum number of sectors required (default: 5)</param>
/// <param name="maxSectorConcentration">Maximum percentage allowed in any single sector (default: 25%)</param>
/// <exception cref="AssertFailedException">Thrown when diversification requirements are not met</exception>
/// <example>
/// <code>
/// var portfolio = new BrokerAccountBuilder()
///     .WithDiversifiedHoldings()
///     .Build();
/// 
/// portfolio.AssertPortfolioDiversification(minSectors: 6, maxSectorConcentration: 0.20m);
/// </code>
/// </example>
/// <remarks>
/// This assertion is particularly useful for testing:
/// - Retirement portfolios that need broad diversification
/// - Risk management compliance
/// - Automated rebalancing algorithms
/// </remarks>
public static void AssertPortfolioDiversification(...)
```

Custom assertions extend Binnaculum's TestUtils framework to provide domain-specific validation for investment tracking functionality. By following these patterns and best practices, you can create maintainable, discoverable, and reliable assertions that integrate seamlessly with the existing testing infrastructure.