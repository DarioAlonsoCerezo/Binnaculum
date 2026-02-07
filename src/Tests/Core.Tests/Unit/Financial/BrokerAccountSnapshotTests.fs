namespace Core.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.Models
open System

[<TestClass>]
type BrokerAccountSnapshotTests () =

    [<TestMethod>]
    member _.``BrokerAccountSnapshot has Financial property of type BrokerFinancialSnapshot`` () =
        // Arrange
        let mockCurrency = { Id = 1; Title = "USD"; Code = "USD"; Symbol = "$" }
        let mockBroker = { Id = 1; Name = "Test Broker"; Image = ""; SupportedBroker = SupportedBroker.Unknown }
        let mockBrokerAccount = {
            Id = 1
            Broker = mockBroker
            AccountNumber = "123456"
        }
        let mockFinancial = {
            Id = 1
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = Some mockBroker
            BrokerAccount = Some mockBrokerAccount
            Currency = mockCurrency
            MovementCounter = 0
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
        let snapshot = {
            Date = DateOnly.FromDateTime(DateTime.Now)
            BrokerAccount = mockBrokerAccount
            PortfolioValue = 2150.0m
            Financial = mockFinancial
            FinancialOtherCurrencies = []
        }
        // Act & Assert
        Assert.IsNotNull(snapshot.Financial)
        Assert.AreEqual(100.0m, snapshot.Financial.RealizedGains)
        Assert.AreEqual(50.0m, snapshot.Financial.UnrealizedGains)
        Assert.IsNotNull(snapshot.FinancialOtherCurrencies)
        Assert.AreEqual(0, snapshot.FinancialOtherCurrencies.Length)

    [<TestMethod>]
    member _.``BrokerAccountSnapshot has FinancialOtherCurrencies property of type BrokerFinancialSnapshot list`` () =
        // Arrange
        let mockCurrency = { Id = 1; Title = "USD"; Code = "USD"; Symbol = "$" }
        let mockCurrency2 = { Id = 2; Title = "EUR"; Code = "EUR"; Symbol = "�" }
        let mockBroker = { Id = 1; Name = "Test Broker"; Image = ""; SupportedBroker = SupportedBroker.Unknown }
        let mockBrokerAccount = {
            Id = 1
            Broker = mockBroker
            AccountNumber = "123456"
        }
        let mockFinancial = {
            Id = 1
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = Some mockBroker
            BrokerAccount = Some mockBrokerAccount
            Currency = mockCurrency
            MovementCounter = 0
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
        let mockFinancialOtherCurrency = {
            Id = 2
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = Some mockBroker
            BrokerAccount = Some mockBrokerAccount
            Currency = mockCurrency2
            MovementCounter = 0
            RealizedGains = 50.0m
            RealizedPercentage = 2.5m
            UnrealizedGains = 25.0m
            UnrealizedGainsPercentage = 1.25m
            Invested = 1000.0m
            Commissions = 5.0m
            Fees = 2.5m
            Deposited = 1000.0m
            Withdrawn = 0.0m
            DividendsReceived = 12.5m
            OptionsIncome = 0.0m
            OtherIncome = 0.0m
            OpenTrades = false
            NetCashFlow = 1000.0m - 0.0m - 5.0m - 2.5m + 12.5m + 0.0m + 0.0m // 1005.0m
        }
        let snapshot = {
            Date = DateOnly.FromDateTime(DateTime.Now)
            BrokerAccount = mockBrokerAccount
            PortfolioValue = 2150.0m
            Financial = mockFinancial
            FinancialOtherCurrencies = [mockFinancialOtherCurrency]
        }
        // Act & Assert
        Assert.IsNotNull(snapshot.FinancialOtherCurrencies)
        Assert.AreEqual(1, snapshot.FinancialOtherCurrencies.Length)
        Assert.AreEqual(2, snapshot.FinancialOtherCurrencies.Head.Currency.Id)
        Assert.AreEqual(50.0m, snapshot.FinancialOtherCurrencies.Head.RealizedGains)