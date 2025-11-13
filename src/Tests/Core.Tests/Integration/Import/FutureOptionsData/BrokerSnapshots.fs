namespace Core.Tests.Integration

open System
open Binnaculum.Core.Models
open TestModels

/// <summary>
/// Future Options Broker Account Financial Snapshot data.
///
/// Contains expected broker account snapshots spanning the trading period.
/// Generated from actual core calculation results from FutureOptions.csv import.
///
/// Date Range: 2025-08-24 (first trade) through 2025-11-12 (test execution date)
/// Trading Activity: Pure future options premium strategies, no underlying shares held
/// Total Snapshots: 8 (covering all trading days with activity)
/// </summary>
module FutureOptionsBrokerSnapshots =

    /// <summary>
    /// Generate expected BrokerAccount financial snapshots with descriptions.
    ///
    /// All 8 snapshots from Aug 24 through Nov 12, 2025.
    /// </summary>
    let getBrokerAccountSnapshots
        (broker: Broker)
        (brokerAccount: BrokerAccount)
        (currency: Currency)
        : ExpectedSnapshot<BrokerFinancialSnapshot> list =
        [
          // Snapshot 0: 2025-08-24 - First trade (/MESU5 Call opened)
          { Data =
              { Id = 0
                Date = DateOnly(2025, 8, 24)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 1
                RealizedGains = 0.00m
                RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m
                UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m
                Commissions = 0.75m
                Fees = 0.52m
                Deposited = 0.00m
                Withdrawn = 0.00m
                DividendsReceived = 0.00m
                OptionsIncome = -15.75m
                OtherIncome = 0.00m
                OpenTrades = true
                NetCashFlow = -17.02m }
            Description = "Snapshot 0: 2025-08-24 - /MESU5 Call opened" }

          // Snapshot 1: 2025-08-25 - /MESU5 Call expired worthless
          { Data =
              { Id = 0
                Date = DateOnly(2025, 8, 25)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 2
                RealizedGains = 0.00m
                RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m
                UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m
                Commissions = 0.75m
                Fees = 0.52m
                Deposited = 0.00m
                Withdrawn = 0.00m
                DividendsReceived = 0.00m
                OptionsIncome = -15.75m
                OtherIncome = 0.00m
                OpenTrades = false
                NetCashFlow = -17.02m }
            Description = "Snapshot 1: 2025-08-25 - /MESU5 expired" }

          // Snapshot 2: 2025-09-02 - /MESZ5 Oct 31 Butterfly opened
          { Data =
              { Id = 0
                Date = DateOnly(2025, 9, 2)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 10
                RealizedGains = 0.00m
                RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m
                UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m
                Commissions = 6.75m
                Fees = 4.20m
                Deposited = 0.00m
                Withdrawn = 0.00m
                DividendsReceived = 0.00m
                OptionsIncome = 211.75m
                OtherIncome = 0.00m
                OpenTrades = true
                NetCashFlow = 200.80m }
            Description = "Snapshot 2: 2025-09-02 - /MESZ5 Oct 31 Butterfly opened" }

          // Snapshot 3: 2025-09-23 - /MESZ5 Nov 28 Butterfly opened
          { Data =
              { Id = 0
                Date = DateOnly(2025, 9, 23)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 18
                RealizedGains = 0.00m
                RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m
                UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m
                Commissions = 12.75m
                Fees = 7.88m
                Deposited = 0.00m
                Withdrawn = 0.00m
                DividendsReceived = 0.00m
                OptionsIncome = 519.25m
                OtherIncome = 0.00m
                OpenTrades = true
                NetCashFlow = 498.62m }
            Description = "Snapshot 3: 2025-09-23 - /MESZ5 Nov 28 Butterfly opened" }

          // Snapshot 4: 2025-10-14 - /MESH6 Feb 20 Multi-leg opened
          { Data =
              { Id = 0
                Date = DateOnly(2025, 10, 14)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 22
                RealizedGains = 0.00m
                RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m
                UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m
                Commissions = 15.75m
                Fees = 9.72m
                Deposited = 0.00m
                Withdrawn = 0.00m
                DividendsReceived = 0.00m
                OptionsIncome = 809.25m
                OtherIncome = 0.00m
                OpenTrades = true
                NetCashFlow = 783.78m }
            Description = "Snapshot 4: 2025-10-14 - /MESH6 Multi-leg opened" }

          // Snapshot 5: 2025-10-24 - Both /MESZ5 Nov 28 and /MESH6 positions closed
          { Data =
              { Id = 0
                Date = DateOnly(2025, 10, 24)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 34
                RealizedGains = 298.46m
                RealizedPercentage = 59.7805m
                UnrealizedGains = 0.00m
                UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m
                Commissions = 24.75m
                Fees = 15.24m
                Deposited = 0.00m
                Withdrawn = 0.00m
                DividendsReceived = 0.00m
                OptionsIncome = 539.25m
                OtherIncome = 0.00m
                OpenTrades = true
                NetCashFlow = 499.26m }
            Description = "Snapshot 5: 2025-10-24 - /MESZ5 Nov 28 & /MESH6 closed" }

          // Snapshot 6: 2025-10-31 - /MESZ5 Oct 31 Butterfly expired
          { Data =
              { Id = 0
                Date = DateOnly(2025, 10, 31)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 42
                RealizedGains = 1308.70m
                RealizedPercentage = 262.1279m
                UnrealizedGains = 0.00m
                UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m
                Commissions = 24.75m
                Fees = 15.24m
                Deposited = 0.00m
                Withdrawn = 0.00m
                DividendsReceived = 0.00m
                OptionsIncome = 539.25m
                OtherIncome = 0.00m
                OpenTrades = false
                NetCashFlow = 499.26m }
            Description = "Snapshot 6: 2025-10-31 - /MESZ5 Oct 31 expired (all closed)" }

          // Snapshot 7: 2025-11-12 - Today (no changes, same as Oct 31)
          { Data =
              { Id = 0
                Date = DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 42
                RealizedGains = 1308.70m
                RealizedPercentage = 262.1279m
                UnrealizedGains = 0.00m
                UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m
                Commissions = 24.75m
                Fees = 15.24m
                Deposited = 0.00m
                Withdrawn = 0.00m
                DividendsReceived = 0.00m
                OptionsIncome = 539.25m
                OtherIncome = 0.00m
                OpenTrades = false
                NetCashFlow = 499.26m }
            Description = "Snapshot 7: 2025-11-12 - Today (unchanged)" } ]
