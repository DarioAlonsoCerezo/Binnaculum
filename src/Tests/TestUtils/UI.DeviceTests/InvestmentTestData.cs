using static Binnaculum.Core.Models;

namespace Binnaculum.UI.DeviceTests;

/// <summary>
/// Realistic investment test data scenarios for comprehensive device testing.
/// Provides pre-configured financial data representing various market conditions and trading strategies.
/// </summary>
public static class InvestmentTestData
{
    #region Individual Stock Scenarios

    /// <summary>
    /// Apple stock scenario - profitable long-term investment with dividends.
    /// </summary>
    public static class AppleStock
    {
        public static readonly BrokerFinancialSnapshot ProfitableScenario = TestDataBuilders.CreateFinancialData()
            .WithId(1001)
            .WithDate(new DateTime(2024, 12, 31))
            .WithBrokerAccount(TestDataBuilders.CreateBrokerAccount().WithInteractiveBrokers().Build())
            .WithCurrency(TestDataBuilders.CreateCurrency().AsUSD().Build())
            .WithInvested(15000m)
            .WithRealizedGains(2500m, 16.67m)
            .WithUnrealizedGains(1200m, 8.0m)
            .WithDividendsReceived(180m)
            .WithCommissions(15m)
            .WithFees(5m)
            .WithDeposited(15000m)
            .WithMovementCounter(12) // 12 transactions over time
            .WithOpenTrades(true)
            .Build();

        public static readonly BrokerFinancialSnapshot LossScenario = TestDataBuilders.CreateFinancialData()
            .WithId(1002)
            .WithDate(new DateTime(2024, 12, 31))
            .WithBrokerAccount(TestDataBuilders.CreateBrokerAccount().WithInteractiveBrokers().Build())
            .WithCurrency(TestDataBuilders.CreateCurrency().AsUSD().Build())
            .WithInvested(20000m)
            .WithRealizedGains(-3000m, -15.0m)
            .WithUnrealizedGains(-800m, -4.0m)
            .WithDividendsReceived(120m)
            .WithCommissions(25m)
            .WithFees(8m)
            .WithDeposited(20000m)
            .WithMovementCounter(8) // Fewer transactions due to losses
            .WithOpenTrades(true)
            .Build();
    }

    /// <summary>
    /// Tesla stock scenario - volatile growth stock with high gains and losses.
    /// </summary>
    public static class TeslaStock
    {
        public static readonly BrokerFinancialSnapshot VolatileGains = TestDataBuilders.CreateFinancialData()
            .WithId(2001)
            .WithDate(new DateTime(2024, 12, 31))
            .WithBrokerAccount(TestDataBuilders.CreateBrokerAccount().WithCharlesSchwab().Build())
            .WithCurrency(TestDataBuilders.CreateCurrency().AsUSD().Build())
            .WithInvested(10000m)
            .WithRealizedGains(4500m, 45.0m)
            .WithUnrealizedGains(-600m, -6.0m)
            .WithDividendsReceived(0m) // Tesla doesn't pay dividends
            .WithCommissions(35m)
            .WithFees(12m)
            .WithDeposited(10000m)
            .WithMovementCounter(25) // High trading frequency
            .WithOpenTrades(true)
            .Build();

        public static readonly BrokerFinancialSnapshot VolatileLosses = TestDataBuilders.CreateFinancialData()
            .WithId(2002)
            .WithDate(new DateTime(2024, 12, 31))
            .WithBrokerAccount(TestDataBuilders.CreateBrokerAccount().WithCharlesSchwab().Build())
            .WithCurrency(TestDataBuilders.CreateCurrency().AsUSD().Build())
            .WithInvested(12000m)
            .WithRealizedGains(-3200m, -26.67m)
            .WithUnrealizedGains(-1800m, -15.0m)
            .WithDividendsReceived(0m)
            .WithCommissions(45m)
            .WithFees(18m)
            .WithDeposited(12000m)
            .WithMovementCounter(30) // High trading frequency during volatile period
            .WithOpenTrades(true)
            .Build();
    }

    /// <summary>
    /// Microsoft stock scenario - stable dividend-paying tech stock.
    /// </summary>
    public static class MicrosoftStock
    {
        public static readonly BrokerFinancialSnapshot StableDividends = TestDataBuilders.CreateFinancialData()
            .WithId(3001)
            .WithDate(new DateTime(2024, 12, 31))
            .WithBrokerAccount(TestDataBuilders.CreateBrokerAccount().WithFidelity().Build())
            .WithCurrency(TestDataBuilders.CreateCurrency().AsUSD().Build())
            .WithInvested(25000m)
            .WithRealizedGains(3750m, 15.0m)
            .WithUnrealizedGains(1250m, 5.0m)
            .WithDividendsReceived(650m) // Consistent quarterly dividends
            .WithCommissions(20m)
            .WithFees(8m)
            .WithDeposited(25000m)
            .WithMovementCounter(16) // Moderate trading frequency
            .WithOpenTrades(true)
            .Build();
    }

