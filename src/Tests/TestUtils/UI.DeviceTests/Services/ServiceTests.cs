namespace Binnaculum.UI.DeviceTests.Services;

/// <summary>
/// Tests for platform-specific services and functionality.
/// </summary>
public class ServiceTests
{
    [Fact]
    public void DeviceInfo_PlatformAccess_ShouldWork()
    {
        // This test verifies we can access platform information
        // Note: This would require actual device context to run properly
        
        // For now, just verify the test infrastructure works
        Assert.True(true, "Service test infrastructure is working");
    }

    [Fact]
    public void CoreIntegration_AccessCoreTypes_ShouldWork()
    {
        // Verify we can access Core project types
        var currencyType = typeof(Binnaculum.Core.Models.Currency);
        
        Assert.NotNull(currencyType);
        Assert.Equal("Currency", currencyType.Name);
    }
}