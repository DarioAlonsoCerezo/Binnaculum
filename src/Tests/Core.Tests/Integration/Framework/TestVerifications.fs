namespace Core.Tests.Integration

open System
open Binnaculum.Core.Models
open Binnaculum.Core.UI
open NUnit.Framework

/// <summary>
/// Reusable verification utilities for reactive integration tests.
///
/// Provides common assertion patterns:
/// - verifyBrokers(minCount) - Verify broker count
/// - verifyCurrencies(minCount) - Verify currency count
/// - verifyTickers(minCount) - Verify ticker count
/// - verifySnapshots(minCount) - Verify snapshot count
/// - verifyAccounts(minCount) - Verify account count
/// - verifyStandardCurrencies() - Verify USD and EUR present
/// - verifyFullDatabaseState() - Run all standard verifications
///
/// Holistic snapshot verification:
/// - verifyBrokerFinancialSnapshot(expected, actual) - Compare all BrokerFinancialSnapshot fields
/// - verifyTickerCurrencySnapshot(expected, actual) - Compare all TickerCurrencySnapshot fields
/// - formatValidationResults(results) - Format validation results as human-readable diff
///
/// USAGE:
/// ------
/// let (success, message) = TestVerifications.verifyBrokers 2
/// Assert.That(success, Is.True, message)
///
/// Or batch verify:
/// let verifications = TestVerifications.verifyFullDatabaseState()
/// for (success, message) in verifications do
///     Assert.That(success, Is.True, message)
///
/// Holistic snapshot verification:
/// let (allMatch, results) = TestVerifications.verifyBrokerFinancialSnapshot expected actual
/// if not allMatch then
///     let formatted = TestVerifications.formatValidationResults results
///     printfn "Snapshot mismatch:\n%s" formatted
///
/// See README.md for more examples.
/// </summary>
module TestVerifications =

    /// <summary>
    /// Represents the result of comparing a single field in a snapshot.
    /// Used by holistic snapshot verification functions.
    /// </summary>
    type ValidationResult = {
        Field: string          // Field name (e.g., "Deposited")
        Expected: string       // Expected value formatted as string
        Actual: string         // Actual value formatted as string
        Match: bool           // True if values match
    }

    /// <summary>
    /// Verifies that brokers were loaded with at least the minimum count.
    /// </summary>
    let verifyBrokers (minCount: int) : (bool * string) =
        let brokerCount = Collections.Brokers.Count
        let message = sprintf "Brokers loaded: %d" brokerCount

        if brokerCount >= minCount then
            (true, message)
        else
            (false, sprintf "%s (expected at least %d)" message minCount)

    /// <summary>
    /// Verifies that currencies were loaded with at least the minimum count.
    /// </summary>
    let verifyCurrencies (minCount: int) : (bool * string) =
        let currencyCount = Collections.Currencies.Count
        let message = sprintf "Currencies loaded: %d" currencyCount

        if currencyCount >= minCount then
            (true, message)
        else
            (false, sprintf "%s (expected at least %d)" message minCount)

    /// <summary>
    /// Verifies that tickers were loaded with at least the minimum count.
    /// </summary>
    let verifyTickers (minCount: int) : (bool * string) =
        let tickerCount = Collections.Tickers.Count
        let message = sprintf "Tickers loaded: %d" tickerCount

        if tickerCount >= minCount then
            (true, message)
        else
            (false, sprintf "%s (expected at least %d)" message minCount)

    /// <summary>
    /// Verifies that snapshots were loaded with at least the minimum count.
    /// </summary>
    let verifySnapshots (minCount: int) : (bool * string) =
        let snapshotCount = Collections.Snapshots.Count
        let message = sprintf "Snapshots loaded: %d" snapshotCount

        if snapshotCount >= minCount then
            (true, message)
        else
            (false, sprintf "%s (expected at least %d)" message minCount)

    /// <summary>
    /// Verifies that accounts were loaded with at least the minimum count.
    /// </summary>
    let verifyAccounts (minCount: int) : (bool * string) =
        let accountCount = Collections.Accounts.Count
        let message = sprintf "Accounts loaded: %d" accountCount

        if accountCount >= minCount then
            (true, message)
        else
            (false, sprintf "%s (expected at least %d)" message minCount)

    /// <summary>
    /// Verifies that a specific currency exists by code.
    /// </summary>
    let verifyCurrencyExists (currencyCode: string) : (bool * string) =
        let hasCurrency =
            Collections.Currencies.Items |> Seq.exists (fun c -> c.Code = currencyCode)

        let message = sprintf "Currency check: %s present" currencyCode

        if hasCurrency then
            (true, message)
        else
            (false, sprintf "%s (NOT FOUND)" message)

    /// <summary>
    /// Verifies that both USD and EUR currencies exist.
    /// </summary>
    let verifyStandardCurrencies () : (bool * string) =
        let hasUsd = Collections.Currencies.Items |> Seq.exists (fun c -> c.Code = "USD")
        let hasEur = Collections.Currencies.Items |> Seq.exists (fun c -> c.Code = "EUR")

        let message = sprintf "USD: %b, EUR: %b" hasUsd hasEur

        if hasUsd && hasEur then
            (true, sprintf "✅ %s" message)
        else
            (false, sprintf "❌ %s" message)

    /// <summary>
    /// Verifies collections state and returns formatted summary.
    /// Used for test reporting and validation.
    /// </summary>
    let verifyCollectionsState () : (bool * string) =
        let brokerCount = Collections.Brokers.Count
        let currencyCount = Collections.Currencies.Count
        let tickerCount = Collections.Tickers.Count
        let snapshotCount = Collections.Snapshots.Count
        let accountCount = Collections.Accounts.Count

        let summary =
            sprintf
                "Brokers: %d, Currencies: %d, Tickers: %d, Accounts: %d, Snapshots: %d"
                brokerCount
                currencyCount
                tickerCount
                accountCount
                snapshotCount

        let allLoaded = brokerCount > 0 && currencyCount > 0 && tickerCount > 0

        if allLoaded then (true, summary) else (false, summary)

    /// <summary>
    /// Runs all standard verifications for a fully initialized database.
    /// Returns list of verification results.
    /// </summary>
    let verifyFullDatabaseState () : (bool * string) list =
        [ ("Brokers", verifyBrokers 2)
          ("Currencies", verifyCurrencies 2)
          ("Tickers", verifyTickers 1)
          ("Standard Currencies", verifyStandardCurrencies ()) ]
        |> List.map (fun (name, (success, msg)) -> (success, sprintf "%s: %s" name msg))

    /// <summary>
    /// Verifies BrokerFinancialSnapshot by comparing all fields holistically.
    /// Pure function - no I/O, no async, just comparison.
    /// Returns: (allMatch, fieldResults)
    /// 
    /// Example:
    /// let expected: BrokerFinancialSnapshot = { Deposited = 5000m; ... }
    /// let actual = BrokerAccounts.GetLatestSnapshot(accountId).Financial
    /// let (allMatch, results) = verifyBrokerFinancialSnapshot expected actual
    /// Assert.That(allMatch, Is.True)
    /// </summary>
    let verifyBrokerFinancialSnapshot
        (expected: BrokerFinancialSnapshot)
        (actual: BrokerFinancialSnapshot)
        : (bool * ValidationResult list) =
        
        let results = [
            { Field = "Id"
              Expected = sprintf "%d" expected.Id
              Actual = sprintf "%d" actual.Id
              Match = expected.Id = actual.Id }
            
            { Field = "Date"
              Expected = expected.Date.ToString("yyyy-MM-dd")
              Actual = actual.Date.ToString("yyyy-MM-dd")
              Match = expected.Date = actual.Date }
            
            { Field = "MovementCounter"
              Expected = sprintf "%d" expected.MovementCounter
              Actual = sprintf "%d" actual.MovementCounter
              Match = expected.MovementCounter = actual.MovementCounter }
            
            { Field = "RealizedGains"
              Expected = sprintf "%.2f" expected.RealizedGains
              Actual = sprintf "%.2f" actual.RealizedGains
              Match = expected.RealizedGains = actual.RealizedGains }
            
            { Field = "RealizedPercentage"
              Expected = sprintf "%.4f" expected.RealizedPercentage
              Actual = sprintf "%.4f" actual.RealizedPercentage
              Match = expected.RealizedPercentage = actual.RealizedPercentage }
            
            { Field = "UnrealizedGains"
              Expected = sprintf "%.2f" expected.UnrealizedGains
              Actual = sprintf "%.2f" actual.UnrealizedGains
              Match = expected.UnrealizedGains = actual.UnrealizedGains }
            
            { Field = "UnrealizedGainsPercentage"
              Expected = sprintf "%.4f" expected.UnrealizedGainsPercentage
              Actual = sprintf "%.4f" actual.UnrealizedGainsPercentage
              Match = expected.UnrealizedGainsPercentage = actual.UnrealizedGainsPercentage }
            
            { Field = "Invested"
              Expected = sprintf "%.2f" expected.Invested
              Actual = sprintf "%.2f" actual.Invested
              Match = expected.Invested = actual.Invested }
            
            { Field = "Commissions"
              Expected = sprintf "%.2f" expected.Commissions
              Actual = sprintf "%.2f" actual.Commissions
              Match = expected.Commissions = actual.Commissions }
            
            { Field = "Fees"
              Expected = sprintf "%.2f" expected.Fees
              Actual = sprintf "%.2f" actual.Fees
              Match = expected.Fees = actual.Fees }
            
            { Field = "Deposited"
              Expected = sprintf "%.2f" expected.Deposited
              Actual = sprintf "%.2f" actual.Deposited
              Match = expected.Deposited = actual.Deposited }
            
            { Field = "Withdrawn"
              Expected = sprintf "%.2f" expected.Withdrawn
              Actual = sprintf "%.2f" actual.Withdrawn
              Match = expected.Withdrawn = actual.Withdrawn }
            
            { Field = "DividendsReceived"
              Expected = sprintf "%.2f" expected.DividendsReceived
              Actual = sprintf "%.2f" actual.DividendsReceived
              Match = expected.DividendsReceived = actual.DividendsReceived }
            
            { Field = "OptionsIncome"
              Expected = sprintf "%.2f" expected.OptionsIncome
              Actual = sprintf "%.2f" actual.OptionsIncome
              Match = expected.OptionsIncome = actual.OptionsIncome }
            
            { Field = "OtherIncome"
              Expected = sprintf "%.2f" expected.OtherIncome
              Actual = sprintf "%.2f" actual.OtherIncome
              Match = expected.OtherIncome = actual.OtherIncome }
            
            { Field = "OpenTrades"
              Expected = sprintf "%b" expected.OpenTrades
              Actual = sprintf "%b" actual.OpenTrades
              Match = expected.OpenTrades = actual.OpenTrades }
            
            { Field = "NetCashFlow"
              Expected = sprintf "%.2f" expected.NetCashFlow
              Actual = sprintf "%.2f" actual.NetCashFlow
              Match = expected.NetCashFlow = actual.NetCashFlow }
        ]
        
        let allMatch = results |> List.forall (fun r -> r.Match)
        (allMatch, results)

    /// <summary>
    /// Verifies TickerCurrencySnapshot by comparing all fields holistically.
    /// Pure function - no I/O, no async, just comparison.
    /// Returns: (allMatch, fieldResults)
    /// 
    /// Example:
    /// let expected: TickerCurrencySnapshot = { TotalShares = 100m; ... }
    /// let actual = Tickers.GetSnapshots(tickerId) |> Seq.head
    /// let (allMatch, results) = verifyTickerCurrencySnapshot expected actual
    /// Assert.That(allMatch, Is.True)
    /// </summary>
    let verifyTickerCurrencySnapshot
        (expected: TickerCurrencySnapshot)
        (actual: TickerCurrencySnapshot)
        : (bool * ValidationResult list) =
        
        // Use pattern matching to destructure - this forces the correct type resolution
        match (expected, actual) with
        | ({ Id = expId; Date = expDate; TotalShares = expShares; Weight = expWeight; 
             CostBasis = expCostBasis; RealCost = expRealCost; Dividends = expDividends; 
             Options = expOptions; TotalIncomes = expTotalIncomes; Unrealized = expUnrealized; 
             Realized = expRealized; Performance = expPerformance; LatestPrice = expLatestPrice; 
             OpenTrades = expOpenTrades },
           { Id = actId; Date = actDate; TotalShares = actShares; Weight = actWeight; 
             CostBasis = actCostBasis; RealCost = actRealCost; Dividends = actDividends; 
             Options = actOptions; TotalIncomes = actTotalIncomes; Unrealized = actUnrealized; 
             Realized = actRealized; Performance = actPerformance; LatestPrice = actLatestPrice; 
             OpenTrades = actOpenTrades }) ->
            
            let results = [
                { Field = "Id"
                  Expected = sprintf "%d" expId
                  Actual = sprintf "%d" actId
                  Match = expId = actId }
                
                { Field = "Date"
                  Expected = expDate.ToString("yyyy-MM-dd")
                  Actual = actDate.ToString("yyyy-MM-dd")
                  Match = expDate = actDate }
                
                { Field = "TotalShares"
                  Expected = sprintf "%.2f" expShares
                  Actual = sprintf "%.2f" actShares
                  Match = expShares = actShares }
                
                { Field = "Weight"
                  Expected = sprintf "%.4f" expWeight
                  Actual = sprintf "%.4f" actWeight
                  Match = expWeight = actWeight }
                
                { Field = "CostBasis"
                  Expected = sprintf "%.2f" expCostBasis
                  Actual = sprintf "%.2f" actCostBasis
                  Match = expCostBasis = actCostBasis }
                
                { Field = "RealCost"
                  Expected = sprintf "%.2f" expRealCost
                  Actual = sprintf "%.2f" actRealCost
                  Match = expRealCost = actRealCost }
                
                { Field = "Dividends"
                  Expected = sprintf "%.2f" expDividends
                  Actual = sprintf "%.2f" actDividends
                  Match = expDividends = actDividends }
                
                { Field = "Options"
                  Expected = sprintf "%.2f" expOptions
                  Actual = sprintf "%.2f" actOptions
                  Match = expOptions = actOptions }
                
                { Field = "TotalIncomes"
                  Expected = sprintf "%.2f" expTotalIncomes
                  Actual = sprintf "%.2f" actTotalIncomes
                  Match = expTotalIncomes = actTotalIncomes }
                
                { Field = "Unrealized"
                  Expected = sprintf "%.2f" expUnrealized
                  Actual = sprintf "%.2f" actUnrealized
                  Match = expUnrealized = actUnrealized }
                
                { Field = "Realized"
                  Expected = sprintf "%.2f" expRealized
                  Actual = sprintf "%.2f" actRealized
                  Match = expRealized = actRealized }
                
                { Field = "Performance"
                  Expected = sprintf "%.4f" expPerformance
                  Actual = sprintf "%.4f" actPerformance
                  Match = expPerformance = actPerformance }
                
                { Field = "LatestPrice"
                  Expected = sprintf "%.2f" expLatestPrice
                  Actual = sprintf "%.2f" actLatestPrice
                  Match = expLatestPrice = actLatestPrice }
                
                { Field = "OpenTrades"
                  Expected = sprintf "%b" expOpenTrades
                  Actual = sprintf "%b" actOpenTrades
                  Match = expOpenTrades = actOpenTrades }
            ]
            
            let allMatch = results |> List.forall (fun r -> r.Match)
            (allMatch, results)

    /// <summary>
    /// Format validation results as human-readable diff.
    /// Useful for logging or assertion messages.
    /// 
    /// Example output:
    ///   ✅ Deposited         : 5000.00 = 5000.00
    ///   ✅ Withdrawn         : 0.00 = 0.00
    ///   ❌ Realized          : -28.67 ≠ -30.00
    ///   ✅ Unrealized        : 83.04 = 83.04
    /// </summary>
    let formatValidationResults (results: ValidationResult list) : string =
        results
        |> List.map (fun r ->
            let icon = if r.Match then "✅" else "❌"
            let comparison = if r.Match then "=" else "≠"
            sprintf "  %s %-25s: %s %s %s" icon r.Field r.Expected comparison r.Actual
        )
        |> String.concat "\n"