    #endregion

    #region Options Trading Scenarios

    /// <summary>
    /// Options trading scenarios for covered calls and cash-secured puts.
    /// </summary>
    public static class OptionsTrading
    {
        public static readonly BrokerFinancialSnapshot CoveredCallsSuccess = TestDataBuilders.CreateFinancialData()
            .WithId(4001)
            .WithDate(new DateTime(2024, 12, 31))
            .WithBrokerAccount(TestDataBuilders.CreateBrokerAccount().WithInteractiveBrokers().Build())
            .WithCurrency(TestDataBuilders.CreateCurrency().AsUSD().Build())
            .WithInvested(50000m)
            .WithRealizedGains(4200m, 8.4m)
            .WithUnrealizedGains(800m, 1.6m)
            .WithDividendsReceived(420m)
            .WithOptionsIncome(2800m) // Premium from selling covered calls
            .WithCommissions(180m) // Higher commissions due to options
            .WithFees(25m)
            .WithDeposited(50000m)
            .WithMovementCounter(65) // Many options transactions
            .WithOpenTrades(true)
            .Build();

        public static readonly BrokerFinancialSnapshot CashSecuredPuts = TestDataBuilders.CreateFinancialData()
            .WithId(4002)
            .WithDate(new DateTime(2024, 12, 31))
            .WithBrokerAccount(TestDataBuilders.CreateBrokerAccount().WithInteractiveBrokers().Build())
            .WithCurrency(TestDataBuilders.CreateCurrency().AsUSD().Build())
            .WithInvested(30000m)
            .WithRealizedGains(1800m, 6.0m)
            .WithUnrealizedGains(-450m, -1.5m)
            .WithDividendsReceived(0m) // No dividends from cash positions
            .WithOptionsIncome(2100m) // Premium from selling puts
            .WithCommissions(125m)
            .WithFees(18m)
            .WithDeposited(30000m)
            .WithMovementCounter(42) // Regular put selling
            .WithOpenTrades(true)
            .Build();
    }

    #endregion

    #region Multi-Currency Scenarios

    /// <summary>
    /// International investing scenarios with currency exposure.
    /// </summary>
    public static class InternationalInvesting
    {
        public static readonly BrokerFinancialSnapshot EuropeanMarkets = TestDataBuilders.CreateFinancialData()
            .WithId(5001)
            .WithDate(new DateTime(2024, 12, 31))
            .WithBrokerAccount(TestDataBuilders.CreateBrokerAccount().WithInteractiveBrokers().Build())
            .WithCurrency(TestDataBuilders.CreateCurrency().AsEUR().Build())
            .WithInvested(18000m) // EUR amount
            .WithRealizedGains(2160m, 12.0m)
            .WithUnrealizedGains(-540m, -3.0m)
            .WithDividendsReceived(360m)
            .WithCommissions(85m) // Higher international commissions
            .WithFees(35m) // Currency conversion fees
            .WithDeposited(18000m)
            .WithMovementCounter(28)
            .WithOpenTrades(true)
            .Build();

        public static readonly BrokerFinancialSnapshot UKMarkets = TestDataBuilders.CreateFinancialData()
            .WithId(5002)
            .WithDate(new DateTime(2024, 12, 31))
            .WithBrokerAccount(TestDataBuilders.CreateBrokerAccount().WithInteractiveBrokers().Build())
            .WithCurrency(TestDataBuilders.CreateCurrency().AsGBP().Build())
            .WithInvested(15000m) // GBP amount
            .WithRealizedGains(1350m, 9.0m)
            .WithUnrealizedGains(225m, 1.5m)
            .WithDividendsReceived(480m) // Higher UK dividend yields
            .WithCommissions(95m)
            .WithFees(42m) // Brexit-related trading costs
            .WithDeposited(15000m)
            .WithMovementCounter(22)
            .WithOpenTrades(true)
            .Build();
    }

    #endregion

    #region Portfolio Composition Scenarios

