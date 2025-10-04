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
            RegisterOverviewReactiveTest(discoveryService, testRunner);
            RegisterBrokerAccountCreationTest(discoveryService, testRunner, testActions);
            RegisterBrokerAccountCreationReactiveTest(discoveryService, testRunner, testActions);
            RegisterBrokerAccountDepositTest(discoveryService, testRunner, testActions);
            RegisterBrokerAccountDepositReactiveTest(discoveryService, testRunner, testActions);
            RegisterBrokerAccountMultipleMovementsTest(discoveryService, testRunner, testActions);
            RegisterBrokerAccountMultipleMovementsReactiveTest(discoveryService, testRunner, testActions);
            RegisterBrokerAccountMultipleMovementsSignalBasedTest(discoveryService, testRunner, testActions);
            RegisterOptionsImportIntegrationTest(discoveryService, testRunner, testActions);
            RegisterOptionsImportIntegrationSignalBasedTest(discoveryService, testRunner, testActions);
            RegisterDepositsWithdrawalsIntegrationTest(discoveryService, testRunner, testActions);
            RegisterTastytradeImportIntegrationTest(discoveryService, testRunner, testActions);
            RegisterTsllImportIntegrationTest(discoveryService, testRunner, testActions);
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
        /// Overview Reactive Test - Validates reactive streams during Overview operations
        /// </summary>
        private static void RegisterOverviewReactiveTest(TestDiscoveryService discoveryService, TestRunner testRunner)
        {
            discoveryService.RegisterTest(() => TestScenarioBuilder.Create()
                .Named("Overview Reactive Validation")
                .WithDescription("Validates reactive stream emissions during Overview.InitDatabase() and Overview.LoadData() operations")
                .WithTags(TestTags.Overview, TestTags.Database, TestTags.Collection, TestTags.Reactive)
                .AddReactiveOverviewSetup(testRunner)
                .AddDelay("Allow reactive processing", TimeSpan.FromMilliseconds(500))
                .AddVerificationStep("Verify Overview.Data Stream", ReactiveTestVerifications.VerifyOverviewDataStream)
                .AddVerificationStep("Verify Currencies Stream", ReactiveTestVerifications.VerifyCurrenciesStream)
                .AddVerificationStep("Verify Brokers Stream", ReactiveTestVerifications.VerifyBrokersStream)
                .AddVerificationStep("Verify Tickers Stream", ReactiveTestVerifications.VerifyTickersStream)
                .AddVerificationStep("Verify Snapshots Stream", ReactiveTestVerifications.VerifySnapshotsStream)
                .AddVerificationStep("Compare with Traditional Test", ReactiveTestVerifications.CompareWithTraditionalTest));
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
                .AddVerificationStep("Find Tastytrade Broker", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindTastytradeBroker();
                    if (success) testRunner.SetTastytradeId(id);
                    return (success, details, error);
                })
                .AddAsyncStep("Create BrokerAccount", () => testActions.CreateBrokerAccountAsync("Trading"))
                .AddVerificationStep("Verify Single Snapshot", TestVerifications.VerifySingleSnapshotExists)
                .AddVerificationStep("Verify Snapshot Type", TestVerifications.VerifySnapshotIsBrokerAccountType));
        }

        /// <summary>
        /// BrokerAccount Creation Reactive Test - Validates reactive streams during broker account creation
        /// </summary>
        private static void RegisterBrokerAccountCreationReactiveTest(TestDiscoveryService discoveryService, TestRunner testRunner, TestActions testActions)
        {
            discoveryService.RegisterTest(() => TestScenarioBuilder.Create()
                .Named("BrokerAccount Creation Reactive Validation")
                .WithDescription("Validates reactive stream emissions during broker account creation and snapshot generation")
                .WithTags(TestTags.BrokerAccount, TestTags.Financial, TestTags.Integration, TestTags.Reactive)
                .AddReactiveBrokerAccountSetup(testRunner)
                .AddVerificationStep("Find Tastytrade Broker", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindTastytradeBroker();
                    if (success) testRunner.SetTastytradeId(id);
                    return (success, details, error);
                })
                .AddAsyncStep("Create BrokerAccount [Reactive]", () => testActions.CreateBrokerAccountAsync("Trading"))
                .AddDelay("Allow reactive processing", TimeSpan.FromMilliseconds(500))
                .AddSyncStep("Stop Reactive Stream Observation", () =>
                {
                    ReactiveTestVerifications.StopObserving();
                    return (true, "Stopped observing reactive streams");
                })
                .AddVerificationStep("Verify Accounts Stream", ReactiveTestVerifications.VerifyAccountsStream)
                .AddVerificationStep("Verify BrokerAccount Creation", ReactiveTestVerifications.VerifyBrokerAccountCreation)
                .AddVerificationStep("Verify BrokerAccount Snapshots", ReactiveTestVerifications.VerifyBrokerAccountSnapshots)
                .AddVerificationStep("Compare with Traditional Test", ReactiveTestVerifications.CompareWithTraditionalBrokerAccountTest));
        }

        /// <summary>
        /// BrokerAccount + Deposit Reactive Test - Validates reactive stream emissions during broker account creation with deposit
        /// </summary>
        private static void RegisterBrokerAccountDepositReactiveTest(TestDiscoveryService discoveryService, TestRunner testRunner, TestActions testActions)
        {
            discoveryService.RegisterTest(() => TestScenarioBuilder.Create()
                .Named("BrokerAccount + Deposit Reactive Validation")
                .WithDescription("Validates reactive stream emissions during broker account creation with deposit movement and snapshot generation")
                .WithTags(TestTags.BrokerAccount, TestTags.Financial, TestTags.Movement, TestTags.Integration, TestTags.Reactive)
                .AddReactiveBrokerAccountDepositSetup(testRunner)
                .AddVerificationStep("Find Tastytrade Broker", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindTastytradeBroker();
                    if (success) testRunner.SetTastytradeId(id);
                    return (success, details, error);
                })
                .AddVerificationStep("Find USD Currency", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindUsdCurrency();
                    if (success) testRunner.SetUsdCurrencyId(id);
                    return (success, details, error);
                })
                .AddAsyncStep("Create BrokerAccount [Reactive]", () => testActions.CreateBrokerAccountAsync("Trading"))
                .AddVerificationStep("Find Created BrokerAccount", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindCreatedBrokerAccount(testRunner.GetTastytradeId());
                    if (success) testRunner.SetBrokerAccountId(id);
                    return (success, details, error);
                })
                .AddDelay("Allow account creation reactive processing", TimeSpan.FromMilliseconds(500))
                .AddAsyncStep("Add Deposit Movement [Reactive]", () => testActions.CreateMovementAsync(5000.00m, Binnaculum.Core.Models.BrokerMovementType.Deposit, -30, "Reactive deposit test"))
                .AddDelay("Allow deposit reactive processing", TimeSpan.FromMilliseconds(500))
                .AddSyncStep("Stop Reactive Stream Observation", () =>
                {
                    ReactiveTestVerifications.StopObserving();
                    return (true, "Stopped observing reactive streams");
                })
                .AddVerificationStep("Verify Movements Stream", ReactiveTestVerifications.VerifyMovementsStream)
                .AddVerificationStep("Verify BrokerAccount + Deposit", ReactiveTestVerifications.VerifyBrokerAccountWithDeposit)
                .AddVerificationStep("Verify Deposit Snapshots", ReactiveTestVerifications.VerifyDepositSnapshots)
                .AddVerificationStep("Compare with Traditional Test", ReactiveTestVerifications.CompareWithTraditionalBrokerAccountDepositTest));
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
                .AddVerificationStep("Find Tastytrade Broker", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindTastytradeBroker();
                    if (success) testRunner.SetTastytradeId(id);
                    return (success, details, error);
                })
                .AddVerificationStep("Find USD Currency", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindUsdCurrency();
                    if (success) testRunner.SetUsdCurrencyId(id);
                    return (success, details, error);
                })
                .AddAsyncStep("Create BrokerAccount", () => testActions.CreateBrokerAccountAsync("Trading"))
                .AddVerificationStep("Find Created BrokerAccount", () =>
                {
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
                .AddVerificationStep("Find Tastytrade Broker", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindTastytradeBroker();
                    if (success) testRunner.SetTastytradeId(id);
                    return (success, details, error);
                })
                .AddVerificationStep("Find USD Currency", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindUsdCurrency();
                    if (success) testRunner.SetUsdCurrencyId(id);
                    return (success, details, error);
                })
                .AddAsyncStep("Create BrokerAccount for Tastytrade", () => testActions.CreateBrokerAccountAsync("Testing"))
                .AddVerificationStep("Find Created BrokerAccount", () =>
                {
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
        /// BrokerAccount Multiple Movements Reactive Test - Validates reactive stream emissions during multiple movements
        /// </summary>
        private static void RegisterBrokerAccountMultipleMovementsReactiveTest(TestDiscoveryService discoveryService, TestRunner testRunner, TestActions testActions)
        {
            discoveryService.RegisterTest(() => TestScenarioBuilder.Create()
                .Named("BrokerAccount Multiple Movements Reactive Validation")
                .WithDescription("Validates reactive stream emissions during broker account creation with multiple movements and complex snapshot calculations")
                .WithTags(TestTags.BrokerAccount, TestTags.Financial, TestTags.Movement, TestTags.Integration, TestTags.Reactive)
                .AddReactiveBrokerAccountMultipleMovementsSetup(testRunner)
                .AddVerificationStep("Find Tastytrade Broker", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindTastytradeBroker();
                    if (success) testRunner.SetTastytradeId(id);
                    return (success, details, error);
                })
                .AddVerificationStep("Find USD Currency", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindUsdCurrency();
                    if (success) testRunner.SetUsdCurrencyId(id);
                    return (success, details, error);
                })
                .AddAsyncStep("Create BrokerAccount [Reactive]", () => testActions.CreateBrokerAccountAsync("Multi-Movement Testing"))
                .AddVerificationStep("Find Created BrokerAccount", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindCreatedBrokerAccount(testRunner.GetTastytradeId());
                    if (success) testRunner.SetBrokerAccountId(id);
                    return (success, details, error);
                })
                .AddDelay("Allow account creation reactive processing", TimeSpan.FromMilliseconds(1000))
                .AddAsyncStep("Create Historical Deposit ($1200, 60 days ago) [Reactive]", () => testActions.CreateMovementAsync(1200m, Binnaculum.Core.Models.BrokerMovementType.Deposit, -60))
                .AddDelay("Allow movement reactive processing", TimeSpan.FromMilliseconds(800))
                .AddAsyncStep("Create Historical Withdrawal ($300, 55 days ago) [Reactive]", () => testActions.CreateMovementAsync(300m, Binnaculum.Core.Models.BrokerMovementType.Withdrawal, -55))
                .AddDelay("Allow movement reactive processing", TimeSpan.FromMilliseconds(800))
                .AddAsyncStep("Create Historical Withdrawal ($300, 50 days ago) [Reactive]", () => testActions.CreateMovementAsync(300m, Binnaculum.Core.Models.BrokerMovementType.Withdrawal, -50))
                .AddDelay("Allow movement reactive processing", TimeSpan.FromMilliseconds(800))
                .AddAsyncStep("Create Historical Deposit ($600, 10 days ago) [Reactive]", () => testActions.CreateMovementAsync(600m, Binnaculum.Core.Models.BrokerMovementType.Deposit, -10))
                .AddDelay("Allow final movement reactive processing", TimeSpan.FromMilliseconds(1200))
                .AddSyncStep("Stop Reactive Stream Observation", () =>
                {
                    ReactiveTestVerifications.StopObserving();
                    return (true, "Stopped observing reactive streams");
                })
                .AddVerificationStep("Verify Movements Stream", ReactiveTestVerifications.VerifyMovementsStream)
                .AddVerificationStep("Verify BrokerAccount + Multiple Movements", ReactiveTestVerifications.VerifyBrokerAccountWithMultipleMovements)
                .AddVerificationStep("Verify Multiple Movements Snapshots", ReactiveTestVerifications.VerifyMultipleMovementsSnapshots));
        }

        /// <summary>
        /// BrokerAccount Multiple Movements Signal-Based Test - Demonstrates signal-based reactive testing without delays
        /// </summary>
        private static void RegisterBrokerAccountMultipleMovementsSignalBasedTest(TestDiscoveryService discoveryService, TestRunner testRunner, TestActions testActions)
        {
            discoveryService.RegisterTest(() => TestScenarioBuilder.Create()
                .Named("BrokerAccount Multiple Movements Signal-Based Validation")
                .WithDescription("Demonstrates signal-based reactive testing - waits for actual reactive signals instead of using delays")
                .WithTags(TestTags.BrokerAccount, TestTags.Financial, TestTags.Movement, TestTags.Integration, TestTags.Reactive)
                .AddReactiveBrokerAccountMultipleMovementsSetup(testRunner)
                .AddVerificationStep("Find Tastytrade Broker", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindTastytradeBroker();
                    if (success) testRunner.SetTastytradeId(id);
                    return (success, details, error);
                })
                .AddVerificationStep("Find USD Currency", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindUsdCurrency();
                    if (success) testRunner.SetUsdCurrencyId(id);
                    return (success, details, error);
                })
                .AddAsyncStep("Create BrokerAccount [Signal-Based]", () => testActions.CreateBrokerAccountAsync("Signal-Based Testing"))
                .AddSignalWaitStep("Wait for Account Creation Signals", TimeSpan.FromSeconds(10), "Accounts_Updated", "Snapshots_Updated")
                .AddVerificationStep("Find Created BrokerAccount", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindCreatedBrokerAccount(testRunner.GetTastytradeId());
                    if (success) testRunner.SetBrokerAccountId(id);
                    return (success, details, error);
                })
                .AddAsyncStep("Create Historical Deposit ($1200, 60 days ago) [Signal-Based]", () => testActions.CreateMovementAsync(1200m, Binnaculum.Core.Models.BrokerMovementType.Deposit, -60))
                .AddSignalWaitStep("Wait for First Movement Signals", TimeSpan.FromSeconds(10), "Movements_Updated", "Snapshots_Updated")
                .AddAsyncStep("Create Historical Withdrawal ($300, 55 days ago) [Signal-Based]", () => testActions.CreateMovementAsync(300m, Binnaculum.Core.Models.BrokerMovementType.Withdrawal, -55))
                .AddSignalWaitStep("Wait for Second Movement Signals", TimeSpan.FromSeconds(10), "Movements_Updated", "Snapshots_Updated")
                .AddAsyncStep("Create Historical Withdrawal ($300, 50 days ago) [Signal-Based]", () => testActions.CreateMovementAsync(300m, Binnaculum.Core.Models.BrokerMovementType.Withdrawal, -50))
                .AddSignalWaitStep("Wait for Third Movement Signals", TimeSpan.FromSeconds(10), "Movements_Updated", "Snapshots_Updated")
                .AddAsyncStep("Create Historical Deposit ($600, 10 days ago) [Signal-Based]", () => testActions.CreateMovementAsync(600m, Binnaculum.Core.Models.BrokerMovementType.Deposit, -10))
                .AddSignalWaitStep("Wait for Final Movement Signals", TimeSpan.FromSeconds(10), "Movements_Updated", "Snapshots_Updated")
                .AddSyncStep("Stop Reactive Stream Observation", () =>
                {
                    ReactiveTestVerifications.StopObserving();
                    return (true, "Stopped observing reactive streams");
                })
                .AddVerificationStep("Verify Movements Stream", ReactiveTestVerifications.VerifyMovementsStream)
                .AddVerificationStep("Verify BrokerAccount + Multiple Movements", ReactiveTestVerifications.VerifyBrokerAccountWithMultipleMovements)
                .AddVerificationStep("Verify Multiple Movements Snapshots", ReactiveTestVerifications.VerifyMultipleMovementsSnapshots));
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
                .AddVerificationStep("Find Tastytrade Broker", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindTastytradeBroker();
                    if (success) testRunner.SetTastytradeId(id);
                    return (success, details, error);
                })
                .AddCustomStep(new OptionsImportIntegrationTest(testRunner.GetExecutionContext())));
        }

        /// <summary>
        /// Options Import Integration Signal-Based Test - Reactive version using signal-based testing approach
        /// </summary>
        private static void RegisterOptionsImportIntegrationSignalBasedTest(TestDiscoveryService discoveryService, TestRunner testRunner, TestActions testActions)
        {
            discoveryService.RegisterTest(() => TestScenarioBuilder.Create()
                .Named("Options Import Integration Signal-Based Validation")
                .WithDescription("Signal-based reactive options import - waits for actual reactive signals during import workflow")
                .WithTags(TestTags.Integration, TestTags.Financial, TestTags.Import, TestTags.Options, TestTags.Reactive)
                .AddReactiveOptionsImportSetup(testRunner)
                .AddVerificationStep("Find Tastytrade Broker", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindTastytradeBroker();
                    if (success) testRunner.SetTastytradeId(id);
                    return (success, details, error);
                })
                .AddCustomStep(new ReactiveOptionsImportIntegrationTest(testRunner.GetExecutionContext())));
        }

        /// <summary>
        /// Deposits and Withdrawals Integration Test - Validates money movements import and snapshot calculations
        /// </summary>
        private static void RegisterDepositsWithdrawalsIntegrationTest(TestDiscoveryService discoveryService, TestRunner testRunner, TestActions testActions)
        {
            discoveryService.RegisterTest(() => TestScenarioBuilder.Create()
                .Named("Deposits & Withdrawals Integration Test")
                .WithDescription("Signal-based reactive test for deposits/withdrawals import. Expected: 20 movements, $19,363.40 deposited, $25.00 withdrawn")
                .WithTags(TestTags.Integration, TestTags.Financial, TestTags.Import, TestTags.Reactive)
                .AddReactiveOptionsImportSetup(testRunner)
                .AddVerificationStep("Find Tastytrade Broker", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindTastytradeBroker();
                    if (success) testRunner.SetTastytradeId(id);
                    return (success, details, error);
                })
                .AddCustomStep(new ReactiveDepositsWithdrawalsIntegrationTest(testRunner.GetExecutionContext())));
        }

        /// <summary>
        /// Tastytrade Import Integration Test - Comprehensive SPX options import validation with ticker-specific analysis
        /// </summary>
        private static void RegisterTastytradeImportIntegrationTest(TestDiscoveryService discoveryService, TestRunner testRunner, TestActions testActions)
        {
            discoveryService.RegisterTest(() => TestScenarioBuilder.Create()
                .Named("Tastytrade Import Integration Test")
                .WithDescription("Comprehensive SPX options import workflow with ticker validation. Expected: $164 commissions, $244 fees, $822 realized")
                .WithTags(TestTags.Integration, TestTags.Financial, TestTags.Import, TestTags.Options, TestTags.SPX)
                .AddCommonSetup(testRunner)
                .AddDelay("Wait for reactive collections", TimeSpan.FromMilliseconds(300))
                .AddVerificationStep("Find Tastytrade Broker", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindTastytradeBroker();
                    if (success) testRunner.SetTastytradeId(id);
                    return (success, details, error);
                })
                .AddCustomStep(new TastytradeImportIntegrationTest(testRunner.GetExecutionContext())));
        }

        /// <summary>
        /// TSLL Import Integration Test - Comprehensive multi-asset import validation with ticker-specific analysis
        /// </summary>
        private static void RegisterTsllImportIntegrationTest(TestDiscoveryService discoveryService, TestRunner testRunner, TestActions testActions)
        {
            discoveryService.RegisterTest(() => TestScenarioBuilder.Create()
                .Named("TSLL Import Integration Test")
                .WithDescription("Comprehensive TSLL multi-asset import workflow with ticker validation. Expected: $235 commissions, $80 fees, $69,290 realized")
                .WithTags(TestTags.Integration, TestTags.Financial, TestTags.Import, TestTags.Options, TestTags.Equity, TestTags.Dividend, TestTags.TSLL)
                .AddCommonSetup(testRunner)
                .AddDelay("Wait for reactive collections", TimeSpan.FromMilliseconds(300))
                .AddVerificationStep("Find Tastytrade Broker", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindTastytradeBroker();
                    if (success) testRunner.SetTastytradeId(id);
                    return (success, details, error);
                })
                .AddCustomStep(new TsllImportIntegrationTest(testRunner.GetExecutionContext())));
        }
    }
}