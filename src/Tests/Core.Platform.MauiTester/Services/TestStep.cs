using Core.Platform.MauiTester.Models;

namespace Core.Platform.MauiTester.Services
{
    /// <summary>
    /// Abstract base class for test step execution that encapsulates common step logic
    /// </summary>
    public abstract class TestStep
    {
        protected TestStep(string stepName)
        {
            StepName = stepName;
        }

        public string StepName { get; }

        /// <summary>
        /// Execute the step and return the result
        /// </summary>
        /// <returns>Tuple containing success status, details, and optional error message</returns>
        public abstract Task<(bool success, string details, string? error)> ExecuteAsync();
    }

    /// <summary>
    /// Test step for synchronous operations that return (bool, string)
    /// </summary>
    public class SyncTestStep : TestStep
    {
        private readonly Func<(bool success, string details)> _action;

        public SyncTestStep(string stepName, Func<(bool success, string details)> action) : base(stepName)
        {
            _action = action;
        }

        public override Task<(bool success, string details, string? error)> ExecuteAsync()
        {
            var (success, details) = _action();
            return Task.FromResult((success, details, (string?)null));
        }
    }

    /// <summary>
    /// Test step for asynchronous operations that return Task&lt;(bool, string)&gt;
    /// </summary>
    public class AsyncTestStep : TestStep
    {
        private readonly Func<Task<(bool success, string details)>> _action;

        public AsyncTestStep(string stepName, Func<Task<(bool success, string details)>> action) : base(stepName)
        {
            _action = action;
        }

        public override async Task<(bool success, string details, string? error)> ExecuteAsync()
        {
            var (success, details) = await _action();
            return (success, details, (string?)null);
        }
    }

    /// <summary>
    /// Test step for verification operations that return (bool, string, string)
    /// </summary>
    public class VerificationTestStep : TestStep
    {
        private readonly Func<(bool success, string details, string error)> _verification;

        public VerificationTestStep(string stepName, Func<(bool success, string details, string error)> verification) : base(stepName)
        {
            _verification = verification;
        }

        public override Task<(bool success, string details, string? error)> ExecuteAsync()
        {
            var (success, details, error) = _verification();
            return Task.FromResult((success, details, (string?)error));
        }
    }

    /// <summary>
    /// Test step for generic synchronous operations with pattern matching for different return types
    /// </summary>
    public class GenericSyncTestStep<T> : TestStep where T : struct
    {
        private readonly Func<T> _action;

        public GenericSyncTestStep(string stepName, Func<T> action) : base(stepName)
        {
            _action = action;
        }

        public override Task<(bool success, string details, string? error)> ExecuteAsync()
        {
            var actionResult = _action();
            bool success;
            string details;
            string? error = null;

            // Handle different return types using pattern matching
            if (actionResult is ValueTuple<bool, string> basicResult)
            {
                (success, details) = basicResult;
            }
            else if (actionResult is ValueTuple<bool, string, string> verificationResult)
            {
                (success, details, error) = verificationResult;
            }
            else
            {
                throw new InvalidOperationException($"Unsupported return type: {typeof(T)}");
            }

            return Task.FromResult((success, details, error));
        }
    }
}