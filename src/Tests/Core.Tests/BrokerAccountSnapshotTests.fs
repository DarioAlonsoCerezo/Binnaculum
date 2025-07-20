namespace Core.Tests

open NUnit.Framework
open Binnaculum.Core.Models
open System

[<TestFixture>]
type BrokerAccountSnapshotTests () =

    [<Test>]
    member _.``BrokerAccountSnapshot has Financial property of type BrokerFinancialSnapshot`` () =
        // Arrange
        let mockCurrency = { Id = 1; Title = "USD"; Code = "USD"; Symbol = "$" }
        let mockBroker = { Id = 1; Name = "Test Broker"; Image = ""; SupportedBroker = "" }
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
        }
        let snapshot = {
            Date = DateOnly.FromDateTime(DateTime.Now)
            BrokerAccount = mockBrokerAccount
            PortfolioValue = 2150.0m
            Financial = mockFinancial
            FinancialOtherCurrencies = []
        }
        // Act & Assert
        Assert.That(snapshot.Financial, Is.Not.Null)
        Assert.That(snapshot.Financial.RealizedGains, Is.EqualTo(100.0m))
        Assert.That(snapshot.Financial.UnrealizedGains, Is.EqualTo(50.0m))
        Assert.That(snapshot.FinancialOtherCurrencies, Is.Not.Null)
        Assert.That(snapshot.FinancialOtherCurrencies.Length, Is.EqualTo(0))

    [<Test>]
    member _.``BrokerAccountSnapshot has FinancialOtherCurrencies property of type BrokerFinancialSnapshot list`` () =
        // Arrange
        let mockCurrency = { Id = 1; Title = "USD"; Code = "USD"; Symbol = "$" }
        let mockCurrency2 = { Id = 2; Title = "EUR"; Code = "EUR"; Symbol = "€" }
        let mockBroker = { Id = 1; Name = "Test Broker"; Image = ""; SupportedBroker = "" }
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
        }
        let snapshot = {
            Date = DateOnly.FromDateTime(DateTime.Now)
            BrokerAccount = mockBrokerAccount
            PortfolioValue = 2150.0m
            Financial = mockFinancial
            FinancialOtherCurrencies = [mockFinancialOtherCurrency]
        }
        // Act & Assert
        Assert.That(snapshot.FinancialOtherCurrencies, Is.Not.Null)
        Assert.That(snapshot.FinancialOtherCurrencies.Length, Is.EqualTo(1))
        Assert.That(snapshot.FinancialOtherCurrencies.Head.Currency.Id, Is.EqualTo(2))
        Assert.That(snapshot.FinancialOtherCurrencies.Head.RealizedGains, Is.EqualTo(50.0m))