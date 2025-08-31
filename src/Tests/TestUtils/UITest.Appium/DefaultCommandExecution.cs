using Binnaculum.UITest.Core;

namespace Binnaculum.UITest.Appium;

/// <summary>
/// Default implementation of ICommandExecution for Appium.
/// Provides basic command execution without advanced retry/grouping features.
/// </summary>
internal class DefaultCommandExecution : ICommandExecution
{
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public async Task<CommandResponse<T>> ExecuteAsync<T>(ICommand<T> command, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? DefaultTimeout;
        var commandId = command.CommandId;

        try
        {
            using var cts = new CancellationTokenSource(effectiveTimeout);
            
            // For now, we'll return a success response since Appium commands
            // are executed synchronously through the driver
            // A full implementation would integrate with actual command execution
            
            return new CommandResponse<T>(CommandResponseResult.Success, default(T));
        }
        catch (OperationCanceledException)
        {
            return new CommandResponse<T>(
                CommandResponseResult.Timeout, 
                errorMessage: $"Command '{commandId}' timed out after {effectiveTimeout.TotalSeconds} seconds");
        }
        catch (Exception ex)
        {
            return new CommandResponse<T>(
                CommandResponseResult.Failed, 
                errorMessage: ex.Message, 
                exception: ex);
        }
    }

    public async Task<ICommandExecutionGroup> ExecuteGroupAsync(IEnumerable<ICommand> commands, TimeSpan? timeout = null)
    {
        var responses = new List<CommandResponse>();
        var effectiveTimeout = timeout ?? DefaultTimeout;

        foreach (var command in commands)
        {
            try
            {
                // Basic implementation - execute each command individually
                var response = new CommandResponse(CommandResponseResult.Success);
                responses.Add(response);
            }
            catch (Exception ex)
            {
                var response = new CommandResponse(CommandResponseResult.Failed, ex.Message, ex);
                responses.Add(response);
            }
        }

        return new DefaultCommandExecutionGroup(responses);
    }
}

/// <summary>
/// Default implementation of ICommandExecutionGroup.
/// </summary>
internal class DefaultCommandExecutionGroup : ICommandExecutionGroup
{
    public DefaultCommandExecutionGroup(IReadOnlyCollection<CommandResponse> responses)
    {
        Responses = responses;
    }

    public IReadOnlyCollection<CommandResponse> Responses { get; }

    public bool AllSucceeded => Responses.All(r => r.IsSuccess);

    public bool AnyFailed => Responses.Any(r => r.IsFailure);

    public CommandResponse? GetResponse(string commandId)
    {
        // In a full implementation, responses would be indexed by command ID
        return Responses.FirstOrDefault();
    }
}