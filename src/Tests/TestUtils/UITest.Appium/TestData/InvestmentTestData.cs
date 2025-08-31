using Binnaculum.UITest.Appium.PageObjects;

namespace Binnaculum.UITest.Appium.TestData;

/// <summary>
/// Provides realistic investment test data for E2E testing scenarios.
/// Based on the existing InvestmentTestData from UI.DeviceTests but adapted for Appium testing.
/// </summary>
public static class InvestmentTestData
{
    /// <summary>
    /// Create a profitable investment movement scenario.
    /// </summary>
    public static InvestmentMovementData CreateProfitableMovement()
    {
        return new InvestmentMovementData
        {
            MovementType = "Buy",
            Ticker = "AAPL",
            Quantity = 10,
            Price = 150.00m,
            Date = DateTime.Now.AddDays(-30),
            Notes = "Apple stock purchase for growth portfolio"
        };
    }

    /// <summary>
    /// Create a loss-making investment movement scenario.
    /// </summary>
    public static InvestmentMovementData CreateLossMovement()
    {
        return new InvestmentMovementData
        {
            MovementType = "Buy",
            Ticker = "TSLA",
            Quantity = 5,
            Price = 250.00m,
            Date = DateTime.Now.AddDays(-60),
            Notes = "Tesla purchase before market downturn"
        };
    }

    /// <summary>
    /// Create a dividend payment scenario.
    /// </summary>
    public static InvestmentMovementData CreateDividendMovement()
    {
        return new InvestmentMovementData
        {
            MovementType = "Dividend",
            Ticker = "MSFT",
            Quantity = 20,
            Price = 2.50m, // Dividend per share
            Date = DateTime.Now.AddDays(-7),
            Notes = "Microsoft quarterly dividend payment"
        };
    }

    /// <summary>
    /// Create a stock sale scenario.
    /// </summary>
    public static InvestmentMovementData CreateSellMovement()
    {
        return new InvestmentMovementData
        {
            MovementType = "Sell",
            Ticker = "GOOGL",
            Quantity = 3,
            Price = 2800.00m,
            Date = DateTime.Now.AddDays(-10),
            Notes = "Partial Google position sale for rebalancing"
        };
    }

    /// <summary>
    /// Get a set of movements for a complete portfolio scenario.
    /// </summary>
    public static List<InvestmentMovementData> CreateCompletePortfolioScenario()
    {
        return new List<InvestmentMovementData>
        {
            new InvestmentMovementData
            {
                MovementType = "Buy",
                Ticker = "AAPL",
                Quantity = 50,
                Price = 145.00m,
                Date = DateTime.Now.AddDays(-90),
                Notes = "Initial Apple position"
            },
            new InvestmentMovementData
            {
                MovementType = "Buy",
                Ticker = "MSFT",
                Quantity = 30,
                Price = 300.00m,
                Date = DateTime.Now.AddDays(-75),
                Notes = "Microsoft for stability"
            },
            new InvestmentMovementData
            {
                MovementType = "Buy",
                Ticker = "TSLA",
                Quantity = 15,
                Price = 220.00m,
                Date = DateTime.Now.AddDays(-60),
                Notes = "Tesla growth play"
            },
            CreateDividendMovement(),
            CreateSellMovement()
        };
    }

    /// <summary>
    /// Create test broker account data.
    /// </summary>
    public static BrokerAccountTestData CreateTestBrokerAccount()
    {
        return new BrokerAccountTestData
        {
            BrokerName = "Test Broker",
            AccountName = "Main Trading Account",
            InitialBalance = 10000.00m,
            Currency = "USD"
        };
    }

    /// <summary>
    /// Create various broker account scenarios for testing.
    /// </summary>
    public static List<BrokerAccountTestData> CreateMultipleBrokerScenarios()
    {
        return new List<BrokerAccountTestData>
        {
            new BrokerAccountTestData
            {
                BrokerName = "Interactive Brokers",
                AccountName = "Growth Portfolio",
                InitialBalance = 25000.00m,
                Currency = "USD"
            },
            new BrokerAccountTestData
            {
                BrokerName = "Charles Schwab",
                AccountName = "Conservative Portfolio",
                InitialBalance = 50000.00m,
                Currency = "USD"
            },
            new BrokerAccountTestData
            {
                BrokerName = "Fidelity",
                AccountName = "International Portfolio",
                InitialBalance = 15000.00m,
                Currency = "EUR"
            }
        };
    }
}

/// <summary>
/// Test data structure for broker account information.
/// </summary>
public class BrokerAccountTestData
{
    public string BrokerName { get; set; } = "";
    public string AccountName { get; set; } = "";
    public decimal InitialBalance { get; set; }
    public string Currency { get; set; } = "USD";
}