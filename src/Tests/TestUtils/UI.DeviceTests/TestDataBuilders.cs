using static Binnaculum.Core.Models;

namespace Binnaculum.UI.DeviceTests;

/// <summary>
/// Test data builders for creating realistic Binnaculum Core model instances.
/// Provides fluent API for building test data with various financial scenarios.
/// </summary>
public static class TestDataBuilders
{
    #region Broker Builder

    /// <summary>
    /// Builder for creating test Broker instances with realistic data.
    /// </summary>
    public class BrokerBuilder
    {
        private int _id = 1;
        private string _name = "Test Broker";
        private string _image = "test_broker_icon";
        private string _supportedBroker = "TEST_BROKER";

        public BrokerBuilder WithId(int id)
        {
            _id = id;
            return this;
        }

        public BrokerBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public BrokerBuilder WithImage(string image)
        {
            _image = image;
            return this;
        }

        public BrokerBuilder WithSupportedBroker(string supportedBroker)
        {
            _supportedBroker = supportedBroker;
            return this;
        }

        /// <summary>
        /// Creates a broker with Interactive Brokers configuration.
        /// </summary>
        public BrokerBuilder AsInteractiveBrokers()
        {
            return WithName("Interactive Brokers")
                .WithImage("ib_logo")
                .WithSupportedBroker("INTERACTIVE_BROKERS");
        }

        /// <summary>
        /// Creates a broker with Charles Schwab configuration.
        /// </summary>
        public BrokerBuilder AsCharlesSchwab()
        {
            return WithName("Charles Schwab")
                .WithImage("schwab_logo")
                .WithSupportedBroker("CHARLES_SCHWAB");
        }

        /// <summary>
        /// Creates a broker with Fidelity configuration.
        /// </summary>
        public BrokerBuilder AsFidelity()
        {
            return WithName("Fidelity")
                .WithImage("fidelity_logo")
                .WithSupportedBroker("FIDELITY");
        }

        public Broker Build()
        {
            return new Broker
            {
                Id = _id,
                Name = _name,
                Image = _image,
                SupportedBroker = _supportedBroker
            };
        }
    }

    #endregion

    #region BrokerAccount Builder

    /// <summary>
    /// Builder for creating test BrokerAccount instances.
    /// </summary>
    public class BrokerAccountBuilder
    {
        private int _id = 1;
        private Broker _broker = new BrokerBuilder().Build();
        private string _accountNumber = "123456789";

        public BrokerAccountBuilder WithId(int id)
        {
            _id = id;
            return this;
        }

        public BrokerAccountBuilder WithBroker(Broker broker)
        {
            _broker = broker;
            return this;
        }

        public BrokerAccountBuilder WithAccountNumber(string accountNumber)
        {
            _accountNumber = accountNumber;
            return this;
        }

        /// <summary>
        /// Creates a broker account with Interactive Brokers configuration.
        /// </summary>
        public BrokerAccountBuilder WithInteractiveBrokers()
        {
            _broker = new BrokerBuilder().AsInteractiveBrokers().Build();
            return WithAccountNumber("U1234567");
        }

        /// <summary>
        /// Creates a broker account with Charles Schwab configuration.
        /// </summary>
        public BrokerAccountBuilder WithCharlesSchwab()
        {
            _broker = new BrokerBuilder().AsCharlesSchwab().Build();
            return WithAccountNumber("12345678");
        }

        /// <summary>
        /// Creates a broker account with Fidelity configuration.
        /// </summary>
        public BrokerAccountBuilder WithFidelity()
        {
            _broker = new BrokerBuilder().AsFidelity().Build();
            return WithAccountNumber("987654321");
        }

        public BrokerAccount Build()
        {
            return new BrokerAccount
            {
                Id = _id,
                Broker = _broker,
                AccountNumber = _accountNumber
            };
        }
    }

    #endregion

    #region Currency Builder

    /// <summary>
    /// Builder for creating test Currency instances.
    /// </summary>
    public class CurrencyBuilder
    {
        private int _id = 1;
        private string _title = "US Dollar";
        private string _code = "USD";
        private string _symbol = "$";

        public CurrencyBuilder WithId(int id)
        {
            _id = id;
            return this;
        }

        public CurrencyBuilder WithTitle(string title)
        {
            _title = title;
            return this;
        }

