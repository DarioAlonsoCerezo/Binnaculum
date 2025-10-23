namespace Core.Tests.Integration

open System
open Binnaculum.Core.Models
open TestModels

/// <summary>
/// Expected snapshot data for Pfizer (PFE) Import integration tests.
///
/// This module provides factory functions to generate expected snapshots for:
/// - PFE ticker (4 snapshots: 2025-08-25, 2025-10-01, 2025-10-03, today)
/// - BrokerAccount (4 snapshots: 2025-08-25, 2025-10-01, 2025-10-03, today)
///
/// These snapshots are based on PfizerImportTest.csv import data.
///
/// CSV Data Summary:
/// - 4 option trades forming 2 complete round-trip pairs
/// - Trade 1: BUY_TO_OPEN PFE 20.00 CALL 01/16/26 @ -$555.12 (2025-08-25)
/// - Trade 2: SELL_TO_OPEN PFE 27.00 CALL 10/10/25 @ $49.88 (2025-10-01)
/// - Trade 3: BUY_TO_CLOSE PFE 27.00 CALL 10/10/25 @ -$64.12 (2025-10-03) - Closes Trade 2
/// - Trade 4: SELL_TO_CLOSE PFE 20.00 CALL 01/16/26 @ $744.88 (2025-10-03) - Closes Trade 1
///
/// Financial Summary:
/// - Total OptionsIncome: $175.52 (sum of all premiums)
/// - Realized Gains: $175.52 (FIFO: $189.76 profit - $14.24 loss)
/// - Commissions: $2.00
/// - Fees: $0.48
/// - Unrealized Gains: $0.00 (all positions closed)
/// </summary>
module PfizerImportExpectedSnapshots =

    // ==================== PFE TICKER SNAPSHOTS ====================

    /// <summary>
    /// Generate expected PFE ticker snapshots with descriptions.
    ///
    /// PFE has 4 snapshots:
    /// 1. 2025-08-25: After BUY_TO_OPEN (first position opened)
    /// 2. 2025-10-01: After SELL_TO_OPEN (second position opened)
    /// 3. 2025-10-03: After both positions closed (2 trades same day)
    /// 4. Today: Current snapshot (same as 2025-10-03)
    /// </summary>
    let getPFESnapshots (ticker: Ticker) (currency: Currency) : ExpectedSnapshot<TickerCurrencySnapshot> list =
        let today = DateOnly.FromDateTime(DateTime.Now)

        [
          // Snapshot 1: 2025-08-25 (After BUY_TO_OPEN)
          { Data =
              { Id = 0 // Will be assigned by database
                Date = DateOnly(2025, 8, 25)
                Ticker = ticker
                Currency = currency
                TotalShares = 0m
                Weight = 0m
                CostBasis = 0m
                RealCost = 0m
                Dividends = 0m
                Options = -555.12m // BUY_TO_OPEN: -$555.12
                TotalIncomes = -555.12m
                Unrealized = 0m
                Realized = 0m
                Performance = 0m
                LatestPrice = 0m
                OpenTrades = true }
            Description = "2025-08-25 - After BUY_TO_OPEN" }

          // Snapshot 2: 2025-10-01 (After SELL_TO_OPEN)
          { Data =
              { Id = 0
                Date = DateOnly(2025, 10, 1)
                Ticker = ticker
                Currency = currency
                TotalShares = 0m
                Weight = 0m
                CostBasis = 0m
                RealCost = 0m
                Dividends = 0m
                Options = -505.24m // Previous -$555.12 + SELL_TO_OPEN $49.88 = -$505.24
                TotalIncomes = -505.24m
                Unrealized = 0m
                Realized = 0m
                Performance = 0m
                LatestPrice = 0m
                OpenTrades = true }
            Description = "2025-10-01 - After SELL_TO_OPEN" }

          // Snapshot 3: 2025-10-03 (After both positions closed)
          { Data =
              { Id = 0
                Date = DateOnly(2025, 10, 3)
                Ticker = ticker
                Currency = currency
                TotalShares = 0m
                Weight = 0m
                CostBasis = 0m
                RealCost = 0m
                Dividends = 0m
                Options = 175.52m // All trades: -$555.12 + $49.88 - $64.12 + $744.88 = $175.52
                TotalIncomes = 175.52m
                Unrealized = 0m
                Realized = 175.52m // FIFO: (-$14.24) + $189.76 = $175.52
                Performance = 0m
                LatestPrice = 0m
                OpenTrades = false }
            Description = "2025-10-03 - After both positions closed" }

          // Snapshot 4: Today (Current snapshot - same as 2025-10-03)
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
                Options = 175.52m
                TotalIncomes = 175.52m
                Unrealized = 0m
                Realized = 175.52m
                Performance = 0m
                LatestPrice = 0m
                OpenTrades = false }
            Description = sprintf "%s - Current snapshot" (today.ToString("yyyy-MM-dd")) } ]

    // ==================== BROKER ACCOUNT SNAPSHOTS ====================

    /// <summary>
    /// Generate expected BrokerAccount financial snapshots with descriptions.
    ///
    /// BrokerAccount has 4 snapshots:
    /// 1. 2025-08-25: After first trade (BUY_TO_OPEN)
    /// 2. 2025-10-01: After second trade (SELL_TO_OPEN)
    /// 3. 2025-10-03: After closing both positions (BUY_TO_CLOSE + SELL_TO_CLOSE)
    /// 4. Today: Current snapshot (same as 2025-10-03)
    /// </summary>
    let getBrokerAccountSnapshots
        (broker: Broker)
        (brokerAccount: BrokerAccount)
        (currency: Currency)
        : ExpectedSnapshot<BrokerFinancialSnapshot> list =

        let today = DateOnly.FromDateTime(DateTime.Now)

        [
          // Snapshot 1: 2025-08-25 (After BUY_TO_OPEN)
          { Data =
              { Id = 0
                Date = DateOnly(2025, 8, 25)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 1
                RealizedGains = 0m // No closed positions yet
                RealizedPercentage = 0m
                UnrealizedGains = 0m
                UnrealizedGainsPercentage = 0m
                Invested = 0m
                Commissions = 1.00m // First trade commission
                Fees = 0.12m // First trade fee
                Deposited = 0m // No deposits in this test
                Withdrawn = 0m
                DividendsReceived = 0m
                OptionsIncome = -554.00m // BUY_TO_OPEN Premium (pure option value)
                OtherIncome = 0m
                OpenTrades = true
                NetCashFlow = -555.12m // NetCashFlow = OptionsIncome - Commissions - Fees = -554.00 - 1.00 - 0.12 = -555.12
              }
            Description = "2025-08-25 - After BUY_TO_OPEN" }

          // Snapshot 2: 2025-10-01 (After SELL_TO_OPEN)
          { Data =
              { Id = 0
                Date = DateOnly(2025, 10, 1)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 2
                RealizedGains = 0m // No closed positions yet
                RealizedPercentage = 0m
                UnrealizedGains = 0m
                UnrealizedGainsPercentage = 0m
                Invested = 0m
                Commissions = 2.00m // $1.00 + $1.00
                Fees = 0.24m // $0.12 + $0.12
                Deposited = 0m
                Withdrawn = 0m
                DividendsReceived = 0m
                OptionsIncome = -503.00m // Premium: -$554.00 + $51.00
                OtherIncome = 0m
                OpenTrades = true
                NetCashFlow = -505.24m } // -$503.00 - $2.00 - $0.24
            Description = "2025-10-01 - After SELL_TO_OPEN" }

          // Snapshot 3: 2025-10-03 (After both positions closed)
          { Data =
              { Id = 0
                Date = DateOnly(2025, 10, 3)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 4
                RealizedGains = 175.52m // FIFO: -$14.24 + $189.76
                RealizedPercentage = 0m // Percentage calculation depends on invested amount
                UnrealizedGains = 0m // All positions closed
                UnrealizedGainsPercentage = 0m
                Invested = 0m
                Commissions = 2.00m // No new commissions on closing trades
                Fees = 0.48m // $0.12 * 4 trades
                Deposited = 0m
                Withdrawn = 0m
                DividendsReceived = 0m
                OptionsIncome = 178.00m // Premium: -$554.00 + $51.00 - $64.00 + $745.00
                OtherIncome = 0m
                OpenTrades = false
                NetCashFlow = 175.52m } // $178.00 - $2.00 - $0.48
            Description = "2025-10-03 - After both positions closed" }

          // Snapshot 4: Today (Current snapshot - same as 2025-10-03)
          { Data =
              { Id = 0
                Date = today
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 4
                RealizedGains = 175.52m
                RealizedPercentage = 0m
                UnrealizedGains = 0m
                UnrealizedGainsPercentage = 0m
                Invested = 0m
                Commissions = 2.00m
                Fees = 0.48m
                Deposited = 0m
                Withdrawn = 0m
                DividendsReceived = 0m
                OptionsIncome = 178.00m // Same as Snapshot 3
                OtherIncome = 0m
                OpenTrades = false
                NetCashFlow = 175.52m } // Same as Snapshot 3
            Description = sprintf "%s - Current snapshot" (today.ToString("yyyy-MM-dd")) } ]
