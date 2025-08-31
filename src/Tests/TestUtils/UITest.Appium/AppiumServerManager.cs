using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace Binnaculum.UITest.Appium;

/// <summary>
/// Manages Appium server lifecycle for automated testing using process-based approach.
/// Handles starting, stopping, and health checking of Appium server instances.
/// </summary>
public class AppiumServerManager : IDisposable
{
    private readonly ILogger<AppiumServerManager> _logger;
    private readonly AppiumServerOptions _options;
    private Process? _appiumProcess;
    private bool _disposed = false;
    private readonly string _logFilePath;
    
    public AppiumServerManager(ILogger<AppiumServerManager>? logger = null, AppiumServerOptions? options = null)
    {
        _logger = logger ?? CreateDefaultLogger();
        _options = options ?? new AppiumServerOptions();
        _logFilePath = Path.Combine(Path.GetTempPath(), $"appium-{Guid.NewGuid():N}.log");
    }

    /// <summary>
    /// Gets the server URL if the service is running, null otherwise.
    /// </summary>
    public Uri? ServerUrl { get; private set; }

    /// <summary>
    /// Indicates whether the Appium server is currently running.
    /// </summary>
    public bool IsRunning => _appiumProcess != null && !_appiumProcess.HasExited;

    /// <summary>
    /// Start the Appium server with configuration options.
    /// </summary>
    public async Task<bool> StartServerAsync(TimeSpan? timeout = null)
    {
        if (IsRunning)
        {
            _logger.LogInformation("Appium server is already running at {Url}", ServerUrl);
            return true;
        }

        try
        {
            _logger.LogInformation("Starting Appium server...");
            
            var actualTimeout = timeout ?? _options.StartupTimeout;
            var port = _options.Port ?? GetAvailablePort();
            var url = $"http://{_options.IPAddress}:{port}";
            
            // Build command arguments
            var args = new StringBuilder();
            args.Append($"--address {_options.IPAddress} ");
            args.Append($"--port {port} ");
            args.Append("--session-override ");
            args.Append($"--log-level {_options.LogLevel.ToString().ToLower()} ");
            args.Append($"--log {_logFilePath} ");
            
            if (_options.EnableCors)
                args.Append("--allow-cors ");
                
            if (_options.RelaxedSecurity)
                args.Append("--relaxed-security ");
                
            // Platform-specific arguments
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                args.Append("--allow-insecure chromedriver_autodownload ");

            _appiumProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "appium",
                    Arguments = args.ToString().Trim(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            _appiumProcess.OutputDataReceived += OnOutputDataReceived;
            _appiumProcess.ErrorDataReceived += OnErrorDataReceived;

            var started = _appiumProcess.Start();
            if (started)
            {
                _appiumProcess.BeginOutputReadLine();
                _appiumProcess.BeginErrorReadLine();
                
                // Wait for server to be ready
                var isReady = await WaitForServerReady(url, actualTimeout);
                if (isReady)
                {
                    ServerUrl = new Uri(url);
                    _logger.LogInformation("Appium server started successfully at {Url}", ServerUrl);
                    return true;
                }
                else
                {
                    _logger.LogError("Appium server started but did not become ready within timeout");
                    StopServer();
                    return false;
                }
            }
            else
            {
                _logger.LogError("Failed to start Appium server process");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Appium server");
            StopServer();
            return false;
        }
    }

    /// <summary>
    /// Stop the Appium server if it's running.
    /// </summary>
    public void StopServer()
    {
        if (_appiumProcess != null)
        {
            try
            {
                _logger.LogInformation("Stopping Appium server...");
                
                if (!_appiumProcess.HasExited)
                {
                    _appiumProcess.Kill();
                    _appiumProcess.WaitForExit(5000); // Wait up to 5 seconds
                }
                
                _appiumProcess.Dispose();
                _logger.LogInformation("Appium server stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping Appium server");
            }
            finally
            {
                _appiumProcess = null;
                ServerUrl = null;
            }
        }

        // Clean up log file
        try
        {
            if (File.Exists(_logFilePath))
                File.Delete(_logFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to delete Appium log file: {LogFile}", _logFilePath);
        }
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

    private async Task<bool> WaitForServerReady(string url, TimeSpan timeout)
    {
        var endTime = DateTime.UtcNow.Add(timeout);
        
        while (DateTime.UtcNow < endTime)
        {
            if (_appiumProcess?.HasExited == true)
                return false;
                
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var response = await client.GetAsync($"{url}/status");
                if (response.IsSuccessStatusCode)
                    return true;
            }
            catch
            {
                // Server not ready yet
            }
            
            await Task.Delay(1000); // Wait 1 second between checks
        }
        
        return false;
    }

    private static int GetAvailablePort()
    {
        // Find an available port starting from 4723 (default Appium port)
        for (int port = 4723; port < 4800; port++)
        {
            try
            {
                using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
                listener.Start();
                listener.Stop();
                return port;
            }
            catch
            {
                // Port is in use, try next
            }
        }
        
        // Fallback to random port
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, 0);
        listener.Start();
        var availablePort = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return availablePort;
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            _logger.LogDebug("Appium stdout: {Output}", e.Data);
        }
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            // Don't log as error since Appium writes normal logs to stderr
            _logger.LogDebug("Appium stderr: {Output}", e.Data);
        }
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