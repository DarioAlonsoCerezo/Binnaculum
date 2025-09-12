namespace Core.Platform.MauiTester.Services
{
    /// <summary>
    /// Demo class showing how to use the new fluent API for creating test scenarios
    /// This demonstrates the modernized TestRunner infrastructure
    /// </summary>
    public static class FluentApiTestDemo
    {
        /// <summary>
        /// Example of creating a custom test scenario using the fluent API
        /// </summary>
        public static TestScenario CreateCustomTestScenario(TestRunner testRunner)
        {
            return TestScenarioBuilder.Create()
                .Named("Custom Demo Test")
                .WithDescription("Demonstrates the fluent API capabilities for test creation")
                .WithTags(TestTags.Smoke, TestTags.Verification, TestTagsExtended.Demo)
                .WithRetryCount(1)
                .WithStepTimeout(TimeSpan.FromSeconds(30))
                
                // Setup steps using the fluent API
                .AddAsyncStep("Initialize Test Environment", () => testRunner.Actions.WipeDataForTestingAsync())
                .AddSyncStep("Check Platform Services", () => testRunner.Actions.InitializePlatformServicesAsync().Result)
                .AddAsyncStep("Initialize Database", () => testRunner.Actions.InitializeDatabaseAsync())
                .AddAsyncStep("Load Initial Data", () => testRunner.Actions.LoadDataAsync())
                
                // Add a delay for reactive collections
                .AddDelay("Wait for Collections to Populate", TimeSpan.FromMilliseconds(300))
                
                // Verification steps using the centralized TestVerifications
                .AddVerificationStep("Verify Database State", TestVerifications.VerifyDatabaseInitialized)
                .AddVerificationStep("Verify Data Loaded", TestVerifications.VerifyDataLoaded)
                .AddVerificationStep("Verify Currencies Available", TestVerifications.VerifyCurrenciesCollection)
                .AddVerificationStep("Verify USD Currency", TestVerifications.VerifyUsdCurrency)
                .AddVerificationStep("Verify Brokers Available", TestVerifications.VerifyBrokersCollection)
                
                .Build();
        }

        /// <summary>
        /// Example of how to discover and filter tests using the new discovery service
        /// </summary>
        public static void DemoTestDiscovery(TestDiscoveryService discovery)
        {
            // Register the custom test
            var testRunner = new TestRunner(new LogService());
            discovery.RegisterTest(CreateCustomTestScenario(testRunner));

            // Demonstrate discovery capabilities
            var allTests = discovery.GetAllTests();
            var smokeTests = discovery.GetTestsByTag(TestTags.Smoke);
            var overviewTests = discovery.GetTestsByTag(TestTags.Overview);
            var demoTests = discovery.GetTestsByNamePattern("*Demo*");
            var infrastructureTests = discovery.GetTestsByTag(TestTagsExtended.Demo);

            // Get test summary
            var summaryByTag = discovery.GetTestSummaryByTag();
        }

        /// <summary>
        /// Example of executing a test scenario using the modernized API
        /// </summary>
        public static async Task<Models.OverallTestResult> DemoTestExecution(TestRunner testRunner)
        {
            // Create a custom scenario
            var scenario = CreateCustomTestScenario(testRunner);

            // Execute using the new scenario runner
            return await testRunner.ExecuteScenarioAsync(scenario, progressMessage =>
            {
                System.Diagnostics.Debug.WriteLine($"Progress: {progressMessage}");
            });
        }
    }

    /// <summary>
    /// Extended test tags for demo purposes
    /// </summary>
    public static class TestTagsExtended
    {
        public const string Demo = "demo";
        public const string Api = "api";
        public const string Infrastructure = "infrastructure";
    }
}