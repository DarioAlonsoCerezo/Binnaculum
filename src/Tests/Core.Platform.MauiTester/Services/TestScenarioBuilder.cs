using Core.Platform.MauiTester.Models;

namespace Core.Platform.MauiTester.Services
{
    /// <summary>
    /// Fluent API for building and configuring test scenarios
    /// </summary>
    public class TestScenarioBuilder
    {
        private readonly List<TestStep> _steps = new();
        private readonly List<string> _tags = new();
        private string _name = "Test Scenario";
        private string _description = "";
        private int _retryCount = 0;
        private TimeSpan _stepTimeout = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Set the name of the test scenario
        /// </summary>
        public TestScenarioBuilder Named(string name)
        {
            _name = name;
            return this;
        }

        /// <summary>
        /// Set the description of the test scenario
        /// </summary>
        public TestScenarioBuilder WithDescription(string description)
        {
            _description = description;
            return this;
        }

        /// <summary>
        /// Add tags to categorize the test scenario
        /// </summary>
        public TestScenarioBuilder WithTags(params string[] tags)
        {
            _tags.AddRange(tags);
            return this;
        }

        /// <summary>
        /// Set the retry count for failed steps
        /// </summary>
        public TestScenarioBuilder WithRetryCount(int retryCount)
        {
            _retryCount = retryCount;
            return this;
        }

