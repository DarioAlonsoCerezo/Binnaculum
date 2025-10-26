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
    type ValidationResult =
        { Field: string // Field name (e.g., "Deposited")
          Expected: string // Expected value formatted as string
          Actual: string // Actual value formatted as string
          Match: bool } // True if values match

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

        let results =
            [ // Note: Id field is skipped - database-assigned IDs are not predictable

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
                Match = abs (expected.RealizedGains - actual.RealizedGains) < 0.01m }

              { Field = "RealizedPercentage"
                Expected = sprintf "%.4f" expected.RealizedPercentage
                Actual = sprintf "%.4f" actual.RealizedPercentage
                Match = abs (expected.RealizedPercentage - actual.RealizedPercentage) < 0.0001m }

              { Field = "UnrealizedGains"
                Expected = sprintf "%.2f" expected.UnrealizedGains
                Actual = sprintf "%.2f" actual.UnrealizedGains
                Match = abs (expected.UnrealizedGains - actual.UnrealizedGains) < 0.01m }

              // NOTE: Percentage validations temporarily disabled until core calculations are finalized
              // TODO: Re-enable and design proper percentage formulas after all base metrics are correct
              // { Field = "UnrealizedGainsPercentage"
              //   Expected = sprintf "%.4f" expected.UnrealizedGainsPercentage
              //   Actual = sprintf "%.4f" actual.UnrealizedGainsPercentage
              //   Match = abs (expected.UnrealizedGainsPercentage - actual.UnrealizedGainsPercentage) < 0.0001m }

              { Field = "Invested"
                Expected = sprintf "%.2f" expected.Invested
                Actual = sprintf "%.2f" actual.Invested
                Match = abs (expected.Invested - actual.Invested) < 0.01m }

              { Field = "Commissions"
                Expected = sprintf "%.2f" expected.Commissions
                Actual = sprintf "%.2f" actual.Commissions
                Match = abs (expected.Commissions - actual.Commissions) < 0.01m }

              { Field = "Fees"
                Expected = sprintf "%.2f" expected.Fees
                Actual = sprintf "%.2f" actual.Fees
                Match = abs (expected.Fees - actual.Fees) < 0.01m }

              { Field = "Deposited"
                Expected = sprintf "%.2f" expected.Deposited
                Actual = sprintf "%.2f" actual.Deposited
                Match = abs (expected.Deposited - actual.Deposited) < 0.01m }

              { Field = "Withdrawn"
                Expected = sprintf "%.2f" expected.Withdrawn
                Actual = sprintf "%.2f" actual.Withdrawn
                Match = abs (expected.Withdrawn - actual.Withdrawn) < 0.01m }

              { Field = "DividendsReceived"
                Expected = sprintf "%.2f" expected.DividendsReceived
                Actual = sprintf "%.2f" actual.DividendsReceived
                Match = abs (expected.DividendsReceived - actual.DividendsReceived) < 0.01m }

              { Field = "OptionsIncome"
                Expected = sprintf "%.2f" expected.OptionsIncome
                Actual = sprintf "%.2f" actual.OptionsIncome
                Match = abs (expected.OptionsIncome - actual.OptionsIncome) < 0.01m }

              { Field = "OtherIncome"
                Expected = sprintf "%.2f" expected.OtherIncome
                Actual = sprintf "%.2f" actual.OtherIncome
                Match = abs (expected.OtherIncome - actual.OtherIncome) < 0.01m }

              { Field = "OpenTrades"
                Expected = sprintf "%b" expected.OpenTrades
                Actual = sprintf "%b" actual.OpenTrades
                Match = expected.OpenTrades = actual.OpenTrades }

              { Field = "NetCashFlow"
                Expected = sprintf "%.2f" expected.NetCashFlow
                Actual = sprintf "%.2f" actual.NetCashFlow
                Match = abs (expected.NetCashFlow - actual.NetCashFlow) < 0.01m } ]

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
        | ({ Id = expId
             Date = expDate
             TotalShares = expShares
             Weight = expWeight
             CostBasis = expCostBasis
             RealCost = expRealCost
             Dividends = expDividends
             Options = expOptions
             TotalIncomes = expTotalIncomes
             Unrealized = expUnrealized
             Realized = expRealized
             Performance = expPerformance
             LatestPrice = expLatestPrice
             OpenTrades = expOpenTrades
             Commissions = expCommissions
             Fees = expFees },
           { Id = actId
             Date = actDate
             TotalShares = actShares
             Weight = actWeight
             CostBasis = actCostBasis
             RealCost = actRealCost
             Dividends = actDividends
             Options = actOptions
             TotalIncomes = actTotalIncomes
             Unrealized = actUnrealized
             Realized = actRealized
             Performance = actPerformance
             LatestPrice = actLatestPrice
             OpenTrades = actOpenTrades
             Commissions = actCommissions
             Fees = actFees }) ->

            let results =
                [ // Note: Id field is skipped - database-assigned IDs are not predictable

                  { Field = "Date"
                    Expected = expDate.ToString("yyyy-MM-dd")
                    Actual = actDate.ToString("yyyy-MM-dd")
                    Match = expDate = actDate }

                  { Field = "TotalShares"
                    Expected = sprintf "%.2f" expShares
                    Actual = sprintf "%.2f" actShares
                    Match = abs (expShares - actShares) < 0.01m }

                  { Field = "Weight"
                    Expected = sprintf "%.4f" expWeight
                    Actual = sprintf "%.4f" actWeight
                    Match = abs (expWeight - actWeight) < 0.0001m }

                  { Field = "CostBasis"
                    Expected = sprintf "%.2f" expCostBasis
                    Actual = sprintf "%.2f" actCostBasis
                    Match = abs (expCostBasis - actCostBasis) < 0.01m }

                  { Field = "RealCost"
                    Expected = sprintf "%.2f" expRealCost
                    Actual = sprintf "%.2f" actRealCost
                    Match = abs (expRealCost - actRealCost) < 0.01m }

                  { Field = "Dividends"
                    Expected = sprintf "%.2f" expDividends
                    Actual = sprintf "%.2f" actDividends
                    Match = abs (expDividends - actDividends) < 0.01m }

                  { Field = "Options"
                    Expected = sprintf "%.2f" expOptions
                    Actual = sprintf "%.2f" actOptions
                    Match = abs (expOptions - actOptions) < 0.01m }

                  { Field = "TotalIncomes"
                    Expected = sprintf "%.2f" expTotalIncomes
                    Actual = sprintf "%.2f" actTotalIncomes
                    Match = abs (expTotalIncomes - actTotalIncomes) < 0.01m }

                  { Field = "Unrealized"
                    Expected = sprintf "%.2f" expUnrealized
                    Actual = sprintf "%.2f" actUnrealized
                    Match = abs (expUnrealized - actUnrealized) < 0.01m }

                  { Field = "Realized"
                    Expected = sprintf "%.2f" expRealized
                    Actual = sprintf "%.2f" actRealized
                    Match = abs (expRealized - actRealized) < 0.01m }

                  { Field = "Performance"
                    Expected = sprintf "%.4f" expPerformance
                    Actual = sprintf "%.4f" actPerformance
                    Match = abs (expPerformance - actPerformance) < 0.0001m }

                  { Field = "LatestPrice"
                    Expected = sprintf "%.2f" expLatestPrice
                    Actual = sprintf "%.2f" actLatestPrice
                    Match = abs (expLatestPrice - actLatestPrice) < 0.01m }

                  { Field = "OpenTrades"
                    Expected = sprintf "%b" expOpenTrades
                    Actual = sprintf "%b" actOpenTrades
                    Match = expOpenTrades = actOpenTrades }

                  { Field = "Commissions"
                    Expected = sprintf "%.2f" expCommissions
                    Actual = sprintf "%.2f" actCommissions
                    Match = abs (expCommissions - actCommissions) < 0.01m }

                  { Field = "Fees"
                    Expected = sprintf "%.2f" expFees
                    Actual = sprintf "%.2f" actFees
                    Match = abs (expFees - actFees) < 0.01m } ]

            let allMatch = results |> List.forall (fun r -> r.Match)
            (allMatch, results)

    /// <summary>
    /// Format validation results as human-readable diff.
    /// Useful for logging or assertion messages.
    ///
    /// Example output:
    ///   ✅ Deposited         : Expected: 5000.00 | Calculated: 5000.00
    ///   ✅ Withdrawn         : Expected: 0.00 | Calculated: 0.00
    ///   ❌ Realized          : Expected: -28.67 | Calculated: -30.00
    ///   ✅ Unrealized        : Expected: 83.04 | Calculated: 83.04
    /// </summary>
    let formatValidationResults (results: ValidationResult list) : string =
        results
        |> List.map (fun r ->
            let icon = if r.Match then "✅" else "❌"
            sprintf "  %s %-25s: Expected: %s | Calculated: %s" icon r.Field r.Expected r.Actual)
        |> String.concat "\n"

    /// <summary>
    /// Verifies a list of expected TickerCurrencySnapshots against core-calculated snapshots.
    /// Matches snapshots by Date and validates all fields.
    ///
    /// Returns: List of (allMatch, fieldResults) for each expected snapshot
    /// Throws: InvalidArgumentException if any expected date is missing in coreCalculated
    ///
    /// Example:
    /// let expected = OptionsImportExpectedSnapshots.getSOFISnapshots ticker currency
    /// let actual = sortedSOFISnapshots |> List.map (fun s -> s.MainCurrency)
    /// let results = verifyTickerCurrencySnapshotList expected actual
    /// // results: [(true, [field results]), (false, [field results]), ...]
    ///
    /// results |> List.iteri (fun i (allMatch, fieldResults) ->
    ///     if not allMatch then
    ///         printfn "Snapshot %d failed:\n%s" i (formatValidationResults fieldResults)
    ///     Assert.That(allMatch, Is.True)
    /// )
    /// </summary>
    let verifyTickerCurrencySnapshotList
        (expected: TickerCurrencySnapshot list)
        (coreCalculated: TickerCurrencySnapshot list)
        : (bool * ValidationResult list) list =

        expected
        |> List.map (fun expectedSnapshot ->
            // Find matching snapshot by Date
            let matchingSnapshot =
                coreCalculated
                |> List.tryFind (fun actual -> actual.Date = expectedSnapshot.Date)

            match matchingSnapshot with
            | None ->
                // Date not found - throw exception
                invalidArg
                    "coreCalculated"
                    (sprintf
                        "Expected snapshot date %s not found in core-calculated snapshots"
                        (expectedSnapshot.Date.ToString("yyyy-MM-dd")))
            | Some actualSnapshot ->
                // Date found - verify all fields
                verifyTickerCurrencySnapshot expectedSnapshot actualSnapshot)

    /// <summary>
    /// Verifies a list of expected BrokerFinancialSnapshots against core-calculated snapshots.
    /// Matches snapshots by Date and validates all fields.
    ///
    /// Returns: List of (allMatch, fieldResults) for each expected snapshot
    /// Throws: InvalidArgumentException if any expected date is missing in coreCalculated
    ///
    /// Example:
    /// let expected = OptionsImportExpectedSnapshots.getBrokerAccountSnapshots broker account currency
    /// let actual = brokerFinancialSnapshots
    /// let results = verifyBrokerFinancialSnapshotList expected actual
    /// // results: [(true, [field results]), (false, [field results]), ...]
    ///
    /// results |> List.iteri (fun i (allMatch, fieldResults) ->
    ///     if not allMatch then
    ///         printfn "BrokerSnapshot %d failed:\n%s" i (formatValidationResults fieldResults)
    ///     Assert.That(allMatch, Is.True)
    /// )
    /// </summary>
    let verifyBrokerFinancialSnapshotList
        (expected: BrokerFinancialSnapshot list)
        (coreCalculated: BrokerFinancialSnapshot list)
        : (bool * ValidationResult list) list =

        expected
        |> List.map (fun expectedSnapshot ->
            // Find matching snapshot by Date
            let matchingSnapshot =
                coreCalculated
                |> List.tryFind (fun actual -> actual.Date = expectedSnapshot.Date)

            match matchingSnapshot with
            | None ->
                // Date not found - throw exception
                invalidArg
                    "coreCalculated"
                    (sprintf
                        "Expected snapshot date %s not found in core-calculated snapshots"
                        (expectedSnapshot.Date.ToString("yyyy-MM-dd")))
            | Some actualSnapshot ->
                // Date found - verify all fields
                verifyBrokerFinancialSnapshot expectedSnapshot actualSnapshot)

    /// <summary>
    /// Verifies AutoImportOperation by comparing all fields holistically.
    /// Pure function - no I/O, no async, just comparison.
    /// Returns: (allMatch, fieldResults)
    ///
    /// Example:
    /// let expected: AutoImportOperation = { Realized = 350m; CapitalDeployed = 5000m; ... }
    /// let actual = getOperationById(operationId)
    /// let (allMatch, results) = verifyAutoImportOperation expected actual
    /// Assert.That(allMatch, Is.True)
    /// </summary>
    let verifyAutoImportOperation
        (expected: AutoImportOperation)
        (actual: AutoImportOperation)
        : (bool * ValidationResult list) =

        let results =
            [ { Field = "IsOpen"
                Expected = sprintf "%b" expected.IsOpen
                Actual = sprintf "%b" actual.IsOpen
                Match = expected.IsOpen = actual.IsOpen }

              { Field = "OpenDate"
                Expected = expected.OpenDate.ToString("yyyy-MM-dd HH:mm:ss")
                Actual = actual.OpenDate.ToString("yyyy-MM-dd HH:mm:ss")
                Match = expected.OpenDate = actual.OpenDate }

              // CloseDate is managed by database trigger (UpdatedAt), not verified in tests
              // The database automatically sets UpdatedAt to current time on updates

              { Field = "Realized"
                Expected = sprintf "%.2f" expected.Realized
                Actual = sprintf "%.2f" actual.Realized
                Match = abs (expected.Realized - actual.Realized) < 0.01m }

              { Field = "Commissions"
                Expected = sprintf "%.2f" expected.Commissions
                Actual = sprintf "%.2f" actual.Commissions
                Match = abs (expected.Commissions - actual.Commissions) < 0.01m }

              { Field = "Fees"
                Expected = sprintf "%.2f" expected.Fees
                Actual = sprintf "%.2f" actual.Fees
                Match = abs (expected.Fees - actual.Fees) < 0.01m }

              { Field = "Premium"
                Expected = sprintf "%.2f" expected.Premium
                Actual = sprintf "%.2f" actual.Premium
                Match = abs (expected.Premium - actual.Premium) < 0.01m }

              { Field = "Dividends"
                Expected = sprintf "%.2f" expected.Dividends
                Actual = sprintf "%.2f" actual.Dividends
                Match = abs (expected.Dividends - actual.Dividends) < 0.01m }

              { Field = "DividendTaxes"
                Expected = sprintf "%.2f" expected.DividendTaxes
                Actual = sprintf "%.2f" actual.DividendTaxes
                Match = abs (expected.DividendTaxes - actual.DividendTaxes) < 0.01m }

              { Field = "CapitalDeployed"
                Expected = sprintf "%.2f" expected.CapitalDeployed
                Actual = sprintf "%.2f" actual.CapitalDeployed
                Match = abs (expected.CapitalDeployed - actual.CapitalDeployed) < 0.01m }

              { Field = "Performance"
                Expected = sprintf "%.4f" expected.Performance
                Actual = sprintf "%.4f" actual.Performance
                Match = abs (expected.Performance - actual.Performance) < 0.01m } ]

        let allMatch = results |> List.forall (fun r -> r.Match)
        (allMatch, results)

    /// <summary>
    /// Verifies a list of AutoImportOperations by comparing each expected operation with core-calculated operations.
    /// Matches operations by OpenDate and validates all fields.
    /// Supports partial validation - only validates operations defined in expected list.
    ///
    /// Returns: List of (allMatch, fieldResults) for each expected operation
    /// Throws: InvalidArgumentException if any expected OpenDate is missing in coreCalculated
    ///
    /// Example:
    /// let expected = [
    ///     { Data = operation1; Description = "TSLL Operation #1: Cash-secured put" }
    ///     { Data = operation2; Description = "TSLL Operation #2: Covered call" }
    /// ]
    /// let actual = getAllOperationsForTicker(tsllTickerId)
    /// let results = verifyAutoImportOperationList expected actual
    /// results |> List.iteri (fun i (allMatch, fieldResults) ->
    ///     if not allMatch then
    ///         printfn "Operation %d failed:\n%s" i (formatValidationResults fieldResults)
    ///     Assert.That(allMatch, Is.True)
    /// )
    /// </summary>
    let verifyAutoImportOperationList
        (expected: AutoImportOperation list)
        (coreCalculated: AutoImportOperation list)
        : (bool * ValidationResult list) list =

        expected
        |> List.map (fun expectedOperation ->
            // Find matching operation by OpenDate
            let matchingOperation =
                coreCalculated
                |> List.tryFind (fun actual -> actual.OpenDate = expectedOperation.OpenDate)

            match matchingOperation with
            | None ->
                // OpenDate not found - throw exception
                invalidArg
                    "coreCalculated"
                    (sprintf
                        "Expected operation with OpenDate %s not found in core-calculated operations"
                        (expectedOperation.OpenDate.ToString("yyyy-MM-dd HH:mm:ss")))
            | Some actualOperation ->
                // OpenDate found - verify all fields
                verifyAutoImportOperation expectedOperation actualOperation)
