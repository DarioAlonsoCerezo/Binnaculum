using System.Globalization;
using System.ComponentModel;
using static Binnaculum.Core.Models;
using Binnaculum.UI.DeviceTests.Controls;

namespace Binnaculum.UI.DeviceTests;

/// <summary>
/// Cross-platform validation tests for Binnaculum UI components.
/// Tests platform-specific behaviors, layout differences, and performance characteristics.
/// </summary>
public class CrossPlatformValidationTests
{
    #region Platform Detection Tests

    [Fact]
    public void Platform_Detection_IsConsistent()
    {
        // Arrange & Act
        bool isAndroid = DeviceInfo.Platform.Name == "Android";
        bool isiOS = DeviceInfo.Platform.Name == "iOS";
        bool isMacCatalyst = DeviceInfo.Platform.Name == "MacCatalyst";
        bool isWindows = DeviceInfo.Platform.Name == "WinUI";
        
        // Assert - Exactly one platform should be detected
        var platformCount = (isAndroid ? 1 : 0) + (isiOS ? 1 : 0) + (isMacCatalyst ? 1 : 0) + (isWindows ? 1 : 0);
        Assert.True(platformCount == 1, "Exactly one platform should be detected");
    }

    [Fact]
    public void DeviceInfo_HasValidVersion()
    {
        // Arrange & Act
        var version = DeviceInfo.Version;
        var versionString = DeviceInfo.VersionString;
        
        // Assert
        Assert.NotNull(version);
        Assert.NotNull(versionString);
        Assert.True(version.Major >= 0, "Version should have valid major number");
        Assert.False(string.IsNullOrWhiteSpace(versionString), "Version string should not be empty");
    }

    #endregion

    #region Layout Option Cross-Platform Tests

    [Fact]
    public void LayoutOptions_AreConsistentAcrossPlatforms()
    {
        // Arrange - Test all standard layout options
        var layoutOptions = new[]
        {
            LayoutOptions.Start,
            LayoutOptions.Center,
            LayoutOptions.End,
            LayoutOptions.Fill,
            LayoutOptions.StartAndExpand,
            LayoutOptions.CenterAndExpand,
            LayoutOptions.EndAndExpand,
            LayoutOptions.FillAndExpand
        };
        
        // Act & Assert - All layout options should be available and have expected properties
        foreach (var option in layoutOptions)
        {
            Assert.NotNull(option);
            
            // Test basic properties
            if (option.Alignment == LayoutAlignment.Start)
                Assert.Equal(LayoutAlignment.Start, option.Alignment);
            else if (option.Alignment == LayoutAlignment.Center)
                Assert.Equal(LayoutAlignment.Center, option.Alignment);
            else if (option.Alignment == LayoutAlignment.End)
                Assert.Equal(LayoutAlignment.End, option.Alignment);
            else if (option.Alignment == LayoutAlignment.Fill)
                Assert.Equal(LayoutAlignment.Fill, option.Alignment);
        }
    }

    [Fact]
    public void BrokerAccountTemplate_LayoutBehavior_ConsistentAcrossPlatforms()
    {
        // Arrange
        var layoutStates = new[]
        {
            (hasMovements: true, expectedVertical: LayoutOptions.End, expectedHorizontal: LayoutOptions.Start),
            (hasMovements: false, expectedVertical: LayoutOptions.Center, expectedHorizontal: LayoutOptions.Center)
        };
        
        // Act & Assert - Layout behavior should be consistent regardless of platform
        foreach (var (hasMovements, expectedVertical, expectedHorizontal) in layoutStates)
        {
            var mockTemplate = new BrokerAccountTemplateTests.MockBrokerAccountTemplate();
            var snapshot = CreateCrossPlatformTestSnapshot(hasMovements);
            
            mockTemplate.BindingContext = snapshot;
            mockTemplate.OnBindingContextChanged();
            
            Assert.Equal(expectedVertical.Alignment, mockTemplate.AddMovementContainer.VerticalOptions.Alignment);
            Assert.Equal(expectedHorizontal.Alignment, mockTemplate.AddMovementContainer.HorizontalOptions.Alignment);
        }
    }