    /// <summary>
    /// Diversified portfolio scenarios representing different investment strategies.
    /// </summary>
    public static class DiversifiedPortfolios
    {
        public static readonly BrokerFinancialSnapshot ConservativePortfolio = TestDataBuilders.CreateFinancialData()
            .WithId(6001)
            .WithDate(new DateTime(2024, 12, 31))
            .WithBrokerAccount(TestDataBuilders.CreateBrokerAccount().WithFidelity().Build())
            .WithCurrency(TestDataBuilders.CreateCurrency().AsUSD().Build())
            .WithInvested(100000m) // Large conservative investment
            .WithRealizedGains(4500m, 4.5m) // Modest but consistent gains
            .WithUnrealizedGains(1800m, 1.8m)
            .WithDividendsReceived(2800m) // Focus on dividend income
            .WithCommissions(125m)
            .WithFees(35m)
            .WithDeposited(95000m)
            .WithWithdrawn(5000m) // Some profit taking
            .WithMovementCounter(85) // Regular rebalancing
            .WithOpenTrades(true)
            .Build();

        public static readonly BrokerFinancialSnapshot AggressiveGrowth = TestDataBuilders.CreateFinancialData()
            .WithId(6002)
            .WithDate(new DateTime(2024, 12, 31))
            .WithBrokerAccount(TestDataBuilders.CreateBrokerAccount().WithCharlesSchwab().Build())
            .WithCurrency(TestDataBuilders.CreateCurrency().AsUSD().Build())
            .WithInvested(75000m)
            .WithRealizedGains(18750m, 25.0m) // High growth gains
            .WithUnrealizedGains(-3750m, -5.0m) // Some recent volatility
            .WithDividendsReceived(380m) // Low dividends from growth stocks
            .WithCommissions(285m) // Higher due to active trading
            .WithFees(95m)
            .WithDeposited(75000m)
            .WithMovementCounter(156) // Very active trading
            .WithOpenTrades(true)
            .Build();

        public static readonly BrokerFinancialSnapshot BalancedPortfolio = TestDataBuilders.CreateFinancialData()
            .WithId(6003)
            .WithDate(new DateTime(2024, 12, 31))
            .WithBrokerAccount(TestDataBuilders.CreateBrokerAccount().WithInteractiveBrokers().Build())
            .WithCurrency(TestDataBuilders.CreateCurrency().AsUSD().Build())
            .WithInvested(60000m)
            .WithRealizedGains(7200m, 12.0m) // Balanced growth
            .WithUnrealizedGains(1200m, 2.0m)
            .WithDividendsReceived(1350m) // Mix of dividend and growth
            .WithOptionsIncome(850m) // Some covered call writing
            .WithCommissions(195m)
            .WithFees(48m)
            .WithDeposited(58000m)
            .WithWithdrawn(2000m)
            .WithMovementCounter(118)
            .WithOpenTrades(true)
            .Build();
    }

    #endregion

    #region Market Condition Scenarios

    /// <summary>
    /// Scenarios representing different market conditions and their impact on portfolios.
    /// </summary>
    public static class MarketConditions
    {
        public static readonly BrokerFinancialSnapshot BullMarket = TestDataBuilders.CreateFinancialData()
            .WithId(7001)
            .WithDate(new DateTime(2024, 12, 31))
            .WithBrokerAccount(TestDataBuilders.CreateBrokerAccount().WithCharlesSchwab().Build())
            .WithCurrency(TestDataBuilders.CreateCurrency().AsUSD().Build())
            .WithInvested(45000m)
            .WithRealizedGains(11250m, 25.0m) // Strong bull market gains
            .WithUnrealizedGains(2250m, 5.0m) // Continued upward momentum
            .WithDividendsReceived(540m)
            .WithCommissions(165m)
            .WithFees(28m)
            .WithDeposited(40000m)
            .WithWithdrawn(5000m) // Taking some profits
            .WithMovementCounter(92)
            .WithOpenTrades(true)
            .Build();

        public static readonly BrokerFinancialSnapshot BearMarket = TestDataBuilders.CreateFinancialData()
            .WithId(7002)
            .WithDate(new DateTime(2024, 12, 31))
            .WithBrokerAccount(TestDataBuilders.CreateBrokerAccount().WithFidelity().Build())
            .WithCurrency(TestDataBuilders.CreateCurrency().AsUSD().Build())
            .WithInvested(80000m)
            .WithRealizedGains(-12000m, -15.0m) // Bear market losses
            .WithUnrealizedGains(-8000m, -10.0m) // Continued downward pressure
            .WithDividendsReceived(1200m) // Some dividend income remains
            .WithCommissions(245m) // Higher due to defensive trading
            .WithFees(68m)
            .WithDeposited(85000m) // Adding to positions during downturn
            .WithWithdrawn(5000m)
            .WithMovementCounter(134) // More active during volatility
            .WithOpenTrades(true)
            .Build();

