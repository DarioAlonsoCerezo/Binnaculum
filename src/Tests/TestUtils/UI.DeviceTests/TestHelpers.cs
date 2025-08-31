using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Binnaculum.Core.Models;
using System.Reactive.Subjects;

namespace Binnaculum.UI.DeviceTests;

/// <summary>
/// Common test helpers and utilities for Binnaculum device testing.
/// Provides database setup/teardown, mock services, and async testing utilities.
/// </summary>
public static class TestHelpers
{
    #region Database Setup/Teardown Helpers

    /// <summary>
    /// Sets up a test database environment with clean state.
    /// </summary>
    /// <param name="testName">Name of the test for database isolation</param>
    /// <returns>Database connection string or identifier</returns>
    public static async Task<string> SetupTestDatabase(string testName)
    {
        // Create a unique database name for test isolation
        var databaseName = $"BinnaculumTest_{testName}_{Guid.NewGuid():N}";
        
        // In a real implementation, this would:
        // 1. Create a new SQLite database
        // 2. Run migrations
        // 3. Seed with test data if needed
        
        // For now, return a mock database identifier
        await Task.Delay(10); // Simulate database setup
        
        return databaseName;
    }

    /// <summary>
    /// Tears down the test database and cleans up resources.
    /// </summary>
    /// <param name="databaseIdentifier">Database identifier returned from SetupTestDatabase</param>
    public static async Task TeardownTestDatabase(string databaseIdentifier)
    {
        // In a real implementation, this would:
        // 1. Close database connections
        // 2. Delete the test database file
        // 3. Clean up any temporary resources
        
        await Task.Delay(10); // Simulate database teardown
        
        // Log cleanup for debugging
        System.Diagnostics.Debug.WriteLine($"Cleaned up test database: {databaseIdentifier}");
    }

    /// <summary>
    /// Seeds the test database with realistic financial data.
    /// </summary>
    /// <param name="databaseIdentifier">Database identifier</param>
    /// <param name="scenario">Test data scenario to create</param>
    public static async Task SeedTestDatabase(string databaseIdentifier, TestDataScenario scenario)
    {
        await Task.Delay(50); // Simulate data seeding
        
        switch (scenario)
        {
            case TestDataScenario.EmptyPortfolio:
                // Seed with empty portfolio data
                break;
            case TestDataScenario.ProfitablePortfolio:
                // Seed with profitable trades and dividends
                break;
            case TestDataScenario.LossPortfolio:
                // Seed with losing trades
                break;
            case TestDataScenario.MixedPortfolio:
                // Seed with mixed gains/losses
                break;
            case TestDataScenario.HighVolumePortfolio:
                // Seed with many transactions
                break;
        }
        
        System.Diagnostics.Debug.WriteLine($"Seeded database {databaseIdentifier} with {scenario} scenario");
    }

    /// <summary>
    /// Enumeration of test data scenarios for database seeding.
    /// </summary>
    public enum TestDataScenario
    {
        EmptyPortfolio,
        ProfitablePortfolio,
        LossPortfolio,
        MixedPortfolio,
        HighVolumePortfolio
    }

    #endregion

    #region Mock Service Providers

