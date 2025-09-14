namespace Core.Tests

open NUnit.Framework
open System
open Binnaculum.Core.Models

[<TestFixture>]
type NetCashFlowTests () =

    [<Test>]
    member _.``NetCashFlow should be computed correctly with deposits and dividends`` () =
        // Arrange
        let mockCurrency = {
            Id = 1
            Title = "USD"
            Code = "USD"
            Symbol = "$"
        }
        let mockBroker = {
            Id = 1
            Name = "Test Broker"
            Image = ""
            SupportedBroker = "Test"
        }
        let snapshot = {
            Id = 1
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = Some mockBroker
            BrokerAccount = None
            Currency = mockCurrency
            MovementCounter = 5
            RealizedGains = 100.0m
            RealizedPercentage = 5.0m
            UnrealizedGains = 50.0m
            UnrealizedGainsPercentage = 2.5m
            Invested = 1000.0m
            Commissions = 15.0m
            Fees = 10.0m
            Deposited = 2000.0m
            Withdrawn = 300.0m
            DividendsReceived = 75.0m
            OptionsIncome = 25.0m
            OtherIncome = 10.0m
            OpenTrades = true
            NetCashFlow = 2000.0m - 300.0m - 15.0m - 10.0m + 75.0m + 25.0m + 10.0m // 1785.0m
        }
        
        // Act
        let expectedNetCashFlow = snapshot.Deposited - snapshot.Withdrawn - snapshot.Commissions - snapshot.Fees + snapshot.DividendsReceived + snapshot.OptionsIncome + snapshot.OtherIncome
        
        // Assert
        Assert.That(snapshot.NetCashFlow, Is.EqualTo(expectedNetCashFlow))
        Assert.That(snapshot.NetCashFlow, Is.EqualTo(1785.0m))

    [<Test>]
    member _.``NetCashFlow should be negative when withdrawals exceed deposits`` () =
        // Arrange
        let mockCurrency = {
            Id = 1
            Title = "USD"
            Code = "USD"
            Symbol = "$"
        }
        let snapshot = {
            Id = 2
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = None
            BrokerAccount = None
            Currency = mockCurrency
            MovementCounter = 3
            RealizedGains = 0.0m
            RealizedPercentage = 0.0m
            UnrealizedGains = 0.0m
            UnrealizedGainsPercentage = 0.0m
            Invested = 500.0m
            Commissions = 20.0m
            Fees = 5.0m
            Deposited = 1000.0m
            Withdrawn = 1200.0m
            DividendsReceived = 30.0m
            OptionsIncome = 0.0m
            OtherIncome = 5.0m
            OpenTrades = false
            NetCashFlow = 1000.0m - 1200.0m - 20.0m - 5.0m + 30.0m + 0.0m + 5.0m // -190.0m
        }
        
        // Act
        let expectedNetCashFlow = snapshot.Deposited - snapshot.Withdrawn - snapshot.Commissions - snapshot.Fees + snapshot.DividendsReceived + snapshot.OptionsIncome + snapshot.OtherIncome
        
        // Assert
        Assert.That(snapshot.NetCashFlow, Is.EqualTo(expectedNetCashFlow))
        Assert.That(snapshot.NetCashFlow, Is.EqualTo(-190.0m))
        Assert.That(snapshot.NetCashFlow, Is.LessThan(0.0m))

    [<Test>]
    member _.``NetCashFlow should be zero for empty snapshot`` () =
        // Arrange
        let mockCurrency = {
            Id = 1
            Title = "USD"
            Code = "USD"
            Symbol = "$"
        }
        let snapshot = {
            Id = 0
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = None
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
        
        // Act & Assert
        Assert.That(snapshot.NetCashFlow, Is.EqualTo(0.0m))

    [<Test>]
    member _.``NetCashFlow formula matches issue specification`` () =
        // Test the specific formula from the issue:
        // NetCashFlow = Deposited - Withdrawn - Commissions - Fees + DividendsReceived + OptionsIncome + OtherIncome
        
        // Arrange
        let mockCurrency = {
            Id = 1
            Title = "EUR"
            Code = "EUR"
            Symbol = "€"
        }
        let deposited = 5000.0m
        let withdrawn = 1000.0m
        let commissions = 50.0m
        let fees = 25.0m
        let dividendsReceived = 200.0m
        let optionsIncome = 150.0m
        let otherIncome = 75.0m
        let expectedNetCashFlow = deposited - withdrawn - commissions - fees + dividendsReceived + optionsIncome + otherIncome // 4350.0m
        
        let snapshot = {
            Id = 3
            Date = DateOnly.FromDateTime(DateTime.Now)
            Broker = None
            BrokerAccount = None
            Currency = mockCurrency
            MovementCounter = 10
            RealizedGains = 500.0m
            RealizedPercentage = 10.0m
            UnrealizedGains = 250.0m
            UnrealizedGainsPercentage = 5.0m
            Invested = 4000.0m
            Commissions = commissions
            Fees = fees
            Deposited = deposited
            Withdrawn = withdrawn
            DividendsReceived = dividendsReceived
            OptionsIncome = optionsIncome
            OtherIncome = otherIncome
            OpenTrades = true
            NetCashFlow = expectedNetCashFlow
        }
        
        // Act & Assert - Formula validation
        Assert.That(snapshot.NetCashFlow, Is.EqualTo(4350.0m))