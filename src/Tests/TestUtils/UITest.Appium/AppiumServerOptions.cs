using Microsoft.Extensions.Logging;

namespace Binnaculum.UITest.Appium;

/// <summary>
/// Configuration options for Appium server management.
/// Can be extended to read from appsettings.json or environment variables.
/// </summary>
public class AppiumServerOptions
{
    public string IPAddress { get; set; } = "127.0.0.1";
    public int? Port { get; set; } = null; // null = use any free port
    public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public bool EnableCors { get; set; } = true;
    public bool RelaxedSecurity { get; set; } = false;

    /// <summary>
    /// Create options from environment variables or defaults.
    /// Useful for CI/CD pipeline configuration.
    /// </summary>
    public static AppiumServerOptions FromEnvironment()
    {
        var options = new AppiumServerOptions();
        
        if (int.TryParse(Environment.GetEnvironmentVariable("APPIUM_PORT"), out var port))
            options.Port = port;
            
        var host = Environment.GetEnvironmentVariable("APPIUM_HOST");
        if (!string.IsNullOrEmpty(host))
            options.IPAddress = host;
            
        return options;
    }
}