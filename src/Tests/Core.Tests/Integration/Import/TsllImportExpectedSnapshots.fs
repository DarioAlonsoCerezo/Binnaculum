namespace Core.Tests.Integration

open System
open Binnaculum.Core.Models
open TestModels

/// <summary>
/// Expected snapshot data for TSLL Import integration tests.
///
/// This module provides factory functions to generate expected snapshots for:
/// - TSLL ticker (currently 2 snapshots: 2024-05-30, 2024-06-07)
/// - BrokerAccount (currently 2 snapshots: 2024-05-30, 2024-06-07)
/// - AutoImportOperations (currently 1 operation: 2024-05-30 to 2024-06-07)
///
/// These snapshots are based on TsllImportTest.csv import data.
///
/// CSV Data Summary (First Operation):
/// - 1 option trade: SELL_TO_OPEN TSLL 240607P00008500 @ $0.18
/// - 1 expiration: PUT expires worthless on 2024-06-07
///
/// Financial Summary (First Operation):
/// - Total Premium: $18.00
/// - Total Realized: $16.87 (premium - commission - fees)
/// - Commissions: $1.00
/// - Fees: $0.13
/// </summary>
module TsllImportExpectedSnapshots =

    // ==================== TSLL TICKER SNAPSHOTS ====================

    /// <summary>
    /// Generate expected TSLL ticker snapshots with descriptions.
    ///
    /// Currently includes 2 snapshots (will expand to 72 total):
    /// 1. 2024-05-30: After SELL_TO_OPEN PUT (first position opened)
    /// 2. 2024-06-07: After PUT expiration (position closed, fully realized)
    /// </summary>
    let getTSLLSnapshots (ticker: Ticker) (currency: Currency) : ExpectedSnapshot<TickerCurrencySnapshot> list =
        [
          // Snapshot 1: 2024-05-30 (After SELL_TO_OPEN PUT)
          // Trade: SELL_TO_OPEN TSLL 240607P00007000 @ $0.15 = $15.00
          // Commission: $1.00, Fee: $0.14
          { Data =
              { Id = 0 // Will be assigned by database
                Date = DateOnly(2024, 5, 30)
                Ticker = ticker
                Currency = currency
                TotalShares = 0m // Options only, no equity shares
                Weight = 0m
                CostBasis = 0m
                RealCost = 0m
                Dividends = 0m
                DividendTaxes = 0m
                Options = 15.00m // SELL_TO_OPEN premium received
                TotalIncomes = 13.86m // Premium $15.00 - Commission $1.00 - Fee $0.14
                Unrealized = 0m // No unrealized gains yet (position just opened)
                Realized = 0m // No closed positions yet
                Performance = 0m
                LatestPrice = 0m
                OpenTrades = true // PUT position is open
                Commissions = 1.00m // SELL_TO_OPEN commission
                Fees = 0.14m } // SELL_TO_OPEN fee
            Description = "2024-05-30 - After SELL_TO_OPEN PUT (initial position)" }

          // Snapshot 2: 2024-06-07 (After PUT expiration - worthless)
          // Event: PUT expires worthless (no trade, automatic closure)
          // Position closed with full profit
          { Data =
              { Id = 0
                Date = DateOnly(2024, 6, 7)
                Ticker = ticker
                Currency = currency
                TotalShares = 0m
                Weight = 0m
                CostBasis = 0m
                RealCost = 0m
                Dividends = 0m
                DividendTaxes = 0m
                Options = 15.00m // Same as previous (no new trades)
                TotalIncomes = 13.86m // Same as previous
                Unrealized = 0m // Position closed
                Realized = 13.86m // Full profit realized (premium - costs)
                Performance = 0m
                LatestPrice = 0m
                OpenTrades = false // PUT expired (position closed)
                Commissions = 1.00m // Same as previous (no new commissions)
                Fees = 0.14m } // Same as previous (no new fees)
            Description = "2024-06-07 - After PUT expiration (position closed, realized profit)" } ]

    // ==================== BROKER ACCOUNT SNAPSHOTS ====================

    /// <summary>
    /// Generate expected BrokerAccount financial snapshots with descriptions.
    ///
    /// Currently includes 2 snapshots (will expand to 72+ total):
    /// 1. 2024-05-30: After first option trade
    /// 2. 2024-06-07: After PUT expiration (position closed)
    /// </summary>
    let getBrokerAccountSnapshots
        (broker: Broker)
        (brokerAccount: BrokerAccount)
        (currency: Currency)
        : ExpectedSnapshot<BrokerFinancialSnapshot> list =

        [
          // Snapshot 1: 2024-05-30 (After SELL_TO_OPEN PUT)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 5, 30)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 1 // First trade
                RealizedGains = 0m // No closed positions yet
                RealizedPercentage = 0m
                UnrealizedGains = 0m
                UnrealizedGainsPercentage = 0m
                Invested = 0m
                Commissions = 1.00m
                Fees = 0.14m
                Deposited = 0m // No deposits in this test
                Withdrawn = 0m
                DividendsReceived = 0m
                OptionsIncome = 15.00m // Premium received from SELL_TO_OPEN
                OtherIncome = 0m
                OpenTrades = true
                NetCashFlow = 13.86m } // Premium $15.00 - Commission $1.00 - Fee $0.14
            Description = "2024-05-30 - After SELL_TO_OPEN PUT" }

          // Snapshot 2: 2024-06-07 (After PUT expiration)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 6, 7)
                Broker = Some broker
                BrokerAccount = Some brokerAccount
                Currency = currency
                MovementCounter = 2 // 2 movements: 1 trade + 1 expiration
                RealizedGains = 13.86m // Full profit realized
                RealizedPercentage = 100.00m // 100% of NetCashFlow is realized
                UnrealizedGains = 0m // Position closed
                UnrealizedGainsPercentage = 0m
                Invested = 0m
                Commissions = 1.00m // Same (no new commissions)
                Fees = 0.14m // Same (no new fees)
                Deposited = 0m
                Withdrawn = 0m
                DividendsReceived = 0m
                OptionsIncome = 15.00m // Same (no new trades)
                OtherIncome = 0m
                OpenTrades = false // Position closed via expiration
                NetCashFlow = 13.86m } // Same (no new cash flow)
            Description = "2024-06-07 - After PUT expiration (position closed)" } ]

    // ==================== AUTO-IMPORT OPERATIONS ====================

    /// <summary>
    /// Generate expected AutoImportOperation data for TSLL ticker.
    ///
    /// Currently includes 1 operation (will expand as more trades are added):
    /// - Operation 1: 2024-05-30 to 2024-06-07 (SELL_TO_OPEN PUT → Expiration)
    ///
    /// Financial Calculation:
    /// - Realized: $13.86 (premium $15.00 - commission $1.00 - fee $0.14)
    /// - Commissions: $1.00 (SELL_TO_OPEN commission)
    /// - Fees: $0.14 (SELL_TO_OPEN fee)
    /// - Premium: $15.00 (SELL_TO_OPEN premium)
    /// - CapitalDeployed: $1.14 (commission + fees paid upfront)
    /// - Performance: 1215.79% ($13.86 / $1.14 × 100)
    ///
    /// Note: For sold options, capital deployed is the commission + fees paid,
    /// as the premium is received upfront (not deployed).
    /// </summary>
    let getTSLLOperations
        (brokerAccount: BrokerAccount)
        (ticker: Ticker)
        (currency: Currency)
        : ExpectedOperation<AutoImportOperation> list =

        [
          // Operation 1: First PUT cycle (2024-05-30 to 2024-06-07)
          { Data =
              { Id = 0 // Will be assigned by database
                BrokerAccount = brokerAccount
                Ticker = ticker
                Currency = currency
                IsOpen = false // Operation is closed (PUT expired)
                OpenDate = DateTime(2024, 5, 30, 0, 0, 1) // Snapshot date
                CloseDate = Some(DateTime(2024, 6, 7, 0, 0, 1)) // Expiration date

                // Financial metrics
                Realized = 13.86m // Premium $15.00 - Commission $1.00 - Fee $0.14
                RealizedToday = 0m // Not used in test expectations
                Commissions = 1.00m // SELL_TO_OPEN commission
                Fees = 0.14m // SELL_TO_OPEN fee
                Premium = 15.00m // SELL_TO_OPEN premium received
                Dividends = 0m
                DividendTaxes = 0m
                CapitalDeployed = 686.14m // Strike $6.85 × 100 shares + Commission $1.00 + Fee $0.14 = $686.14
                CapitalDeployedToday = 0m // Not used in test expectations
                Performance = 2.02m } // ($13.86 / $686.14) × 100 = 2.02%
            Description = "TSLL Operation #1: SELL_TO_OPEN PUT → Expiration (2024-05-30 to 2024-06-07)" } ]
