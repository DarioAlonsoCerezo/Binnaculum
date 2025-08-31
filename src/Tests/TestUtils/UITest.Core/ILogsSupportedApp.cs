namespace Binnaculum.UITest.Core;

/// <summary>
/// Interface for applications that support log access capabilities.
/// Provides access to application logs for debugging and testing purposes.
/// </summary>
public interface ILogsSupportedApp
{
    /// <summary>
    /// Get application logs of the specified type.
    /// </summary>
    /// <param name="logType">Type of logs to retrieve (e.g., "logcat" for Android)</param>
    /// <returns>Collection of log entries</returns>
    IReadOnlyCollection<LogEntry> GetLogs(string logType);

    /// <summary>
    /// Get all available log types for this application.
    /// </summary>
    /// <returns>Available log types</returns>
    IReadOnlyCollection<string> GetAvailableLogTypes();
}

/// <summary>
/// Represents a single log entry from the application.
/// </summary>
public record LogEntry(
    DateTime Timestamp,
    LogLevel Level,
    string Source,
    string Message);

/// <summary>
/// Log levels for application logging.
/// </summary>
public enum LogLevel
{
    All = 0,
    Debug = 1,
    Info = 2,
    Warning = 3,
    Error = 4,
    Severe = 5,
    Off = 6
}