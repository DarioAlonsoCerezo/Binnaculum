namespace Binnaculum.UITest.Core;

/// <summary>
/// Interface for managing test server context and environment.
/// Provides server-side test infrastructure management.
/// </summary>
public interface IServerContext
{
    /// <summary>
    /// Server endpoint URL if applicable.
    /// </summary>
    Uri? ServerEndpoint { get; }

    /// <summary>
    /// Server capabilities and supported features.
    /// </summary>
    IReadOnlyDictionary<string, object> ServerCapabilities { get; }

    /// <summary>
    /// Start the test server if needed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop the test server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the server is running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Server health status.
    /// </summary>
    ServerHealthStatus HealthStatus { get; }
}

/// <summary>
/// Interface for managing client-side test context.
/// Provides client-side test execution environment.
/// </summary>
public interface IUIClientContext
{
    /// <summary>
    /// Client configuration.
    /// </summary>
    IConfig Config { get; }

    /// <summary>
    /// Target device information.
    /// </summary>
    TestDevice Device { get; }

    /// <summary>
    /// Session identifier for this test context.
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// Initialize the client context.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleanup the client context.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CleanupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create an app instance for testing.
    /// </summary>
    /// <returns>App instance</returns>
    Task<IApp> CreateAppAsync();
}

/// <summary>
/// Server health status enumeration.
/// </summary>
public enum ServerHealthStatus
{
    Unknown,
    Healthy,
    Degraded,
    Unhealthy,
    Stopped
}