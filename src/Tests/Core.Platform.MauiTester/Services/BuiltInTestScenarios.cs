using Core.Platform.MauiTester.Models;
using Core.Platform.MauiTester.TestCases;

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
            RegisterBrokerAccountDepositTest(discoveryService, testRunner, testActions);
            RegisterBrokerAccountMultipleMovementsTest(discoveryService, testRunner, testActions);
            RegisterOptionsImportIntegrationTest(discoveryService, testRunner, testActions);
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

        /// <summary>
        /// BrokerAccount + Deposit Test - Validates creating a broker account and adding a deposit movement
        /// </summary>
        private static void RegisterBrokerAccountDepositTest(TestDiscoveryService discoveryService, TestRunner testRunner, TestActions testActions)
        {
            discoveryService.RegisterTest(() => TestScenarioBuilder.Create()
                .Named("BrokerAccount + Deposit")
                .WithDescription("Validates the complete flow: Create BrokerAccount, Add Deposit movement, Verify snapshot financial data")
                .WithTags(TestTags.BrokerAccount, TestTags.Financial, TestTags.Movement, TestTags.Integration)
                .AddCommonSetup(testRunner)
                .AddDelay("Wait for reactive collections", TimeSpan.FromMilliseconds(300))
                .AddVerificationStep("Find Tastytrade Broker", () => {
                    var (success, details, error, id) = TestVerifications.FindTastytradeBroker();
                    if (success) testRunner.SetTastytradeId(id);
                    return (success, details, error);
                })
                .AddVerificationStep("Find USD Currency", () => {
                    var (success, details, error, id) = TestVerifications.FindUsdCurrency();
                    if (success) testRunner.SetUsdCurrencyId(id);
                    return (success, details, error);
                })
                .AddAsyncStep("Create BrokerAccount", () => testActions.CreateBrokerAccountAsync("Trading"))
                .AddVerificationStep("Find Created BrokerAccount", () => {
                    var (success, details, error, id) = TestVerifications.FindCreatedBrokerAccount(testRunner.GetTastytradeId());
                    if (success) testRunner.SetBrokerAccountId(id);
                    return (success, details, error);
                })
                .AddAsyncStep("Create Historical Deposit Movement", () => testActions.CreateMovementAsync(1200m, Binnaculum.Core.Models.BrokerMovementType.Deposit, -60, "Historical deposit test"))
                .AddDelay("Wait for snapshots to update", TimeSpan.FromMilliseconds(500))
                .AddVerificationStep("Verify Single Snapshot", TestVerifications.VerifySingleSnapshotExists)
                .AddVerificationStep("Verify Snapshot Type", TestVerifications.VerifySnapshotIsBrokerAccountType)
                .AddVerificationStep("Verify Snapshot Financial Data", () => TestVerifications.VerifySnapshotFinancialData(1200.0m, 1)));
        }

        /// <summary>
        /// BrokerAccount Multiple Movements Test - Validates creating a broker account and adding multiple deposit/withdrawal movements
        /// </summary>
        private static void RegisterBrokerAccountMultipleMovementsTest(TestDiscoveryService discoveryService, TestRunner testRunner, TestActions testActions)
        {
            discoveryService.RegisterTest(() => TestScenarioBuilder.Create()
                .Named("BrokerAccount Multiple Movements")
                .WithDescription("Validates creating a BrokerAccount and adding multiple deposit/withdrawal movements to ensure robust snapshot calculations")
                .WithTags(TestTags.BrokerAccount, TestTags.Financial, TestTags.Movement, TestTags.Integration)
                .AddCommonSetup(testRunner)
                .AddDelay("Wait for reactive collections", TimeSpan.FromMilliseconds(300))
                .AddVerificationStep("Find Tastytrade Broker", () => {
                    var (success, details, error, id) = TestVerifications.FindTastytradeBroker();
                    if (success) testRunner.SetTastytradeId(id);
                    return (success, details, error);
                })
                .AddVerificationStep("Find USD Currency", () => {
                    var (success, details, error, id) = TestVerifications.FindUsdCurrency();
                    if (success) testRunner.SetUsdCurrencyId(id);
                    return (success, details, error);
                })
                .AddAsyncStep("Create BrokerAccount for Tastytrade", () => testActions.CreateBrokerAccountAsync("Testing"))
                .AddVerificationStep("Find Created BrokerAccount", () => {
                    var (success, details, error, id) = TestVerifications.FindCreatedBrokerAccount(testRunner.GetTastytradeId());
                    if (success) testRunner.SetBrokerAccountId(id);
                    return (success, details, error);
                })
                .AddAsyncStep("Create Historical Deposit ($1200, 60 days ago)", () => testActions.CreateMovementAsync(1200m, Binnaculum.Core.Models.BrokerMovementType.Deposit, -60))
                .AddAsyncStep("Create Historical Withdrawal ($300, 55 days ago)", () => testActions.CreateMovementAsync(300m, Binnaculum.Core.Models.BrokerMovementType.Withdrawal, -55))
                .AddAsyncStep("Create Historical Withdrawal ($300, 50 days ago)", () => testActions.CreateMovementAsync(300m, Binnaculum.Core.Models.BrokerMovementType.Withdrawal, -50))
                .AddAsyncStep("Create Historical Deposit ($600, 10 days ago)", () => testActions.CreateMovementAsync(600m, Binnaculum.Core.Models.BrokerMovementType.Deposit, -10))
                .AddVerificationStep("Verify Single Snapshot", TestVerifications.VerifySingleSnapshotExists)
                .AddVerificationStep("Verify Snapshot Type", TestVerifications.VerifySnapshotIsBrokerAccountType)
                .AddVerificationStep("Verify Snapshot Financial Data (Multiple Movements)", TestVerifications.VerifyMultipleMovementsFinancialData));
        }

        /// <summary>
        /// Options Import Integration Test - End-to-end validation of Tastytrade CSV import workflow
        /// </summary>
        private static void RegisterOptionsImportIntegrationTest(TestDiscoveryService discoveryService, TestRunner testRunner, TestActions testActions)
        {
            discoveryService.RegisterTest(() => TestScenarioBuilder.Create()
                .Named("Options Import Integration Test")
                .WithDescription("End-to-end Tastytrade options import workflow validation with real trading data")
                .WithTags(TestTags.Integration, TestTags.Financial, TestTags.Import, TestTags.Options)
                .AddCommonSetup(testRunner)
                .AddDelay("Wait for reactive collections", TimeSpan.FromMilliseconds(300))
                .AddVerificationStep("Find Tastytrade Broker", () => {
                    var (success, details, error, id) = TestVerifications.FindTastytradeBroker();
                    if (success) testRunner.SetTastytradeId(id);
                    return (success, details, error);
                })
                .AddCustomStep(new OptionsImportIntegrationTest(testRunner.GetExecutionContext())));
        }
    }
}