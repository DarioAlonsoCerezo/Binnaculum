namespace Core.Tests

open System
open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.Patterns
open Binnaculum.Core.Database
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Storage
open OptionTradeExtensions

module private BrokerFinancialCalculateTestHelpers =
    let sampleTrade tradeCode =
        { Id = 1
          TimeStamp = DateTimePattern.FromDateTime(DateTime(2024, 4, 22, 12, 0, 0, DateTimeKind.Utc))
          TickerId = 99
          BrokerAccountId = 1
          CurrencyId = 141
          Quantity = 1m
          Price = Money.FromAmount 10m
          Commissions = Money.FromAmount 0m
          Fees = Money.FromAmount 0m
          TradeCode = tradeCode
          TradeType = TradeType.Long
          Leveraged = 1m
          Notes = None
          Audit = AuditableEntity.Default }

    let currencyMovements trades optionTrades =
        { CurrencyId = 141
          BrokerMovements = []
          Trades = trades
          Dividends = []
          DividendTaxes = []
          OptionTrades = optionTrades
          TotalCount = List.length trades + List.length optionTrades
          UniqueDates = Set.empty<DateTimePattern> }

[<TestClass>]
type BrokerFinancialCalculateTests() =

    [<TestMethod>]
    member _.``Scenario D direct update replaces existing totals``() =
        // Arrange
        let targetDate = DateTime(2024, 4, 22, 23, 59, 59, DateTimeKind.Utc)

        let baseSnapshot =
            { Id = 42
              Date = DateTimePattern.FromDateTime(targetDate)
              Audit = AuditableEntity.Default }

        let existingSnapshot =
            { Base = baseSnapshot
              BrokerId = -1
              BrokerAccountId = 1
              CurrencyId = 141
              MovementCounter = 99
              BrokerSnapshotId = -1
              BrokerAccountSnapshotId = 5
              RealizedGains = Money.FromAmount 50m
              RealizedPercentage = 12.5m
              UnrealizedGains = Money.FromAmount 5m
              UnrealizedGainsPercentage = 1.0m
              Invested = Money.FromAmount 200m
              Commissions = Money.FromAmount 7m
              Fees = Money.FromAmount 3m
              Deposited = Money.FromAmount 123m
              Withdrawn = Money.FromAmount 2m
              DividendsReceived = Money.FromAmount 4m
              OptionsIncome = Money.FromAmount 6m
              OtherIncome = Money.FromAmount 8m
              NetCashFlow = Money.FromAmount(123m - 2m + 4m + 6m + 8m - 7m - 3m)
              OpenTrades = true }

        let recalculatedMetrics =
            { Deposited = Money.FromAmount 10m
              Withdrawn = Money.FromAmount 0m
              Invested = Money.FromAmount 0m
              RealizedGains = Money.FromAmount 0m
              DividendsReceived = Money.FromAmount 0m
              OptionsIncome = Money.FromAmount 0m
              OtherIncome = Money.FromAmount 0m
              Commissions = Money.FromAmount 0m
              Fees = Money.FromAmount 0m
              CurrentPositions = Map.empty
              CostBasisInfo = Map.empty
              HasOpenPositions = false
              OptionUnrealizedGains = Money.FromAmount 0m
              MovementCounter = 1 }

        let stockUnrealizedGains = Money.FromAmount 0m

        let movements =
            BrokerFinancialCalculateTestHelpers.currencyMovements
                [ BrokerFinancialCalculateTestHelpers.sampleTrade TradeCode.SellToClose ]
                []

        // Act
        let updatedSnapshot =
            BrokerFinancialCalculate.applyDirectSnapshotMetricsWithPreservation
                movements
                existingSnapshot
                recalculatedMetrics
                stockUnrealizedGains

        // Assert
        Assert.AreEqual(10m, updatedSnapshot.Deposited.Value, "Deposits should match recalculated metrics without accumulation")

        Assert.AreEqual(0m, updatedSnapshot.Withdrawn.Value)
        Assert.AreEqual(0m, updatedSnapshot.RealizedGains.Value)
        Assert.AreEqual(1, updatedSnapshot.MovementCounter)
        Assert.AreEqual(0m, updatedSnapshot.Commissions.Value)
        Assert.AreEqual(0m, updatedSnapshot.Fees.Value)
        Assert.IsFalse(updatedSnapshot.OpenTrades)
        Assert.AreEqual(0m, updatedSnapshot.RealizedPercentage)
        Assert.AreEqual(0m, updatedSnapshot.UnrealizedGains.Value)
        Assert.AreEqual(0m, updatedSnapshot.UnrealizedGainsPercentage)

    [<TestMethod>]
    member _.``Scenario D direct update preserves realized gains without closing movements``() =
        // Arrange
        let targetDate = DateTime(2024, 4, 22, 23, 59, 59, DateTimeKind.Utc)

        let baseSnapshot =
            { Id = 7
              Date = DateTimePattern.FromDateTime(targetDate)
              Audit = AuditableEntity.Default }

        let existingSnapshot =
            { Base = baseSnapshot
              BrokerId = -1
              BrokerAccountId = 1
              CurrencyId = 141
              MovementCounter = 16
              BrokerSnapshotId = -1
              BrokerAccountSnapshotId = 9
              RealizedGains = Money.FromAmount 23.65m
              RealizedPercentage = 2.5m
              UnrealizedGains = Money.FromAmount 14.86m
              UnrealizedGainsPercentage = 1.7m
              Invested = Money.FromAmount 500m
              Commissions = Money.FromAmount 12m
              Fees = Money.FromAmount 1m
              Deposited = Money.FromAmount 878.79m
              Withdrawn = Money.FromAmount 0m
              DividendsReceived = Money.FromAmount 0m
              OptionsIncome = Money.FromAmount 0m
              OtherIncome = Money.FromAmount 0m
              NetCashFlow = Money.FromAmount(878.79m - 0m + 0m + 0m + 0m - 12m - 1m)
              OpenTrades = false }

        let recalculatedMetrics =
            { Deposited = Money.FromAmount 878.79m
              Withdrawn = Money.FromAmount 0m
              Invested = Money.FromAmount 0m
              RealizedGains = Money.FromAmount 0m
              DividendsReceived = Money.FromAmount 0m
              OptionsIncome = Money.FromAmount 0m
              OtherIncome = Money.FromAmount 0m
              Commissions = Money.FromAmount 0m
              Fees = Money.FromAmount 0m
              CurrentPositions = Map.empty
              CostBasisInfo = Map.empty
              HasOpenPositions = false
              OptionUnrealizedGains = Money.FromAmount 0m
              MovementCounter = 0 }

        let stockUnrealizedGains = Money.FromAmount 0m
        let movements = BrokerFinancialCalculateTestHelpers.currencyMovements [] []

        // Act
        let updatedSnapshot =
            BrokerFinancialCalculate.applyDirectSnapshotMetricsWithPreservation
                movements
                existingSnapshot
                recalculatedMetrics
                stockUnrealizedGains

        // Assert
        Assert.AreEqual(23.65m, updatedSnapshot.RealizedGains.Value)
        Assert.AreEqual(2.5m, updatedSnapshot.RealizedPercentage)
        Assert.AreEqual(878.79m, updatedSnapshot.Deposited.Value)

    [<TestMethod>]
    member _.``Option summary replicates Tastytrade integration dataset``() =
        // Arrange
        let createOptionTrade
            (id: int)
            (timestamp: DateTime)
            (expiration: DateTime)
            (tickerId: int)
            (code: OptionCode)
            (strike: decimal)
            (valueAmount: decimal)
            (commissions: decimal)
            (fees: decimal)
            (optionType: OptionType)
            =
            // Premium preserves sign: positive for SELL, negative for BUY (matches CSV import behavior)
            let premium = valueAmount
            let commissionCost = Math.Abs(commissions)
            let feeCost = Math.Abs(fees)

            // NetPremium: Premium - Commissions - Fees
            // For SELL: positive premium - costs = net income
            // For BUY: negative premium - costs = net cost (more negative)
            let netPremium = premium - commissionCost - feeCost

            { Id = id
              TimeStamp = DateTimePattern.FromDateTime(timestamp)
              ExpirationDate = DateTimePattern.FromDateTime(expiration)
              Premium = Money.FromAmount(premium)
              NetPremium = Money.FromAmount(netPremium)
              TickerId = tickerId
              BrokerAccountId = 1
              CurrencyId = 141
              OptionType = optionType
              Code = code
              Strike = Money.FromAmount(strike)
              Commissions = Money.FromAmount(Math.Abs(commissions))
              Fees = Money.FromAmount(Math.Abs(fees))
              IsOpen = true
              ClosedWith = None
              Multiplier = 100m
              Notes = None
              Audit = AuditableEntity.Default }

        let trades =
            [ createOptionTrade
                  1
                  (DateTime(2024, 4, 30, 15, 45, 8, DateTimeKind.Utc))
                  (DateTime(2024, 5, 10, 0, 0, 0, DateTimeKind.Utc))
                  1
                  OptionCode.SellToOpen
                  6.5m
                  16m
                  1m
                  0.14m
                  OptionType.Put
              createOptionTrade
                  2
                  (DateTime(2024, 4, 29, 18, 55, 34, DateTimeKind.Utc))
                  (DateTime(2024, 5, 3, 0, 0, 0, DateTimeKind.Utc))
                  2
                  OptionCode.SellToClose
                  21m
                  5m
                  0m
                  0.14m
                  OptionType.Put
              createOptionTrade
                  3
                  (DateTime(2024, 4, 29, 18, 55, 34, DateTimeKind.Utc))
                  (DateTime(2024, 5, 3, 0, 0, 0, DateTimeKind.Utc))
                  2
                  OptionCode.BuyToClose
                  21.5m
                  -9m
                  0m
                  0.13m
                  OptionType.Put
              createOptionTrade
                  4
                  (DateTime(2024, 4, 29, 15, 46, 5, DateTimeKind.Utc))
                  (DateTime(2024, 5, 3, 0, 0, 0, DateTimeKind.Utc))
                  1
                  OptionCode.SellToOpen
                  7m
                  17m
                  1m
                  0.14m
                  OptionType.Put
              createOptionTrade
                  5
                  (DateTime(2024, 4, 29, 15, 43, 41, DateTimeKind.Utc))
                  (DateTime(2024, 5, 3, 0, 0, 0, DateTimeKind.Utc))
                  1
                  OptionCode.BuyToClose
                  7m
                  -17m
                  0m
                  0.13m
                  OptionType.Put
              createOptionTrade
                  6
                  (DateTime(2024, 4, 29, 15, 19, 56, DateTimeKind.Utc))
                  (DateTime(2024, 5, 3, 0, 0, 0, DateTimeKind.Utc))
                  3
                  OptionCode.SellToClose
                  4m
                  1m
                  0m
                  0.14m
                  OptionType.Put
              createOptionTrade
                  7
                  (DateTime(2024, 4, 29, 15, 19, 56, DateTimeKind.Utc))
                  (DateTime(2024, 5, 3, 0, 0, 0, DateTimeKind.Utc))
                  3
                  OptionCode.BuyToClose
                  4.5m
                  -8m
                  0m
                  0.13m
                  OptionType.Put
              createOptionTrade
                  8
                  (DateTime(2024, 4, 26, 20, 22, 28, DateTimeKind.Utc))
                  (DateTime(2024, 5, 3, 0, 0, 0, DateTimeKind.Utc))
                  2
                  OptionCode.BuyToOpen
                  21m
                  -11m
                  1m
                  0.13m
                  OptionType.Put
              createOptionTrade
                  9
                  (DateTime(2024, 4, 26, 20, 22, 28, DateTimeKind.Utc))
                  (DateTime(2024, 5, 3, 0, 0, 0, DateTimeKind.Utc))
                  2
                  OptionCode.SellToOpen
                  21.5m
                  19m
                  1m
                  0.14m
                  OptionType.Put
              createOptionTrade
                  10
                  (DateTime(2024, 4, 26, 19, 8, 13, DateTimeKind.Utc))
                  (DateTime(2024, 5, 3, 0, 0, 0, DateTimeKind.Utc))
                  3
                  OptionCode.SellToOpen
                  4.5m
                  19m
                  1m
                  0.14m
                  OptionType.Put
              createOptionTrade
                  11
                  (DateTime(2024, 4, 26, 19, 8, 13, DateTimeKind.Utc))
                  (DateTime(2024, 5, 3, 0, 0, 0, DateTimeKind.Utc))
                  3
                  OptionCode.BuyToOpen
                  4m
                  -4m
                  1m
                  0.13m
                  OptionType.Put
              createOptionTrade
                  12
                  (DateTime(2024, 4, 25, 14, 41, 11, DateTimeKind.Utc))
                  (DateTime(2024, 5, 3, 0, 0, 0, DateTimeKind.Utc))
                  1
                  OptionCode.SellToOpen
                  7m
                  35m
                  1m
                  0.14m
                  OptionType.Put ]

        // Act
        let summary =
            OptionTradeCalculations.calculateOptionsSummary (trades, DateTime(2024, 5, 10, 0, 0, 0, DateTimeKind.Utc))

        // Assert
        // OptionsIncome = Sum of all Premium values (positive for sells, negative for buys)
        // 16 + 5 - 9 + 17 - 17 + 1 - 8 - 11 + 19 + 19 - 4 + 35 = 63.00
        Assert.IsTrue(abs(summary.OptionsIncome.Value - 63.00m) <= 0.01m)
        // OptionsInvestment = Total cost of buying options = Sum of absolute NetPremiums for buys
        Assert.IsTrue(abs(summary.OptionsInvestment.Value - 51.65m) <= 0.01m)
        // RealizedGains: Correctly calculated with FIFO matching
        // Pair 1: SellToOpen(33.86) + BuyToClose(-17.13) = 16.73
        // Pair 2: BuyToOpen(-12.13) + SellToClose(4.86) = -7.27
        // Pair 3: SellToOpen(17.86) + BuyToClose(-9.13) = 8.73
        // Pair 4: BuyToOpen(-5.13) + SellToClose(0.86) = -4.27
        // Pair 5: SellToOpen(17.86) + BuyToClose(-8.13) = 9.73
        // Total: 16.73 + (-7.27) + 8.73 + (-4.27) + 9.73 = 23.65
        Assert.IsTrue(abs(summary.RealizedGains.Value - 23.65m) <= 0.01m)
        Assert.IsTrue(abs(summary.UnrealizedGains.Value - 14.86m) <= 0.01m)
