namespace Binnaculum.UITest.Core;

/// <summary>
/// Configuration interface for UITest framework settings and capabilities.
/// Based on Microsoft MAUI UITest.Core IConfig pattern.
/// </summary>
public interface IConfig
{
    /// <summary>
    /// Gets the platform this config is targeting.
    /// </summary>
    string Platform { get; }

    /// <summary>
    /// Gets the application package identifier.
    /// </summary>
    string? AppPackage { get; }

    /// <summary>
    /// Gets the path to the application file.
    /// </summary>
    string? AppPath { get; }

    /// <summary>
    /// Gets the device identifier for testing.
    /// </summary>
    string? DeviceId { get; }

    /// <summary>
    /// Gets additional capabilities for the driver.
    /// </summary>
    Dictionary<string, object> Capabilities { get; }

    /// <summary>
    /// Gets the timeout for element waiting.
    /// </summary>
    TimeSpan DefaultTimeout { get; }

    /// <summary>
    /// Gets whether to reset app state between tests.
    /// </summary>
    bool ResetApp { get; }
}