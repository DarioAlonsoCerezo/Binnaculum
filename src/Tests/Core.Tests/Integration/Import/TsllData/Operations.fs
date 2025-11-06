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
                Commissions = 51.00m
                Fees = 17.94m
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
                Commissions = 61.00m
                Fees = 19.18m
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
                Commissions = 3.00m
                Fees = 0.81m
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
                Commissions = 5.00m
                Fees = 1.35m
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
                Realized = 6385.84m
                RealizedToday = 0m
                Commissions = 14.00m
                Fees = 6.27m
                Premium = 797.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 2000.00m
                CapitalDeployedToday = 0m
                Performance = 319.2918m }
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
                Realized = 8703.38m
                RealizedToday = 0m
                Commissions = 83.00m
                Fees = 29.61m
                Premium = 4220.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                CapitalDeployed = 4950.00m
                CapitalDeployedToday = 0m
                Performance = 175.8258m }
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
                Realized = 8787.76m
                RealizedToday = 0m
                Commissions = 2.00m
                Fees = 0.70m
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
                Realized = 8930.25m
                RealizedToday = 0m
                Commissions = 10.00m
                Fees = 2.51m
                Premium = 4240.00m
                Dividends = 134.43m
                DividendTaxes = 20.16m
                CapitalDeployed = 10750.00m
                CapitalDeployedToday = 0m
                Performance = 83.0721m }
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
                Realized = 8948.01m
                RealizedToday = 0m
                Commissions = 1.00m
                Fees = 0.24m
                Premium = 4259.00m
                Dividends = 134.43m
                DividendTaxes = 20.16m
                CapitalDeployed = 1000.00m
                CapitalDeployedToday = 0m
                Performance = 894.8009m }
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
                Realized = 9211.07m
                RealizedToday = 0m
                Commissions = 4.00m
                Fees = 1.02m
                Premium = 3870.00m
                Dividends = 143.36m
                DividendTaxes = 21.50m
                CapitalDeployed = 1250.00m
                CapitalDeployedToday = 0m
                Performance = 736.8855m }
            Description = "Operation 10: 2025-09-05" }

          ]
