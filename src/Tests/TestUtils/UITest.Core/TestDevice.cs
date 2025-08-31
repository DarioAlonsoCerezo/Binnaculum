using System.Runtime.InteropServices;

namespace Binnaculum.UITest.Core;

/// <summary>
/// Represents a test device with its capabilities and platform information.
/// Provides platform-specific device detection and management.
/// </summary>
public class TestDevice
{
    public TestDevice(
        string deviceId,
        TestPlatform platform,
        string name,
        string? version = null,
        Dictionary<string, object>? capabilities = null)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        Platform = platform;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version;
        Capabilities = capabilities ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Unique device identifier.
    /// </summary>
    public string DeviceId { get; }

    /// <summary>
    /// Device platform.
    /// </summary>
    public TestPlatform Platform { get; }

    /// <summary>
    /// Human-readable device name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Device OS version.
    /// </summary>
    public string? Version { get; }

    /// <summary>
    /// Device-specific capabilities and features.
    /// </summary>
    public Dictionary<string, object> Capabilities { get; }

    /// <summary>
    /// Whether this device supports the specified capability.
    /// </summary>
    /// <param name="capability">Capability name</param>
    /// <returns>True if supported</returns>
    public bool SupportsCapability(string capability)
    {
        return Capabilities.ContainsKey(capability) && 
               Capabilities[capability] is bool supported && 
               supported;
    }

    /// <summary>
    /// Get the current host device for testing.
    /// </summary>
    /// <returns>Host device information</returns>
    public static TestDevice GetHostDevice()
    {
        var platform = DetectHostPlatform();
        var deviceId = Environment.MachineName ?? "localhost";
        var name = GetHostDeviceName();
        var version = Environment.OSVersion.VersionString;

        var capabilities = new Dictionary<string, object>
        {
            ["supportsScreenshots"] = true,
            ["supportsLogs"] = platform == TestPlatform.Android || platform == TestPlatform.MacCatalyst,
            ["supportsKeyboard"] = true,
            ["supportsTouch"] = platform != TestPlatform.Windows,
            ["supportsMouse"] = platform == TestPlatform.Windows || platform == TestPlatform.MacCatalyst
        };

        return new TestDevice(deviceId, platform, name, version, capabilities);
    }

    /// <summary>
    /// Detect the current host platform.
    /// </summary>
    /// <returns>Detected platform</returns>
    public static TestPlatform DetectHostPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return TestPlatform.Windows;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return TestPlatform.MacCatalyst;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return TestPlatform.Android; // Assume Android emulator/device on Linux
        
        return TestPlatform.Unknown;
    }

    private static string GetHostDeviceName()
    {
        try
        {
            return Environment.MachineName ?? "Unknown Device";
        }
        catch
        {
            return "Unknown Device";
        }
    }

    public override string ToString()
    {
        return $"{Name} ({Platform}) - {DeviceId}";
    }
}

/// <summary>
/// Supported test platforms for Binnaculum.
/// </summary>
public enum TestPlatform
{
    Unknown,
    Android,
    iOS,
    MacCatalyst,
    Windows
}