        public CurrencyBuilder WithCode(string code)
        {
            _code = code;
            return this;
        }

        public CurrencyBuilder WithSymbol(string symbol)
        {
            _symbol = symbol;
            return this;
        }

        /// <summary>
        /// Creates USD currency configuration.
        /// </summary>
        public CurrencyBuilder AsUSD()
        {
            return WithTitle("US Dollar")
                .WithCode("USD")
                .WithSymbol("$");
        }

        /// <summary>
        /// Creates EUR currency configuration.
        /// </summary>
        public CurrencyBuilder AsEUR()
        {
            return WithTitle("Euro")
                .WithCode("EUR")
                .WithSymbol("€");
        }

        /// <summary>
        /// Creates GBP currency configuration.
        /// </summary>
        public CurrencyBuilder AsGBP()
        {
            return WithTitle("British Pound")
                .WithCode("GBP")
                .WithSymbol("£");
        }

        public Currency Build()
        {
            return new Currency
            {
                Id = _id,
                Title = _title,
                Code = _code,
                Symbol = _symbol
            };
        }
    }

    #endregion

    #region Financial Data Builder

    /// <summary>
    /// Builder for creating test BrokerFinancialSnapshot instances with various financial scenarios.
    /// </summary>
    public class FinancialDataBuilder
    {
        private int _id = 1;
        private DateOnly _date = DateOnly.FromDateTime(DateTime.Today);
        private Broker? _broker = null;
        private BrokerAccount? _brokerAccount = null;
        private Currency _currency = new CurrencyBuilder().AsUSD().Build();
        private int _movementCounter = 0;
        private decimal _realizedGains = 0m;
        private decimal _realizedPercentage = 0m;
        private decimal _unrealizedGains = 0m;
        private decimal _unrealizedGainsPercentage = 0m;
        private decimal _invested = 0m;
        private decimal _commissions = 0m;
        private decimal _fees = 0m;
        private decimal _deposited = 0m;
        private decimal _withdrawn = 0m;
        private decimal _dividendsReceived = 0m;
        private decimal _optionsIncome = 0m;
        private decimal _otherIncome = 0m;
        private bool _openTrades = false;

        public FinancialDataBuilder WithId(int id)
        {
            _id = id;
            return this;
        }

        public FinancialDataBuilder WithDate(DateOnly date)
        {
            _date = date;
            return this;
        }

        public FinancialDataBuilder WithDate(DateTime date)
        {
            _date = DateOnly.FromDateTime(date);
            return this;
        }

        public FinancialDataBuilder WithBroker(Broker broker)
        {
            _broker = broker;
            return this;
        }

        public FinancialDataBuilder WithBrokerAccount(BrokerAccount brokerAccount)
        {
            _brokerAccount = brokerAccount;
            return this;
        }

        public FinancialDataBuilder WithCurrency(Currency currency)
        {
            _currency = currency;
            return this;
        }

        public FinancialDataBuilder WithMovementCounter(int movementCounter)
        {
            _movementCounter = movementCounter;
            return this;
        }

        public FinancialDataBuilder WithRealizedGains(decimal gains, decimal percentage)
        {
            _realizedGains = gains;
            _realizedPercentage = percentage;
            return this;
        }

        public FinancialDataBuilder WithUnrealizedGains(decimal gains, decimal percentage)
        {
            _unrealizedGains = gains;
            _unrealizedGainsPercentage = percentage;
            return this;
        }

        public FinancialDataBuilder WithInvested(decimal invested)
        {
            _invested = invested;
            return this;
        }

        public FinancialDataBuilder WithCommissions(decimal commissions)
        {
            _commissions = commissions;
            return this;
        }

        public FinancialDataBuilder WithFees(decimal fees)
        {
            _fees = fees;
            return this;
        }

        public FinancialDataBuilder WithDeposited(decimal deposited)
        {
            _deposited = deposited;
            return this;
        }

        public FinancialDataBuilder WithWithdrawn(decimal withdrawn)
        {
            _withdrawn = withdrawn;
            return this;
        }

        public FinancialDataBuilder WithDividendsReceived(decimal dividends)
        {
            _dividendsReceived = dividends;
            return this;
        }

