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
            RegisterOverviewReactiveTest(discoveryService, testRunner);
            RegisterBrokerAccountCreationReactiveTest(discoveryService, testRunner, testActions);
            RegisterBrokerAccountDepositReactiveTest(discoveryService, testRunner, testActions);
            RegisterBrokerAccountMultipleMovementsReactiveTest(discoveryService, testRunner, testActions);
            RegisterBrokerAccountMultipleMovementsSignalBasedTest(discoveryService, testRunner, testActions);
            RegisterOptionsImportIntegrationSignalBasedTest(discoveryService, testRunner, testActions);
            RegisterDepositsWithdrawalsIntegrationTest(discoveryService, testRunner, testActions);
            RegisterTastytradeImportIntegrationTest(discoveryService, testRunner, testActions);
            RegisterTsllImportIntegrationTest(discoveryService, testRunner, testActions);
            RegisterPfizerImportIntegrationTest(discoveryService, testRunner, testActions);
        }

        /// <summary>
        /// Overview Reactive Test - Validates reactive streams during Overview operations using signal-based testing
        /// </summary>
        private static void RegisterOverviewReactiveTest(TestDiscoveryService discoveryService, TestRunner testRunner)
        {
            discoveryService.RegisterTest(() => TestScenarioBuilder.Create()
                .Named("Overview Reactive Validation")
                .WithDescription("Validates reactive stream emissions during Overview.InitDatabase() and Overview.LoadData() operations using signal-based approach")
                .WithTags(TestTags.Overview, TestTags.Database, TestTags.Collection, TestTags.Reactive)
                .AddAsyncStep("Wipe All Data for Testing", () => testRunner.Actions.WipeDataForTestingAsync())
                .AddSyncStep("Initialize MAUI Platform Services", () => testRunner.Actions.InitializePlatformServicesAsync().Result)
                .AddSyncStep("Start Reactive Stream Observation [Overview]", () =>
                {
                    ReactiveTestVerifications.StartObserving();
                    return (true, "Started observing reactive streams for Overview");
                })
                .AddSyncStep("Prepare to Expect Database Initialization Signal", () =>
                {
                    ReactiveTestVerifications.ExpectSignals("Database_Initialized");
                    return (true, "Ready to capture Database_Initialized signal");
                })
                .AddAsyncStep("Overview.InitDatabase() [Reactive]", () => testRunner.Actions.InitializeDatabaseAsync())
                .AddSignalWaitStepOnly("Wait for Database Initialization Signal", TimeSpan.FromSeconds(10), "Database_Initialized")
                .AddDelay("Allow database state to settle after initialization", TimeSpan.FromMilliseconds(500))
                .AddSyncStep("Prepare to Expect Data Loaded Signals", () =>
                {
                    ReactiveTestVerifications.ExpectSignals("Snapshots_Updated", "Accounts_Updated", "Data_Loaded");
                    return (true, "Ready to capture data loading signals");
                })
                .AddAsyncStep("Overview.LoadData() [Reactive]", () => testRunner.Actions.LoadDataAsync())
                .AddSignalWaitStepOnly("Wait for Data Loaded Signals", TimeSpan.FromSeconds(10), "Snapshots_Updated", "Accounts_Updated", "Data_Loaded")
                .AddSyncStep("Stop Reactive Stream Observation", () =>
                {
                    ReactiveTestVerifications.StopObserving();
                    return (true, "Stopped observing reactive streams");
                })
                .AddVerificationStep("Verify Overview.Data Stream", ReactiveTestVerifications.VerifyOverviewDataStream)
                .AddVerificationStep("Verify Currencies Stream", ReactiveTestVerifications.VerifyCurrenciesStream)
                .AddVerificationStep("Verify Brokers Stream", ReactiveTestVerifications.VerifyBrokersStream)
                .AddVerificationStep("Verify Tickers Stream", ReactiveTestVerifications.VerifyTickersStream)
                .AddVerificationStep("Verify Snapshots Stream", ReactiveTestVerifications.VerifySnapshotsStream)
                .AddVerificationStep("Compare with Traditional Test", ReactiveTestVerifications.CompareWithTraditionalTest));
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
                .AddSyncStep("Prepare to Expect Account Creation Signals", () =>
                {
                    ReactiveTestVerifications.ExpectSignals("Accounts_Updated", "Snapshots_Updated");
                    return (true, "ExpectSignals called - ready to capture signals");
                })
                .AddAsyncStep("Create BrokerAccount [Signal-Based]", () => testActions.CreateBrokerAccountAsync("Signal-Based Testing"))
                .AddSignalWaitStepOnly("Wait for Account Creation Signals", TimeSpan.FromSeconds(10), "Accounts_Updated", "Snapshots_Updated")
                .AddVerificationStep("Find Created BrokerAccount", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindCreatedBrokerAccount(testRunner.GetTastytradeId());
                    if (success) testRunner.SetBrokerAccountId(id);
                    return (success, details, error);
                })
                .AddSyncStep("Prepare to Expect First Movement Signals", () =>
                {
                    ReactiveTestVerifications.ExpectSignals("Movements_Updated", "Snapshots_Updated");
                    return (true, "ExpectSignals called for first movement");
                })
                .AddAsyncStep("Create Historical Deposit ($1200, 60 days ago) [Signal-Based]", () => testActions.CreateMovementAsync(1200m, Binnaculum.Core.Models.BrokerMovementType.Deposit, -60))
                .AddSignalWaitStepOnly("Wait for First Movement Signals", TimeSpan.FromSeconds(10), "Movements_Updated", "Snapshots_Updated")
                .AddSyncStep("Prepare to Expect Second Movement Signals", () =>
                {
                    ReactiveTestVerifications.ExpectSignals("Movements_Updated", "Snapshots_Updated");
                    return (true, "ExpectSignals called for second movement");
                })
                .AddAsyncStep("Create Historical Withdrawal ($300, 55 days ago) [Signal-Based]", () => testActions.CreateMovementAsync(300m, Binnaculum.Core.Models.BrokerMovementType.Withdrawal, -55))
                .AddSignalWaitStepOnly("Wait for Second Movement Signals", TimeSpan.FromSeconds(10), "Movements_Updated", "Snapshots_Updated")
                .AddSyncStep("Prepare to Expect Third Movement Signals", () =>
                {
                    ReactiveTestVerifications.ExpectSignals("Movements_Updated", "Snapshots_Updated");
                    return (true, "ExpectSignals called for third movement");
                })
                .AddAsyncStep("Create Historical Withdrawal ($300, 50 days ago) [Signal-Based]", () => testActions.CreateMovementAsync(300m, Binnaculum.Core.Models.BrokerMovementType.Withdrawal, -50))
                .AddSignalWaitStepOnly("Wait for Third Movement Signals", TimeSpan.FromSeconds(10), "Movements_Updated", "Snapshots_Updated")
                .AddSyncStep("Prepare to Expect Final Movement Signals", () =>
                {
                    ReactiveTestVerifications.ExpectSignals("Movements_Updated", "Snapshots_Updated");
                    return (true, "ExpectSignals called for final movement");
                })
                .AddAsyncStep("Create Historical Deposit ($600, 10 days ago) [Signal-Based]", () => testActions.CreateMovementAsync(600m, Binnaculum.Core.Models.BrokerMovementType.Deposit, -10))
                .AddSignalWaitStepOnly("Wait for Final Movement Signals", TimeSpan.FromSeconds(10), "Movements_Updated", "Snapshots_Updated")
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

        /// <summary>
        /// Pfizer Import Integration Test - Validates Pfizer (PFE) options import with 4 movements
        /// </summary>
        private static void RegisterPfizerImportIntegrationTest(TestDiscoveryService discoveryService, TestRunner testRunner, TestActions testActions)
        {
            discoveryService.RegisterTest(() => TestScenarioBuilder.Create()
                .Named("Pfizer Import Integration Test")
                .WithDescription("Reactive test for Pfizer (PFE) options import. Expected: 4 movements, $175.52 realized")
                .WithTags(TestTags.Integration, TestTags.Financial, TestTags.Import, TestTags.Options, TestTags.Reactive)
                .AddReactiveOptionsImportSetup(testRunner)
                .AddVerificationStep("Find Tastytrade Broker", () =>
                {
                    var (success, details, error, id) = TestVerifications.FindTastytradeBroker();
                    if (success) testRunner.SetTastytradeId(id);
                    return (success, details, error);
                })
                .AddCustomStep(new ReactivePfizerImportIntegrationTest(testRunner.GetExecutionContext())));
        }
    }
}