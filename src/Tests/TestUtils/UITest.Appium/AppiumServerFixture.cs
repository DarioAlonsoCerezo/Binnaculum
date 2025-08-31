using Microsoft.Extensions.Logging;
using Xunit;

namespace Binnaculum.UITest.Appium;

/// <summary>
/// xUnit collection fixture for managing Appium server lifecycle across test runs.
/// Ensures server starts once per test collection and is properly disposed.
/// </summary>
public class AppiumServerFixture : IAsyncLifetime
{
    private readonly ILogger<AppiumServerFixture> _logger;
    private AppiumServerManager? _serverManager;

    public AppiumServerFixture()
    {
        using var factory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        _logger = factory.CreateLogger<AppiumServerFixture>();
    }

    /// <summary>
    /// Gets the server URL if available, null otherwise.
    /// </summary>
    public Uri? ServerUrl => _serverManager?.ServerUrl;

    /// <summary>
    /// Indicates whether the Appium server is running and healthy.
    /// </summary>
    public bool IsServerAvailable => _serverManager?.IsRunning == true;

    /// <summary>
    /// Initialize the fixture - start Appium server.
    /// </summary>
    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing Appium server fixture...");
        
        _serverManager = new AppiumServerManager(_logger);
        
        var started = await _serverManager.StartServerAsync(TimeSpan.FromMinutes(3));
        if (!started)
        {
            _logger.LogWarning("Failed to start Appium server - tests will be skipped");
            return;
        }

        // Wait a moment and verify server health
        await Task.Delay(TimeSpan.FromSeconds(2));
        
        var healthy = await _serverManager.IsHealthyAsync();
        if (!healthy)
        {
            _logger.LogWarning("Appium server started but health check failed - tests may be unstable");
        }
        else
        {
            _logger.LogInformation("Appium server is running and healthy at {Url}", ServerUrl);
        }
    }

    /// <summary>
    /// Cleanup the fixture - stop Appium server.
    /// </summary>
    public async Task DisposeAsync()
    {
        _logger.LogInformation("Disposing Appium server fixture...");
        
        if (_serverManager != null)
        {
            _serverManager.Dispose();
            _serverManager = null;
        }
        
        _logger.LogInformation("Appium server fixture disposed");
        await Task.CompletedTask;
    }
}

/// <summary>
/// xUnit collection definition for Appium server tests.
/// All test classes that use [Collection("AppiumServer")] will share the same server instance.
/// </summary>
[CollectionDefinition("AppiumServer")]
public class AppiumServerCollection : ICollectionFixture<AppiumServerFixture>
{
    // This class has no code, it exists only to define the collection
}