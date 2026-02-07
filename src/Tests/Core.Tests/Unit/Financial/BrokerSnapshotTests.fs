namespace Core.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.Models
open System

[<TestClass>]
type BrokerSnapshotTests () =

    [<TestMethod>]
    member _.``BrokerSnapshot has Financial property of type BrokerFinancialSnapshot`` () =
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
            NetCashFlow = 2000.0m - 0.0m - 20.0m - 10.0m + 50.0m + 0.0m + 0.0m // 2020.0m
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

    [<TestMethod>]
    member _.``BrokerSnapshot has FinancialOtherCurrencies property of type BrokerFinancialSnapshot list`` () =
        // Arrange
        let mockCurrency = { Id = 1; Title = "USD"; Code = "USD"; Symbol = "$" }
        let mockCurrency2 = { Id = 2; Title = "EUR"; Code = "EUR"; Symbol = "�" }
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
            NetCashFlow = 2000.0m - 0.0m - 20.0m - 10.0m + 50.0m + 0.0m + 0.0m // 2020.0m
        }
        let mockFinancialOtherCurrency = {
            Id = 2
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = Some mockBroker
            BrokerAccount = None
            Currency = mockCurrency2
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
            NetCashFlow = 1500.0m - 0.0m - 15.0m - 7.5m + 37.5m + 0.0m + 0.0m // 1515.0m
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
        Assert.AreEqual(2, snapshot.FinancialOtherCurrencies.Head.Currency.Id)
        Assert.AreEqual(150.0m, snapshot.FinancialOtherCurrencies.Head.RealizedGains)