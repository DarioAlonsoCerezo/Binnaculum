namespace Core.Tests.Integration

open System
open Binnaculum.Core.Models
open TestModels

/// <summary>
/// Expected snapshot data for Future Options Import integration tests.
///
/// This module provides factory functions to generate expected snapshots for:
/// - BrokerAccount (snapshots spanning the trading period)
/// - AutoImportOperations (operations representing distinct future options strategies)
///
/// These snapshots are based on FutureOptions.csv import data and are
/// generated from core-calculated values to ensure consistency.
///
/// Note: This test validates 3 separate tickers (/MESU5, /MESZ5, /MESH6) exist
/// but does not verify individual ticker snapshots - focus is on operations and broker snapshots.
/// </summary>
module FutureOptionsImportExpectedSnapshots =

    // ==================== BROKER ACCOUNT SNAPSHOTS ====================

    /// <summary>
    /// Generate expected BrokerAccount financial snapshots with descriptions.
    ///
    /// Includes snapshots from the Future Options import test.
    /// Snapshots span from 2025-08-24 (first trade) through 2025-11-12 (today).
    /// Includes option premiums collected/paid and realized gains/losses.
    /// </summary>
    let getBrokerAccountSnapshots
        (broker: Broker)
        (brokerAccount: BrokerAccount)
        (currency: Currency)
        : ExpectedSnapshot<BrokerFinancialSnapshot> list =

        FutureOptionsBrokerSnapshots.getBrokerAccountSnapshots broker brokerAccount currency

    // ==================== AUTO-IMPORT OPERATIONS ====================

    /// <summary>
    /// Generate expected AutoImportOperation data for Future Options trading.
    ///
    /// Includes operations from the Future Options import test.
    /// These represent distinct trading strategies with their financial metrics,
    /// all pure options plays with no underlying shares held.
    ///
    /// NOTE: Requires the 3 future tickers as parameters since operations span multiple tickers.
    /// </summary>
    let getFutureOptionsOperations
        (brokerAccount: BrokerAccount)
        (mesu5Ticker: Ticker)
        (mesz5Ticker: Ticker)
        (mesh6Ticker: Ticker)
        (currency: Currency)
        : ExpectedOperation<AutoImportOperation> list =
        FutureOptionsOperations.getFutureOptionsOperations brokerAccount mesu5Ticker mesz5Ticker mesh6Ticker currency
