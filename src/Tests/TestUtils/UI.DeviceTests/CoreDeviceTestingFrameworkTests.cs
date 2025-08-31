namespace Binnaculum.UI.DeviceTests;

/// <summary>
/// Simple working tests for the core Binnaculum device testing framework functionality.
/// These tests validate basic currency formatting, percentage calculations, and memory leak detection.
/// </summary>
public class CoreDeviceTestingFrameworkTests
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
    public void BinnaculumCurrencyFormat_Dollar_FormatsCorrectly()
    {
        // Arrange
        var amount = 1234.56m;
        var symbol = "$";
        var expected = "$1234.56";
        
        // Act & Assert
        amount.AssertBinnaculumCurrencyFormat(symbol, expected);
    }

    [Fact]
    public void BinnaculumCurrencyFormat_Euro_FormatsCorrectly()
    {
        // Arrange
        var amount = 1234.56m;
        var symbol = "€";
        var expected = "€1234.56";
        
        // Act & Assert
        amount.AssertBinnaculumCurrencyFormat(symbol, expected);
    }

    [Fact]
    public void SimplifiedFormat_WholeNumber_FormatsCorrectly()
    {
        // Arrange & Act & Assert
        100m.AssertSimplifiedFormat("100");
    }

    [Fact]
    public void SimplifiedFormat_Zero_FormatsCorrectly()
    {
        // Arrange & Act & Assert
        0m.AssertSimplifiedFormat("0");
    }

    [Fact]
    public void SimplifiedFormat_SmallDecimal_FormatsCorrectly()
    {
        // Arrange & Act & Assert
        0.1234m.AssertSimplifiedFormat("0.1234");
    }

    [Fact]
    public void SimplifiedFormat_RegularDecimal_FormatsCorrectly()
    {
        // Arrange & Act & Assert
        123.45m.AssertSimplifiedFormat("123.45");
    }

    [Fact]
    public async Task ObservableMemoryLeak_SimpleObservable_DoesNotLeak()
    {
        // Arrange & Act & Assert
        await BinnaculumAssertionExtensions.AssertObservableMemoryLeak(
            () => System.Reactive.Linq.Observable.Return(42),
            TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task TestHelpers_RunAsyncTest_ExecutesCorrectly()
    {
        // Arrange
        var executed = false;
        
        // Act & Assert
        await TestHelpers.RunAsyncTest(async () =>
        {
            await Task.Delay(10);
            executed = true;
        }, timeout: TimeSpan.FromSeconds(5), testName: "Test execution");
        
        Assert.True(executed);
    }

    [Fact]
    public async Task TestHelpers_WaitForCondition_WaitsCorrectly()
    {
        // Arrange
        var counter = 0;
        
        // Act & Assert - condition becomes true after 3 increments
        await TestHelpers.WaitForCondition(
            () => ++counter >= 3,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(10),
            "Counter reaches 3");
        
        Assert.True(counter >= 3);
    }

    [Fact]
    public void TestHelpers_CreateTestSubject_CreatesSubject()
    {
        // Arrange & Act
        var subject = TestHelpers.CreateTestSubject<int>();
        
        // Assert
        Assert.NotNull(subject);
    }

    [Fact]
    public async Task TestHelpers_AssertObservableSequence_ValidatesSequence()
    {
        // Arrange
        var subject = TestHelpers.CreateTestSubject<int>();
        var expectedValues = new[] { 1, 2, 3 };
        
        // Act - emit values on background thread
        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            subject.OnNext(1);
            await Task.Delay(50);
            subject.OnNext(2);
            await Task.Delay(50);
            subject.OnNext(3);
            await Task.Delay(50);
            subject.OnCompleted();
        });
        
        // Assert
        await TestHelpers.AssertObservableSequence(subject, expectedValues, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void InvestmentTestData_AllScenarios_AreAvailable()
    {
        // Arrange & Act
        var allScenarios = InvestmentTestData.GetAllScenarios().ToList();
        
        // Assert
        Assert.NotEmpty(allScenarios);
        Assert.True(allScenarios.Count > 10); // Should have many scenarios
    }

    [Fact]
    public void InvestmentTestData_ProfitableScenarios_HavePositiveGains()
    {
        // Arrange & Act
        var profitableScenarios = InvestmentTestData.GetProfitableScenarios().ToList();
        
        // Assert
        Assert.NotEmpty(profitableScenarios);
        Assert.True(profitableScenarios.All(s => s.RealizedGains > 0));
    }

    [Fact]
    public void InvestmentTestData_LossScenarios_HaveNegativeGains()
    {
        // Arrange & Act
        var lossScenarios = InvestmentTestData.GetLossScenarios().ToList();
        
        // Assert
        Assert.NotEmpty(lossScenarios);
        Assert.True(lossScenarios.All(s => s.RealizedGains < 0));
    }

    [Fact]
    public void InvestmentTestData_HighActivityScenarios_HaveManySenarios()
    {
        // Arrange & Act
        var highActivityScenarios = InvestmentTestData.GetHighActivityScenarios().ToList();
        
        // Assert
        Assert.NotEmpty(highActivityScenarios);
        Assert.True(highActivityScenarios.All(s => s.MovementCounter > 100));
    }

    [Fact]
    public void InvestmentTestData_DividendScenarios_HaveDividends()
    {
        // Arrange & Act
        var dividendScenarios = InvestmentTestData.GetDividendScenarios().ToList();
        
        // Assert
        Assert.NotEmpty(dividendScenarios);
        Assert.True(dividendScenarios.All(s => s.DividendsReceived > 0));
    }

    [Fact]
    public void InvestmentTestData_OptionsScenarios_HaveOptionsIncome()
    {
        // Arrange & Act
        var optionsScenarios = InvestmentTestData.GetOptionsScenarios().ToList();
        
        // Assert
        Assert.NotEmpty(optionsScenarios);
        Assert.True(optionsScenarios.All(s => s.OptionsIncome > 0));
    }
}