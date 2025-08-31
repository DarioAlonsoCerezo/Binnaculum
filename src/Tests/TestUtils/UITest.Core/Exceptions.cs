namespace Binnaculum.UITest.Core;

/// <summary>
/// Base exception class for UITest framework errors.
/// </summary>
public class UITestException : Exception
{
    public UITestException(string message) : base(message)
    {
    }

    public UITestException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a UI element cannot be found.
/// </summary>
public class ElementNotFoundException : UITestException
{
    public ElementNotFoundException(string query) 
        : base($"Element not found with query: {query}")
    {
        Query = query;
    }

    public ElementNotFoundException(string query, Exception innerException) 
        : base($"Element not found with query: {query}", innerException)
    {
        Query = query;
    }

    /// <summary>
    /// The query that failed to find an element.
    /// </summary>
    public string Query { get; }
}

/// <summary>
/// Exception thrown when an operation times out.
/// </summary>
public class TimeoutException : UITestException
{
    public TimeoutException(string operation, TimeSpan timeout) 
        : base($"Operation '{operation}' timed out after {timeout.TotalSeconds} seconds")
    {
        Operation = operation;
        Timeout = timeout;
    }

    public TimeoutException(string operation, TimeSpan timeout, Exception innerException) 
        : base($"Operation '{operation}' timed out after {timeout.TotalSeconds} seconds", innerException)
    {
        Operation = operation;
        Timeout = timeout;
    }

    /// <summary>
    /// The operation that timed out.
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// The timeout duration that was exceeded.
    /// </summary>
    public TimeSpan Timeout { get; }
}

/// <summary>
/// Exception thrown when a platform-specific feature is not supported.
/// </summary>
public class PlatformNotSupportedException : UITestException
{
    public PlatformNotSupportedException(string feature, TestPlatform platform) 
        : base($"Feature '{feature}' is not supported on platform '{platform}'")
    {
        Feature = feature;
        Platform = platform;
    }

    public PlatformNotSupportedException(string feature, TestPlatform platform, Exception innerException) 
        : base($"Feature '{feature}' is not supported on platform '{platform}'", innerException)
    {
        Feature = feature;
        Platform = platform;
    }

    /// <summary>
    /// The feature that is not supported.
    /// </summary>
    public string Feature { get; }

    /// <summary>
    /// The platform that doesn't support the feature.
    /// </summary>
    public TestPlatform Platform { get; }
}

/// <summary>
/// Exception thrown when app state operations fail.
/// </summary>
public class AppStateException : UITestException
{
    public AppStateException(string operation, ApplicationState currentState, ApplicationState expectedState) 
        : base($"Cannot perform operation '{operation}' in current state '{currentState}'. Expected state: '{expectedState}'")
    {
        Operation = operation;
        CurrentState = currentState;
        ExpectedState = expectedState;
    }

    public AppStateException(string operation, ApplicationState currentState, ApplicationState expectedState, Exception innerException) 
        : base($"Cannot perform operation '{operation}' in current state '{currentState}'. Expected state: '{expectedState}'", innerException)
    {
        Operation = operation;
        CurrentState = currentState;
        ExpectedState = expectedState;
    }

    /// <summary>
    /// The operation that failed.
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// The current app state.
    /// </summary>
    public ApplicationState CurrentState { get; }

    /// <summary>
    /// The expected app state for the operation.
    /// </summary>
    public ApplicationState ExpectedState { get; }
}

/// <summary>
/// Exception thrown when command execution fails.
/// </summary>
public class CommandExecutionException : UITestException
{
    public CommandExecutionException(string commandId, string message) 
        : base($"Command '{commandId}' failed: {message}")
    {
        CommandId = commandId;
    }

    public CommandExecutionException(string commandId, string message, Exception innerException) 
        : base($"Command '{commandId}' failed: {message}", innerException)
    {
        CommandId = commandId;
    }

    /// <summary>
    /// The ID of the command that failed.
    /// </summary>
    public string CommandId { get; }
}