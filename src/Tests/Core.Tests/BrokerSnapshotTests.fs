namespace Core.Tests

open NUnit.Framework
open Binnaculum.Core.Models
open System

[<TestFixture>]
type BrokerSnapshotTests () =

    [<Test>]
    member _.``BrokerSnapshot has Financial property of type BrokerFinancialSnapshot`` () =
        // Arrange
        let mockBroker = {
            Id = 1
            Name = "Test Broker"
            Image = ""
            SupportedBroker = ""
        }

        let mockFinancial = {
            Date = DateOnly.FromDateTime(DateTime.Now)
            CurrencyId = 1
            MovementCounter = 0
            RealizedGains = 200.0m
            RealizedPercentage = 10.0m
            UnrealizedGains = 100.0m
            UnrealizedGainsPercentage = 5.0m
            Invested = 2000.0m
            Commissions = 20.0m
            Fees = 10.0m
            Deposited = 2000.0m
            Withdrawn = 0.0m
            DividendsReceived = 50.0m
            OptionsIncome = 0.0m
            OtherIncome = 0.0m
            OpenTrades = false
        }

        let snapshot = {
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = mockBroker
            PortfoliosValue = 4300.0m
            AccountCount = 2
            Financial = mockFinancial
            FinancialOtherCurrencies = []
        }

        // Act & Assert
        Assert.IsNotNull(snapshot.Financial)
        Assert.AreEqual(200.0m, snapshot.Financial.RealizedGains)
        Assert.AreEqual(100.0m, snapshot.Financial.UnrealizedGains)
        Assert.AreEqual(2, snapshot.AccountCount)
        Assert.IsNotNull(snapshot.FinancialOtherCurrencies)
        Assert.AreEqual(0, snapshot.FinancialOtherCurrencies.Length)

    [<Test>]
    member _.``BrokerSnapshot has FinancialOtherCurrencies property of type BrokerFinancialSnapshot list`` () =
        // Arrange
        let mockBroker = {
            Id = 1
            Name = "Test Broker"
            Image = ""
            SupportedBroker = ""
        }

        let mockFinancial = {
            Date = DateOnly.FromDateTime(DateTime.Now)
            CurrencyId = 1
            MovementCounter = 0
            RealizedGains = 200.0m
            RealizedPercentage = 10.0m
            UnrealizedGains = 100.0m
            UnrealizedGainsPercentage = 5.0m
            Invested = 2000.0m
            Commissions = 20.0m
            Fees = 10.0m
            Deposited = 2000.0m
            Withdrawn = 0.0m
            DividendsReceived = 50.0m
            OptionsIncome = 0.0m
            OtherIncome = 0.0m
            OpenTrades = false
        }

        let mockFinancialOtherCurrency = {
            Date = DateOnly.FromDateTime(DateTime.Now)
            CurrencyId = 2 // Different currency (EUR)
            MovementCounter = 0
            RealizedGains = 150.0m
            RealizedPercentage = 7.5m
            UnrealizedGains = 75.0m
            UnrealizedGainsPercentage = 3.75m
            Invested = 1500.0m
            Commissions = 15.0m
            Fees = 7.5m
            Deposited = 1500.0m
            Withdrawn = 0.0m
            DividendsReceived = 37.5m
            OptionsIncome = 0.0m
            OtherIncome = 0.0m
            OpenTrades = false
        }

        let snapshot = {
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = mockBroker
            PortfoliosValue = 4300.0m
            AccountCount = 2
            Financial = mockFinancial
            FinancialOtherCurrencies = [mockFinancialOtherCurrency]
        }

        // Act & Assert
        Assert.IsNotNull(snapshot.FinancialOtherCurrencies)
        Assert.AreEqual(1, snapshot.FinancialOtherCurrencies.Length)
        Assert.AreEqual(2, snapshot.FinancialOtherCurrencies.Head.CurrencyId)
        Assert.AreEqual(150.0m, snapshot.FinancialOtherCurrencies.Head.RealizedGains)