namespace Core.Tests.Unit.Verifications

open NUnit.Framework
open System
open Binnaculum.Core.Models
open Core.Tests.Integration

/// <summary>
/// Unit tests for holistic snapshot verification functions.
/// Tests verifyBrokerFinancialSnapshot and verifyTickerCurrencySnapshot.
/// </summary>
[<TestFixture>]
type SnapshotVerificationTests() =

    /// <summary>
    /// Helper to create a minimal BrokerFinancialSnapshot for testing
    /// </summary>
    let createBrokerSnapshot deposited withdrawn optionsIncome realizedGains unrealizedGains movementCounter =
        {
            Id = 1
            Date = DateOnly(2023, 1, 1)
            Broker = None
            BrokerAccount = None
            Currency = { Id = 1; Title = "US Dollar"; Code = "USD"; Symbol = "$" }
            MovementCounter = movementCounter
            RealizedGains = realizedGains
            RealizedPercentage = 0m
            UnrealizedGains = unrealizedGains
            UnrealizedGainsPercentage = 0m
            Invested = 0m
            Commissions = 0m
            Fees = 0m
            Deposited = deposited
            Withdrawn = withdrawn
            DividendsReceived = 0m
            OptionsIncome = optionsIncome
            OtherIncome = 0m
            OpenTrades = false
            NetCashFlow = deposited - withdrawn
        }

    /// <summary>
    /// Helper to create a minimal TickerCurrencySnapshot for testing
    /// </summary>
    let createTickerSnapshot totalShares unrealized realized optionsIncome =
        {
            Id = 1
            Date = DateOnly(2023, 1, 1)
            Ticker = { Id = 1; Symbol = "AAPL"; Image = None; Name = Some "Apple Inc." }
            Currency = { Id = 1; Title = "US Dollar"; Code = "USD"; Symbol = "$" }
            TotalShares = totalShares
            Weight = 0m
            CostBasis = 0m
            RealCost = 0m
            Dividends = 0m
            Options = optionsIncome
            TotalIncomes = 0m
            Unrealized = unrealized
            Realized = realized
            Performance = 0m
            LatestPrice = 0m
            OpenTrades = false
            Commissions = 0m
            Fees = 0m
        }

    [<Test>]
    member _.``verifyBrokerFinancialSnapshot returns true when all fields match``() =
        // Arrange
        let snapshot = createBrokerSnapshot 5000m 0m 54.37m -28.67m 83.04m 16
        
        // Act
        let (allMatch, results) = TestVerifications.verifyBrokerFinancialSnapshot snapshot snapshot
        
        // Assert
        Assert.That(allMatch, Is.True, "All fields should match when comparing identical snapshots")
        Assert.That(results.Length, Is.GreaterThan(0), "Should have validation results")
        Assert.That(results |> List.forall (fun r -> r.Match), Is.True, "All individual field results should match")

    [<Test>]
    member _.``verifyBrokerFinancialSnapshot detects Deposited mismatch``() =
        // Arrange
        let expected = createBrokerSnapshot 5000m 0m 54.37m -28.67m 83.04m 16
        let actual = createBrokerSnapshot 4999m 0m 54.37m -28.67m 83.04m 16
        
        // Act
        let (allMatch, results) = TestVerifications.verifyBrokerFinancialSnapshot expected actual
        
        // Assert
        Assert.That(allMatch, Is.False, "Should detect mismatch")
        let depositedResult = results |> List.find (fun r -> r.Field = "Deposited")
        Assert.That(depositedResult.Match, Is.False, "Deposited field should not match")
        Assert.That(depositedResult.Expected, Is.EqualTo("5000.00"))
        Assert.That(depositedResult.Actual, Is.EqualTo("4999.00"))

    [<Test>]
    member _.``verifyBrokerFinancialSnapshot detects OptionsIncome mismatch``() =
        // Arrange
        let expected = createBrokerSnapshot 5000m 0m 54.37m -28.67m 83.04m 16
        let actual = createBrokerSnapshot 5000m 0m 50.00m -28.67m 83.04m 16
        
        // Act
        let (allMatch, results) = TestVerifications.verifyBrokerFinancialSnapshot expected actual
        
        // Assert
        Assert.That(allMatch, Is.False, "Should detect mismatch")
        let optionsResult = results |> List.find (fun r -> r.Field = "OptionsIncome")
        Assert.That(optionsResult.Match, Is.False, "OptionsIncome field should not match")

    [<Test>]
    member _.``verifyBrokerFinancialSnapshot detects multiple mismatches``() =
        // Arrange
        let expected = createBrokerSnapshot 5000m 0m 54.37m -28.67m 83.04m 16
        let actual = createBrokerSnapshot 4999m 100m 50.00m -30.00m 80.00m 15
        
        // Act
        let (allMatch, results) = TestVerifications.verifyBrokerFinancialSnapshot expected actual
        
        // Assert
        Assert.That(allMatch, Is.False, "Should detect mismatches")
        let mismatches = results |> List.filter (fun r -> not r.Match)
        // Expected differences: Deposited, Withdrawn, OptionsIncome, RealizedGains, UnrealizedGains, MovementCounter, NetCashFlow
        Assert.That(mismatches.Length, Is.EqualTo(7), "Should detect 7 mismatched fields")

    [<Test>]
    member _.``verifyTickerCurrencySnapshot returns true when all fields match``() =
        // Arrange
        let snapshot = createTickerSnapshot 100m 250.50m -50.25m 75.00m
        
        // Act
        let (allMatch, results) = TestVerifications.verifyTickerCurrencySnapshot snapshot snapshot
        
        // Assert
        Assert.That(allMatch, Is.True, "All fields should match when comparing identical snapshots")
        Assert.That(results.Length, Is.GreaterThan(0), "Should have validation results")
        Assert.That(results |> List.forall (fun r -> r.Match), Is.True, "All individual field results should match")

    [<Test>]
    member _.``verifyTickerCurrencySnapshot detects TotalShares mismatch``() =
        // Arrange
        let expected = createTickerSnapshot 100m 250.50m -50.25m 75.00m
        let actual = createTickerSnapshot 99m 250.50m -50.25m 75.00m
        
        // Act
        let (allMatch, results) = TestVerifications.verifyTickerCurrencySnapshot expected actual
        
        // Assert
        Assert.That(allMatch, Is.False, "Should detect mismatch")
        let sharesResult = results |> List.find (fun r -> r.Field = "TotalShares")
        Assert.That(sharesResult.Match, Is.False, "TotalShares field should not match")
        Assert.That(sharesResult.Expected, Is.EqualTo("100.00"))
        Assert.That(sharesResult.Actual, Is.EqualTo("99.00"))

    [<Test>]
    member _.``verifyTickerCurrencySnapshot detects Unrealized mismatch``() =
        // Arrange
        let expected = createTickerSnapshot 100m 250.50m -50.25m 75.00m
        let actual = createTickerSnapshot 100m 200.00m -50.25m 75.00m
        
        // Act
        let (allMatch, results) = TestVerifications.verifyTickerCurrencySnapshot expected actual
        
        // Assert
        Assert.That(allMatch, Is.False, "Should detect mismatch")
        let unrealizedResult = results |> List.find (fun r -> r.Field = "Unrealized")
        Assert.That(unrealizedResult.Match, Is.False, "Unrealized field should not match")

    [<Test>]
    member _.``formatValidationResults produces readable output``() =
        // Arrange
        let expected = createBrokerSnapshot 5000m 0m 54.37m -28.67m 83.04m 16
        let actual = createBrokerSnapshot 4999m 100m 54.37m -28.67m 83.04m 16
        let (_, results) = TestVerifications.verifyBrokerFinancialSnapshot expected actual
        
        // Act
        let formatted = TestVerifications.formatValidationResults results
        
        // Assert
        Assert.That(formatted, Is.Not.Null)
        Assert.That(formatted, Is.Not.Empty)
        Assert.That(formatted, Does.Contain("✅"), "Should contain success icon for matching fields")
        Assert.That(formatted, Does.Contain("❌"), "Should contain error icon for mismatched fields")
        Assert.That(formatted, Does.Contain("Deposited"), "Should contain field names")
        Assert.That(formatted, Does.Contain("5000.00"), "Should contain expected values")
        Assert.That(formatted, Does.Contain("4999.00"), "Should contain actual values")

    [<Test>]
    member _.``formatValidationResults shows all fields``() =
        // Arrange
        let snapshot = createBrokerSnapshot 5000m 0m 54.37m -28.67m 83.04m 16
        let (_, results) = TestVerifications.verifyBrokerFinancialSnapshot snapshot snapshot
        
        // Act
        let formatted = TestVerifications.formatValidationResults results
        
        // Assert - Check that key field names are present
        Assert.That(formatted, Does.Contain("Deposited"))
        Assert.That(formatted, Does.Contain("Withdrawn"))
        Assert.That(formatted, Does.Contain("OptionsIncome"))
        Assert.That(formatted, Does.Contain("RealizedGains"))
        Assert.That(formatted, Does.Contain("UnrealizedGains"))
        Assert.That(formatted, Does.Contain("MovementCounter"))
        Assert.That(formatted, Does.Contain("NetCashFlow"))

    [<Test>]
    member _.``verifyTickerCurrencySnapshot detects Commissions mismatch``() =
        // Arrange
        let expected = createTickerSnapshot 100m 250.50m -50.25m 75.00m
        let actual = { createTickerSnapshot 100m 250.50m -50.25m 75.00m with Commissions = 10.50m }
        
        // Act
        let (allMatch, results) = TestVerifications.verifyTickerCurrencySnapshot expected actual
        
        // Assert
        Assert.That(allMatch, Is.False, "Should detect mismatch")
        let commissionsResult = results |> List.find (fun r -> r.Field = "Commissions")
        Assert.That(commissionsResult.Match, Is.False, "Commissions field should not match")
        Assert.That(commissionsResult.Expected, Is.EqualTo("0.00"))
        Assert.That(commissionsResult.Actual, Is.EqualTo("10.50"))

    [<Test>]
    member _.``verifyTickerCurrencySnapshot detects Fees mismatch``() =
        // Arrange
        let expected = createTickerSnapshot 100m 250.50m -50.25m 75.00m
        let actual = { createTickerSnapshot 100m 250.50m -50.25m 75.00m with Fees = 5.25m }
        
        // Act
        let (allMatch, results) = TestVerifications.verifyTickerCurrencySnapshot expected actual
        
        // Assert
        Assert.That(allMatch, Is.False, "Should detect mismatch")
        let feesResult = results |> List.find (fun r -> r.Field = "Fees")
        Assert.That(feesResult.Match, Is.False, "Fees field should not match")
        Assert.That(feesResult.Expected, Is.EqualTo("0.00"))
        Assert.That(feesResult.Actual, Is.EqualTo("5.25"))
