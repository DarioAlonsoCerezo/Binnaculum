namespace Binnaculum.UI.DeviceTests;

/// <summary>
/// Core device testing framework validation tests.
/// These tests verify that the basic testing infrastructure works correctly.
/// </summary>
public class CoreDeviceTestingFrameworkValidationTests
{
    [Fact]
    public void CurrencyFormat_US_Culture_FormatsCorrectly()
    {
        // Arrange
        var amount = 1234.56m;
        var culture = "en-US";
        var expected = "$1,234.56";
        
        // Act & Assert
        amount.AssertCurrencyFormat(culture, expected);
    }

    [Fact]
    public void CurrencyFormat_Euro_Culture_FormatsCorrectly()
    {
        // Arrange
        var amount = 1234.56m;
        var culture = "de-DE";
        var expected = "1.234,56 €";
        
        // Act & Assert
        amount.AssertCurrencyFormat(culture, expected);
    }

    [Fact]
    public void BinnaculumCurrencyFormat_USD_Symbol_FormatsCorrectly()
    {
        // Arrange
        var amount = 1234.56m;
        var symbol = "$";
        var expected = "$1234.56";
        
        // Act & Assert
        amount.AssertBinnaculumCurrencyFormat(symbol, expected);
    }

    [Fact]
    public void SimplifiedFormat_FormatsCorrectly()
    {
        // Arrange
        var wholeNumber = 1000m;
        var decimalNumber = 1234.56m;
        var smallNumber = 0.1234m;
        
        // Act & Assert
        wholeNumber.AssertSimplifiedFormat("1000");
        decimalNumber.AssertSimplifiedFormat("1234.56");
        smallNumber.AssertSimplifiedFormat("0.1234");
    }

    [Fact]
    public void FSharpInterop_OptionTypes_WorkCorrectly()
    {
        // Arrange
        var someValue = FSharpInteropHelpersSimple.CreateSome(42);
        var noneValue = FSharpInteropHelpersSimple.CreateNone<int>();
        
        // Act & Assert
        Assert.True(FSharpInteropHelpersSimple.HasValue(someValue));
        Assert.False(FSharpInteropHelpersSimple.HasValue(noneValue));
        Assert.Equal(42, FSharpInteropHelpersSimple.GetValue(someValue));
        Assert.Equal(100, FSharpInteropHelpersSimple.GetValueOrDefault(noneValue, 100));
    }

    [Fact]
    public void TestDataBuilders_CreateValidCurrencies()
    {
        // Arrange & Act
        var usdCurrency = TestDataBuilders.CreateCurrency().AsUSD().Build();
        var eurCurrency = TestDataBuilders.CreateCurrency().AsEUR().Build();
        var gbpCurrency = TestDataBuilders.CreateCurrency().AsGBP().Build();
        
        // Assert
        FSharpInteropHelpersSimple.ValidateCurrency(usdCurrency);
        FSharpInteropHelpersSimple.ValidateCurrency(eurCurrency);
        FSharpInteropHelpersSimple.ValidateCurrency(gbpCurrency);
        
        Assert.Equal("USD", usdCurrency.Code);
        Assert.Equal("EUR", eurCurrency.Code);
        Assert.Equal("GBP", gbpCurrency.Code);
    }

    [Fact]
    public void TestDataBuilders_CreateValidBrokers()
    {
        // Arrange & Act
        var interactiveBrokers = TestDataBuilders.CreateBroker().AsInteractiveBrokers().Build();
        var charlesSchwab = TestDataBuilders.CreateBroker().AsCharlesSchwab().Build();
        var fidelity = TestDataBuilders.CreateBroker().AsFidelity().Build();
        
        // Assert
        FSharpInteropHelpersSimple.ValidateBroker(interactiveBrokers);
        FSharpInteropHelpersSimple.ValidateBroker(charlesSchwab);
        FSharpInteropHelpersSimple.ValidateBroker(fidelity);
        
        Assert.Equal("Interactive Brokers", interactiveBrokers.Name);
        Assert.Equal("Charles Schwab", charlesSchwab.Name);
        Assert.Equal("Fidelity", fidelity.Name);
    }