        public FinancialDataBuilder WithOptionsIncome(decimal income)
        {
            _optionsIncome = income;
            return this;
        }

        public FinancialDataBuilder WithOtherIncome(decimal income)
        {
            _otherIncome = income;
            return this;
        }

        public FinancialDataBuilder WithOpenTrades(bool hasOpenTrades)
        {
            _openTrades = hasOpenTrades;
            return this;
        }

        /// <summary>
        /// Creates a profitable scenario with gains and dividends.
        /// </summary>
        public FinancialDataBuilder AsProfitableScenario()
        {
            return WithInvested(10000m)
                .WithRealizedGains(1500m, 15.0m)
                .WithUnrealizedGains(800m, 8.0m)
                .WithDividendsReceived(250m)
                .WithOptionsIncome(150m)
                .WithCommissions(25m)
                .WithFees(10m)
                .WithDeposited(10000m)
                .WithMovementCounter(45);
        }

        /// <summary>
        /// Creates a loss scenario with negative gains.
        /// </summary>
        public FinancialDataBuilder AsLossScenario()
        {
            return WithInvested(15000m)
                .WithRealizedGains(-2000m, -13.33m)
                .WithUnrealizedGains(-1200m, -8.0m)
                .WithDividendsReceived(100m)
                .WithCommissions(75m)
                .WithFees(25m)
                .WithDeposited(15000m)
                .WithMovementCounter(38)
                .WithOpenTrades(true);
        }

        /// <summary>
        /// Creates a mixed scenario with both gains and losses.
        /// </summary>
        public FinancialDataBuilder AsMixedScenario()
        {
            return WithInvested(25000m)
                .WithRealizedGains(500m, 2.0m)
                .WithUnrealizedGains(-300m, -1.2m)
                .WithDividendsReceived(400m)
                .WithOptionsIncome(200m)
                .WithCommissions(120m)
                .WithFees(45m)
                .WithDeposited(20000m)
                .WithWithdrawn(5000m)
                .WithMovementCounter(67)
                .WithOpenTrades(true);
        }

        /// <summary>
        /// Creates a high-volume trading scenario.
        /// </summary>
        public FinancialDataBuilder AsHighVolumeScenario()
        {
            return WithInvested(100000m)
                .WithRealizedGains(8500m, 8.5m)
                .WithUnrealizedGains(2100m, 2.1m)
                .WithDividendsReceived(1200m)
                .WithOptionsIncome(3500m)
                .WithCommissions(850m)
                .WithFees(320m)
                .WithDeposited(95000m)
                .WithWithdrawn(25000m)
                .WithMovementCounter(245)
                .WithOpenTrades(true);
        }

        public BrokerFinancialSnapshot Build()
        {
            return new BrokerFinancialSnapshot
            {
                Id = _id,
                Date = _date,
                Broker = _broker,
                BrokerAccount = _brokerAccount,
                Currency = _currency,
                MovementCounter = _movementCounter,
                RealizedGains = _realizedGains,
                RealizedPercentage = _realizedPercentage,
                UnrealizedGains = _unrealizedGains,
                UnrealizedGainsPercentage = _unrealizedGainsPercentage,
                Invested = _invested,
                Commissions = _commissions,
                Fees = _fees,
                Deposited = _deposited,
                Withdrawn = _withdrawn,
                DividendsReceived = _dividendsReceived,
                OptionsIncome = _optionsIncome,
                OtherIncome = _otherIncome,
                OpenTrades = _openTrades
            };
        }
    }

    #endregion

    #region Overview Snapshot Builder

    /// <summary>
    /// Builder for creating test OverviewSnapshot instances.
    /// </summary>
    public class OverviewSnapshotBuilder
    {
        private OverviewSnapshotType _type = OverviewSnapshotType.Empty;
        private InvestmentOverviewSnapshot? _investmentOverview = null;
        private BrokerSnapshot? _broker = null;
        private BankSnapshot? _bank = null;
        private BrokerAccountSnapshot? _brokerAccount = null;
        private BankAccountSnapshot? _bankAccount = null;

        public OverviewSnapshotBuilder WithType(OverviewSnapshotType type)
        {
            _type = type;
            return this;
        }

        public OverviewSnapshotBuilder WithInvestmentOverview(InvestmentOverviewSnapshot investmentOverview)
        {
            _investmentOverview = investmentOverview;
            _type = OverviewSnapshotType.InvestmentOverview;
            return this;
        }

