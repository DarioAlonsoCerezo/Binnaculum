namespace Core.Tests

open NUnit.Framework
open Binnaculum.Core.Models
open System

[<TestFixture>]
type BrokerFinancialSnapshotRefactoredTests () =

    [<Test>]
    member _.``BrokerFinancialSnapshot has Id property`` () =
        // Arrange & Act
        let currency = {
            Id = 1
            Title = "US Dollar"
            Code = "USD"
            Symbol = "$"
        }
        
        let snapshot = {
            Id = 123
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = None
            BrokerAccount = None
            Currency = currency
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
        }
        
        // Assert
        Assert.AreEqual(123, snapshot.Id)

    [<Test>]
    member _.``BrokerFinancialSnapshot uses strongly-typed Broker reference`` () =
        // Arrange
        let broker = {
            Id = 1
            Name = "Test Broker"
            Image = ""
            SupportedBroker = ""
        }
        
        let currency = {
            Id = 1
            Title = "US Dollar"
            Code = "USD"
            Symbol = "$"
        }
        
        let snapshot = {
            Id = 1
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = Some broker
            BrokerAccount = None
            Currency = currency
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
        }
        
        // Assert
        Assert.IsTrue(snapshot.Broker.IsSome)
        Assert.AreEqual("Test Broker", snapshot.Broker.Value.Name)

    [<Test>]
    member _.``BrokerFinancialSnapshot uses strongly-typed BrokerAccount reference`` () =
        // Arrange
        let broker = {
            Id = 1
            Name = "Test Broker"
            Image = ""
            SupportedBroker = ""
        }
        
        let brokerAccount = {
            Id = 2
            Broker = broker
            AccountNumber = "ACC123"
        }
        
        let currency = {
            Id = 1
            Title = "US Dollar"
            Code = "USD"
            Symbol = "$"
        }
        
        let snapshot = {
            Id = 1
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = Some broker
            BrokerAccount = Some brokerAccount
            Currency = currency
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
        }
        
        // Assert
        Assert.IsTrue(snapshot.BrokerAccount.IsSome)
        Assert.AreEqual("ACC123", snapshot.BrokerAccount.Value.AccountNumber)
        Assert.AreEqual(2, snapshot.BrokerAccount.Value.Id)

    [<Test>]
    member _.``BrokerFinancialSnapshot uses strongly-typed Currency reference`` () =
        // Arrange
        let currency = {
            Id = 1
            Title = "US Dollar"
            Code = "USD"
            Symbol = "$"
        }
        
        let snapshot = {
            Id = 1
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = None
            BrokerAccount = None
            Currency = currency
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
        }
        
        // Assert
        Assert.AreEqual("USD", snapshot.Currency.Code)
        Assert.AreEqual("$", snapshot.Currency.Symbol)
        Assert.AreEqual(1, snapshot.Currency.Id)

    [<Test>]
    member _.``BrokerFinancialSnapshot allows None values for optional references`` () =
        // Arrange
        let currency = {
            Id = 1
            Title = "US Dollar"
            Code = "USD"
            Symbol = "$"
        }
        
        let snapshot = {
            Id = 1
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = None
            BrokerAccount = None
            Currency = currency
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
        }
        
        // Assert
        Assert.IsTrue(snapshot.Broker.IsNone)
        Assert.IsTrue(snapshot.BrokerAccount.IsNone)
        Assert.IsNotNull(snapshot.Currency) // Currency is not optional