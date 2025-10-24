namespace Core.Tests.Integration

open System
open Binnaculum.Core.Models
open TestModels

/// <summary>
/// Expected snapshot data for Options Import integration tests.
///
/// This module provides factory functions to generate expected snapshots for:
/// - SOFI ticker (4 snapshots: 2024-04-25, 04-29, 04-30, today)
/// - MPW ticker (3 snapshots: 2024-04-26, 04-29, today)
/// - PLTR ticker (3 snapshots: 2024-04-26, 04-29, today)
/// - BrokerAccount (9 snapshots: 2024-04-22 through 2024-04-30, today)
///
/// These snapshots are based on TastytradeOptionsTest.csv import data.
/// </summary>
module OptionsImportExpectedSnapshots =

    // ==================== SOFI TICKER SNAPSHOTS ====================

    /// <summary>
    /// Generate expected SOFI ticker snapshots with descriptions.
    ///
    /// SOFI has 4 snapshots:
    /// 1. 2024-04-25: After initial SELL_TO_OPEN
    /// 2. 2024-04-29: After BUY_TO_CLOSE + new SELL_TO_OPEN
    /// 3. 2024-04-30: After second SELL_TO_OPEN
    /// 4. Today: Current snapshot (same as 2024-04-30)
    /// </summary>
    let getSOFISnapshots (ticker: Ticker) (currency: Currency) : ExpectedSnapshot<TickerCurrencySnapshot> list =
        let today = DateOnly.FromDateTime(DateTime.Now)

        [
          // Snapshot 1: 2024-04-25 (After SELL_TO_OPEN #1)
          { Data =
              { Id = 0 // Will be assigned by database
                Date = DateOnly(2024, 4, 25)
                Ticker = ticker
                Currency = currency
                TotalShares = 0m
                Weight = 0m
                CostBasis = 0m
                RealCost = 0m
                Dividends = 0m
                DividendTaxes = 0m
                Options = 33.86m // SELL_TO_OPEN NetPremium
                TotalIncomes = 33.86m
                Unrealized = 0m
                Realized = 0m
                Performance = 0m
                LatestPrice = 0m
                OpenTrades = true
                Commissions = 0.0m
                Fees = 0.0m }
            Description = "2024-04-25 - After SELL_TO_OPEN" }

          // Snapshot 2: 2024-04-29 (After BUY_TO_CLOSE + SELL_TO_OPEN)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 4, 29)
                Ticker = ticker
                Currency = currency
                TotalShares = 0m
                Weight = 0m
                CostBasis = 0m
                RealCost = 0m
                Dividends = 0m
                DividendTaxes = 0m
                Options = 32.59m // Cumulative: 33.86 - 17.13 + 15.86
                TotalIncomes = 32.59m
                Unrealized = 0m
                Realized = 16.73m // First position closed: 33.86 - 17.13
                Performance = 0m
                LatestPrice = 0m
                OpenTrades = true
                Commissions = 0.0m
                Fees = 0.0m }
            Description = "2024-04-29 - After close and reopen" }

          // Snapshot 3: 2024-04-30 (After second SELL_TO_OPEN)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 4, 30)
                Ticker = ticker
                Currency = currency
                TotalShares = 0m
                Weight = 0m
                CostBasis = 0m
                RealCost = 0m
                Dividends = 0m
                DividendTaxes = 0m
                Options = 47.45m // Cumulative: 32.59 + 14.86
                TotalIncomes = 47.45m
                Unrealized = 0m
                Realized = 16.73m
                Performance = 0m
                LatestPrice = 0m
                OpenTrades = true
                Commissions = 0.0m
                Fees = 0.0m }
            Description = "2024-04-30 - After second SELL_TO_OPEN" }

          // Snapshot 4: Today (Current snapshot - same as 2024-04-30)
          { Data =
              { Id = 0
                Date = today
                Ticker = ticker
                Currency = currency
                TotalShares = 0m
                Weight = 0m
                CostBasis = 0m
                RealCost = 0m
                Dividends = 0m
                DividendTaxes = 0m
                Options = 47.45m // Same as Snapshot 3
                TotalIncomes = 47.45m
                Unrealized = 0m
                Realized = 16.73m
                Performance = 0m
                LatestPrice = 0m
                OpenTrades = true
                Commissions = 0.0m
                Fees = 0.0m }
            Description = sprintf "%s - Current snapshot" (today.ToString("yyyy-MM-dd")) } ]

    // ==================== MPW TICKER SNAPSHOTS ====================

    /// <summary>
    /// Generate expected MPW ticker snapshots with descriptions.
    ///
    /// MPW has 3 snapshots:
    /// 1. 2024-04-26: After opening vertical spread
    /// 2. 2024-04-29: After closing vertical spread
    /// 3. Today: Current snapshot (same as 2024-04-29)
    /// </summary>
    let getMPWSnapshots (ticker: Ticker) (currency: Currency) : ExpectedSnapshot<TickerCurrencySnapshot> list =
        let today = DateOnly.FromDateTime(DateTime.Now)

        [
          // Snapshot 1: 2024-04-26 (After opening vertical spread)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 4, 26)
                Ticker = ticker
                Currency = currency
                TotalShares = 0m
                Weight = 0m
                CostBasis = 0m
                RealCost = 0m
                Dividends = 0m
                DividendTaxes = 0m
                Options = 12.73m // 17.86 - 5.13 (net from opening spread)
                TotalIncomes = 12.73m
                Unrealized = 0m
                Realized = 0m
                Performance = 0m
                LatestPrice = 0m
                OpenTrades = true
                Commissions = 0.0m
                Fees = 0.0m }
            Description = "2024-04-26 - After opening vertical spread" }

          // Snapshot 2: 2024-04-29 (After closing vertical spread)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 4, 29)
                Ticker = ticker
                Currency = currency
                TotalShares = 0m
                Weight = 0m
                CostBasis = 0m
                RealCost = 0m
                Dividends = 0m
                DividendTaxes = 0m
                Options = 5.46m // Cumulative: 12.73 + 0.86 - 8.13 = 5.46
                TotalIncomes = 5.46m
                Unrealized = 0m
                Realized = 5.46m // Positions closed
                Performance = 0m
                LatestPrice = 0m
                OpenTrades = false
                Commissions = 0.0m
                Fees = 0.0m }
            Description = "2024-04-29 - After closing vertical spread" }

          // Snapshot 3: Today (Current snapshot - same as 2024-04-29)
          { Data =
              { Id = 0
                Date = today
                Ticker = ticker
                Currency = currency
                TotalShares = 0m
                Weight = 0m
                CostBasis = 0m
                RealCost = 0m
                Dividends = 0m
                DividendTaxes = 0m
                Options = 5.46m // Same as Snapshot 2 (no new trades)
                TotalIncomes = 5.46m
                Unrealized = 0m
                Realized = 5.46m
                Performance = 0m
                LatestPrice = 0m
                OpenTrades = false
                Commissions = 0.0m
                Fees = 0.0m }
            Description = sprintf "%s - Current snapshot" (today.ToString("yyyy-MM-dd")) } ]

    // ==================== PLTR TICKER SNAPSHOTS ====================

    /// <summary>
    /// Generate expected PLTR ticker snapshots with descriptions.
    ///
    /// PLTR has 3 snapshots:
    /// 1. 2024-04-26: After opening vertical spread
    /// 2. 2024-04-29: After closing vertical spread
    /// 3. Today: Current snapshot (same as 2024-04-29)
    /// </summary>
    let getPLTRSnapshots (ticker: Ticker) (currency: Currency) : ExpectedSnapshot<TickerCurrencySnapshot> list =
        let today = DateOnly.FromDateTime(DateTime.Now)

        [
          // Snapshot 1: 2024-04-26 (After opening vertical spread)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 4, 26)
                Ticker = ticker
                Currency = currency
                TotalShares = 0m
                Weight = 0m
                CostBasis = 0m
                RealCost = 0m
                Dividends = 0m
                DividendTaxes = 0m
                Options = 5.73m // 17.86 - 12.13 (net from opening spread)
                TotalIncomes = 5.73m
                Unrealized = 0m
                Realized = 0m
                Performance = 0m
                LatestPrice = 0m
                OpenTrades = true
                Commissions = 0.0m
                Fees = 0.0m }
            Description = "2024-04-26 - After opening vertical spread" }

          // Snapshot 2: 2024-04-29 (After closing vertical spread)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 4, 29)
                Ticker = ticker
                Currency = currency
                TotalShares = 0m
                Weight = 0m
                CostBasis = 0m
                RealCost = 0m
                Dividends = 0m
                DividendTaxes = 0m
                Options = 1.46m // Cumulative: 5.73 + 4.86 - 9.13 = 1.46
                TotalIncomes = 1.46m
                Unrealized = 0m
                Realized = 1.46m // Positions closed
                Performance = 0m
                LatestPrice = 0m
                OpenTrades = false
                Commissions = 0.0m
                Fees = 0.0m }
            Description = "2024-04-29 - After closing vertical spread" }

          // Snapshot 3: Today (Current snapshot - same as 2024-04-29)
          { Data =
              { Id = 0
                Date = today
                Ticker = ticker
                Currency = currency
                TotalShares = 0m
                Weight = 0m
                CostBasis = 0m
                RealCost = 0m
                Dividends = 0m
                DividendTaxes = 0m
                Options = 1.46m // Same as Snapshot 2 (no new trades)
                TotalIncomes = 1.46m
                Unrealized = 0m
                Realized = 1.46m
                Performance = 0m
                LatestPrice = 0m
                OpenTrades = false
                Commissions = 0.0m
                Fees = 0.0m }
            Description = sprintf "%s - Current snapshot" (today.ToString("yyyy-MM-dd")) } ]

    // ==================== BROKER ACCOUNT SNAPSHOTS ====================

    /// <summary>
    /// Generate expected BrokerAccount financial snapshots with descriptions.
    ///
    /// BrokerAccount has 9 snapshots:
    /// 1. 2024-04-22: First deposit ($10.00)
    /// 2. 2024-04-23: Second deposit ($34.23)
    /// 3. 2024-04-24: Third deposit ($878.79)
    /// 4. 2024-04-25: SOFI SELL_TO_OPEN
    /// 5. 2024-04-26: MPW + PLTR vertical spreads opened
    /// 6. 2024-04-27: Balance adjustment
    /// 7. 2024-04-29: Multiple closing + reopening trades
    /// 8. 2024-04-30: Final SOFI SELL_TO_OPEN
    /// 9. Today: Current snapshot (same as 2024-04-30)
    /// </summary>
    let getBrokerAccountSnapshots
        (broker: Broker)
        (brokerAccount: BrokerAccount)
        (currency: Currency)
        : ExpectedSnapshot<BrokerFinancialSnapshot> list =

        let today = DateOnly.FromDateTime(DateTime.Now)

        [
          // Snapshot 1: 2024-04-22 (First deposit)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 4, 22)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 1
                RealizedGains = 0m
                RealizedPercentage = 0m
                UnrealizedGains = 0m
                UnrealizedGainsPercentage = 0m
                Invested = 0m
                Commissions = 0m
                Fees = 0m
                Deposited = 10.00m
                Withdrawn = 0m
                DividendsReceived = 0m
                OptionsIncome = 0m
                OtherIncome = 0m
                OpenTrades = false
                NetCashFlow = 10.00m }
            Description = "2024-04-22 - First deposit" }

          // Snapshot 2: 2024-04-23 (Second deposit)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 4, 23)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 2
                RealizedGains = 0m
                RealizedPercentage = 0m
                UnrealizedGains = 0m
                UnrealizedGainsPercentage = 0m
                Invested = 0m
                Commissions = 0m
                Fees = 0m
                Deposited = 34.23m
                Withdrawn = 0m
                DividendsReceived = 0m
                OptionsIncome = 0m
                OtherIncome = 0m
                OpenTrades = false
                NetCashFlow = 34.23m }
            Description = "2024-04-23 - Second deposit" }

          // Snapshot 3: 2024-04-24 (Third deposit)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 4, 24)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 3
                RealizedGains = 0m
                RealizedPercentage = 0m
                UnrealizedGains = 0m
                UnrealizedGainsPercentage = 0m
                Invested = 0m
                Commissions = 0m
                Fees = 0m
                Deposited = 878.79m
                Withdrawn = 0m
                DividendsReceived = 0m
                OptionsIncome = 0m
                OtherIncome = 0m
                OpenTrades = false
                NetCashFlow = 878.79m }
            Description = "2024-04-24 - Third deposit" }

          // Snapshot 4: 2024-04-25 (SOFI trade)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 4, 25)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 4
                RealizedGains = 0m
                RealizedPercentage = 0m
                UnrealizedGains = 0m
                UnrealizedGainsPercentage = 0m
                Invested = 0m
                Commissions = 1.00m
                Fees = 0.14m
                Deposited = 878.79m
                Withdrawn = 0m
                DividendsReceived = 0m
                OptionsIncome = 35.00m // Pure premium from CSV Value column
                OtherIncome = 0m
                OpenTrades = true
                NetCashFlow = 912.65m } // 878.79 + 35.00 - 1.00 - 0.14
            Description = "2024-04-25 - SOFI trade" }

          // Snapshot 5: 2024-04-26 (MPW + PLTR trades)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 4, 26)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 8
                RealizedGains = 0m
                RealizedPercentage = 0m
                UnrealizedGains = 0m
                UnrealizedGainsPercentage = 0m
                Invested = 0m
                Commissions = 5.00m
                Fees = 0.68m
                Deposited = 878.79m
                Withdrawn = 0m
                DividendsReceived = 0m
                OptionsIncome = 58.00m // 35.00 - 4.00 + 19.00 + 19.00 - 11.00
                OtherIncome = 0m
                OpenTrades = true
                NetCashFlow = 931.11m } // 878.79 + 58.00 - 5.00 - 0.68
            Description = "2024-04-26 - MPW + PLTR trades" }

          // Snapshot 6: 2024-04-27 (Balance adjustment)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 4, 27)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 9
                RealizedGains = 0m
                RealizedPercentage = 0m
                UnrealizedGains = 0m
                UnrealizedGainsPercentage = 0m
                Invested = 0m
                Commissions = 5.00m
                Fees = 0.70m
                Deposited = 878.79m
                Withdrawn = 0m
                DividendsReceived = 0m
                OptionsIncome = 58.00m // Same as Snapshot 5 (no new trades)
                OtherIncome = 0m
                OpenTrades = true
                NetCashFlow = 931.09m } // 878.79 + 58.00 - 5.00 - 0.70
            Description = "2024-04-27 - Balance adjustment" }

          // Snapshot 7: 2024-04-29 (Multiple closing + reopening trades)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 4, 29)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 15
                RealizedGains = 23.65m
                RealizedPercentage = 2.5754m
                UnrealizedGains = 0m
                UnrealizedGainsPercentage = 0m
                Invested = 0m
                Commissions = 6.00m
                Fees = 1.51m // Includes $0.02 balance adjustment fee
                Deposited = 878.79m
                Withdrawn = 0m
                DividendsReceived = 0m
                OptionsIncome = 47.00m // 58.00 - 17.00 + 17.00 - 8.00 + 1.00 + 5.00 - 9.00
                OtherIncome = 0m
                OpenTrades = true
                NetCashFlow = 918.28m } // 878.79 + 47.00 - 6.00 - 1.51
            Description = "2024-04-29 - Closing + reopening trades" }

          // Snapshot 8: 2024-04-30 (Final SOFI trade)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 4, 30)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 16
                RealizedGains = 23.65m
                RealizedPercentage = 2.5344m
                UnrealizedGains = 0m
                UnrealizedGainsPercentage = 0m
                Invested = 0m
                Commissions = 7.00m
                Fees = 1.65m // Includes $0.02 balance adjustment fee
                Deposited = 878.79m
                Withdrawn = 0m
                DividendsReceived = 0m
                OptionsIncome = 63.00m // 47.00 + 16.00 (SOFI SELL_TO_OPEN)
                OtherIncome = 0m
                OpenTrades = true
                NetCashFlow = 933.14m } // 878.79 + 63.00 - 7.00 - 1.65
            Description = "2024-04-30 - Final SOFI trade" }

          // Snapshot 9: Today (Current snapshot - same as 2024-04-30)
          { Data =
              { Id = 0
                Date = today
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 16
                RealizedGains = 23.65m
                RealizedPercentage = 2.5344m
                UnrealizedGains = 0m
                UnrealizedGainsPercentage = 0m
                Invested = 0m
                Commissions = 7.00m
                Fees = 1.65m // Includes $0.02 balance adjustment fee
                Deposited = 878.79m
                Withdrawn = 0m
                DividendsReceived = 0m
                OptionsIncome = 63.00m // Same as Snapshot 8 (47.00 + 16.00)
                OtherIncome = 0m
                OpenTrades = true
                NetCashFlow = 933.14m } // 878.79 + 63.00 - 7.00 - 1.65 (same as Snapshot 8)
            Description = sprintf "%s - Current snapshot" (today.ToString("yyyy-MM-dd")) } ]
