namespace Core.Tests.Integration

open System
open Binnaculum.Core.Models

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
    /// Generate expected SOFI ticker snapshots.
    ///
    /// SOFI has 4 snapshots:
    /// 1. 2024-04-25: After initial SELL_TO_OPEN
    /// 2. 2024-04-29: After BUY_TO_CLOSE + new SELL_TO_OPEN
    /// 3. 2024-04-30: After second SELL_TO_OPEN
    /// 4. Today: Current snapshot (same as 2024-04-30)
    /// </summary>
    let getSOFISnapshots (ticker: Ticker) (currency: Currency) : TickerCurrencySnapshot list =
        let today = DateOnly.FromDateTime(DateTime.Now)

        [
          // Snapshot 1: 2024-04-25 (After SELL_TO_OPEN #1)
          { Id = 0 // Will be assigned by database
            Date = DateOnly(2024, 4, 25)
            Ticker = ticker
            Currency = currency
            TotalShares = 0m
            Weight = 0m
            CostBasis = 0m
            RealCost = 0m
            Dividends = 0m
            Options = 33.86m // SELL_TO_OPEN NetPremium
            TotalIncomes = 33.86m
            Unrealized = 0m
            Realized = 0m
            Performance = 0m
            LatestPrice = 0m
            OpenTrades = true }

          // Snapshot 2: 2024-04-29 (After BUY_TO_CLOSE + SELL_TO_OPEN)
          { Id = 0
            Date = DateOnly(2024, 4, 29)
            Ticker = ticker
            Currency = currency
            TotalShares = 0m
            Weight = 0m
            CostBasis = 0m
            RealCost = 0m
            Dividends = 0m
            Options = 32.59m // Cumulative: 33.86 - 17.13 + 15.86
            TotalIncomes = 32.59m
            Unrealized = 0m
            Realized = 16.73m // First position closed: 33.86 - 17.13
            Performance = 0m
            LatestPrice = 0m
            OpenTrades = true }

          // Snapshot 3: 2024-04-30 (After second SELL_TO_OPEN)
          { Id = 0
            Date = DateOnly(2024, 4, 30)
            Ticker = ticker
            Currency = currency
            TotalShares = 0m
            Weight = 0m
            CostBasis = 0m
            RealCost = 0m
            Dividends = 0m
            Options = 47.45m // Cumulative: 32.59 + 14.86
            TotalIncomes = 47.45m
            Unrealized = 0m
            Realized = 16.73m
            Performance = 0m
            LatestPrice = 0m
            OpenTrades = true }

          // Snapshot 4: Today (Current snapshot - same as 2024-04-30)
          { Id = 0
            Date = today
            Ticker = ticker
            Currency = currency
            TotalShares = 0m
            Weight = 0m
            CostBasis = 0m
            RealCost = 0m
            Dividends = 0m
            Options = 47.45m // Same as Snapshot 3
            TotalIncomes = 47.45m
            Unrealized = 0m
            Realized = 16.73m
            Performance = 0m
            LatestPrice = 0m
            OpenTrades = true } ]

    // ==================== MPW TICKER SNAPSHOTS ====================

    /// <summary>
    /// Generate expected MPW ticker snapshots.
    ///
    /// MPW has 3 snapshots:
    /// 1. 2024-04-26: After opening vertical spread
    /// 2. 2024-04-29: After closing vertical spread
    /// 3. Today: Current snapshot (same as 2024-04-29)
    /// </summary>
    let getMPWSnapshots (ticker: Ticker) (currency: Currency) : TickerCurrencySnapshot list =
        let today = DateOnly.FromDateTime(DateTime.Now)

        [
          // Snapshot 1: 2024-04-26 (After opening vertical spread)
          { Id = 0
            Date = DateOnly(2024, 4, 26)
            Ticker = ticker
            Currency = currency
            TotalShares = 0m
            Weight = 0m
            CostBasis = 0m
            RealCost = 0m
            Dividends = 0m
            Options = 12.73m // 17.86 - 5.13 (net from opening spread)
            TotalIncomes = 12.73m
            Unrealized = 0m
            Realized = 0m
            Performance = 0m
            LatestPrice = 0m
            OpenTrades = true }

          // Snapshot 2: 2024-04-29 (After closing vertical spread)
          { Id = 0
            Date = DateOnly(2024, 4, 29)
            Ticker = ticker
            Currency = currency
            TotalShares = 0m
            Weight = 0m
            CostBasis = 0m
            RealCost = 0m
            Dividends = 0m
            Options = 5.46m // Cumulative: 12.73 + 0.86 - 8.13 = 5.46
            TotalIncomes = 5.46m
            Unrealized = 0m
            Realized = 5.46m // Positions closed
            Performance = 0m
            LatestPrice = 0m
            OpenTrades = false }

          // Snapshot 3: Today (Current snapshot - same as 2024-04-29)
          { Id = 0
            Date = today
            Ticker = ticker
            Currency = currency
            TotalShares = 0m
            Weight = 0m
            CostBasis = 0m
            RealCost = 0m
            Dividends = 0m
            Options = 5.46m // Same as Snapshot 2 (no new trades)
            TotalIncomes = 5.46m
            Unrealized = 0m
            Realized = 5.46m
            Performance = 0m
            LatestPrice = 0m
            OpenTrades = false } ]

    // ==================== PLTR TICKER SNAPSHOTS ====================

    /// <summary>
    /// Generate expected PLTR ticker snapshots.
    ///
    /// PLTR has 3 snapshots:
    /// 1. 2024-04-26: After opening vertical spread
    /// 2. 2024-04-29: After closing vertical spread
    /// 3. Today: Current snapshot (same as 2024-04-29)
    /// </summary>
    let getPLTRSnapshots (ticker: Ticker) (currency: Currency) : TickerCurrencySnapshot list =
        let today = DateOnly.FromDateTime(DateTime.Now)

        [
          // Snapshot 1: 2024-04-26 (After opening vertical spread)
          { Id = 0
            Date = DateOnly(2024, 4, 26)
            Ticker = ticker
            Currency = currency
            TotalShares = 0m
            Weight = 0m
            CostBasis = 0m
            RealCost = 0m
            Dividends = 0m
            Options = 5.73m // 17.86 - 12.13 (net from opening spread)
            TotalIncomes = 5.73m
            Unrealized = 0m
            Realized = 0m
            Performance = 0m
            LatestPrice = 0m
            OpenTrades = true }

          // Snapshot 2: 2024-04-29 (After closing vertical spread)
          { Id = 0
            Date = DateOnly(2024, 4, 29)
            Ticker = ticker
            Currency = currency
            TotalShares = 0m
            Weight = 0m
            CostBasis = 0m
            RealCost = 0m
            Dividends = 0m
            Options = 1.46m // Cumulative: 5.73 + 4.86 - 9.13 = 1.46
            TotalIncomes = 1.46m
            Unrealized = 0m
            Realized = 1.46m // Positions closed
            Performance = 0m
            LatestPrice = 0m
            OpenTrades = false }

          // Snapshot 3: Today (Current snapshot - same as 2024-04-29)
          { Id = 0
            Date = today
            Ticker = ticker
            Currency = currency
            TotalShares = 0m
            Weight = 0m
            CostBasis = 0m
            RealCost = 0m
            Dividends = 0m
            Options = 1.46m // Same as Snapshot 2 (no new trades)
            TotalIncomes = 1.46m
            Unrealized = 0m
            Realized = 1.46m
            Performance = 0m
            LatestPrice = 0m
            OpenTrades = false } ]

    // ==================== BROKER ACCOUNT SNAPSHOTS ====================

    /// <summary>
    /// Generate expected BrokerAccount financial snapshots.
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
        : BrokerFinancialSnapshot list =

        let today = DateOnly.FromDateTime(DateTime.Now)

        [
          // Snapshot 1: 2024-04-22 (First deposit)
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

          // Snapshot 2: 2024-04-23 (Second deposit)
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

          // Snapshot 3: 2024-04-24 (Third deposit)
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

          // Snapshot 4: 2024-04-25 (SOFI trade)
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
            OptionsIncome = 33.86m
            OtherIncome = 0m
            OpenTrades = true
            NetCashFlow = 912.65m }

          // Snapshot 5: 2024-04-26 (MPW + PLTR trades)
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
            OptionsIncome = 52.32m
            OtherIncome = 0m
            OpenTrades = true
            NetCashFlow = 931.11m }

          // Snapshot 6: 2024-04-27 (Balance adjustment)
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
            OptionsIncome = 52.32m
            OtherIncome = 0m
            OpenTrades = true
            NetCashFlow = 931.11m }

          // Snapshot 7: 2024-04-29 (Multiple closing + reopening trades)
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
            Fees = 1.49m
            Deposited = 878.79m
            Withdrawn = 0m
            DividendsReceived = 0m
            OptionsIncome = 39.51m
            OtherIncome = 0m
            OpenTrades = true
            NetCashFlow = 941.95m }

          // Snapshot 8: 2024-04-30 (Final SOFI trade)
          { Id = 0
            Date = DateOnly(2024, 4, 30)
            Broker = Some broker
            BrokerAccount = Some brokerAccount
            Currency = currency
            MovementCounter = 16
            RealizedGains = 23.65m
            RealizedPercentage = 0m
            UnrealizedGains = 0m
            UnrealizedGainsPercentage = 0m
            Invested = 0m
            Commissions = 7.00m
            Fees = 1.63m
            Deposited = 878.79m
            Withdrawn = 0m
            DividendsReceived = 0m
            OptionsIncome = 47.45m
            OtherIncome = 0m
            OpenTrades = true
            NetCashFlow = 949.89m }

          // Snapshot 9: Today (Current snapshot - same as 2024-04-30)
          { Id = 0
            Date = today
            Broker = Some broker
            BrokerAccount = Some brokerAccount
            Currency = currency
            MovementCounter = 16
            RealizedGains = 23.65m
            RealizedPercentage = 0m
            UnrealizedGains = 0m
            UnrealizedGainsPercentage = 0m
            Invested = 0m
            Commissions = 7.00m
            Fees = 1.63m
            Deposited = 878.79m
            Withdrawn = 0m
            DividendsReceived = 0m
            OptionsIncome = 47.45m
            OtherIncome = 0m
            OpenTrades = true
            NetCashFlow = 949.89m } ]