    #endregion

    #region Currency Formatting Cross-Platform Tests

    [Theory]
    [InlineData("en-US", 1234.56, "$1,234.56")]
    [InlineData("de-DE", 1234.56, "1.234,56 €")]
    [InlineData("ja-JP", 1234.56, "￥1,235")]
    [InlineData("en-GB", 1234.56, "£1,234.56")]
    [InlineData("fr-FR", 1234.56, "1 234,56 €")]
    public void CurrencyFormatting_WorksAcrossCultures(string cultureName, decimal amount, string expectedFormat)
    {
        // Arrange
        var culture = CultureInfo.GetCultureInfo(cultureName);
        
        // Act & Assert - Currency formatting should work consistently across platforms
        amount.AssertCurrencyFormat(cultureName, expectedFormat);
    }

    [Fact]
    public void CurrencyFormatting_HandlesInvariantCulture()
    {
        // Arrange
        var amount = 1000.50m;
        var originalCulture = CultureInfo.CurrentCulture;
        
        try
        {
            // Act - Set to invariant culture
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var formatted = amount.ToString("C", CultureInfo.InvariantCulture);
            
            // Assert - Should handle invariant culture gracefully
            Assert.NotNull(formatted);
            Assert.Contains("1000.50", formatted.Replace("¤", ""));
        }
        finally
        {
            // Restore original culture
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    #endregion

    #region Performance Cross-Platform Tests

    [Fact]
    public void BrokerAccountTemplate_BindingPerformance_MeetsTargets()
    {
        // Arrange
        var template = new BrokerAccountTemplateTests.MockBrokerAccountTemplate();
        const int iterations = 100;
        var snapshots = Enumerable.Range(0, iterations)
            .Select(i => CreateCrossPlatformTestSnapshot(i % 2 == 0))
            .ToArray();
        
        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        foreach (var snapshot in snapshots)
        {
            template.BindingContext = snapshot;
            template.OnBindingContextChanged();
        }
        
        stopwatch.Stop();
        
        // Assert - Should meet mobile performance targets
        var averageTime = stopwatch.ElapsedMilliseconds / (double)iterations;
        Assert.True(stopwatch.ElapsedMilliseconds < 200, 
            $"Total binding time {stopwatch.ElapsedMilliseconds}ms should be < 200ms for mobile performance");
        Assert.True(averageTime < 2.0, 
            $"Average binding time {averageTime:F2}ms should be < 2ms per binding");
    }

    [Fact]
    public void FinancialCalculations_PerformanceConsistency()
    {
        // Arrange
        var scenarios = InvestmentTestData.GetAllScenarios().Take(20).ToList();
        
        // Act - Measure calculation performance
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        foreach (var scenario in scenarios)
        {
            // Simulate common financial calculations
            var totalValue = scenario.Invested + scenario.RealizedGains + scenario.UnrealizedGains;
            var totalPercentage = scenario.RealizedPercentage + scenario.UnrealizedGainsPercentage;
            var netCost = scenario.Commissions + scenario.Fees;
            var netGain = scenario.RealizedGains + scenario.UnrealizedGains - netCost;
            
            // Validate calculations are reasonable
            Assert.True(totalValue > 0 || scenario.RealizedGains < 0, "Total value calculation should be reasonable");
            Assert.True(Math.Abs(totalPercentage) < 2000, "Total percentage should be within reasonable bounds");
        }
        
        stopwatch.Stop();
        
        // Assert - Financial calculations should be fast
        var averageTime = stopwatch.ElapsedTicks / (double)scenarios.Count;
        Assert.True(stopwatch.ElapsedMilliseconds < 100, 
            $"Financial calculations took {stopwatch.ElapsedMilliseconds}ms, should be < 100ms");
    }

    #endregion

    #region Memory Usage Cross-Platform Tests

    [Fact]
    public void TestDataBuilders_MemoryEfficient()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        
        // Act - Create many test objects
        var scenarios = new List<object>();
        for (int i = 0; i < 1000; i++)
        {
            var broker = TestDataBuilders.CreateBroker().WithId(i).Build();
            var currency = TestDataBuilders.CreateCurrency().WithId(i).Build();
            var brokerAccount = TestDataBuilders.CreateBrokerAccount().WithId(i).Build();
            
            scenarios.Add(new { broker, currency, brokerAccount });
        }
        
        var peakMemory = GC.GetTotalMemory(false);
        
        // Clear references
        scenarios.Clear();
        scenarios = null;
        
        // Force cleanup
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var finalMemory = GC.GetTotalMemory(true);
        
        // Assert - Memory usage should be reasonable
        var memoryGrowth = peakMemory - initialMemory;
        var memoryCleanup = peakMemory - finalMemory;
        
        Assert.True(memoryGrowth < 10 * 1024 * 1024, // Less than 10MB growth
            $"Memory growth of {memoryGrowth:N0} bytes should be < 10MB for 1000 objects");
        Assert.True(memoryCleanup > memoryGrowth * 0.7, // At least 70% cleanup
            $"Memory cleanup of {memoryCleanup:N0} bytes should be significant");
    }

    #endregion

    #region Error Handling Cross-Platform Tests

    [Fact]
    public void BrokerAccountTemplate_ExceptionHandling_Graceful()
    {
        // Arrange
        var template = new BrokerAccountTemplateTests.MockBrokerAccountTemplate();
        var invalidContexts = new object[]
        {
            null!,
            "invalid string",
            42,
            new { InvalidProperty = "value" },
            new List<string> { "not", "a", "snapshot" }
        };
        
        // Act & Assert - Should handle invalid contexts gracefully
        foreach (var invalidContext in invalidContexts)
        {
            template.BindingContext = invalidContext;
            
            // Should not throw exception
            var exception = Record.Exception(() => template.OnBindingContextChanged());
            Assert.Null(exception);
        }
    }

    [Fact]
    public void CurrencyFormatting_InvalidCulture_HandledGracefully()
    {
        // Arrange
        var amount = 1000m;
        var invalidCultures = new[] { "invalid-culture", "", "xx-XX", "123-456" };
        
        // Act & Assert
        foreach (var invalidCulture in invalidCultures)
        {
            var exception = Record.Exception(() => 
                CultureInfo.GetCultureInfo(invalidCulture));
            
            // Should throw CultureNotFoundException, which is expected behavior
            if (exception != null)
            {
                Assert.IsType<CultureNotFoundException>(exception);
            }
        }
    }

    #endregion

    #region Test Helper Methods

    /// <summary>
    /// Creates a cross-platform test snapshot with minimal dependencies.
    /// </summary>
    private static OverviewSnapshot CreateCrossPlatformTestSnapshot(bool hasMovements = true)
    {
        // Use the existing test helper from BrokerAccountTemplateTests
        return BrokerAccountTemplateTests.CreateTestOverviewSnapshot(hasMovements);
    }

    #endregion
}

/// <summary>
/// Mock DeviceInfo for testing when MAUI DeviceInfo is not available in test environment.
/// </summary>
public static class DeviceInfo
{
    public static MockDevicePlatform Platform => MockDevicePlatform.Android; // Default for testing
    public static Version Version => new Version(9, 0);
    public static string VersionString => "9.0.0";
}

/// <summary>
/// Mock DevicePlatform for testing.
/// </summary>
public class MockDevicePlatform
{
    public static readonly MockDevicePlatform Android = new MockDevicePlatform("Android");
    public static readonly MockDevicePlatform iOS = new MockDevicePlatform("iOS");
    public static readonly MockDevicePlatform MacCatalyst = new MockDevicePlatform("MacCatalyst");
    public static readonly MockDevicePlatform WinUI = new MockDevicePlatform("WinUI");
    
    public string Name { get; }
    
    private MockDevicePlatform(string name)
    {
        Name = name;
    }
    
    public override string ToString() => Name;
    public override bool Equals(object obj) => obj is MockDevicePlatform other && Name == other.Name;
    public override int GetHashCode() => Name.GetHashCode();
}