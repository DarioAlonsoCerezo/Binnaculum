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
                CloseDate = Some(DateTime(2025, 11, 5, 0, 0, 1))
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
                CloseDate = Some(DateTime(2025, 11, 5, 0, 0, 1))
                Realized = 3433.80m
                RealizedToday = 0m
                Commissions = 52.00m
                Fees = 18.08m
                Premium = 3223.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 12245.00m
                CapitalDeployedToday = 0m
                Performance = 28.0425m }
            Description = "Operation 1: 2024-10-15" }

          // Operation 2: 2024-11-20
          { Data =
              { Id = 0
                BrokerAccount = brokerAccount
                Ticker = ticker
                Currency = currency
                IsOpen = false
                OpenDate = DateTime(2024, 11, 20, 0, 0, 1)
                CloseDate = Some(DateTime(2025, 11, 5, 0, 0, 1))
                Realized = 5805.01m
                RealizedToday = 0m
                Commissions = 113.00m
                Fees = 37.26m
                Premium = 1420.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 4673.00m
                CapitalDeployedToday = 0m
                Performance = 124.2246m }
            Description = "Operation 2: 2024-11-20" }

          // Operation 3: 2024-12-11
          { Data =
              { Id = 0
                BrokerAccount = brokerAccount
                Ticker = ticker
                Currency = currency
                IsOpen = false
                OpenDate = DateTime(2024, 12, 11, 0, 0, 1)
                CloseDate = Some(DateTime(2025, 11, 5, 0, 0, 1))
                Realized = 5577.20m
                RealizedToday = 0m
                Commissions = 116.00m
                Fees = 38.07m
                Premium = 1196.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 1043.00m
                CapitalDeployedToday = 0m
                Performance = 534.7271m }
            Description = "Operation 3: 2024-12-11" }

          // Operation 4: 2025-04-07
          { Data =
              { Id = 0
                BrokerAccount = brokerAccount
                Ticker = ticker
                Currency = currency
                IsOpen = false
                OpenDate = DateTime(2025, 4, 7, 0, 0, 1)
                CloseDate = Some(DateTime(2025, 11, 5, 0, 0, 1))
                Realized = 5611.85m
                RealizedToday = 0m
                Commissions = 121.00m
                Fees = 39.42m
                Premium = 1237.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 3200.00m
                CapitalDeployedToday = 0m
                Performance = 175.3704m }
            Description = "Operation 4: 2025-04-07" }

          // Operation 5: 2025-04-30
          { Data =
              { Id = 0
                BrokerAccount = brokerAccount
                Ticker = ticker
                Currency = currency
                IsOpen = false
                OpenDate = DateTime(2025, 4, 30, 0, 0, 1)
                CloseDate = Some(DateTime(2025, 11, 5, 0, 0, 1))
                Realized = 624.47m
                RealizedToday = 0m
                Commissions = 135.00m
                Fees = 45.69m
                Premium = 797.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 2000.00m
                CapitalDeployedToday = 0m
                Performance = 31.2235m }
            Description = "Operation 5: 2025-04-30" }

          // Operation 6: 2025-05-12
          { Data =
              { Id = 0
                BrokerAccount = brokerAccount
                Ticker = ticker
                Currency = currency
                IsOpen = false
                OpenDate = DateTime(2025, 5, 12, 0, 0, 1)
                CloseDate = Some(DateTime(2025, 11, 5, 0, 0, 1))
                Realized = 3938.01m
                RealizedToday = 0m
                Commissions = 218.00m
                Fees = 75.30m
                Premium = 4220.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 4950.00m
                CapitalDeployedToday = 0m
                Performance = 79.5558m }
            Description = "Operation 6: 2025-05-12" }

          // Operation 7: 2025-07-08
          { Data =
              { Id = 0
                BrokerAccount = brokerAccount
                Ticker = ticker
                Currency = currency
                IsOpen = false
                OpenDate = DateTime(2025, 7, 8, 0, 0, 1)
                CloseDate = Some(DateTime(2025, 11, 5, 0, 0, 1))
                Realized = 3800.49m
                RealizedToday = 0m
                Commissions = 220.00m
                Fees = 76.00m
                Premium = 4085.00m
                Dividends = 134.43m
                DividendTaxes = 20.16m
                CapitalDeployed = 0.00m
                CapitalDeployedToday = 0m
                Performance = 0.0000m }
            Description = "Operation 7: 2025-07-08" }

          // Operation 8: 2025-07-29
          { Data =
              { Id = 0
                BrokerAccount = brokerAccount
                Ticker = ticker
                Currency = currency
                IsOpen = false
                OpenDate = DateTime(2025, 7, 29, 0, 0, 1)
                CloseDate = Some(DateTime(2025, 11, 5, 0, 0, 1))
                Realized = 3942.98m
                RealizedToday = 0m
                Commissions = 230.00m
                Fees = 78.51m
                Premium = 4240.00m
                Dividends = 134.43m
                DividendTaxes = 20.16m
                CapitalDeployed = 10750.00m
                CapitalDeployedToday = 0m
                Performance = 36.6789m }
            Description = "Operation 8: 2025-07-29" }

          // Operation 9: 2025-09-02
          { Data =
              { Id = 0
                BrokerAccount = brokerAccount
                Ticker = ticker
                Currency = currency
                IsOpen = false
                OpenDate = DateTime(2025, 9, 2, 0, 0, 1)
                CloseDate = Some(DateTime(2025, 11, 5, 0, 0, 1))
                Realized = 3960.74m
                RealizedToday = 0m
                Commissions = 231.00m
                Fees = 78.75m
                Premium = 4259.00m
                Dividends = 134.43m
                DividendTaxes = 20.16m
                CapitalDeployed = 1000.00m
                CapitalDeployedToday = 0m
                Performance = 396.0740m }
            Description = "Operation 9: 2025-09-02" }

          // Operation 10: 2025-09-05
          { Data =
              { Id = 0
                BrokerAccount = brokerAccount
                Ticker = ticker
                Currency = currency
                IsOpen = false
                OpenDate = DateTime(2025, 9, 5, 0, 0, 1)
                CloseDate = Some(DateTime(2025, 11, 5, 0, 0, 1))
                Realized = 3566.90m
                RealizedToday = 0m
                Commissions = 235.00m
                Fees = 79.77m
                Premium = 3870.00m
                Dividends = 143.36m
                DividendTaxes = 21.50m
                CapitalDeployed = 1250.00m
                CapitalDeployedToday = 0m
                Performance = 285.3520m }
            Description = "Operation 10: 2025-09-05" }

          ]