        public static readonly BrokerFinancialSnapshot SidewaysMarket = TestDataBuilders.CreateFinancialData()
            .WithId(7003)
            .WithDate(new DateTime(2024, 12, 31))
            .WithBrokerAccount(TestDataBuilders.CreateBrokerAccount().WithInteractiveBrokers().Build())
            .WithCurrency(TestDataBuilders.CreateCurrency().AsUSD().Build())
            .WithInvested(35000m)
            .WithRealizedGains(525m, 1.5m) // Minimal capital gains
            .WithUnrealizedGains(-175m, -0.5m) // Flat to slightly negative
            .WithDividendsReceived(1050m) // Focus on income during sideways market
            .WithOptionsIncome(1250m) // Active options strategies
            .WithCommissions(185m)
            .WithFees(32m)
            .WithDeposited(35000m)
            .WithMovementCounter(147) // High activity to generate income
            .WithOpenTrades(true)
            .Build();
    }

    #endregion

    #region Account Size Scenarios

    /// <summary>
    /// Scenarios representing different account sizes and their typical characteristics.
    /// </summary>
    public static class AccountSizes
    {
        public static readonly BrokerFinancialSnapshot SmallAccount = TestDataBuilders.CreateFinancialData()
            .WithId(8001)
            .WithDate(new DateTime(2024, 12, 31))
            .WithBrokerAccount(TestDataBuilders.CreateBrokerAccount().WithCharlesSchwab().Build())
            .WithCurrency(TestDataBuilders.CreateCurrency().AsUSD().Build())
            .WithInvested(5000m) // Small retail account
            .WithRealizedGains(350m, 7.0m)
            .WithUnrealizedGains(150m, 3.0m)
            .WithDividendsReceived(45m)
            .WithCommissions(25m) // Relatively high impact on small account
            .WithFees(8m)
            .WithDeposited(5000m)
            .WithMovementCounter(18) // Limited trading due to account size
            .WithOpenTrades(true)
            .Build();

        public static readonly BrokerFinancialSnapshot MediumAccount = TestDataBuilders.CreateFinancialData()
            .WithId(8002)
            .WithDate(new DateTime(2024, 12, 31))
            .WithBrokerAccount(TestDataBuilders.CreateBrokerAccount().WithInteractiveBrokers().Build())
            .WithCurrency(TestDataBuilders.CreateCurrency().AsUSD().Build())
            .WithInvested(50000m) // Mid-size retail account
            .WithRealizedGains(4500m, 9.0m)
            .WithUnrealizedGains(1500m, 3.0m)
            .WithDividendsReceived(750m)
            .WithOptionsIncome(420m) // Can engage in options strategies
            .WithCommissions(145m)
            .WithFees(32m)
            .WithDeposited(48000m)
            .WithWithdrawn(2000m)
            .WithMovementCounter(78)
            .WithOpenTrades(true)
            .Build();

        public static readonly BrokerFinancialSnapshot LargeAccount = TestDataBuilders.CreateFinancialData()
            .WithId(8003)
            .WithDate(new DateTime(2024, 12, 31))
            .WithBrokerAccount(TestDataBuilders.CreateBrokerAccount().WithFidelity().Build())
            .WithCurrency(TestDataBuilders.CreateCurrency().AsUSD().Build())
            .WithInvested(500000m) // Large retail/small institutional account
            .WithRealizedGains(45000m, 9.0m)
            .WithUnrealizedGains(15000m, 3.0m)
            .WithDividendsReceived(12500m) // Significant dividend income
            .WithOptionsIncome(8500m) // Advanced options strategies
            .WithCommissions(875m) // Higher absolute but lower relative cost
            .WithFees(185m)
            .WithDeposited(480000m)
            .WithWithdrawn(20000m)
            .WithOtherIncome(2500m) // Interest, lending income, etc.
            .WithMovementCounter(285) // Very active trading
            .WithOpenTrades(true)
            .Build();
    }

    #endregion

    #region Overview Snapshot Scenarios

    /// <summary>
    /// Complete overview snapshots representing different portfolio states.
    /// </summary>
    public static class OverviewSnapshots
    {
        public static readonly OverviewSnapshot BeginnerInvestor = TestDataBuilders.CreateOverviewSnapshot()
            .AsInvestmentOverview(
                portfoliosValue: 8500m,
                realizedGains: 350m,
                realizedPercentage: 4.29m,
                invested: 8150m,
                commissions: 35m,
                fees: 12m)
            .Build();

