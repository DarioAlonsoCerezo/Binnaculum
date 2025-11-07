namespace Core.Tests.Integration

open System
open Binnaculum.Core.Models
open TestModels

/// <summary>
/// MPW Broker Account Snapshot data.
///
/// Contains all 67 expected broker account financial snapshots for MPW trading.
/// Generated from actual core calculation results.
/// </summary>
module MpwBrokerSnapshots =

    /// <summary>
    /// Generate expected BrokerAccount financial snapshots with descriptions.
    ///
    /// Includes all 67 snapshots from the MPW import test.
    /// Snapshots span from 2024-04-26 (first trade) through 2025-11-07 (today).
    /// Includes cash deposits, dividends, option premiums, and realized gains/losses.
    /// </summary>
    let getBrokerAccountSnapshots
        (broker: Broker)
        (brokerAccount: BrokerAccount)
        (currency: Currency)
        : ExpectedSnapshot<BrokerFinancialSnapshot> list =
        [
          // BrokerSnapshot 0: 2024-04-26
          { Data =
              { Id = 0; Date = DateOnly(2024, 4, 26)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 2
                RealizedGains = 0.00m; RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 2.00m; Fees = 0.27m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 0.00m; OptionsIncome = 15.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 12.73m }
            Description = "BrokerSnapshot 0: 2024-04-26" }
          // BrokerSnapshot 1: 2024-04-29
          { Data =
              { Id = 0; Date = DateOnly(2024, 4, 29)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 4
                RealizedGains = 5.46m; RealizedPercentage = 100.0000m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 2.00m; Fees = 0.54m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 0.00m; OptionsIncome = 8.00m; OtherIncome = 0.00m
                OpenTrades = false; NetCashFlow = 5.46m }
            Description = "BrokerSnapshot 1: 2024-04-29" }
          // BrokerSnapshot 2: 2024-05-03
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 3)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 6
                RealizedGains = 5.46m; RealizedPercentage = 44.7908m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 4.00m; Fees = 0.81m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 0.00m; OptionsIncome = 17.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 12.19m }
            Description = "BrokerSnapshot 2: 2024-05-03" }
          // BrokerSnapshot 3: 2024-05-06
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 6)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 12
                RealizedGains = 5.46m; RealizedPercentage = 9.6791m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 10.00m; Fees = 1.59m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 0.00m; OptionsIncome = 68.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 56.41m }
            Description = "BrokerSnapshot 3: 2024-05-06" }
          // BrokerSnapshot 4: 2024-05-09
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 9)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 23
                RealizedGains = -9.65m; RealizedPercentage = -88.2084m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 13.00m; Fees = 3.06m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 0.00m; OptionsIncome = 27.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 10.94m }
            Description = "BrokerSnapshot 4: 2024-05-09" }
          // BrokerSnapshot 5: 2024-05-10
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 10)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 26
                RealizedGains = -9.65m; RealizedPercentage = -5.0086m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 15.00m; Fees = 3.33m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 0.00m; OptionsIncome = 211.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 192.67m }
            Description = "BrokerSnapshot 5: 2024-05-10" }
          // BrokerSnapshot 6: 2024-05-13
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 13)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 31
                RealizedGains = 46.03m; RealizedPercentage = 100.0000m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 15.00m; Fees = 3.97m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 0.00m; OptionsIncome = 65.00m; OtherIncome = 0.00m
                OpenTrades = false; NetCashFlow = 46.03m }
            Description = "BrokerSnapshot 6: 2024-05-13" }
          // BrokerSnapshot 7: 2024-05-15
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 15)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 39
                RealizedGains = 46.03m; RealizedPercentage = 44.2852m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 23.00m; Fees = 5.06m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 0.00m; OptionsIncome = 132.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 103.94m }
            Description = "BrokerSnapshot 7: 2024-05-15" }
          // BrokerSnapshot 8: 2024-05-16
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 16)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 41
                RealizedGains = 46.03m; RealizedPercentage = 27.1307m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 25.00m; Fees = 5.34m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 0.00m; OptionsIncome = 200.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 169.66m }
            Description = "BrokerSnapshot 8: 2024-05-16" }
          // BrokerSnapshot 9: 2024-05-20
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 20)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 57
                RealizedGains = 85.93m; RealizedPercentage = 47.0669m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 33.00m; Fees = 7.43m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 0.00m; OptionsIncome = 223.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 182.57m }
            Description = "BrokerSnapshot 9: 2024-05-20" }
          // BrokerSnapshot 10: 2024-05-23
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 23)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 77
                RealizedGains = 165.31m; RealizedPercentage = 58.6206m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 43.00m; Fees = 10.00m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 0.00m; OptionsIncome = 335.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 282.00m }
            Description = "BrokerSnapshot 10: 2024-05-23" }
          // BrokerSnapshot 11: 2024-05-31
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 31)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 97
                RealizedGains = 140.73m; RealizedPercentage = 32.0263m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 53.00m; Fees = 12.58m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 0.00m; OptionsIncome = 505.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 439.42m }
            Description = "BrokerSnapshot 11: 2024-05-31" }
          // BrokerSnapshot 12: 2024-06-03
          { Data =
              { Id = 0; Date = DateOnly(2024, 6, 3)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 117
                RealizedGains = -141.85m; RealizedPercentage = -22.6301m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 63.00m; Fees = 15.18m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 0.00m; OptionsIncome = 705.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 626.82m }
            Description = "BrokerSnapshot 12: 2024-06-03" }
          // BrokerSnapshot 13: 2024-06-14
          { Data =
              { Id = 0; Date = DateOnly(2024, 6, 14)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 157
                RealizedGains = -141.85m; RealizedPercentage = -16.4626m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 83.00m; Fees = 20.35m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 0.00m; OptionsIncome = 965.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 861.65m }
            Description = "BrokerSnapshot 13: 2024-06-14" }
          // BrokerSnapshot 14: 2024-07-01
          { Data =
              { Id = 0; Date = DateOnly(2024, 7, 1)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 177
                RealizedGains = 345.55m; RealizedPercentage = 32.0232m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 93.00m; Fees = 22.94m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 0.00m; OptionsIncome = 1195.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 1079.06m }
            Description = "BrokerSnapshot 14: 2024-07-01" }
          // BrokerSnapshot 15: 2024-07-02
          { Data =
              { Id = 0; Date = DateOnly(2024, 7, 2)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 197
                RealizedGains = 352.96m; RealizedPercentage = 28.3168m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 103.00m; Fees = 25.53m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 0.00m; OptionsIncome = 1375.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 1246.47m }
            Description = "BrokerSnapshot 15: 2024-07-02" }
          // BrokerSnapshot 16: 2024-07-09
          { Data =
              { Id = 0; Date = DateOnly(2024, 7, 9)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 199
                RealizedGains = 352.96m; RealizedPercentage = 25.6891m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 103.00m; Fees = 25.53m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 1375.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 1373.97m }
            Description = "BrokerSnapshot 16: 2024-07-09" }
          // BrokerSnapshot 17: 2024-07-16
          { Data =
              { Id = 0; Date = DateOnly(2024, 7, 16)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 222
                RealizedGains = -119.63m; RealizedPercentage = -11.0875m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 116.00m; Fees = 28.54m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 1096.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 1078.96m }
            Description = "BrokerSnapshot 17: 2024-07-16" }
          // BrokerSnapshot 18: 2024-07-24
          { Data =
              { Id = 0; Date = DateOnly(2024, 7, 24)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 244
                RealizedGains = -386.76m; RealizedPercentage = -54.1605m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 126.00m; Fees = 31.40m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 744.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 714.10m }
            Description = "BrokerSnapshot 18: 2024-07-24" }
          // BrokerSnapshot 19: 2024-07-31
          { Data =
              { Id = 0; Date = DateOnly(2024, 7, 31)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 247
                RealizedGains = -386.76m; RealizedPercentage = -51.5206m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 129.00m; Fees = 31.81m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 784.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 750.69m }
            Description = "BrokerSnapshot 19: 2024-07-31" }
          // BrokerSnapshot 20: 2024-08-01
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 1)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 249
                RealizedGains = -387.03m; RealizedPercentage = -50.2356m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 130.00m; Fees = 32.08m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 805.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 770.42m }
            Description = "BrokerSnapshot 20: 2024-08-01" }
          // BrokerSnapshot 21: 2024-08-02
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 2)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 251
                RealizedGains = -387.29m; RealizedPercentage = -48.8910m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 131.00m; Fees = 32.35m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 828.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 792.15m }
            Description = "BrokerSnapshot 21: 2024-08-02" }
          // BrokerSnapshot 22: 2024-08-06
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 6)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 254
                RealizedGains = -351.09m; RealizedPercentage = -46.8264m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 131.00m; Fees = 32.73m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 786.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 749.77m }
            Description = "BrokerSnapshot 22: 2024-08-06" }
          // BrokerSnapshot 23: 2024-08-15
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 15)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 309
                RealizedGains = -150.16m; RealizedPercentage = -13.8956m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 151.00m; Fees = 39.90m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 1144.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 1080.60m }
            Description = "BrokerSnapshot 23: 2024-08-15" }
          // BrokerSnapshot 24: 2024-08-16
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 16)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 319
                RealizedGains = -122.74m; RealizedPercentage = -13.8788m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 151.00m; Fees = 41.16m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 949.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 884.34m }
            Description = "BrokerSnapshot 24: 2024-08-16" }
          // BrokerSnapshot 25: 2024-08-19
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 19)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 332
                RealizedGains = -45.32m; RealizedPercentage = -5.3904m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 154.00m; Fees = 42.82m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 910.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 840.68m }
            Description = "BrokerSnapshot 25: 2024-08-19" }
          // BrokerSnapshot 26: 2024-08-20
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 20)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 343
                RealizedGains = -42.08m; RealizedPercentage = -4.6485m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 164.00m; Fees = 44.32m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 986.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 905.18m }
            Description = "BrokerSnapshot 26: 2024-08-20" }
          // BrokerSnapshot 27: 2024-08-22
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 22)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 345
                RealizedGains = -38.61m; RealizedPercentage = -4.2948m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 164.00m; Fees = 44.57m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 980.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 898.93m }
            Description = "BrokerSnapshot 27: 2024-08-22" }
          // BrokerSnapshot 28: 2024-08-23
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 23)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 346
                RealizedGains = -38.61m; RealizedPercentage = -4.2343m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 165.00m; Fees = 44.71m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 994.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 911.79m }
            Description = "BrokerSnapshot 28: 2024-08-23" }
          // BrokerSnapshot 29: 2024-08-26
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 26)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 349
                RealizedGains = -48.39m; RealizedPercentage = -5.5656m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 165.00m; Fees = 45.09m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 952.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 869.41m }
            Description = "BrokerSnapshot 29: 2024-08-26" }
          // BrokerSnapshot 30: 2024-08-29
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 29)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 357
                RealizedGains = 21.52m; RealizedPercentage = 2.5549m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 165.00m; Fees = 46.09m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 926.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 842.41m }
            Description = "BrokerSnapshot 30: 2024-08-29" }
          // BrokerSnapshot 31: 2024-08-30
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 30)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 358
                RealizedGains = 25.25m; RealizedPercentage = 3.0305m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 165.00m; Fees = 46.22m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 917.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 833.28m }
            Description = "BrokerSnapshot 31: 2024-08-30" }
          // BrokerSnapshot 32: 2024-09-03
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 3)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 364
                RealizedGains = 25.25m; RealizedPercentage = 2.9278m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 171.00m; Fees = 47.00m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 953.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 862.50m }
            Description = "BrokerSnapshot 32: 2024-09-03" }
          // BrokerSnapshot 33: 2024-09-04
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 4)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 369
                RealizedGains = 71.46m; RealizedPercentage = 10.4794m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 171.00m; Fees = 47.64m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 773.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 681.86m }
            Description = "BrokerSnapshot 33: 2024-09-04" }
          // BrokerSnapshot 34: 2024-09-06
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 6)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 373
                RealizedGains = 71.46m; RealizedPercentage = 10.1025m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 175.00m; Fees = 48.20m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 803.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 707.30m }
            Description = "BrokerSnapshot 34: 2024-09-06" }
          // BrokerSnapshot 35: 2024-09-10
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 10)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 383
                RealizedGains = 173.85m; RealizedPercentage = 48.8329m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 175.00m; Fees = 49.49m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 453.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 356.01m }
            Description = "BrokerSnapshot 35: 2024-09-10" }
          // BrokerSnapshot 36: 2024-09-12
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 12)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 388
                RealizedGains = 43.31m; RealizedPercentage = 11.6322m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 178.00m; Fees = 50.17m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 473.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 372.33m }
            Description = "BrokerSnapshot 36: 2024-09-12" }
          // BrokerSnapshot 37: 2024-09-13
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 13)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 394
                RealizedGains = 87.91m; RealizedPercentage = 23.6108m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 178.00m; Fees = 50.17m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 473.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 372.33m }
            Description = "BrokerSnapshot 37: 2024-09-13" }
          // BrokerSnapshot 38: 2024-09-16
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 16)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 396
                RealizedGains = 87.91m; RealizedPercentage = 21.4913m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 180.00m; Fees = 50.45m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 512.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 409.05m }
            Description = "BrokerSnapshot 38: 2024-09-16" }
          // BrokerSnapshot 39: 2024-09-17
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 17)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 404
                RealizedGains = -235.17m; RealizedPercentage = -52.4968m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 184.00m; Fees = 51.53m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 556.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 447.97m }
            Description = "BrokerSnapshot 39: 2024-09-17" }
          // BrokerSnapshot 40: 2024-09-24
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 24)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 408
                RealizedGains = -235.17m; RealizedPercentage = -48.6481m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 188.00m; Fees = 52.09m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 596.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 483.41m }
            Description = "BrokerSnapshot 40: 2024-09-24" }
          // BrokerSnapshot 41: 2024-09-25
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 25)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 416
                RealizedGains = -64.25m; RealizedPercentage = -14.5254m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 192.00m; Fees = 53.17m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 560.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 442.33m }
            Description = "BrokerSnapshot 41: 2024-09-25" }
          // BrokerSnapshot 42: 2024-10-02
          { Data =
              { Id = 0; Date = DateOnly(2024, 10, 2)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 418
                RealizedGains = -59.79m; RealizedPercentage = -14.5804m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 192.00m; Fees = 53.43m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 528.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 410.07m }
            Description = "BrokerSnapshot 42: 2024-10-02" }
          // BrokerSnapshot 43: 2024-10-04
          { Data =
              { Id = 0; Date = DateOnly(2024, 10, 4)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 422
                RealizedGains = -24.35m; RealizedPercentage = -5.9380m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 192.00m; Fees = 53.43m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 528.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 410.07m }
            Description = "BrokerSnapshot 43: 2024-10-04" }
          // BrokerSnapshot 44: 2024-10-07
          { Data =
              { Id = 0; Date = DateOnly(2024, 10, 7)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 425
                RealizedGains = 47.84m; RealizedPercentage = 13.9199m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 192.00m; Fees = 53.82m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 462.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 343.68m }
            Description = "BrokerSnapshot 44: 2024-10-07" }
          // BrokerSnapshot 45: 2024-10-08
          { Data =
              { Id = 0; Date = DateOnly(2024, 10, 8)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 427
                RealizedGains = 129.30m; RealizedPercentage = 53.5581m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 192.00m; Fees = 54.08m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 127.50m; OptionsIncome = 360.00m; OtherIncome = 0.00m
                OpenTrades = false; NetCashFlow = 241.42m }
            Description = "BrokerSnapshot 45: 2024-10-08" }
          // BrokerSnapshot 46: 2024-10-10
          { Data =
              { Id = 0; Date = DateOnly(2024, 10, 10)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 430
                RealizedGains = 5078.19m; RealizedPercentage = 1647.1052m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 192.00m; Fees = 55.19m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = 360.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = 308.31m }
            Description = "BrokerSnapshot 46: 2024-10-10" }
          // BrokerSnapshot 47: 2024-10-24
          { Data =
              { Id = 0; Date = DateOnly(2024, 10, 24)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 435
                RealizedGains = 5078.19m; RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 197.00m; Fees = 55.83m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = -740.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = -797.33m }
            Description = "BrokerSnapshot 47: 2024-10-24" }
          // BrokerSnapshot 48: 2024-11-01
          { Data =
              { Id = 0; Date = DateOnly(2024, 11, 1)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 436
                RealizedGains = 5078.19m; RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 198.00m; Fees = 55.97m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = -719.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = -777.47m }
            Description = "BrokerSnapshot 48: 2024-11-01" }
          // BrokerSnapshot 49: 2024-11-05
          { Data =
              { Id = 0; Date = DateOnly(2024, 11, 5)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 437
                RealizedGains = 5080.92m; RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 198.00m; Fees = 56.10m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = -736.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = -794.60m }
            Description = "BrokerSnapshot 49: 2024-11-05" }
          // BrokerSnapshot 50: 2024-11-07
          { Data =
              { Id = 0; Date = DateOnly(2024, 11, 7)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 442
                RealizedGains = 4854.61m; RealizedPercentage = 5729.5055m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 198.00m; Fees = 56.77m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = 144.00m; OtherIncome = 0.00m
                OpenTrades = false; NetCashFlow = 84.73m }
            Description = "BrokerSnapshot 50: 2024-11-07" }
          // BrokerSnapshot 51: 2024-12-13
          { Data =
              { Id = 0; Date = DateOnly(2024, 12, 13)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 444
                RealizedGains = 4854.61m; RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 200.00m; Fees = 57.02m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = 14.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = -47.52m }
            Description = "BrokerSnapshot 51: 2024-12-13" }
          // BrokerSnapshot 52: 2024-12-17
          { Data =
              { Id = 0; Date = DateOnly(2024, 12, 17)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 448
                RealizedGains = 4854.61m; RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 204.00m; Fees = 57.52m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = 40.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = -26.02m }
            Description = "BrokerSnapshot 52: 2024-12-17" }
          // BrokerSnapshot 53: 2025-01-17
          { Data =
              { Id = 0; Date = DateOnly(2025, 1, 17)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 450
                RealizedGains = 4878.35m; RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 205.00m; Fees = 57.79m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = 62.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = -5.29m }
            Description = "BrokerSnapshot 53: 2025-01-17" }
          // BrokerSnapshot 54: 2025-01-21
          { Data =
              { Id = 0; Date = DateOnly(2025, 1, 21)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 456
                RealizedGains = 4873.78m; RealizedPercentage = 4690.8373m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 205.00m; Fees = 58.60m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = 172.00m; OtherIncome = 0.00m
                OpenTrades = false; NetCashFlow = 103.90m }
            Description = "BrokerSnapshot 54: 2025-01-21" }
          // BrokerSnapshot 55: 2025-02-18
          { Data =
              { Id = 0; Date = DateOnly(2025, 2, 18)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 457
                RealizedGains = 4873.78m; RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 206.00m; Fees = 58.73m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = -138.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = -207.23m }
            Description = "BrokerSnapshot 55: 2025-02-18" }
          // BrokerSnapshot 56: 2025-02-20
          { Data =
              { Id = 0; Date = DateOnly(2025, 2, 20)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 458
                RealizedGains = 4873.78m; RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 207.00m; Fees = 58.87m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = -111.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = -181.37m }
            Description = "BrokerSnapshot 56: 2025-02-20" }
          // BrokerSnapshot 57: 2025-02-28
          { Data =
              { Id = 0; Date = DateOnly(2025, 2, 28)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 459
                RealizedGains = 4812.51m; RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 207.00m; Fees = 59.00m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = -198.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = -268.50m }
            Description = "BrokerSnapshot 57: 2025-02-28" }
          // BrokerSnapshot 58: 2025-03-07
          { Data =
              { Id = 0; Date = DateOnly(2025, 3, 7)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 460
                RealizedGains = 4812.51m; RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 208.00m; Fees = 59.14m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = -168.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = -239.64m }
            Description = "BrokerSnapshot 58: 2025-03-07" }
          // BrokerSnapshot 59: 2025-03-27
          { Data =
              { Id = 0; Date = DateOnly(2025, 3, 27)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 462
                RealizedGains = 4914.96m; RealizedPercentage = 3387.7585m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 208.00m; Fees = 59.42m
                Deposited = 0.00m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = 217.00m; OtherIncome = 0.00m
                OpenTrades = false; NetCashFlow = 145.08m }
            Description = "BrokerSnapshot 59: 2025-03-27" }
          // BrokerSnapshot 60: 2025-04-07
          { Data =
              { Id = 0; Date = DateOnly(2025, 4, 7)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 463
                RealizedGains = 4914.96m; RealizedPercentage = 2932.9037m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 208.00m; Fees = 59.42m
                Deposited = 22.50m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = 217.00m; OtherIncome = 0.00m
                OpenTrades = false; NetCashFlow = 167.58m }
            Description = "BrokerSnapshot 60: 2025-04-07" }
          // BrokerSnapshot 61: 2025-08-26
          { Data =
              { Id = 0; Date = DateOnly(2025, 8, 26)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 466
                RealizedGains = 4914.96m; RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 211.00m; Fees = 59.79m
                Deposited = 22.50m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = -251.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = -303.79m }
            Description = "BrokerSnapshot 61: 2025-08-26" }
          // BrokerSnapshot 62: 2025-09-18
          { Data =
              { Id = 0; Date = DateOnly(2025, 9, 18)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 469
                RealizedGains = 4914.96m; RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 214.00m; Fees = 60.15m
                Deposited = 22.50m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = -203.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = -259.15m }
            Description = "BrokerSnapshot 62: 2025-09-18" }
          // BrokerSnapshot 63: 2025-09-26
          { Data =
              { Id = 0; Date = DateOnly(2025, 9, 26)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 472
                RealizedGains = 4959.60m; RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 214.00m; Fees = 60.15m
                Deposited = 22.50m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = -203.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = -259.15m }
            Description = "BrokerSnapshot 63: 2025-09-26" }
          // BrokerSnapshot 64: 2025-10-01
          { Data =
              { Id = 0; Date = DateOnly(2025, 10, 1)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 475
                RealizedGains = 4959.60m; RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 217.00m; Fees = 60.52m
                Deposited = 22.50m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = -173.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = -232.52m }
            Description = "BrokerSnapshot 64: 2025-10-01" }
          // BrokerSnapshot 65: 2025-10-17
          { Data =
              { Id = 0; Date = DateOnly(2025, 10, 17)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 478
                RealizedGains = 4986.23m; RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 217.00m; Fees = 60.52m
                Deposited = 22.50m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = -173.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = -232.52m }
            Description = "BrokerSnapshot 65: 2025-10-17" }
          // BrokerSnapshot 66: 2025-11-07
          { Data =
              { Id = 0; Date = DateOnly(2025, 11, 7)
                Broker = Some broker; BrokerAccount = Some brokerAccount; Currency = currency
                MovementCounter = 478
                RealizedGains = 4986.23m; RealizedPercentage = 0.0000m
                UnrealizedGains = 0.00m; UnrealizedGainsPercentage = 0.0000m
                Invested = 0.00m; Commissions = 217.00m; Fees = 60.52m
                Deposited = 22.50m; Withdrawn = 0.00m
                DividendsReceived = 195.50m; OptionsIncome = -173.00m; OtherIncome = 0.00m
                OpenTrades = true; NetCashFlow = -232.52m }
            Description = "BrokerSnapshot 66: 2025-11-07" }
        ]
