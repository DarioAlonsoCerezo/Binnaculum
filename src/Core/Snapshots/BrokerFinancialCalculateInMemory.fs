namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging

/// <summary>
/// In-memory financial calculation engine for batch processing.
/// Performs all financial calculations without database I/O for maximum performance.
/// </summary>
module internal BrokerFinancialCalculateInMemory =

    /// <summary>
    /// Calculate a single snapshot using pure in-memory operations.
    /// This is the core calculation logic extracted for batch processing.
    /// </summary>
    /// <param name="currencyMovements">The currency-specific movements for calculations</param>
    /// <param name="previousSnapshot">Optional previous snapshot for cumulative calculations</param>
    /// <param name="date">The date for the new snapshot</param>
    /// <param name="currencyId">The currency ID for this snapshot</param>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="brokerAccountSnapshotId">The broker account snapshot ID</param>
    /// <returns>BrokerFinancialSnapshot created in memory (not persisted)</returns>
    let calculateSnapshot
        (currencyMovements: CurrencyMovementData)
        (previousSnapshot: BrokerFinancialSnapshot option)
        (date: DateTimePattern)
        (currencyId: int)
        (brokerAccountId: int)
        (brokerAccountSnapshotId: int)
        : BrokerFinancialSnapshot =

        // Calculate financial metrics from movements
        let calculatedMetrics =
            BrokerFinancialsMetricsFromMovements.calculate currencyMovements currencyId date

        // Calculate cumulative values by adding previous snapshot values (if any) to current metrics
        let cumulativeDeposited =
            match previousSnapshot with
            | Some prev -> Money.FromAmount(prev.Deposited.Value + calculatedMetrics.Deposited.Value)
            | None -> calculatedMetrics.Deposited

        let cumulativeWithdrawn =
            match previousSnapshot with
            | Some prev -> Money.FromAmount(prev.Withdrawn.Value + calculatedMetrics.Withdrawn.Value)
            | None -> calculatedMetrics.Withdrawn

        let cumulativeInvested =
            match previousSnapshot with
            | Some prev -> Money.FromAmount(prev.Invested.Value + calculatedMetrics.Invested.Value)
            | None -> calculatedMetrics.Invested

        let cumulativeRealizedGains =
            match previousSnapshot with
            | Some prev -> Money.FromAmount(prev.RealizedGains.Value + calculatedMetrics.RealizedGains.Value)
            | None -> calculatedMetrics.RealizedGains

        let cumulativeDividendsReceived =
            match previousSnapshot with
            | Some prev -> Money.FromAmount(prev.DividendsReceived.Value + calculatedMetrics.DividendsReceived.Value)
            | None -> calculatedMetrics.DividendsReceived

        let cumulativeOptionsIncome =
            match previousSnapshot with
            | Some prev -> Money.FromAmount(prev.OptionsIncome.Value + calculatedMetrics.OptionsIncome.Value)
            | None -> calculatedMetrics.OptionsIncome

        let cumulativeOtherIncome =
            match previousSnapshot with
            | Some prev -> Money.FromAmount(prev.OtherIncome.Value + calculatedMetrics.OtherIncome.Value)
            | None -> calculatedMetrics.OtherIncome

        let cumulativeCommissions =
            match previousSnapshot with
            | Some prev -> Money.FromAmount(prev.Commissions.Value + calculatedMetrics.Commissions.Value)
            | None -> calculatedMetrics.Commissions

        let cumulativeFees =
            match previousSnapshot with
            | Some prev -> Money.FromAmount(prev.Fees.Value + calculatedMetrics.Fees.Value)
            | None -> calculatedMetrics.Fees

        let cumulativeMovementCounter =
            match previousSnapshot with
            | Some prev -> prev.MovementCounter + calculatedMetrics.MovementCounter
            | None -> calculatedMetrics.MovementCounter

        // Calculate unrealized gains synchronously (this is a simplified version for batch processing)
        // In production, we use BrokerFinancialUnrealizedGains.calculateUnrealizedGains which is async
        // For now, we'll use the option unrealized gains from metrics and estimate stock unrealized gains
        let stockUnrealizedGains = Money.FromAmount(0m) // Will be calculated when positions are available

        let totalUnrealizedGains =
            Money.FromAmount(stockUnrealizedGains.Value + calculatedMetrics.OptionUnrealizedGains.Value)

        // Calculate cumulative NetCashFlow as the actual contributed capital
        let cumulativeNetCashFlow =
            cumulativeDeposited.Value
            - cumulativeWithdrawn.Value
            - cumulativeCommissions.Value
            - cumulativeFees.Value
            + cumulativeDividendsReceived.Value
            + cumulativeOptionsIncome.Value
            + cumulativeOtherIncome.Value

        let unrealizedGainsPercentage =
            if cumulativeNetCashFlow > 0m then
                (totalUnrealizedGains.Value / cumulativeNetCashFlow) * 100m
            else
                0m

        let realizedPercentage =
            if cumulativeNetCashFlow > 0m then
                (cumulativeRealizedGains.Value / cumulativeNetCashFlow) * 100m
            else
                0m

        // Create the financial snapshot in memory (not persisted)
        { Base = SnapshotManagerUtils.createBaseSnapshot date
          BrokerId = 0 // Set to 0 for account-level snapshots
          BrokerAccountId = brokerAccountId
          CurrencyId = currencyId
          MovementCounter = cumulativeMovementCounter
          BrokerSnapshotId = 0 // Set to 0 for account-level snapshots
          BrokerAccountSnapshotId = brokerAccountSnapshotId
          RealizedGains = cumulativeRealizedGains
          RealizedPercentage = realizedPercentage
          UnrealizedGains = totalUnrealizedGains
          UnrealizedGainsPercentage = unrealizedGainsPercentage
          Invested = cumulativeInvested
          Commissions = cumulativeCommissions
          Fees = cumulativeFees
          Deposited = cumulativeDeposited
          Withdrawn = cumulativeWithdrawn
          DividendsReceived = cumulativeDividendsReceived
          OptionsIncome = cumulativeOptionsIncome
          OtherIncome = cumulativeOtherIncome
          OpenTrades = calculatedMetrics.HasOpenPositions }

    /// <summary>
    /// Calculate snapshot when movements exist but no previous snapshot.
    /// This is SCENARIO B - initial snapshot creation.
    /// </summary>
    let calculateInitialSnapshot
        (currencyMovements: CurrencyMovementData)
        (date: DateTimePattern)
        (currencyId: int)
        (brokerAccountId: int)
        (brokerAccountSnapshotId: int)
        : BrokerFinancialSnapshot =

        calculateSnapshot currencyMovements None date currencyId brokerAccountId brokerAccountSnapshotId

    /// <summary>
    /// Calculate snapshot when both movements and previous snapshot exist.
    /// This is SCENARIO A - standard cumulative snapshot creation.
    /// </summary>
    let calculateNewSnapshot
        (currencyMovements: CurrencyMovementData)
        (previousSnapshot: BrokerFinancialSnapshot)
        (date: DateTimePattern)
        (currencyId: int)
        (brokerAccountId: int)
        (brokerAccountSnapshotId: int)
        : BrokerFinancialSnapshot =

        calculateSnapshot
            currencyMovements
            (Some previousSnapshot)
            date
            currencyId
            brokerAccountId
            brokerAccountSnapshotId

    /// <summary>
    /// Carry forward a previous snapshot when no movements exist for a date.
    /// This is SCENARIO E - maintaining continuity without changes.
    /// </summary>
    let carryForwardSnapshot
        (previousSnapshot: BrokerFinancialSnapshot)
        (newDate: DateTimePattern)
        (brokerAccountSnapshotId: int)
        : BrokerFinancialSnapshot =

        // Create a copy of the previous snapshot with the new date
        { previousSnapshot with
            Base = SnapshotManagerUtils.createBaseSnapshot newDate
            BrokerAccountSnapshotId = brokerAccountSnapshotId }
