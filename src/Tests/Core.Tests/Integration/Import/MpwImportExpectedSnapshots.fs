namespace Core.Tests.Integration

open System
open Binnaculum.Core.Models
open TestModels

/// <summary>
/// Expected snapshot data for MPW Import integration tests.
///
/// This module provides factory functions to generate expected snapshots for:
/// - MPW ticker (snapshots spanning multiple dates with equity shares and options)
/// - BrokerAccount (snapshots spanning multiple dates)
/// - AutoImportOperations (operations representing distinct trading periods)
///
/// These snapshots are based on MPWImportTest.csv import data and are
/// generated from core-calculated values to ensure consistency.
/// </summary>
module MpwImportExpectedSnapshots =

    // ==================== MPW TICKER SNAPSHOTS ====================

    /// <summary>
    /// Generate expected MPW ticker snapshots with descriptions.
    ///
    /// Includes snapshots from the MPW import test CSV file.
    /// Snapshots span from 2024-09-17 (first trade) through 2025-10-17 (last movement).
    /// Includes both equity shares and option contracts (calls and puts).
    /// </summary>
    let getMPWSnapshots (ticker: Ticker) (currency: Currency) : ExpectedSnapshot<TickerCurrencySnapshot> list =
        MpwTickerSnapshots.getMPWSnapshots ticker currency

    // ==================== BROKER ACCOUNT SNAPSHOTS ====================

    /// <summary>
    /// Generate expected BrokerAccount financial snapshots with descriptions.
    ///
    /// Includes snapshots from the MPW import test.
    /// Snapshots span from 2024-09-17 (first trade) through 2025-10-17 (last movement).
    /// Includes cash deposits, dividends, option premiums, and realized gains/losses.
    /// </summary>
    let getBrokerAccountSnapshots
        (broker: Broker)
        (brokerAccount: BrokerAccount)
        (currency: Currency)
        : ExpectedSnapshot<BrokerFinancialSnapshot> list =

        MpwBrokerSnapshots.getBrokerAccountSnapshots broker brokerAccount currency

    // ==================== AUTO-IMPORT OPERATIONS ====================

    /// <summary>
    /// Generate expected AutoImportOperation data for MPW ticker.
    ///
    /// Includes operations from the MPW import test.
    /// These represent distinct trading periods with their financial metrics,
    /// including equity share trading and options trading.
    /// </summary>
    let getMPWOperations
        (brokerAccount: BrokerAccount)
        (ticker: Ticker)
        (currency: Currency)
        : ExpectedOperation<AutoImportOperation> list =
        MpwOperations.getMPWOperations brokerAccount ticker currency
