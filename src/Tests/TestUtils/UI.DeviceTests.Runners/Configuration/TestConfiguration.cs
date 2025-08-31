namespace Binnaculum.UI.DeviceTests.Runners.Configuration;

/// <summary>
/// Configuration and setup utilities for device test execution.
/// </summary>
public class TestConfiguration
{
    [Fact]
    public void TestConfiguration_DefaultSettings_ShouldBeValid()
    {
        // Arrange
        var config = new DeviceTestConfig();
        
        // Act & Assert
        Assert.NotNull(config);
        Assert.True(config.IsValid, "Default test configuration should be valid");
    }

    [Fact]
    public void TestConfiguration_CanSetTestTimeout()
    {
        // Arrange
        var config = new DeviceTestConfig();
        var timeout = TimeSpan.FromMinutes(5);
        
        // Act
        config.TestTimeout = timeout;
        
        // Assert
        Assert.Equal(timeout, config.TestTimeout);
    }
}

/// <summary>
/// Sample configuration class for device testing.
/// </summary>
public class DeviceTestConfig
{
    public TimeSpan TestTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public bool IsValid => TestTimeout > TimeSpan.Zero;
}