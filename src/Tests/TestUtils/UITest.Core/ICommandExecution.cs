namespace Binnaculum.UITest.Core;

/// <summary>
/// Interface for executing commands on UI elements and applications.
/// Provides a framework for command execution with response handling.
/// </summary>
public interface ICommandExecution
{
    /// <summary>
    /// Execute a command and return a response.
    /// </summary>
    /// <typeparam name="T">Type of the expected response</typeparam>
    /// <param name="command">Command to execute</param>
    /// <param name="timeout">Maximum time to wait for command completion</param>
    /// <returns>Command response with result</returns>
    Task<CommandResponse<T>> ExecuteAsync<T>(ICommand<T> command, TimeSpan? timeout = null);

    /// <summary>
    /// Execute multiple commands as a group.
    /// </summary>
    /// <param name="commands">Commands to execute</param>
    /// <param name="timeout">Maximum time to wait for all commands</param>
    /// <returns>Grouped command response</returns>
    Task<ICommandExecutionGroup> ExecuteGroupAsync(IEnumerable<ICommand> commands, TimeSpan? timeout = null);

    /// <summary>
    /// Default timeout for command execution.
    /// </summary>
    TimeSpan DefaultTimeout { get; set; }
}

/// <summary>
/// Base interface for all commands.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Command identifier for logging and debugging.
    /// </summary>
    string CommandId { get; }

    /// <summary>
    /// Command description.
    /// </summary>
    string Description { get; }
}

/// <summary>
/// Generic interface for commands that return a specific type.
/// </summary>
/// <typeparam name="T">Type of the command result</typeparam>
public interface ICommand<T> : ICommand
{
}

/// <summary>
/// Interface for grouped command execution results.
/// </summary>
public interface ICommandExecutionGroup
{
    /// <summary>
    /// All command responses in the group.
    /// </summary>
    IReadOnlyCollection<CommandResponse> Responses { get; }

    /// <summary>
    /// Whether all commands in the group succeeded.
    /// </summary>
    bool AllSucceeded { get; }

    /// <summary>
    /// Whether any command in the group failed.
    /// </summary>
    bool AnyFailed { get; }

    /// <summary>
    /// Get response for a specific command by ID.
    /// </summary>
    /// <param name="commandId">Command identifier</param>
    /// <returns>Command response if found</returns>
    CommandResponse? GetResponse(string commandId);
}

/// <summary>
/// Standardized response for command execution.
/// </summary>
/// <typeparam name="T">Type of the response value</typeparam>
public class CommandResponse<T> : CommandResponse
{
    public CommandResponse(CommandResponseResult result, T? value = default, string? errorMessage = null, Exception? exception = null)
        : base(result, errorMessage, exception)
    {
        Value = value;
    }

    /// <summary>
    /// The response value if the command succeeded.
    /// </summary>
    public T? Value { get; }
}

/// <summary>
/// Base class for command responses.
/// </summary>
public class CommandResponse
{
    public CommandResponse(CommandResponseResult result, string? errorMessage = null, Exception? exception = null)
    {
        Result = result;
        ErrorMessage = errorMessage;
        Exception = exception;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Result of the command execution.
    /// </summary>
    public CommandResponseResult Result { get; }

    /// <summary>
    /// Error message if the command failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Exception that caused the command to fail, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Timestamp when the response was created.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Whether the command succeeded.
    /// </summary>
    public bool IsSuccess => Result == CommandResponseResult.Success;

    /// <summary>
    /// Whether the command failed.
    /// </summary>
    public bool IsFailure => Result != CommandResponseResult.Success;
}

/// <summary>
/// Possible results of command execution.
/// </summary>
public enum CommandResponseResult
{
    /// <summary>
    /// Command executed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Command failed due to an error.
    /// </summary>
    Failed,

    /// <summary>
    /// Command timed out.
    /// </summary>
    Timeout,

    /// <summary>
    /// Command was cancelled.
    /// </summary>
    Cancelled
}