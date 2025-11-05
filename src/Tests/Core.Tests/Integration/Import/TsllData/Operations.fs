namespace Core.Tests.Integration

open System
open Binnaculum.Core.Models
open TestModels

/// <summary>
/// TSLL Auto-Import Operation data.
///
/// Contains all 4 expected operations representing distinct trading periods.
/// </summary>
module TsllOperations =

    /// <summary>
    /// Generate expected AutoImportOperation data for TSLL ticker.
    ///
    /// Includes all 4 operations from the TSLL import test.
    /// These represent distinct trading periods with their financial metrics.
    /// </summary>
    let getTSLLOperations
        (brokerAccount: BrokerAccount)
        (ticker: Ticker)
        (currency: Currency)
        : ExpectedOperation<AutoImportOperation> list =
        [
          // Operation 0: 2024-05-30
          { Data =
              { Id = 0
                BrokerAccount = brokerAccount
                Ticker = ticker
                Currency = currency
                IsOpen = false
                OpenDate = DateTime(2024, 5, 30, 0, 0, 1)
                // CloseDate is managed by database trigger (UpdatedAt), not verified in tests
                CloseDate = Some(DateTime(2024, 5, 30, 0, 0, 1))
                Realized = 13.86m
                RealizedToday = 0m
                Commissions = 1.00m
                Fees = 0.14m
                Premium = 15.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 700.00m
                CapitalDeployedToday = 0m
                Performance = 1.9800m }
            Description = "Operation 0: 2024-05-30" }

          // Operation 1: 2024-10-15
          { Data =
              { Id = 0
                BrokerAccount = brokerAccount
                Ticker = ticker
                Currency = currency
                IsOpen = false
                OpenDate = DateTime(2024, 10, 15, 0, 0, 1)
                CloseDate = Some(DateTime(2025, 10, 26, 0, 0, 1))
                Realized = 3155.38m
                RealizedToday = 0m
                Commissions = 52.00m
                Fees = 18.08m
                Premium = 3223.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 12245.00m
                CapitalDeployedToday = 0m
                Performance = 25.7687m }
            Description = "Operation 1: 2024-10-15" }

          // Operation 2: 2024-11-20
          { Data =
              { Id = 0
                BrokerAccount = brokerAccount
                Ticker = ticker
                Currency = currency
                IsOpen = false
                OpenDate = DateTime(2024, 11, 20, 0, 0, 1)
                CloseDate = Some(DateTime(2025, 10, 26, 0, 0, 1))
                Realized = 1275.36m
                RealizedToday = 0m
                Commissions = 113.00m
                Fees = 37.26m
                Premium = 1420.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 4673.00m
                CapitalDeployedToday = 0m
                Performance = 27.2921m }
            Description = "Operation 2: 2024-11-20" }

          // Operation 3: 2024-12-11
          { Data =
              { Id = 0
                BrokerAccount = brokerAccount
                Ticker = ticker
                Currency = currency
                IsOpen = false
                OpenDate = DateTime(2024, 12, 11, 0, 0, 1)
                CloseDate = None
                Realized = 1047.55m
                RealizedToday = 0m
                Commissions = 116.00m
                Fees = 38.07m
                Premium = 1196.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 1043.00m
                CapitalDeployedToday = 0m
                Performance = 100.4362m }
            Description = "Operation 3: 2024-12-11" } ]
