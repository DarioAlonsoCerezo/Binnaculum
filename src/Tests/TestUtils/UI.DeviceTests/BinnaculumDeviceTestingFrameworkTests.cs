namespace Binnaculum.UI.DeviceTests;

/// <summary>
/// Basic tests for the Binnaculum device testing framework.
/// Tests the core assertion extensions and test data builders.
/// </summary>
public class BinnaculumDeviceTestingFrameworkTests
{
    [Fact]
    public void TestDataBuilders_CanCreateBroker()
    {
        // Arrange & Act
        var broker = TestDataBuilders.CreateBroker()
            .AsInteractiveBrokers()
            .Build();
        
        // Assert
        Assert.NotNull(broker);
        Assert.Equal("Interactive Brokers", broker.Name);
        Assert.Equal("ib_logo", broker.Image);
        Assert.Equal("INTERACTIVE_BROKERS", broker.SupportedBroker);
    }

    [Fact]
    public void TestDataBuilders_CanCreateBrokerAccount()
    {
        // Arrange & Act
        var account = TestDataBuilders.CreateBrokerAccount()
            .WithInteractiveBrokers()
            .Build();
        
        // Assert
        Assert.NotNull(account);
        Assert.NotNull(account.Broker);
        Assert.Equal("Interactive Brokers", account.Broker.Name);
        Assert.Equal("U1234567", account.AccountNumber);
    }

    [Fact]
    public void TestDataBuilders_CanCreateCurrency()
    {
        // Arrange & Act
        var currency = TestDataBuilders.CreateCurrency()
            .AsUSD()
            .Build();
        
        // Assert
        Assert.NotNull(currency);
        Assert.Equal("US Dollar", currency.Title);
        Assert.Equal("USD", currency.Code);
        Assert.Equal("$", currency.Symbol);
    }

    [Fact]
    public void TestDataBuilders_CanCreateFinancialData()
    {
        // Arrange & Act
        var financialData = TestDataBuilders.CreateFinancialData()
            .AsProfitableScenario()
            .Build();
        
        // Assert
        Assert.NotNull(financialData);
        Assert.Equal(10000m, financialData.Invested);
        Assert.Equal(1500m, financialData.RealizedGains);
        Assert.Equal(15.0m, financialData.RealizedPercentage);
        Assert.Equal(800m, financialData.UnrealizedGains);
        Assert.Equal(45, financialData.MovementCounter);
    }

    [Fact]
    public void BinnaculumAssertionExtensions_AssertCurrencyFormat_Works()
    {
        // Arrange
        var amount = 1234.56m;
        var culture = "en-US";
        var expected = "$1,234.56";
        
        // Act & Assert
        amount.AssertCurrencyFormat(culture, expected);
    }

    [Fact]
    public void BinnaculumAssertionExtensions_AssertBinnaculumCurrencyFormat_Works()
    {
        // Arrange
        var amount = 1234.56m;
        var symbol = "$";
        var expected = "$1234.56";
        
        // Act & Assert
        amount.AssertBinnaculumCurrencyFormat(symbol, expected);
    }

    [Fact]
    public void BinnaculumAssertionExtensions_AssertSimplifiedFormat_Works()
    {
        // Arrange & Act & Assert
        0m.AssertSimplifiedFormat("0");
        100m.AssertSimplifiedFormat("100");
        0.5m.AssertSimplifiedFormat("0.5000");
        123.45m.AssertSimplifiedFormat("123.45");
    }

    [Fact]
    public void InvestmentTestData_ProfitableScenarios_Available()
    {
        // Arrange & Act
        var profitableScenarios = InvestmentTestData.GetProfitableScenarios().ToList();
        
        // Assert
        Assert.NotEmpty(profitableScenarios);
        Assert.True(profitableScenarios.All(s => s.RealizedGains > 0));
    }

    [Fact]
    public void InvestmentTestData_LossScenarios_Available()
    {
        // Arrange & Act
        var lossScenarios = InvestmentTestData.GetLossScenarios().ToList();
        
        // Assert
        Assert.NotEmpty(lossScenarios);
        Assert.True(lossScenarios.All(s => s.RealizedGains < 0));
    }

    [Fact]
    public void TestHelpers_CanCreateMockServiceProvider()
    {
        // Arrange & Act
        var serviceProvider = TestHelpers.CreateMockServiceProvider();
        
        // Assert
        Assert.NotNull(serviceProvider);
        var mockBrokerService = serviceProvider.GetService<TestHelpers.IMockBrokerService>();
        Assert.NotNull(mockBrokerService);
    }

    [Fact]
    public async Task TestHelpers_CanRunAsyncTest()
    {
        // Arrange
        var executed = false;
        
        // Act & Assert
        await TestHelpers.RunAsyncTest(async () =>
        {
            await Task.Delay(10);
            executed = true;
        });
        
        Assert.True(executed);
    }

    [Fact]
    public async Task TestHelpers_WaitForCondition_Works()
    {
        // Arrange
        var counter = 0;
        
        // Act & Assert
        await TestHelpers.WaitForCondition(
            () => ++counter >= 3,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(10));
        
        Assert.True(counter >= 3);
    }
}