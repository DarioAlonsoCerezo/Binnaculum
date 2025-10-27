namespace Core.Tests.Integration

open System
open Binnaculum.Core.Models
open TestModels

/// <summary>
/// Expected snapshot data for TSLL Import integration tests.
///
/// This module provides factory functions to generate expected snapshots for:
/// - TSLL ticker (72 snapshots spanning multiple dates)
/// - BrokerAccount (72 snapshots spanning multiple dates)
/// - AutoImportOperations (4 operations)
///
/// These snapshots are based on TsllImportTest.csv import data and are
/// generated from core-calculated values to ensure consistency.
/// </summary>
module TsllImportExpectedSnapshots =

    // ==================== TSLL TICKER SNAPSHOTS ====================

    /// <summary>
    /// Generate expected TSLL ticker snapshots with descriptions.
    ///
    /// Includes all 71 snapshots from the TSLL import test CSV file.
    /// Snapshots span from 2024-05-30 (first trade) through 2025-10-23 (last movement).
    /// </summary>
    let getTSLLSnapshots (ticker: Ticker) (currency: Currency) : ExpectedSnapshot<TickerCurrencySnapshot> list =
        TsllTickerSnapshots.getTSLLSnapshots ticker currency

    // ==================== BROKER ACCOUNT SNAPSHOTS ====================

    /// <summary>
    /// Generate expected BrokerAccount financial snapshots with descriptions.
    ///
    /// Includes all 71 snapshots from the TSLL import test.
    /// Snapshots span from 2024-05-30 (first trade) through 2025-10-23 (last movement).
    /// </summary>
    let getBrokerAccountSnapshots
        (broker: Broker)
        (brokerAccount: BrokerAccount)
        (currency: Currency)
        : ExpectedSnapshot<BrokerFinancialSnapshot> list =

        TsllBrokerSnapshots.getBrokerAccountSnapshots broker brokerAccount currency
    // ==================== AUTO-IMPORT OPERATIONS ====================

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
        TsllOperations.getTSLLOperations brokerAccount ticker currency
