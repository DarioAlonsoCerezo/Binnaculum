using Core.Platform.MauiTester.Models;

namespace Core.Platform.MauiTester.Services
{
    /// <summary>
    /// Defines built-in test scenarios for the platform validation suite
    /// This extraction separates scenario definitions from TestRunner orchestration
    /// </summary>
    public static class BuiltInTestScenarios
    {
        /// <summary>
        /// Register all built-in test scenarios with the discovery service
        /// </summary>
        public static void RegisterAll(TestDiscoveryService discoveryService, TestRunner testRunner, TestActions testActions)
        {
            RegisterOverviewTest(discoveryService, testRunner);
            RegisterBrokerAccountCreationTest(discoveryService, testRunner, testActions);
        }

        /// <summary>
        /// Overview Test - Basic platform validation
        /// </summary>
        private static void RegisterOverviewTest(TestDiscoveryService discoveryService, TestRunner testRunner)
        {
            discoveryService.RegisterTest(() => TestScenarioBuilder.Create()
                .Named("Overview Platform Validation")
                .WithDescription("Validates Overview.InitDatabase() and Overview.LoadData() work in MAUI environment")
                .WithTags(TestTags.Overview, TestTags.Database, TestTags.Collection, TestTags.Smoke)
                .AddCommonSetup(testRunner)
                .AddDelay("Wait for reactive collections", TimeSpan.FromMilliseconds(300))
                .AddVerificationStep("Verify Database Initialized", TestVerifications.VerifyDatabaseInitialized)
                .AddVerificationStep("Verify Data Loaded", TestVerifications.VerifyDataLoaded)
                .AddVerificationStep("Verify Currencies Collection", TestVerifications.VerifyCurrenciesCollection)
                .AddVerificationStep("Verify USD Currency", TestVerifications.VerifyUsdCurrency)
                .AddVerificationStep("Verify Brokers Collection", TestVerifications.VerifyBrokersCollection)
                .AddVerificationStep("Verify IBKR Broker", TestVerifications.VerifyIbkrBroker)
                .AddVerificationStep("Verify Tastytrade Broker", TestVerifications.VerifyTastytradeBroker)
                .AddVerificationStep("Verify Sigma Trade Broker", TestVerifications.VerifySigmaTradeBroker)
                .AddVerificationStep("Verify SPY Ticker", TestVerifications.VerifySpyTicker)
                .AddVerificationStep("Verify Snapshots Collection", TestVerifications.VerifySnapshotsCollection));
        }

        /// <summary>
        /// BrokerAccount Creation Test - Validates broker account creation and snapshot generation
        /// </summary>
        private static void RegisterBrokerAccountCreationTest(TestDiscoveryService discoveryService, TestRunner testRunner, TestActions testActions)
        {
            discoveryService.RegisterTest(() => TestScenarioBuilder.Create()
                .Named("BrokerAccount Creation")
                .WithDescription("Validates creating a new broker account and verifying snapshot generation")
                .WithTags(TestTags.BrokerAccount, TestTags.Financial, TestTags.Integration)
                .AddCommonSetup(testRunner)
                .AddDelay("Wait for reactive collections", TimeSpan.FromMilliseconds(300))
                .AddVerificationStep("Find Tastytrade Broker", () => {
                    var (success, details, error, id) = TestVerifications.FindTastytradeBroker();
                    if (success) testRunner.SetTastytradeId(id);
                    return (success, details, error);
                })
                .AddAsyncStep("Create BrokerAccount", () => testActions.CreateBrokerAccountAsync("Trading"))
                .AddVerificationStep("Verify Single Snapshot", TestVerifications.VerifySingleSnapshotExists)
                .AddVerificationStep("Verify Snapshot Type", TestVerifications.VerifySnapshotIsBrokerAccountType));
        }
    }
}