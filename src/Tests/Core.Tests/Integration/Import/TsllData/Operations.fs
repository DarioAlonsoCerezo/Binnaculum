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
                Realized = 3419.94m
                RealizedToday = 0m
                Commissions = 51.00m
                Fees = 17.94m
                Premium = 3208.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 30445.00m
                CapitalDeployedToday = 0m
                Performance = 11.2332m }
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
                Realized = 2371.21m
                RealizedToday = 0m
                Commissions = 61.00m
                Fees = 19.18m
                Premium = -1803.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 31657.98m
                CapitalDeployedToday = 0m
                Performance = 7.4901m }
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
                Realized = -227.81m
                RealizedToday = 0m
                Commissions = 3.00m
                Fees = 0.81m
                Premium = -224.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 1043.00m
                CapitalDeployedToday = 0m
                Performance = -21.8418m }
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
                Realized = 34.65m
                RealizedToday = 0m
                Commissions = 5.00m
                Fees = 1.35m
                Premium = 41.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 3200.00m
                CapitalDeployedToday = 0m
                Performance = 1.0828m }
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
                Realized = 773.98m
                RealizedToday = 0m
                Commissions = 14.00m
                Fees = 6.27m
                Premium = -440.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 14601.05m
                CapitalDeployedToday = 0m
                Performance = 5.3009m }
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
                Realized = 2317.54m
                RealizedToday = 0m
                Commissions = 83.00m
                Fees = 29.61m
                Premium = 3423.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 28214.34m
                CapitalDeployedToday = 0m
                Performance = 8.2141m }
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
                Realized = 84.38m
                RealizedToday = 0m
                Commissions = 2.00m
                Fees = 0.70m
                Premium = -135.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 1048.00m
                CapitalDeployedToday = 0m
                Performance = 8.0515m }
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
                Realized = 142.49m
                RealizedToday = 0m
                Commissions = 10.00m
                Fees = 2.51m
                Premium = 155.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 10750.00m
                CapitalDeployedToday = 0m
                Performance = 1.3255m }
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
                Realized = 17.76m
                RealizedToday = 0m
                Commissions = 1.00m
                Fees = 0.24m
                Premium = 19.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 1000.00m
                CapitalDeployedToday = 0m
                Performance = 1.7760m }
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
                Realized = 263.06m
                RealizedToday = 0m
                Commissions = 4.00m
                Fees = 1.02m
                Premium = -389.00m
                Dividends = 8.93m
                DividendTaxes = 1.34m
                CapitalDeployed = 2583.00m
                CapitalDeployedToday = 0m
                Performance = 10.1843m }
            Description = "Operation 10: 2025-09-05" }

          ]
