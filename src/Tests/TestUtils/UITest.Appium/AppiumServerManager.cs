using OpenQA.Selenium.Appium.Service;
using OpenQA.Selenium.Appium.Service.Options;
using Microsoft.Extensions.Logging;

namespace Binnaculum.UITest.Appium;

/// <summary>
/// Manages Appium server lifecycle for automated testing.
/// Handles starting, stopping, and health checking of Appium server instances.
/// </summary>
public class AppiumServerManager : IDisposable
{
    private readonly ILogger<AppiumServerManager> _logger;
    private AppiumLocalService? _service;
    private bool _disposed = false;

    public AppiumServerManager(ILogger<AppiumServerManager>? logger = null)
    {
        _logger = logger ?? CreateDefaultLogger();
    }

    /// <summary>
    /// Gets the server URL if the service is running, null otherwise.
    /// </summary>
    public Uri? ServerUrl => _service?.ServiceUrl;

    /// <summary>
    /// Indicates whether the Appium server is currently running.
    /// </summary>
    public bool IsRunning => _service?.IsRunning == true;

    /// <summary>
    /// Start the Appium server with default configuration.
    /// </summary>
    public async Task<bool> StartServerAsync(TimeSpan? timeout = null)
    {
        if (_service?.IsRunning == true)
        {
            _logger.LogInformation("Appium server is already running at {Url}", _service.ServiceUrl);
            return true;
        }

        try
        {
            _logger.LogInformation("Starting Appium server...");
            
            var builder = new AppiumServiceBuilder()
                .WithIPAddress("127.0.0.1")
                .UsingAnyFreePort()
                .WithTimeout(timeout ?? TimeSpan.FromMinutes(2))
                .WithArgument(GeneralOptionList.SessionOverride)
                .WithArgument(GeneralOptionList.LogLevel, "info");

            // Add platform-specific arguments based on current OS
            ConfigurePlatformSpecificOptions(builder);

            _service = builder.Build();
            _service.Start();

            if (_service.IsRunning)
            {
                _logger.LogInformation("Appium server started successfully at {Url}", _service.ServiceUrl);
                return true;
            }
            else
            {
                _logger.LogError("Failed to start Appium server - service reports not running");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Appium server");
            return false;
        }
    }

    /// <summary>
    /// Stop the Appium server if it's running.
    /// </summary>
    public void StopServer()
    {
        if (_service?.IsRunning == true)
        {
            _logger.LogInformation("Stopping Appium server...");
            try
            {
                _service.Dispose();
                _logger.LogInformation("Appium server stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping Appium server");
            }
        }
        
        _service = null;
    }

    /// <summary>
    /// Check if the Appium server is responding to requests.
    /// </summary>
    public async Task<bool> IsHealthyAsync()
    {
        if (!IsRunning || ServerUrl == null)
            return false;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await client.GetAsync($"{ServerUrl}status");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void ConfigurePlatformSpecificOptions(AppiumServiceBuilder builder)
    {
        // Add platform-specific drivers and capabilities
        if (OperatingSystem.IsWindows())
        {
            builder.WithArgument(GeneralOptionList.AllowCors);
        }
        
        if (OperatingSystem.IsMacOS())
        {
            // macOS can support iOS and MacCatalyst
            builder.WithArgument(GeneralOptionList.RelaxedSecurityEnabled);
        }
        
        // Android is supported on all platforms
        builder.WithArgument(GeneralOptionList.AllowInsecure, "chromedriver_autodownload");
    }

    private static ILogger<AppiumServerManager> CreateDefaultLogger()
    {
        using var factory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        return factory.CreateLogger<AppiumServerManager>();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopServer();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}