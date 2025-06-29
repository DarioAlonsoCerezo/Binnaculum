namespace Core.Tests

open NUnit.Framework
open Binnaculum.Core.Models
open System

[<TestFixture>]
type BrokerAccountSnapshotTests () =

    [<Test>]
    member _.``BrokerAccountSnapshot has Financial property of type BrokerFinancialSnapshot`` () =
        // Arrange
        let mockBrokerAccount = {
            Id = 1
            Broker = { Id = 1; Name = "Test Broker"; Image = ""; SupportedBroker = "" }
            AccountNumber = "123456"
        }
        
        let mockFinancial = {
            Date = DateOnly.FromDateTime(DateTime.Now)
            BrokerId = -1 // Default value
            BrokerAccountId = 1 // For specific broker account
            CurrencyId = 1
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
        Assert.IsNotNull(snapshot.Financial)
        Assert.AreEqual(100.0m, snapshot.Financial.RealizedGains)
        Assert.AreEqual(50.0m, snapshot.Financial.UnrealizedGains)
        Assert.IsNotNull(snapshot.FinancialOtherCurrencies)
        Assert.AreEqual(0, snapshot.FinancialOtherCurrencies.Length)

    [<Test>]
    member _.``BrokerAccountSnapshot has FinancialOtherCurrencies property of type BrokerFinancialSnapshot list`` () =
        // Arrange
        let mockBrokerAccount = {
            Id = 1
            Broker = { Id = 1; Name = "Test Broker"; Image = ""; SupportedBroker = "" }
            AccountNumber = "123456"
        }
        
        let mockFinancial = {
            Date = DateOnly.FromDateTime(DateTime.Now)
            BrokerId = -1 // Default value
            BrokerAccountId = 1 // For specific broker account
            CurrencyId = 1
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
            Date = DateOnly.FromDateTime(DateTime.Now)
            BrokerId = -1 // Default value
            BrokerAccountId = 1 // For specific broker account
            CurrencyId = 2 // Different currency
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
        Assert.IsNotNull(snapshot.FinancialOtherCurrencies)
        Assert.AreEqual(1, snapshot.FinancialOtherCurrencies.Length)
        Assert.AreEqual(2, snapshot.FinancialOtherCurrencies.Head.CurrencyId)
        Assert.AreEqual(50.0m, snapshot.FinancialOtherCurrencies.Head.RealizedGains)