        public OverviewSnapshotBuilder WithBroker(BrokerSnapshot broker)
        {
            _broker = broker;
            _type = OverviewSnapshotType.Broker;
            return this;
        }

        public OverviewSnapshotBuilder WithBank(BankSnapshot bank)
        {
            _bank = bank;
            _type = OverviewSnapshotType.Bank;
            return this;
        }

        public OverviewSnapshotBuilder WithBrokerAccount(BrokerAccountSnapshot brokerAccount)
        {
            _brokerAccount = brokerAccount;
            _type = OverviewSnapshotType.BrokerAccount;
            return this;
        }

        public OverviewSnapshotBuilder WithBankAccount(BankAccountSnapshot bankAccount)
        {
            _bankAccount = bankAccount;
            _type = OverviewSnapshotType.BankAccount;
            return this;
        }

        /// <summary>
        /// Creates an investment overview snapshot with realistic data.
        /// </summary>
        public OverviewSnapshotBuilder AsInvestmentOverview(
            decimal portfoliosValue = 50000m,
            decimal realizedGains = 3500m,
            decimal realizedPercentage = 7.0m,
            decimal invested = 45000m,
            decimal commissions = 150m,
            decimal fees = 75m)
        {
            _investmentOverview = new InvestmentOverviewSnapshot
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                PortfoliosValue = portfoliosValue,
                RealizedGains = realizedGains,
                RealizedPercentage = realizedPercentage,
                Invested = invested,
                Commissions = commissions,
                Fees = fees
            };
            _type = OverviewSnapshotType.InvestmentOverview;
            return this;
        }

        /// <summary>
        /// Creates a broker account snapshot with realistic data.
        /// </summary>
        public OverviewSnapshotBuilder AsBrokerAccount(
            BrokerAccount? brokerAccount = null,
            decimal portfolioValue = 25000m,
            BrokerFinancialSnapshot? financial = null)
        {
            brokerAccount ??= new BrokerAccountBuilder().WithInteractiveBrokers().Build();
            financial ??= new FinancialDataBuilder()
                .WithBrokerAccount(brokerAccount)
                .AsProfitableScenario()
                .Build();

            _brokerAccount = new BrokerAccountSnapshot
            {
                Date = DateOnly.FromDateTime(DateTime.Today),
                BrokerAccount = brokerAccount,
                PortfolioValue = portfolioValue,
                Financial = financial,
                FinancialOtherCurrencies = new List<BrokerFinancialSnapshot>()
            };
            _type = OverviewSnapshotType.BrokerAccount;
            return this;
        }

        public OverviewSnapshot Build()
        {
            return new OverviewSnapshot
            {
                Type = _type,
                InvestmentOverview = _investmentOverview,
                Broker = _broker,
                Bank = _bank,
                BrokerAccount = _brokerAccount,
                BankAccount = _bankAccount
            };
        }
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Factory method to create a new BrokerBuilder.
    /// </summary>
    /// <returns>A new BrokerBuilder instance</returns>
    public static BrokerBuilder CreateBroker() => new BrokerBuilder();

    /// <summary>
    /// Factory method to create a new BrokerAccountBuilder.
    /// </summary>
    /// <returns>A new BrokerAccountBuilder instance</returns>
    public static BrokerAccountBuilder CreateBrokerAccount() => new BrokerAccountBuilder();

    /// <summary>
    /// Factory method to create a new CurrencyBuilder.
    /// </summary>
    /// <returns>A new CurrencyBuilder instance</returns>
    public static CurrencyBuilder CreateCurrency() => new CurrencyBuilder();

    /// <summary>
    /// Factory method to create a new FinancialDataBuilder.
    /// </summary>
    /// <returns>A new FinancialDataBuilder instance</returns>
    public static FinancialDataBuilder CreateFinancialData() => new FinancialDataBuilder();

    /// <summary>
    /// Factory method to create a new OverviewSnapshotBuilder.
    /// </summary>
    /// <returns>A new OverviewSnapshotBuilder instance</returns>
    public static OverviewSnapshotBuilder CreateOverviewSnapshot() => new OverviewSnapshotBuilder();

    #endregion
}