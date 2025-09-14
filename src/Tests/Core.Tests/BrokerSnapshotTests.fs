namespace Core.Tests

open NUnit.Framework
open Binnaculum.Core.Models
open System

[<TestFixture>]
type BrokerSnapshotTests () =

    [<Test>]
    member _.``BrokerSnapshot has Financial property of type BrokerFinancialSnapshot`` () =
        // Arrange
        let mockCurrency = { Id = 1; Title = "USD"; Code = "USD"; Symbol = "$" }
        let mockBroker = {
            Id = 1
            Name = "Test Broker"
            Image = ""
            SupportedBroker = ""
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
        Assert.That(snapshot.Financial, Is.Not.Null)
        Assert.That(snapshot.Financial.RealizedGains, Is.EqualTo(200.0m))
        Assert.That(snapshot.Financial.UnrealizedGains, Is.EqualTo(100.0m))
        Assert.That(snapshot.AccountCount, Is.EqualTo(2))
        Assert.That(snapshot.FinancialOtherCurrencies, Is.Not.Null)
        Assert.That(snapshot.FinancialOtherCurrencies.Length, Is.EqualTo(0))

    [<Test>]
    member _.``BrokerSnapshot has FinancialOtherCurrencies property of type BrokerFinancialSnapshot list`` () =
        // Arrange
        let mockCurrency = { Id = 1; Title = "USD"; Code = "USD"; Symbol = "$" }
        let mockCurrency2 = { Id = 2; Title = "EUR"; Code = "EUR"; Symbol = "�" }
        let mockBroker = {
            Id = 1
            Name = "Test Broker"
            Image = ""
            SupportedBroker = ""
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
        Assert.That(snapshot.FinancialOtherCurrencies, Is.Not.Null)
        Assert.That(snapshot.FinancialOtherCurrencies.Length, Is.EqualTo(1))
        Assert.That(snapshot.FinancialOtherCurrencies.Head.Currency.Id, Is.EqualTo(2))
        Assert.That(snapshot.FinancialOtherCurrencies.Head.RealizedGains, Is.EqualTo(150.0m))