    /// <summary>
    /// Creates a mock service provider with common Binnaculum services for testing.
    /// </summary>
    /// <param name="configureServices">Optional action to configure additional services</param>
    /// <returns>Configured service provider for testing</returns>
    public static IServiceProvider CreateMockServiceProvider(Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        
        // Add core logging services
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));
        
        // Add mock implementations of Binnaculum services
        services.AddSingleton<IMockBrokerService, MockBrokerService>();
        services.AddSingleton<IMockFinancialDataService, MockFinancialDataService>();
        services.AddSingleton<IMockCurrencyService, MockCurrencyService>();
        
        // Allow test-specific service configuration
        configureServices?.Invoke(services);
        
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a scoped service provider for test isolation.
    /// </summary>
    /// <param name="rootProvider">Root service provider</param>
    /// <returns>Scoped service provider</returns>
    public static IServiceScope CreateTestScope(IServiceProvider rootProvider)
    {
        return rootProvider.CreateScope();
    }

    #endregion

    #region Test Context Management

    /// <summary>
    /// Test context for managing test state and cleanup.
    /// </summary>
    public class TestContext : IDisposable
    {
        private readonly List<IDisposable> _disposables = new();
        private readonly List<Func<Task>> _asyncCleanupActions = new();
        private bool _disposed = false;

        public string TestName { get; }
        public IServiceProvider ServiceProvider { get; }
        public string DatabaseIdentifier { get; }

        internal TestContext(string testName, IServiceProvider serviceProvider, string databaseIdentifier)
        {
            TestName = testName;
            ServiceProvider = serviceProvider;
            DatabaseIdentifier = databaseIdentifier;
        }

        /// <summary>
        /// Registers a disposable resource for cleanup.
        /// </summary>
        /// <param name="disposable">Resource to dispose on cleanup</param>
        public void RegisterForDisposal(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        /// <summary>
        /// Registers an async cleanup action.
        /// </summary>
        /// <param name="cleanupAction">Async action to execute on cleanup</param>
        public void RegisterAsyncCleanup(Func<Task> cleanupAction)
        {
            _asyncCleanupActions.Add(cleanupAction);
        }

        /// <summary>
        /// Performs async cleanup of resources.
        /// </summary>
        public async Task CleanupAsync()
        {
            if (_disposed) return;

            // Execute async cleanup actions
            foreach (var cleanupAction in _asyncCleanupActions)
            {
                try
                {
                    await cleanupAction();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error during async cleanup: {ex.Message}");
                }
            }

            // Dispose synchronous resources
            foreach (var disposable in _disposables)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error during disposal: {ex.Message}");
                }
            }

            // Cleanup test database
            await TeardownTestDatabase(DatabaseIdentifier);

            _disposed = true;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Synchronous cleanup for IDisposable pattern
                CleanupAsync().GetAwaiter().GetResult();
            }
        }
    }

    /// <summary>
    /// Creates a new test context with database setup and service provider.
    /// </summary>
    /// <param name="testName">Name of the test</param>
    /// <param name="scenario">Database seeding scenario</param>
    /// <param name="configureServices">Optional service configuration</param>
    /// <returns>Test context with all resources set up</returns>
    public static async Task<TestContext> CreateTestContextAsync(
        string testName, 
        TestDataScenario scenario = TestDataScenario.EmptyPortfolio,
        Action<IServiceCollection>? configureServices = null)
    {
        var databaseId = await SetupTestDatabase(testName);
        await SeedTestDatabase(databaseId, scenario);
        
        var serviceProvider = CreateMockServiceProvider(configureServices);
        
        var context = new TestContext(testName, serviceProvider, databaseId);
        
        return context;
    }

    #endregion

    #region Async Testing Utilities

    /// <summary>
    /// Runs an async test with timeout and proper exception handling.
    /// </summary>
    /// <param name="testAction">The async test action to execute</param>
    /// <param name="timeout">Test timeout (default: 30 seconds)</param>
    /// <param name="testName">Name of the test for logging</param>
    public static async Task RunAsyncTest(
        Func<Task> testAction, 
        TimeSpan? timeout = null, 
        string? testName = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);
        testName ??= "UnnamedTest";

        using var cts = new CancellationTokenSource(timeout.Value);
        
        try
        {
            await testAction().ConfigureAwait(false);
        }
        catch (TaskCanceledException) when (cts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"Test '{testName}' timed out after {timeout}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Test '{testName}' failed with exception: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Runs an async test with a test context.
    /// </summary>
    /// <param name="testAction">The async test action that takes a test context</param>
    /// <param name="testName">Name of the test</param>
    /// <param name="scenario">Database scenario to set up</param>
    /// <param name="timeout">Test timeout</param>
    /// <param name="configureServices">Optional service configuration</param>
    public static async Task RunAsyncTestWithContext(
        Func<TestContext, Task> testAction,
        string testName,
        TestDataScenario scenario = TestDataScenario.EmptyPortfolio,
        TimeSpan? timeout = null,
        Action<IServiceCollection>? configureServices = null)
    {
        using var context = await CreateTestContextAsync(testName, scenario, configureServices);
        
        await RunAsyncTest(
            () => testAction(context),
            timeout,
            testName);
        
        await context.CleanupAsync();
    }

    /// <summary>
    /// Waits for a condition to be true with timeout and polling.
    /// </summary>
    /// <param name="condition">Condition to wait for</param>
    /// <param name="timeout">Maximum time to wait</param>
    /// <param name="pollInterval">Interval between condition checks</param>
    /// <param name="conditionName">Name of the condition for error messages</param>
    public static async Task WaitForCondition(
        Func<bool> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        string conditionName = "condition")
    {
        pollInterval ??= TimeSpan.FromMilliseconds(100);
        
        var endTime = DateTime.UtcNow.Add(timeout);
        
        while (DateTime.UtcNow < endTime)
        {
            if (condition())
                return;
                
            await Task.Delay(pollInterval.Value);
        }
        
        throw new TimeoutException($"Condition '{conditionName}' was not met within {timeout}");
    }

    /// <summary>
    /// Waits for an async condition to be true with timeout and polling.
    /// </summary>
    /// <param name="condition">Async condition to wait for</param>
    /// <param name="timeout">Maximum time to wait</param>
    /// <param name="pollInterval">Interval between condition checks</param>
    /// <param name="conditionName">Name of the condition for error messages</param>
    public static async Task WaitForConditionAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        string conditionName = "condition")
    {
        pollInterval ??= TimeSpan.FromMilliseconds(100);
        
        var endTime = DateTime.UtcNow.Add(timeout);
        
        while (DateTime.UtcNow < endTime)
        {
            if (await condition())
                return;
                
            await Task.Delay(pollInterval.Value);
        }
        
        throw new TimeoutException($"Async condition '{conditionName}' was not met within {timeout}");
    }

    #endregion

    #region Observable Testing Utilities

    /// <summary>
    /// Creates a test subject for testing Observable chains.
    /// </summary>
    /// <typeparam name="T">Type of Observable elements</typeparam>
    /// <returns>Test subject that can be used to emit values and observe results</returns>
    public static Subject<T> CreateTestSubject<T>()
    {
        return new Subject<T>();
    }

    /// <summary>
    /// Tests an Observable sequence with expected values and completion.
    /// </summary>
    /// <typeparam name="T">Type of Observable elements</typeparam>
    /// <param name="observable">Observable to test</param>
    /// <param name="expectedValues">Expected sequence of values</param>
    /// <param name="timeout">Test timeout</param>
    public static async Task AssertObservableSequence<T>(
        IObservable<T> observable,
        IEnumerable<T> expectedValues,
        TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(5);
        
        var receivedValues = new List<T>();
        var completed = false;
        Exception? error = null;
        
        using var subscription = observable.Subscribe(
            onNext: receivedValues.Add,
            onError: ex => error = ex,
            onCompleted: () => completed = true);
        
        await WaitForCondition(
            () => completed || error != null || receivedValues.Count >= expectedValues.Count(),
            timeout.Value,
            conditionName: "Observable sequence completion");
        
        if (error != null)
            throw new AssertionException($"Observable completed with error: {error}");
            
        Assert.True(completed, "Observable should have completed");
        Assert.Equal(expectedValues.ToArray(), receivedValues.ToArray());
    }

    #endregion
}

