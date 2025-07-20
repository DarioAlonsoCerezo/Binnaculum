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
        Assert.That(snapshot.Id, NUnit.Framework.Is.EqualTo(123))

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
        Assert.That(snapshot.Broker.IsSome, NUnit.Framework.Is.True)
        Assert.That(snapshot.Broker.Value.Name, NUnit.Framework.Is.EqualTo("Test Broker"))

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
        Assert.That(snapshot.BrokerAccount.IsSome, NUnit.Framework.Is.True)
        Assert.That(snapshot.BrokerAccount.Value.AccountNumber, NUnit.Framework.Is.EqualTo("ACC123"))
        Assert.That(snapshot.BrokerAccount.Value.Id, NUnit.Framework.Is.EqualTo(2))

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
        Assert.That(snapshot.Currency.Code, NUnit.Framework.Is.EqualTo("USD"))
        Assert.That(snapshot.Currency.Symbol, NUnit.Framework.Is.EqualTo("$"))
        Assert.That(snapshot.Currency.Id, NUnit.Framework.Is.EqualTo(1))

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
        Assert.That(snapshot.Broker.IsNone, NUnit.Framework.Is.True)
        Assert.That(snapshot.BrokerAccount.IsNone, NUnit.Framework.Is.True)
        Assert.That(snapshot.Currency, NUnit.Framework.Is.Not.Null)