    [Fact]
    public void TestDataBuilders_CreateValidFinancialSnapshots()
    {
        // Arrange & Act
        var profitableSnapshot = InvestmentTestData.AppleStock.ProfitableScenario;
        var lossSnapshot = InvestmentTestData.AppleStock.LossScenario;
        var volatileSnapshot = InvestmentTestData.TeslaStock.VolatileGains;
        
        // Assert
        FSharpInteropHelpersSimple.ValidateFinancialSnapshot(profitableSnapshot);
        FSharpInteropHelpersSimple.ValidateFinancialSnapshot(lossSnapshot);
        FSharpInteropHelpersSimple.ValidateFinancialSnapshot(volatileSnapshot);
        
        Assert.True(profitableSnapshot.RealizedGains > 0);
        Assert.True(lossSnapshot.RealizedGains < 0);
        Assert.True(volatileSnapshot.RealizedGains > 0);
    }

    [Fact]
    public void InvestmentTestData_ProfitableScenarios_HavePositiveReturns()
    {
        // Arrange
        var profitableScenarios = InvestmentTestData.GetProfitableScenarios();
        
        // Act & Assert
        Assert.NotEmpty(profitableScenarios);
        
        foreach (var scenario in profitableScenarios)
        {
            Assert.True(scenario.RealizedGains > 0, $"Scenario {scenario.Id} should have positive realized gains");
            FSharpInteropHelpersSimple.ValidateFinancialSnapshot(scenario);
        }
    }

    [Fact]
    public void InvestmentTestData_LossScenarios_HaveNegativeReturns()
    {
        // Arrange
        var lossScenarios = InvestmentTestData.GetLossScenarios();
        
        // Act & Assert
        Assert.NotEmpty(lossScenarios);
        
        foreach (var scenario in lossScenarios)
        {
            Assert.True(scenario.RealizedGains < 0, $"Scenario {scenario.Id} should have negative realized gains");
            FSharpInteropHelpersSimple.ValidateFinancialSnapshot(scenario);
        }
    }

    [Fact]
    public void InvestmentTestData_DividendScenarios_HaveDividendIncome()
    {
        // Arrange
        var dividendScenarios = InvestmentTestData.GetDividendScenarios();
        
        // Act & Assert
        Assert.NotEmpty(dividendScenarios);
        
        foreach (var scenario in dividendScenarios)
        {
            Assert.True(scenario.DividendsReceived > 0, $"Scenario {scenario.Id} should have dividend income");
            FSharpInteropHelpersSimple.ValidateFinancialSnapshot(scenario);
        }
    }

    [Fact]
    public void PercentageCalculations_AreRealistic()
    {
        // Arrange
        var allScenarios = InvestmentTestData.GetAllScenarios();
        
        // Act & Assert
        Assert.NotEmpty(allScenarios);
        
        foreach (var scenario in allScenarios)
        {
            // Validate percentage calculations are within reasonable bounds
            Assert.True(scenario.RealizedPercentage >= -100, 
                $"Scenario {scenario.Id} has unrealistic realized percentage: {scenario.RealizedPercentage}");
            Assert.True(scenario.RealizedPercentage <= 1000, 
                $"Scenario {scenario.Id} has unrealistic realized percentage: {scenario.RealizedPercentage}");
                
            if (scenario.UnrealizedGainsPercentage != 0) // In F# models, this is a decimal, not nullable
            {
                Assert.True(scenario.UnrealizedGainsPercentage >= -100, 
                    $"Scenario {scenario.Id} has unrealistic unrealized percentage: {scenario.UnrealizedGainsPercentage}");
                Assert.True(scenario.UnrealizedGainsPercentage <= 1000, 
                    $"Scenario {scenario.Id} has unrealistic unrealized percentage: {scenario.UnrealizedGainsPercentage}");
            }
        }
    }

    [Fact]
    public async Task TestHelpers_WaitForCondition_WorksWithSimpleConditions()
    {
        // Arrange
        var completed = false;
        var startTime = DateTime.UtcNow;
        
        // Act - simulate a condition that becomes true after a short delay
        var task = Task.Run(async () =>
        {
            await Task.Delay(100); // 100ms delay
            completed = true;
        });
        
        // Wait for condition to become true
        await TestHelpers.WaitForCondition(() => completed, TimeSpan.FromSeconds(1));
        
        // Assert
        Assert.True(completed, "Condition should have been met");
        
        var elapsed = DateTime.UtcNow - startTime;
        Assert.True(elapsed < TimeSpan.FromSeconds(1), "Should not have waited the full timeout");
    }
}