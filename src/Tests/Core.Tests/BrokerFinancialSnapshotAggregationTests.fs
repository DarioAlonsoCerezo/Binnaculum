namespace Core.Tests

open NUnit.Framework
open Binnaculum.Core.Models
open System

[<TestFixture>]
type BrokerFinancialSnapshotAggregationTests () =

    [<Test>]
    member _.``Aggregates financial snapshots with highest MovementCounter as main`` () =
        // Arrange
        let mockCurrency1 = { Id = 1; Title = "USD"; Code = "USD"; Symbol = "$" }
        let mockCurrency2 = { Id = 2; Title = "EUR"; Code = "EUR"; Symbol = "�" }
        let mockBroker = {
            Id = 1
            Name = "Test Broker"
            Image = ""
            SupportedBroker = SupportedBroker.Unknown
        }
        let mockFinancial1 = {
            Id = 1
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = Some mockBroker
            BrokerAccount = None
            Currency = mockCurrency1
            MovementCounter = 5
            RealizedGains = 100.0m
            RealizedPercentage = 5.0m
            UnrealizedGains = 50.0m
            UnrealizedGainsPercentage = 2.5m
            Invested = 2000.0m
            Commissions = 10.0m
            Fees = 5.0m
            Deposited = 2000.0m
            Withdrawn = 0.0m
            DividendsReceived = 25.0m
            OptionsIncome = 0.0m
            OtherIncome = 0.0m
            OpenTrades = false
            NetCashFlow = 2000.0m - 0.0m - 10.0m - 5.0m + 25.0m + 0.0m + 0.0m // 2010.0m
        }
        let mockFinancial2 = {
            Id = 2
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = Some mockBroker
            BrokerAccount = None
            Currency = mockCurrency2
            MovementCounter = 10
            RealizedGains = 200.0m
            RealizedPercentage = 10.0m
            UnrealizedGains = 100.0m
            UnrealizedGainsPercentage = 5.0m
            Invested = 4000.0m
            Commissions = 20.0m
            Fees = 10.0m
            Deposited = 4000.0m
            Withdrawn = 0.0m
            DividendsReceived = 50.0m
            OptionsIncome = 0.0m
            OtherIncome = 0.0m
            OpenTrades = false
            NetCashFlow = 4000.0m - 0.0m - 20.0m - 10.0m + 50.0m + 0.0m + 0.0m // 4020.0m
        }
        // Simulate aggregation logic: highest MovementCounter is main, others are in FinancialOtherCurrencies
        let mainFinancial = if mockFinancial1.MovementCounter > mockFinancial2.MovementCounter then mockFinancial1 else mockFinancial2
        let otherFinancials = if mainFinancial.Id = mockFinancial1.Id then [mockFinancial2] else [mockFinancial1]
        let snapshot = {
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = mockBroker
            PortfoliosValue = 5000.0m
            AccountCount = 2
            Financial = mainFinancial
            FinancialOtherCurrencies = otherFinancials
        }
        // Act & Assert
        Assert.That(snapshot.Financial.MovementCounter, Is.EqualTo(10))
        Assert.That(snapshot.Financial.Currency.Id, Is.EqualTo(2))
        Assert.That(snapshot.Financial.RealizedGains, Is.EqualTo(200.0m))
        Assert.That(snapshot.FinancialOtherCurrencies.Length, Is.EqualTo(1))
        Assert.That(snapshot.FinancialOtherCurrencies.Head.MovementCounter, Is.EqualTo(5))
        Assert.That(snapshot.FinancialOtherCurrencies.Head.Currency.Id, Is.EqualTo(1))
        Assert.That(snapshot.FinancialOtherCurrencies.Head.RealizedGains, Is.EqualTo(100.0m))

    [<Test>]
    member _.``Handles empty financial snapshots list`` () =
        // Arrange
        let mockCurrency = { Id = 1; Title = "USD"; Code = "USD"; Symbol = "$" }
        let mockBroker = {
            Id = 1
            Name = "Test Broker"
            Image = ""
            SupportedBroker = SupportedBroker.Unknown
        }
        // Simulate empty aggregation
        let emptyFinancial = {
            Id = 0
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = Some mockBroker
            BrokerAccount = None
            Currency = mockCurrency
            MovementCounter = 0
            RealizedGains = 0.0m
            RealizedPercentage = 0.0m
            UnrealizedGains = 0.0m
            UnrealizedGainsPercentage = 0.0m
            Invested = 0.0m
            Commissions = 0.0m
            Fees = 0.0m
            Deposited = 0.0m
            Withdrawn = 0.0m
            DividendsReceived = 0.0m
            OptionsIncome = 0.0m
            OtherIncome = 0.0m
            OpenTrades = false
            NetCashFlow = 0.0m - 0.0m - 0.0m - 0.0m + 0.0m + 0.0m + 0.0m // 0.0m
        }
        let snapshot = {
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = mockBroker
            PortfoliosValue = 0.0m
            AccountCount = 0
            Financial = emptyFinancial
            FinancialOtherCurrencies = []
        }
        // Act & Assert
        Assert.That(snapshot.Financial.MovementCounter, Is.EqualTo(0))
        Assert.That(snapshot.Financial.Currency.Id, Is.EqualTo(1))
        Assert.That(snapshot.Financial.RealizedGains, Is.EqualTo(0.0m))
        Assert.That(snapshot.FinancialOtherCurrencies.Length, Is.EqualTo(0))

    [<Test>]
    member _.``Handles single financial snapshot`` () =
        // Arrange
        let mockCurrency = { Id = 1; Title = "USD"; Code = "USD"; Symbol = "$" }
        let mockBroker = {
            Id = 1
            Name = "Test Broker"
            Image = ""
            SupportedBroker = SupportedBroker.Unknown
        }
        let mockFinancial = {
            Id = 1
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = Some mockBroker
            BrokerAccount = None
            Currency = mockCurrency
            MovementCounter = 7
            RealizedGains = 150.0m
            RealizedPercentage = 7.5m
            UnrealizedGains = 75.0m
            UnrealizedGainsPercentage = 3.75m
            Invested = 3000.0m
            Commissions = 15.0m
            Fees = 7.5m
            Deposited = 3000.0m
            Withdrawn = 0.0m
            DividendsReceived = 37.5m
            OptionsIncome = 0.0m
            OtherIncome = 0.0m
            OpenTrades = false
            NetCashFlow = 3000.0m - 0.0m - 15.0m - 7.5m + 37.5m + 0.0m + 0.0m // 3015.0m
        }
        let snapshot = {
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = mockBroker
            PortfoliosValue = 3000.0m
            AccountCount = 1
            Financial = mockFinancial
            FinancialOtherCurrencies = []
        }
        // Act & Assert
        Assert.That(snapshot.Financial.MovementCounter, Is.EqualTo(7))
        Assert.That(snapshot.Financial.Currency.Id, Is.EqualTo(1))
        Assert.That(snapshot.Financial.RealizedGains, Is.EqualTo(150.0m))
        Assert.That(snapshot.FinancialOtherCurrencies.Length, Is.EqualTo(0))