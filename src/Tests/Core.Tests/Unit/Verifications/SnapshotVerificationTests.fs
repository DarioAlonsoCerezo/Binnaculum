namespace Core.Tests.Unit.Verifications

open Microsoft.VisualStudio.TestTools.UnitTesting
open System
open Binnaculum.Core.Models
open Core.Tests.Integration

/// <summary>
/// Unit tests for holistic snapshot verification functions.
/// Tests verifyBrokerFinancialSnapshot and verifyTickerCurrencySnapshot.
/// </summary>
[<TestClass>]
type SnapshotVerificationTests() =

    /// <summary>
    /// Helper to create a minimal BrokerFinancialSnapshot for testing
    /// </summary>
    let createBrokerSnapshot deposited withdrawn optionsIncome realizedGains unrealizedGains movementCounter =
        { Id = 1
          Date = DateOnly(2023, 1, 1)
          Broker = None
          BrokerAccount = None
          Currency =
            { Id = 1
              Title = "US Dollar"
              Code = "USD"
              Symbol = "$" }
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
          NetCashFlow = deposited - withdrawn }

    /// <summary>
    /// Helper to create a minimal TickerCurrencySnapshot for testing
    /// </summary>
    let createTickerSnapshot totalShares unrealized realized optionsIncome =
        { Id = 1
          Date = DateOnly(2023, 1, 1)
          Ticker =
            { Id = 1
              Symbol = "AAPL"
              Image = None
              Name = Some "Apple Inc."
              OptionsEnabled = true
              OptionContractMultiplier = 100 }
          Currency =
            { Id = 1
              Title = "US Dollar"
              Code = "USD"
              Symbol = "$" }
          TotalShares = totalShares
          Weight = 0m
          CostBasis = 0m
          RealCost = 0m
          Dividends = 0m
          DividendTaxes = 0m
          Options = optionsIncome
          TotalIncomes = 0m
          CapitalDeployed = 0m
          Realized = realized
          Performance = 0m
          OpenTrades = false
          Commissions = 0m
          Fees = 0m }

    [<TestMethod>]
    member _.``verifyBrokerFinancialSnapshot returns true when all fields match``() =
        // Arrange
        let snapshot = createBrokerSnapshot 5000m 0m 54.37m -28.67m 83.04m 16

        // Act
        let (allMatch, results) =
            TestVerifications.verifyBrokerFinancialSnapshot snapshot snapshot

        // Assert
        Assert.IsTrue(allMatch, "All fields should match when comparing identical snapshots")
        Assert.IsTrue(results.Length > 0, "Should have validation results")
        Assert.IsTrue(results |> List.forall (fun r -> r.Match), "All individual field results should match")

    [<TestMethod>]
    member _.``verifyBrokerFinancialSnapshot detects Deposited mismatch``() =
        // Arrange
        let expected = createBrokerSnapshot 5000m 0m 54.37m -28.67m 83.04m 16
        let actual = createBrokerSnapshot 4999m 0m 54.37m -28.67m 83.04m 16

        // Act
        let (allMatch, results) =
            TestVerifications.verifyBrokerFinancialSnapshot expected actual

        // Assert
        Assert.IsFalse(allMatch, "Should detect mismatch")
        let depositedResult = results |> List.find (fun r -> r.Field = "Deposited")
        Assert.IsFalse(depositedResult.Match, "Deposited field should not match")
        Assert.AreEqual("5000.00", depositedResult.Expected)
        Assert.AreEqual("4999.00", depositedResult.Actual)

    [<TestMethod>]
    member _.``verifyBrokerFinancialSnapshot detects OptionsIncome mismatch``() =
        // Arrange
        let expected = createBrokerSnapshot 5000m 0m 54.37m -28.67m 83.04m 16
        let actual = createBrokerSnapshot 5000m 0m 50.00m -28.67m 83.04m 16

        // Act
        let (allMatch, results) =
            TestVerifications.verifyBrokerFinancialSnapshot expected actual

        // Assert
        Assert.IsFalse(allMatch, "Should detect mismatch")
        let optionsResult = results |> List.find (fun r -> r.Field = "OptionsIncome")
        Assert.IsFalse(optionsResult.Match, "OptionsIncome field should not match")

    [<TestMethod>]
    member _.``verifyBrokerFinancialSnapshot detects multiple mismatches``() =
        // Arrange
        let expected = createBrokerSnapshot 5000m 0m 54.37m -28.67m 83.04m 16
        let actual = createBrokerSnapshot 4999m 100m 50.00m -30.00m 80.00m 15

        // Act
        let (allMatch, results) =
            TestVerifications.verifyBrokerFinancialSnapshot expected actual

        // Assert
        Assert.IsFalse(allMatch, "Should detect mismatches")
        let mismatches = results |> List.filter (fun r -> not r.Match)
        // Expected differences: Deposited, Withdrawn, OptionsIncome, RealizedGains, UnrealizedGains, MovementCounter, NetCashFlow
        Assert.AreEqual(7, mismatches.Length, "Should detect 7 mismatched fields")

    [<TestMethod>]
    member _.``verifyTickerCurrencySnapshot returns true when all fields match``() =
        // Arrange
        let snapshot = createTickerSnapshot 100m 250.50m -50.25m 75.00m

        // Act
        let (allMatch, results) =
            TestVerifications.verifyTickerCurrencySnapshot snapshot snapshot

        // Assert
        Assert.IsTrue(allMatch, "All fields should match when comparing identical snapshots")
        Assert.IsTrue(results.Length > 0, "Should have validation results")
        Assert.IsTrue(results |> List.forall (fun r -> r.Match), "All individual field results should match")

    [<TestMethod>]
    member _.``verifyTickerCurrencySnapshot detects TotalShares mismatch``() =
        // Arrange
        let expected = createTickerSnapshot 100m 250.50m -50.25m 75.00m
        let actual = createTickerSnapshot 99m 250.50m -50.25m 75.00m

        // Act
        let (allMatch, results) =
            TestVerifications.verifyTickerCurrencySnapshot expected actual

        // Assert
        Assert.IsFalse(allMatch, "Should detect mismatch")
        let sharesResult = results |> List.find (fun r -> r.Field = "TotalShares")
        Assert.IsFalse(sharesResult.Match, "TotalShares field should not match")
        Assert.AreEqual("100.00", sharesResult.Expected)
        Assert.AreEqual("99.00", sharesResult.Actual)

    [<TestMethod>]
    member _.``verifyTickerCurrencySnapshot detects Options mismatch``() =
        // Arrange
        let expected = createTickerSnapshot 100m 250.50m -50.25m 75.00m
        let actual = createTickerSnapshot 100m 250.50m -50.25m 80.00m // Different options

        // Act
        let (allMatch, results) =
            TestVerifications.verifyTickerCurrencySnapshot expected actual

        // Assert
        Assert.IsFalse(allMatch, "Should detect mismatch")
        let optionsResult = results |> List.find (fun r -> r.Field = "Options")
        Assert.IsFalse(optionsResult.Match, "Options field should not match")

    [<TestMethod>]
    member _.``formatValidationResults produces readable output``() =
        // Arrange
        let expected = createBrokerSnapshot 5000m 0m 54.37m -28.67m 83.04m 16
        let actual = createBrokerSnapshot 4999m 100m 54.37m -28.67m 83.04m 16
        let (_, results) = TestVerifications.verifyBrokerFinancialSnapshot expected actual

        // Act
        let formatted = TestVerifications.formatValidationResults results

        // Assert
        Assert.IsNotNull(formatted)
        Assert.IsTrue(formatted.Length > 0)
        StringAssert.Contains(formatted, "✅", "Should contain success icon for matching fields")
        StringAssert.Contains(formatted, "❌", "Should contain error icon for mismatched fields")
        StringAssert.Contains(formatted, "Deposited", "Should contain field names")
        StringAssert.Contains(formatted, "5000.00", "Should contain expected values")
        StringAssert.Contains(formatted, "4999.00", "Should contain actual values")

    [<TestMethod>]
    member _.``formatValidationResults shows all fields``() =
        // Arrange
        let snapshot = createBrokerSnapshot 5000m 0m 54.37m -28.67m 83.04m 16
        let (_, results) = TestVerifications.verifyBrokerFinancialSnapshot snapshot snapshot

        // Act
        let formatted = TestVerifications.formatValidationResults results

        // Assert - Check that key field names are present
        StringAssert.Contains(formatted, "Deposited")
        StringAssert.Contains(formatted, "Withdrawn")
        StringAssert.Contains(formatted, "OptionsIncome")
        StringAssert.Contains(formatted, "RealizedGains")
        StringAssert.Contains(formatted, "UnrealizedGains")
        StringAssert.Contains(formatted, "MovementCounter")
        StringAssert.Contains(formatted, "NetCashFlow")

    [<TestMethod>]
    member _.``verifyTickerCurrencySnapshot detects Commissions mismatch``() =
        // Arrange
        let expected = createTickerSnapshot 100m 250.50m -50.25m 75.00m

        let actual =
            { createTickerSnapshot 100m 250.50m -50.25m 75.00m with
                Commissions = 10.50m }

        // Act
        let (allMatch, results) =
            TestVerifications.verifyTickerCurrencySnapshot expected actual

        // Assert
        Assert.IsFalse(allMatch, "Should detect mismatch")
        let commissionsResult = results |> List.find (fun r -> r.Field = "Commissions")
        Assert.IsFalse(commissionsResult.Match, "Commissions field should not match")
        Assert.AreEqual("0.00", commissionsResult.Expected)
        Assert.AreEqual("10.50", commissionsResult.Actual)

    [<TestMethod>]
    member _.``verifyTickerCurrencySnapshot detects Fees mismatch``() =
        // Arrange
        let expected = createTickerSnapshot 100m 250.50m -50.25m 75.00m

        let actual =
            { createTickerSnapshot 100m 250.50m -50.25m 75.00m with
                Fees = 5.25m }

        // Act
        let (allMatch, results) =
            TestVerifications.verifyTickerCurrencySnapshot expected actual

        // Assert
        Assert.IsFalse(allMatch, "Should detect mismatch")
        let feesResult = results |> List.find (fun r -> r.Field = "Fees")
        Assert.IsFalse(feesResult.Match, "Fees field should not match")
        Assert.AreEqual("0.00", feesResult.Expected)
        Assert.AreEqual("5.25", feesResult.Actual)
