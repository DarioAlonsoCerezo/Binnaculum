using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Binnaculum.UITest.Appium.Tests;

/// <summary>
/// Tests for the Appium server management functionality.
/// These tests verify server lifecycle management works independently of Appium driver issues.
/// </summary>
public class AppiumServerManagerTests
{
    private readonly ITestOutputHelper _output;

    public AppiumServerManagerTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ServerManager_InitialState_IsNotRunning()
    {
        // Arrange & Act
        using var serverManager = new AppiumServerManager();

        // Assert
        Assert.False(serverManager.IsRunning);
        Assert.Null(serverManager.ServerUrl);
    }

    [Fact]
    public async Task ServerManager_StartWithoutAppium_ReturnsFailure()
    {
        // This test demonstrates that the server management handles
        // the case where Appium is not installed gracefully
        
        // Arrange
        using var serverManager = new AppiumServerManager();

        // Act
        var started = await serverManager.StartServerAsync(TimeSpan.FromSeconds(5));

        // Assert - Should fail gracefully when Appium not installed
        Assert.False(started);
        Assert.Null(serverManager.ServerUrl);
    }

    [Fact]
    public async Task ServerManager_IsHealthyWhenNotRunning_ReturnsFalse()
    {
        // Arrange
        using var serverManager = new AppiumServerManager();

        // Act
        var healthy = await serverManager.IsHealthyAsync();

        // Assert
        Assert.False(healthy);
    }

    [Fact]
    public void ServerManager_StopWhenNotRunning_DoesNotThrow()
    {
        // Arrange
        using var serverManager = new AppiumServerManager();

        // Act & Assert - Should not throw
        serverManager.StopServer();
    }

    [Fact]
    public void AppiumServerOptions_FromEnvironment_ReadsEnvironmentVariables()
    {
        // Arrange
        Environment.SetEnvironmentVariable("APPIUM_HOST", "192.168.1.100");
        Environment.SetEnvironmentVariable("APPIUM_PORT", "5555");

        try
        {
            // Act
            var options = AppiumServerOptions.FromEnvironment();

            // Assert
            Assert.Equal("192.168.1.100", options.IPAddress);
            Assert.Equal(5555, options.Port);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("APPIUM_HOST", null);
            Environment.SetEnvironmentVariable("APPIUM_PORT", null);
        }
    }
    
    [Fact]
    public void AppiumServerOptions_Defaults_AreCorrect()
    {
        // Arrange & Act
        var options = new AppiumServerOptions();

        // Assert
        Assert.Equal("127.0.0.1", options.IPAddress);
        Assert.Null(options.Port); // Should use auto-detect
        Assert.Equal(TimeSpan.FromMinutes(2), options.StartupTimeout);
        Assert.Equal(LogLevel.Information, options.LogLevel);
        Assert.True(options.EnableCors);
        Assert.False(options.RelaxedSecurity);
    }
}