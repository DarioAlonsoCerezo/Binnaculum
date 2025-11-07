namespace Core.Tests.Integration

open System
open Binnaculum.Core.Models
open TestModels

/// <summary>
/// MPW Auto-Import Operation data.
///
/// Contains all 6 expected operations representing distinct trading periods.
/// Generated from actual core calculation results.
/// </summary>
module MpwOperations =

    /// <summary>
    /// Generate expected AutoImportOperation data for MPW ticker.
    ///
    /// Includes all 6 operations from the MPW import test.
    /// These represent distinct trading periods with their financial metrics,
    /// including equity share trading and options trading.
    /// </summary>
    let getMPWOperations
        (brokerAccount: BrokerAccount)
        (ticker: Ticker)
        (currency: Currency)
        : ExpectedOperation<AutoImportOperation> list =
        [
          // Operation 0: 2024-04-26
          { Data =
              { Id = 0; BrokerAccount = brokerAccount; Ticker = ticker; Currency = currency
                IsOpen = false
                OpenDate = DateTime(2024, 4, 26, 0, 0, 1)
                CloseDate = Some(DateTime(2025, 11, 7, 0, 0, 1))
                Realized = 5.46m; RealizedToday = 5.46m
                Commissions = 2.00m; Fees = 0.54m
                Premium = 8.00m; Dividends = 0.00m; DividendTaxes = 0.00m
                CapitalDeployed = 850.00m; CapitalDeployedToday = 0.00m
                Performance = 0.6424m }
            Description = "Operation 0: 2024-04-26" }
          // Operation 1: 2024-05-03
          { Data =
              { Id = 0; BrokerAccount = brokerAccount; Ticker = ticker; Currency = currency
                IsOpen = false
                OpenDate = DateTime(2024, 5, 3, 0, 0, 1)
                CloseDate = Some(DateTime(2025, 11, 7, 0, 0, 1))
                Realized = 5072.73m; RealizedToday = 4948.89m
                Commissions = 190.00m; Fees = 54.65m
                Premium = 352.00m; Dividends = 230.00m; DividendTaxes = 34.50m
                CapitalDeployed = 25900.00m; CapitalDeployedToday = 0.00m
                Performance = 19.5858m }
            Description = "Operation 1: 2024-05-03" }
          // Operation 2: 2024-10-24
          { Data =
              { Id = 0; BrokerAccount = brokerAccount; Ticker = ticker; Currency = currency
                IsOpen = false
                OpenDate = DateTime(2024, 10, 24, 0, 0, 1)
                CloseDate = Some(DateTime(2025, 11, 7, 0, 0, 1))
                Realized = -223.58m; RealizedToday = -226.31m
                Commissions = 6.00m; Fees = 1.58m
                Premium = -216.00m; Dividends = 0.00m; DividendTaxes = 0.00m
                CapitalDeployed = 1950.00m; CapitalDeployedToday = 0.00m
                Performance = -11.4656m }
            Description = "Operation 2: 2024-10-24" }
          // Operation 3: 2024-12-13
          { Data =
              { Id = 0; BrokerAccount = brokerAccount; Ticker = ticker; Currency = currency
                IsOpen = false
                OpenDate = DateTime(2024, 12, 13, 0, 0, 1)
                CloseDate = Some(DateTime(2025, 11, 7, 0, 0, 1))
                Realized = 19.17m; RealizedToday = -4.57m
                Commissions = 7.00m; Fees = 1.83m
                Premium = 28.00m; Dividends = 0.00m; DividendTaxes = 0.00m
                CapitalDeployed = 1400.00m; CapitalDeployedToday = 0.00m
                Performance = 1.3693m }
            Description = "Operation 3: 2024-12-13" }
          // Operation 4: 2025-02-18
          { Data =
              { Id = 0; BrokerAccount = brokerAccount; Ticker = ticker; Currency = currency
                IsOpen = false
                OpenDate = DateTime(2025, 2, 18, 0, 0, 1)
                CloseDate = Some(DateTime(2025, 11, 7, 0, 0, 1))
                Realized = 41.18m; RealizedToday = 102.45m
                Commissions = 3.00m; Fees = 0.82m
                Premium = 45.00m; Dividends = 0.00m; DividendTaxes = 0.00m
                CapitalDeployed = 200.00m; CapitalDeployedToday = 0.00m
                Performance = 20.5900m }
            Description = "Operation 4: 2025-02-18" }
          // Operation 5: 2025-08-26
          { Data =
              { Id = 0; BrokerAccount = brokerAccount; Ticker = ticker; Currency = currency
                IsOpen = true
                OpenDate = DateTime(2025, 8, 26, 0, 0, 1)
                CloseDate = None
                Realized = 71.27m; RealizedToday = 0.00m
                Commissions = 9.00m; Fees = 1.10m
                Premium = -390.00m; Dividends = 0.00m; DividendTaxes = 0.00m
                CapitalDeployed = 900.00m; CapitalDeployedToday = 0.00m
                Performance = 7.9189m }
            Description = "Operation 5: 2025-08-26" }
        ]