        public static readonly OverviewSnapshot ExperiencedInvestor = TestDataBuilders.CreateOverviewSnapshot()
            .AsInvestmentOverview(
                portfoliosValue: 125000m,
                realizedGains: 18750m,
                realizedPercentage: 15.0m,
                invested: 106250m,
                commissions: 485m,
                fees: 125m)
            .Build();

        public static readonly OverviewSnapshot WealthyInvestor = TestDataBuilders.CreateOverviewSnapshot()
            .AsInvestmentOverview(
                portfoliosValue: 850000m,
                realizedGains: 102000m,
                realizedPercentage: 12.0m,
                invested: 748000m,
                commissions: 2850m,
                fees: 780m)
            .Build();

        public static readonly OverviewSnapshot RetirementPortfolio = TestDataBuilders.CreateOverviewSnapshot()
            .AsInvestmentOverview(
                portfoliosValue: 750000m,
                realizedGains: 52500m,
                realizedPercentage: 7.0m,
                invested: 697500m,
                commissions: 1250m,
                fees: 420m)
            .Build();
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Gets a collection of all available test scenarios.
    /// </summary>
    /// <returns>Collection of all financial test scenarios</returns>
    public static IEnumerable<BrokerFinancialSnapshot> GetAllScenarios()
    {
        var scenarios = new List<BrokerFinancialSnapshot>
        {
            // Individual stocks
            AppleStock.ProfitableScenario,
            AppleStock.LossScenario,
            TeslaStock.VolatileGains,
            TeslaStock.VolatileLosses,
            MicrosoftStock.StableDividends,
            
            // Options trading
            OptionsTrading.CoveredCallsSuccess,
            OptionsTrading.CashSecuredPuts,
            
            // International
            InternationalInvesting.EuropeanMarkets,
            InternationalInvesting.UKMarkets,
            
            // Portfolio compositions
            DiversifiedPortfolios.ConservativePortfolio,
            DiversifiedPortfolios.AggressiveGrowth,
            DiversifiedPortfolios.BalancedPortfolio,
            
            // Market conditions
            MarketConditions.BullMarket,
            MarketConditions.BearMarket,
            MarketConditions.SidewaysMarket,
            
            // Account sizes
            AccountSizes.SmallAccount,
            AccountSizes.MediumAccount,
            AccountSizes.LargeAccount
        };

        return scenarios;
    }

    /// <summary>
    /// Gets scenarios filtered by a specific condition.
    /// </summary>
    /// <param name="predicate">Filter condition</param>
    /// <returns>Filtered collection of scenarios</returns>
    public static IEnumerable<BrokerFinancialSnapshot> GetScenariosWhere(Func<BrokerFinancialSnapshot, bool> predicate)
    {
        return GetAllScenarios().Where(predicate);
    }

    /// <summary>
    /// Gets scenarios with profitable outcomes (positive realized gains).
    /// </summary>
    /// <returns>Profitable scenarios</returns>
    public static IEnumerable<BrokerFinancialSnapshot> GetProfitableScenarios()
    {
        return GetScenariosWhere(s => s.RealizedGains > 0);
    }

    /// <summary>
    /// Gets scenarios with loss outcomes (negative realized gains).
    /// </summary>
    /// <returns>Loss scenarios</returns>
    public static IEnumerable<BrokerFinancialSnapshot> GetLossScenarios()
    {
        return GetScenariosWhere(s => s.RealizedGains < 0);
    }

    /// <summary>
    /// Gets scenarios with high trading activity (more than 100 movements).
    /// </summary>
    /// <returns>High-activity scenarios</returns>
    public static IEnumerable<BrokerFinancialSnapshot> GetHighActivityScenarios()
    {
        return GetScenariosWhere(s => s.MovementCounter > 100);
    }

    /// <summary>
    /// Gets scenarios with dividend income.
    /// </summary>
    /// <returns>Dividend-paying scenarios</returns>
    public static IEnumerable<BrokerFinancialSnapshot> GetDividendScenarios()
    {
        return GetScenariosWhere(s => s.DividendsReceived > 0);
    }

    /// <summary>
    /// Gets scenarios with options income.
    /// </summary>
    /// <returns>Options trading scenarios</returns>
    public static IEnumerable<BrokerFinancialSnapshot> GetOptionsScenarios()
    {
        return GetScenariosWhere(s => s.OptionsIncome > 0);
    }

    #endregion
}