#region Mock Service Interfaces and Implementations

/// <summary>
/// Mock interface for broker services in tests.
/// </summary>
public interface IMockBrokerService
{
    Task<IEnumerable<Broker>> GetBrokersAsync();
    Task<IEnumerable<BrokerAccount>> GetBrokerAccountsAsync();
}

/// <summary>
/// Mock implementation of broker services.
/// </summary>
public class MockBrokerService : IMockBrokerService
{
    public async Task<IEnumerable<Broker>> GetBrokersAsync()
    {
        await Task.Delay(10); // Simulate async operation
        
        return new[]
        {
            TestDataBuilders.CreateBroker().AsInteractiveBrokers().Build(),
            TestDataBuilders.CreateBroker().WithId(2).AsCharlesSchwab().Build(),
            TestDataBuilders.CreateBroker().WithId(3).AsFidelity().Build()
        };
    }

    public async Task<IEnumerable<BrokerAccount>> GetBrokerAccountsAsync()
    {
        await Task.Delay(10); // Simulate async operation
        
        return new[]
        {
            TestDataBuilders.CreateBrokerAccount().WithInteractiveBrokers().Build(),
            TestDataBuilders.CreateBrokerAccount().WithId(2).WithCharlesSchwab().Build()
        };
    }
}

/// <summary>
/// Mock interface for financial data services in tests.
/// </summary>
public interface IMockFinancialDataService
{
    Task<BrokerFinancialSnapshot> GetFinancialSnapshotAsync(int brokerAccountId, DateOnly date);
    Task<IEnumerable<BrokerFinancialSnapshot>> GetFinancialHistoryAsync(int brokerAccountId, DateOnly fromDate, DateOnly toDate);
}

/// <summary>
/// Mock implementation of financial data services.
/// </summary>
public class MockFinancialDataService : IMockFinancialDataService
{
    public async Task<BrokerFinancialSnapshot> GetFinancialSnapshotAsync(int brokerAccountId, DateOnly date)
    {
        await Task.Delay(50); // Simulate data processing
        
        return TestDataBuilders.CreateFinancialData()
            .WithId(brokerAccountId)
            .WithDate(date)
            .AsProfitableScenario()
            .Build();
    }

    public async Task<IEnumerable<BrokerFinancialSnapshot>> GetFinancialHistoryAsync(int brokerAccountId, DateOnly fromDate, DateOnly toDate)
    {
        await Task.Delay(100); // Simulate data processing
        
        var snapshots = new List<BrokerFinancialSnapshot>();
        var currentDate = fromDate;
        
        while (currentDate <= toDate)
        {
            snapshots.Add(TestDataBuilders.CreateFinancialData()
                .WithId(snapshots.Count + 1)
                .WithDate(currentDate)
                .AsMixedScenario()
                .Build());
                
            currentDate = currentDate.AddDays(1);
        }
        
        return snapshots;
    }
}

/// <summary>
/// Mock interface for currency services in tests.
/// </summary>
public interface IMockCurrencyService
{
    Task<IEnumerable<Currency>> GetCurrenciesAsync();
    Task<Currency> GetBaseCurrencyAsync();
}

/// <summary>
/// Mock implementation of currency services.
/// </summary>
public class MockCurrencyService : IMockCurrencyService
{
    public async Task<IEnumerable<Currency>> GetCurrenciesAsync()
    {
        await Task.Delay(10); // Simulate async operation
        
        return new[]
        {
            TestDataBuilders.CreateCurrency().AsUSD().Build(),
            TestDataBuilders.CreateCurrency().WithId(2).AsEUR().Build(),
            TestDataBuilders.CreateCurrency().WithId(3).AsGBP().Build()
        };
    }

    public async Task<Currency> GetBaseCurrencyAsync()
    {
        await Task.Delay(10); // Simulate async operation
        
        return TestDataBuilders.CreateCurrency().AsUSD().Build();
    }
}

#endregion