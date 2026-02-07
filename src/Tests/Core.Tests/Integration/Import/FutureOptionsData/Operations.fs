namespace Core.Tests.Integration

open System
open Binnaculum.Core.Models
open TestModels

/// <summary>
/// Future Options Auto-Import Operation data.
///
/// Contains expected operations representing distinct future options trading strategies.
/// Generated from actual core calculation results from FutureOptions.csv import.
///
/// Strategies:
/// 1. /MESU5 Aug 25 Call (expired worthless - loss)
/// 2. /MESZ5 Oct 31 Put Butterfly + Nov 28 Put Butterfly (combined into one operation - profit)
/// 3. /MESH6 Feb 20 Multi-leg (closed early for profit)
/// </summary>
module FutureOptionsOperations =

    /// <summary>
    /// Generate expected AutoImportOperation data for Future Options trading.
    ///
    /// NOTE: The system consolidates multiple strategies on the same ticker into a single operation.
    /// This is why we see 3 operations total (one per ticker) instead of 4 (one per strategy).
    /// </summary>
    let getFutureOptionsOperations
        (brokerAccount: BrokerAccount)
        (mesu5Ticker: Ticker)
        (mesz5Ticker: Ticker)
        (mesh6Ticker: Ticker)
        (currency: Currency)
        : ExpectedOperation<AutoImportOperation> list =
        [
          // Operation 0: /MESU5 Aug 25 Call - Expired worthless
          { Data =
              { Id = 0
                BrokerAccount = brokerAccount
                Ticker = mesu5Ticker
                Currency = currency
                IsOpen = false
                OpenDate = DateTime(2025, 8, 24, 0, 0, 1)
                CloseDate = Some(DateTime(2025, 11, 12, 20, 35, 18))
                Realized = 0.00m
                RealizedToday = 0.00m
                Commissions = 0.75m
                Fees = 0.52m
                Premium = -15.75m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 651000.00m
                CapitalDeployedToday = 0.00m
                Performance = 0.0000m }
            Description = "Operation 0: /MESU5 Aug 24-25 Call (expired worthless)" }

          // Operation 1: /MESZ5 Combined Strategies - Oct 31 + Nov 28 Put Butterflies
          { Data =
              { Id = 0
                BrokerAccount = brokerAccount
                Ticker = mesz5Ticker
                Currency = currency
                IsOpen = false
                OpenDate = DateTime(2025, 9, 2, 0, 0, 1)
                CloseDate = Some(DateTime(2025, 11, 12, 20, 35, 18))
                Realized = 1208.38m
                RealizedToday = 1010.24m
                Commissions = 18.00m
                Fees = 11.04m
                Premium = 445.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 9375000.00m
                CapitalDeployedToday = 0.00m
                Performance = 0.0129m }
            Description = "Operation 1: /MESZ5 Sep 2-Nov 12 Combined Butterflies" }

          // Operation 2: /MESH6 Feb 20 Multi-leg - Closed early for profit
          { Data =
              { Id = 0
                BrokerAccount = brokerAccount
                Ticker = mesh6Ticker
                Currency = currency
                IsOpen = false
                OpenDate = DateTime(2025, 10, 14, 0, 0, 1)
                CloseDate = Some(DateTime(2025, 11, 12, 20, 35, 18))
                Realized = 100.32m
                RealizedToday = 100.32m
                Commissions = 6.00m
                Fees = 3.68m
                Premium = 110.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 2195000.00m
                CapitalDeployedToday = 0.00m
                Performance = 0.0046m }
            Description = "Operation 2: /MESH6 Oct 14-24 Multi-leg strategy" } ]
