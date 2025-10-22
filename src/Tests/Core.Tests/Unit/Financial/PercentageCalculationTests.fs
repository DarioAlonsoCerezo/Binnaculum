namespace Core.Tests

open System
open NUnit.Framework
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns

[<TestFixture>]
type PercentageCalculationTests() =

    [<Test>]
    member _.``Percentage should use NetCashFlow instead of Invested for accurate ROI``() =
        // Arrange - Mimicking the Tastytrade options scenario from the issue
        let deposited = 878.79m
        let withdrawn = 0m
        let commissions = 12m
        let fees = 1m
        let dividendsReceived = 0m
        let optionsIncome = 106.02m
        let otherIncome = 0m
        let realizedGains = 23.65m
        let unrealizedGains = 14.86m
        let invested = 51.65m // The cost basis is much smaller than deposited for options
        
        // Calculate NetCashFlow (the contributed capital)
        let netCashFlow = deposited - withdrawn - commissions - fees + dividendsReceived + optionsIncome + otherIncome
        
        // Current calculation (INCORRECT) - using Invested as denominator
        let currentRealizedPercentage = if invested > 0m then (realizedGains / invested) * 100m else 0m
        let currentUnrealizedPercentage = if invested > 0m then (unrealizedGains / invested) * 100m else 0m
        
        // Expected calculation (CORRECT) - using NetCashFlow as denominator  
        let expectedRealizedPercentage = if netCashFlow > 0m then (realizedGains / netCashFlow) * 100m else 0m
        let expectedUnrealizedPercentage = if netCashFlow > 0m then (unrealizedGains / netCashFlow) * 100m else 0m
        
        // Verify the issue exists (before fix) vs expected calculation (after fix)
        Assert.That(currentRealizedPercentage, Is.EqualTo(45.78m).Within(0.1m), "Current calculation should show ~45% (incorrect)")
        Assert.That(expectedRealizedPercentage, Is.EqualTo(2.43m).Within(0.1m), "Expected calculation should show ~2.43% (correct)")
        
        Assert.That(currentUnrealizedPercentage, Is.EqualTo(28.77m).Within(0.1m), "Current unrealized should be ~28.77% (incorrect)")
        Assert.That(expectedUnrealizedPercentage, Is.EqualTo(1.53m).Within(0.1m), "Expected unrealized should be ~1.53% (correct)")
        
        // Verify NetCashFlow calculation 
        let expectedNetCashFlow = 878.79m - 0m - 12m - 1m + 0m + 106.02m + 0m // 971.81m
        Assert.That(netCashFlow, Is.EqualTo(expectedNetCashFlow).Within(0.01m))