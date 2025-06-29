namespace Core.Tests

open NUnit.Framework
open Binnaculum.Core.Models
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Storage.DatabaseToModels
open System
open Binnaculum.Core.Patterns

[<TestFixture>]
type BrokerFinancialSnapshotAggregationTests () =

    [<Test>]
    member _.``brokerSnapshotToOverviewSnapshot aggregates financial snapshots with highest MovementCounter as main`` () =
        // Arrange
        let mockBroker = {
            Id = 1
            Name = "Test Broker"
            Image = ""
            SupportedBroker = ""
        }

        let baseSnapshot = {
            Id = 1
            Date = DateTimePattern.FromDateTime(DateTime.Now)
            Audit = { CreatedAt = DateTime.Now; UpdatedAt = None }
        }

        let dbBrokerSnapshot = {
            Base = baseSnapshot
            BrokerId = 1
            PortfoliosValue = Money.FromAmount(5000.0m)
            AccountCount = 2
        }

        let financialSnapshot1 = {
            Base = { baseSnapshot with Id = 1 }
            CurrencyId = 1
            MovementCounter = 5  // Lower counter
            RealizedGains = Money.FromAmount(100.0m)
            RealizedPercentage = 5.0m
            UnrealizedGains = Money.FromAmount(50.0m)
            UnrealizedGainsPercentage = 2.5m
            Invested = Money.FromAmount(2000.0m)
            Commissions = Money.FromAmount(10.0m)
            Fees = Money.FromAmount(5.0m)
            Deposited = Money.FromAmount(2000.0m)
            Withdrawn = Money.FromAmount(0.0m)
            DividendsReceived = Money.FromAmount(25.0m)
            OptionsIncome = Money.FromAmount(0.0m)
            OtherIncome = Money.FromAmount(0.0m)
            OpenTrades = false
        }

        let financialSnapshot2 = {
            Base = { baseSnapshot with Id = 2 }
            CurrencyId = 2
            MovementCounter = 10  // Higher counter - this should be main
            RealizedGains = Money.FromAmount(200.0m)
            RealizedPercentage = 10.0m
            UnrealizedGains = Money.FromAmount(100.0m)
            UnrealizedGainsPercentage = 5.0m
            Invested = Money.FromAmount(4000.0m)
            Commissions = Money.FromAmount(20.0m)
            Fees = Money.FromAmount(10.0m)
            Deposited = Money.FromAmount(4000.0m)
            Withdrawn = Money.FromAmount(0.0m)
            DividendsReceived = Money.FromAmount(50.0m)
            OptionsIncome = Money.FromAmount(0.0m)
            OtherIncome = Money.FromAmount(0.0m)
            OpenTrades = false
        }

        let financialSnapshots = [financialSnapshot1; financialSnapshot2]

        // Act
        let result = dbBrokerSnapshot.brokerSnapshotToOverviewSnapshot(mockBroker, financialSnapshots)

        // Assert
        Assert.IsNotNull(result.Broker)
        let brokerSnapshot = result.Broker.Value
        
        // The financial snapshot with MovementCounter 10 should be the main one
        Assert.AreEqual(10, brokerSnapshot.Financial.MovementCounter)
        Assert.AreEqual(2, brokerSnapshot.Financial.CurrencyId)
        Assert.AreEqual(200.0m, brokerSnapshot.Financial.RealizedGains)
        
        // The other snapshot should be in FinancialOtherCurrencies
        Assert.AreEqual(1, brokerSnapshot.FinancialOtherCurrencies.Length)
        Assert.AreEqual(5, brokerSnapshot.FinancialOtherCurrencies.Head.MovementCounter)
        Assert.AreEqual(1, brokerSnapshot.FinancialOtherCurrencies.Head.CurrencyId)
        Assert.AreEqual(100.0m, brokerSnapshot.FinancialOtherCurrencies.Head.RealizedGains)

    [<Test>]
    member _.``brokerSnapshotToOverviewSnapshot handles empty financial snapshots list`` () =
        // Arrange
        let mockBroker = {
            Id = 1
            Name = "Test Broker"
            Image = ""
            SupportedBroker = ""
        }

        let baseSnapshot = {
            Id = 1
            Date = DateTimePattern.FromDateTime(DateTime.Now)
            Audit = { CreatedAt = DateTime.Now; UpdatedAt = None }
        }

        let dbBrokerSnapshot = {
            Base = baseSnapshot
            BrokerId = 1
            PortfoliosValue = Money.FromAmount(5000.0m)
            AccountCount = 2
        }

        let financialSnapshots = []

        // Act
        let result = dbBrokerSnapshot.brokerSnapshotToOverviewSnapshot(mockBroker, financialSnapshots)

        // Assert
        Assert.IsNotNull(result.Broker)
        let brokerSnapshot = result.Broker.Value
        
        // Should have default/empty financial data
        Assert.AreEqual(0, brokerSnapshot.Financial.MovementCounter)
        Assert.AreEqual(0, brokerSnapshot.Financial.CurrencyId)
        Assert.AreEqual(0.0m, brokerSnapshot.Financial.RealizedGains)
        
        // Should have empty FinancialOtherCurrencies
        Assert.AreEqual(0, brokerSnapshot.FinancialOtherCurrencies.Length)

    [<Test>]
    member _.``brokerSnapshotToOverviewSnapshot handles single financial snapshot`` () =
        // Arrange
        let mockBroker = {
            Id = 1
            Name = "Test Broker"
            Image = ""
            SupportedBroker = ""
        }

        let baseSnapshot = {
            Id = 1
            Date = DateTimePattern.FromDateTime(DateTime.Now)
            Audit = { CreatedAt = DateTime.Now; UpdatedAt = None }
        }

        let dbBrokerSnapshot = {
            Base = baseSnapshot
            BrokerId = 1
            PortfoliosValue = Money.FromAmount(5000.0m)
            AccountCount = 2
        }

        let financialSnapshot = {
            Base = baseSnapshot
            CurrencyId = 1
            MovementCounter = 7
            RealizedGains = Money.FromAmount(150.0m)
            RealizedPercentage = 7.5m
            UnrealizedGains = Money.FromAmount(75.0m)
            UnrealizedGainsPercentage = 3.75m
            Invested = Money.FromAmount(3000.0m)
            Commissions = Money.FromAmount(15.0m)
            Fees = Money.FromAmount(7.5m)
            Deposited = Money.FromAmount(3000.0m)
            Withdrawn = Money.FromAmount(0.0m)
            DividendsReceived = Money.FromAmount(37.5m)
            OptionsIncome = Money.FromAmount(0.0m)
            OtherIncome = Money.FromAmount(0.0m)
            OpenTrades = false
        }

        let financialSnapshots = [financialSnapshot]

        // Act
        let result = dbBrokerSnapshot.brokerSnapshotToOverviewSnapshot(mockBroker, financialSnapshots)

        // Assert
        Assert.IsNotNull(result.Broker)
        let brokerSnapshot = result.Broker.Value
        
        // The single financial snapshot should be the main one
        Assert.AreEqual(7, brokerSnapshot.Financial.MovementCounter)
        Assert.AreEqual(1, brokerSnapshot.Financial.CurrencyId)
        Assert.AreEqual(150.0m, brokerSnapshot.Financial.RealizedGains)
        
        // Should have empty FinancialOtherCurrencies since there's only one snapshot
        Assert.AreEqual(0, brokerSnapshot.FinancialOtherCurrencies.Length)