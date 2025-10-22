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
/// USAGE:
/// ------
/// let (success, message) = ReactiveTestVerifications.verifyBrokers 2
/// Assert.That(success, Is.True, message)
///
/// Or batch verify:
/// let verifications = ReactiveTestVerifications.verifyFullDatabaseState()
/// for (success, message) in verifications do
///     Assert.That(success, Is.True, message)
///
/// See README.md for more examples.
/// </summary>
module ReactiveTestVerifications =

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