        /// <summary>
        /// Set the timeout for individual steps
        /// </summary>
        public TestScenarioBuilder WithStepTimeout(TimeSpan timeout)
        {
            _stepTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Add an async test step
        /// </summary>
        public TestScenarioBuilder AddAsyncStep(string stepName, Func<Task<(bool success, string details)>> action)
        {
            _steps.Add(new AsyncTestStep(stepName, action));
            return this;
        }

        /// <summary>
        /// Add a sync test step
        /// </summary>
        public TestScenarioBuilder AddSyncStep(string stepName, Func<(bool success, string details)> action)
        {
            _steps.Add(new SyncTestStep(stepName, action));
            return this;
        }

        /// <summary>
        /// Add a verification step
        /// </summary>
        public TestScenarioBuilder AddVerificationStep(string stepName, Func<(bool success, string details, string error)> verification)
        {
            _steps.Add(new VerificationTestStep(stepName, verification));
            return this;
        }

        /// <summary>
        /// Add a signal-based waiting step that waits for expected reactive signals
        /// </summary>
        public TestScenarioBuilder AddSignalWaitStep(string stepName, TimeSpan timeout, params string[] expectedSignals)
        {
            AddAsyncStep(stepName, async () =>
            {
                ReactiveTestVerifications.ExpectSignals(expectedSignals);
                var success = await ReactiveTestVerifications.WaitForAllSignalsAsync(timeout);

                if (success)
                {
                    return (true, $"All expected signals received: {string.Join(", ", expectedSignals)}");
                }
                else
                {
                    var (expected, received, missing) = ReactiveTestVerifications.GetSignalStatus();
                    return (false, $"Timeout waiting for signals. Expected: [{string.Join(", ", expected)}], Received: [{string.Join(", ", received)}], Missing: [{string.Join(", ", missing)}]");
                }
            });
            return this;
        }

        /// <summary>
        /// Add a custom test step
        /// </summary>
        public TestScenarioBuilder AddCustomStep(TestStep customStep)
        {
            _steps.Add(customStep);
            return this;
        }

        /// <summary>
        /// Add the common setup steps (Wipe Data, Init Platform, Init Database, Load Data)
        /// </summary>
        public TestScenarioBuilder AddCommonSetup(TestRunner testRunner)
        {
            AddAsyncStep("Wipe All Data for Testing", () => testRunner.Actions.WipeDataForTestingAsync());
            AddSyncStep("Initialize MAUI Platform Services", () => testRunner.Actions.InitializePlatformServicesAsync().Result);
            AddAsyncStep("Overview.InitDatabase()", () => testRunner.Actions.InitializeDatabaseAsync());
            AddAsyncStep("Overview.LoadData()", () => testRunner.Actions.LoadDataAsync());
            return this;
        }

        /// <summary>
        /// Add reactive overview setup - same as common setup but with stream observation
        /// </summary>
        public TestScenarioBuilder AddReactiveOverviewSetup(TestRunner testRunner)
        {
            AddAsyncStep("Wipe All Data for Testing", () => testRunner.Actions.WipeDataForTestingAsync());
            AddSyncStep("Initialize MAUI Platform Services", () => testRunner.Actions.InitializePlatformServicesAsync().Result);
            AddSyncStep("Start Reactive Stream Observation", () =>
            {
                ReactiveTestVerifications.StartObserving();
                return (true, "Started observing reactive streams");
            });
            AddAsyncStep("Overview.InitDatabase() [Reactive]", () => testRunner.Actions.InitializeDatabaseAsync());
            AddAsyncStep("Overview.LoadData() [Reactive]", () => testRunner.Actions.LoadDataAsync());
            AddSyncStep("Stop Reactive Stream Observation", () =>
            {
                ReactiveTestVerifications.StopObserving();
                return (true, "Stopped observing reactive streams");
            });
            return this;
        }

        /// <summary>
        /// Add reactive BrokerAccount setup - setup with stream observation for BrokerAccount creation
        /// </summary>
        public TestScenarioBuilder AddReactiveBrokerAccountSetup(TestRunner testRunner)
        {
            AddAsyncStep("Wipe All Data for Testing", () => testRunner.Actions.WipeDataForTestingAsync());
            AddSyncStep("Initialize MAUI Platform Services", () => testRunner.Actions.InitializePlatformServicesAsync().Result);
            AddAsyncStep("Overview.InitDatabase()", () => testRunner.Actions.InitializeDatabaseAsync());
            AddAsyncStep("Overview.LoadData()", () => testRunner.Actions.LoadDataAsync());
            AddDelay("Wait for reactive collections", TimeSpan.FromMilliseconds(300));
            AddSyncStep("Start Reactive Stream Observation [BrokerAccount]", () =>
            {
                ReactiveTestVerifications.StartObserving();
                return (true, "Started observing reactive streams for BrokerAccount creation");
            });
            return this;
        }

        /// <summary>
        /// Add reactive BrokerAccount + Deposit setup - setup with stream observation for BrokerAccount creation and deposit
        /// </summary>
        public TestScenarioBuilder AddReactiveBrokerAccountDepositSetup(TestRunner testRunner)
        {
            AddAsyncStep("Wipe All Data for Testing", () => testRunner.Actions.WipeDataForTestingAsync());
            AddSyncStep("Initialize MAUI Platform Services", () => testRunner.Actions.InitializePlatformServicesAsync().Result);
            AddAsyncStep("Overview.InitDatabase()", () => testRunner.Actions.InitializeDatabaseAsync());
            AddAsyncStep("Overview.LoadData()", () => testRunner.Actions.LoadDataAsync());
            AddDelay("Wait for reactive collections", TimeSpan.FromMilliseconds(300));
            AddSyncStep("Start Reactive Stream Observation [BrokerAccount + Deposit]", () =>
            {
                ReactiveTestVerifications.StartObserving();
                return (true, "Started observing reactive streams for BrokerAccount creation and deposit");
            });
            return this;
        }

        /// <summary>
        /// Add reactive BrokerAccount + Multiple Movements setup - setup with stream observation for BrokerAccount creation and multiple movements
        /// </summary>
        public TestScenarioBuilder AddReactiveBrokerAccountMultipleMovementsSetup(TestRunner testRunner)
        {
            AddAsyncStep("Wipe All Data for Testing", () => testRunner.Actions.WipeDataForTestingAsync());
            AddSyncStep("Initialize MAUI Platform Services", () => testRunner.Actions.InitializePlatformServicesAsync().Result);
            AddAsyncStep("Overview.InitDatabase()", () => testRunner.Actions.InitializeDatabaseAsync());
            AddAsyncStep("Overview.LoadData()", () => testRunner.Actions.LoadDataAsync());
            AddDelay("Wait for reactive collections", TimeSpan.FromMilliseconds(300));
            AddSyncStep("Start Reactive Stream Observation [BrokerAccount + Multiple Movements]", () =>
            {
                ReactiveTestVerifications.StartObserving();
                return (true, "Started observing reactive streams for BrokerAccount creation and multiple movements");
            });
            return this;
        }

        /// <summary>
        /// Add a delay step for waiting
        /// </summary>
        public TestScenarioBuilder AddDelay(string stepName, TimeSpan delay)
        {
            AddAsyncStep(stepName, async () =>
            {
                await Task.Delay(delay);
                return (true, $"Waited {delay.TotalMilliseconds}ms");
            });
            return this;
        }

        /// <summary>
        /// Build the test scenario
        /// </summary>
        public TestScenario Build()
        {
            return new TestScenario
            {
                Name = _name,
                Description = _description,
                Tags = new List<string>(_tags),
                Steps = new List<TestStep>(_steps),
                RetryCount = _retryCount,
                StepTimeout = _stepTimeout
            };
        }

        /// <summary>
        /// Create a new builder instance
        /// </summary>
        public static TestScenarioBuilder Create() => new TestScenarioBuilder();
    }

    /// <summary>
    /// Represents a complete test scenario with metadata and steps
    /// </summary>
    public class TestScenario
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> Tags { get; set; } = new();
        public List<TestStep> Steps { get; set; } = new();
        public int RetryCount { get; set; } = 0;
        public TimeSpan StepTimeout { get; set; } = TimeSpan.FromMinutes(2);
    }
}