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

    /// <summary>
    /// Update an existing snapshot with new movements and previous snapshot baseline.
    /// This is SCENARIO C - when new movements are added to a date that already has a snapshot.
    /// Recalculates by combining previous baseline + all movements (existing + new).
    /// </summary>
    let updateExistingSnapshot
        (currencyMovements: CurrencyMovementData)
        (previousSnapshot: BrokerFinancialSnapshot)
        (existingSnapshot: BrokerFinancialSnapshot)
        (date: DateTimePattern)
        (currencyId: int)
        (brokerAccountId: int)
        (brokerAccountSnapshotId: int)
        : BrokerFinancialSnapshot =

        // Calculate financial metrics from ALL movements for this date
        let calculatedMetrics =
            BrokerFinancialsMetricsFromMovements.calculate currencyMovements currencyId date

        // Calculate cumulative values using previous snapshot as baseline
        let cumulativeDeposited =
            Money.FromAmount(previousSnapshot.Deposited.Value + calculatedMetrics.Deposited.Value)

        let cumulativeWithdrawn =
            Money.FromAmount(previousSnapshot.Withdrawn.Value + calculatedMetrics.Withdrawn.Value)

        let cumulativeInvested =
            Money.FromAmount(previousSnapshot.Invested.Value + calculatedMetrics.Invested.Value)

        let cumulativeRealizedGains =
            Money.FromAmount(previousSnapshot.RealizedGains.Value + calculatedMetrics.RealizedGains.Value)

        let cumulativeDividendsReceived =
            Money.FromAmount(previousSnapshot.DividendsReceived.Value + calculatedMetrics.DividendsReceived.Value)

        let cumulativeOptionsIncome =
            Money.FromAmount(previousSnapshot.OptionsIncome.Value + calculatedMetrics.OptionsIncome.Value)

        let cumulativeOtherIncome =
            Money.FromAmount(previousSnapshot.OtherIncome.Value + calculatedMetrics.OtherIncome.Value)

        let cumulativeCommissions =
            Money.FromAmount(previousSnapshot.Commissions.Value + calculatedMetrics.Commissions.Value)

        let cumulativeFees =
            Money.FromAmount(previousSnapshot.Fees.Value + calculatedMetrics.Fees.Value)

        let cumulativeMovementCounter = previousSnapshot.MovementCounter + calculatedMetrics.MovementCounter

        // Calculate unrealized gains (simplified for now - will be enhanced in Phase 2)
        let stockUnrealizedGains = Money.FromAmount(0m) // TODO: Phase 2 - pre-loaded market prices

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

        // Update the existing snapshot with recalculated values, keeping original base info
        { existingSnapshot with
            MovementCounter = cumulativeMovementCounter
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
    /// Directly update an existing snapshot with new movements without previous snapshot baseline.
    /// This is SCENARIO D - when new movements exist but no previous snapshot is found.
    /// The existing snapshot itself serves as the baseline.
    /// </summary>
    let directUpdateSnapshot
        (currencyMovements: CurrencyMovementData)
        (existingSnapshot: BrokerFinancialSnapshot)
        (date: DateTimePattern)
        (currencyId: int)
        (brokerAccountId: int)
        (brokerAccountSnapshotId: int)
        : BrokerFinancialSnapshot =

        // Calculate financial metrics from new movements
        let calculatedMetrics =
            BrokerFinancialsMetricsFromMovements.calculate currencyMovements currencyId date

        // Calculate unrealized gains (simplified for now - will be enhanced in Phase 2)
        let stockUnrealizedGains = Money.FromAmount(0m) // TODO: Phase 2 - pre-loaded market prices

        let totalUnrealizedGains =
            Money.FromAmount(stockUnrealizedGains.Value + calculatedMetrics.OptionUnrealizedGains.Value)

        // Calculate NetCashFlow as the actual contributed capital
        let netCashFlow =
            calculatedMetrics.Deposited.Value
            - calculatedMetrics.Withdrawn.Value
            - calculatedMetrics.Commissions.Value
            - calculatedMetrics.Fees.Value
            + calculatedMetrics.DividendsReceived.Value
            + calculatedMetrics.OptionsIncome.Value
            + calculatedMetrics.OtherIncome.Value

        let unrealizedPercentage =
            if netCashFlow > 0m then
                (totalUnrealizedGains.Value / netCashFlow) * 100m
            else
                0m

        let realizedPercentage =
            if netCashFlow > 0m then
                (calculatedMetrics.RealizedGains.Value / netCashFlow) * 100m
            else
                0m

        // Check if we should preserve existing realized gains (when no realized activity occurred)
        let hasRealizedTradeActivity =
            currencyMovements.Trades
            |> List.exists (fun trade ->
                trade.TradeCode = TradeCode.SellToClose || trade.TradeCode = TradeCode.BuyToClose)

        let hasRealizedOptionActivity =
            currencyMovements.OptionTrades
            |> List.exists (fun optionTrade ->
                match optionTrade.Code with
                | OptionCode.BuyToClose
                | OptionCode.SellToClose
                | OptionCode.Assigned
                | OptionCode.CashSettledAssigned
                | OptionCode.CashSettledExercised
                | OptionCode.Exercised -> true
                | _ -> false)

        let shouldPreserveRealized =
            not hasRealizedTradeActivity
            && not hasRealizedOptionActivity
            && existingSnapshot.RealizedGains.Value <> 0m
            && calculatedMetrics.RealizedGains.Value = 0m

        let (finalRealizedGains, finalRealizedPercentage) =
            if shouldPreserveRealized then
                (existingSnapshot.RealizedGains, existingSnapshot.RealizedPercentage)
            else
                (calculatedMetrics.RealizedGains, realizedPercentage)

        // Replace existing snapshot values with recalculated metrics
        { existingSnapshot with
            MovementCounter = calculatedMetrics.MovementCounter
            RealizedGains = finalRealizedGains
            RealizedPercentage = finalRealizedPercentage
            UnrealizedGains = totalUnrealizedGains
            UnrealizedGainsPercentage = unrealizedPercentage
            Invested = calculatedMetrics.Invested
            Commissions = calculatedMetrics.Commissions
            Fees = calculatedMetrics.Fees
            Deposited = calculatedMetrics.Deposited
            Withdrawn = calculatedMetrics.Withdrawn
            DividendsReceived = calculatedMetrics.DividendsReceived
            OptionsIncome = calculatedMetrics.OptionsIncome
            OtherIncome = calculatedMetrics.OtherIncome
            OpenTrades = calculatedMetrics.HasOpenPositions }

    /// <summary>
    /// Validate and correct an existing snapshot to match the previous snapshot.
    /// This is SCENARIO G - when no movements exist but both previous and existing snapshots are present.
    /// Ensures consistency by correcting any discrepancies.
    /// </summary>
    let validateAndCorrectSnapshot
        (previousSnapshot: BrokerFinancialSnapshot)
        (existingSnapshot: BrokerFinancialSnapshot)
        : BrokerFinancialSnapshot option =

        let snapshotsDiffer =
            previousSnapshot.RealizedGains <> existingSnapshot.RealizedGains
            || previousSnapshot.RealizedPercentage <> existingSnapshot.RealizedPercentage
            || previousSnapshot.UnrealizedGains <> existingSnapshot.UnrealizedGains
            || previousSnapshot.UnrealizedGainsPercentage <> existingSnapshot.UnrealizedGainsPercentage
            || previousSnapshot.Invested <> existingSnapshot.Invested
            || previousSnapshot.Commissions <> existingSnapshot.Commissions
            || previousSnapshot.Fees <> existingSnapshot.Fees
            || previousSnapshot.Deposited <> existingSnapshot.Deposited
            || previousSnapshot.Withdrawn <> existingSnapshot.Withdrawn
            || previousSnapshot.DividendsReceived <> existingSnapshot.DividendsReceived
            || previousSnapshot.OptionsIncome <> existingSnapshot.OptionsIncome
            || previousSnapshot.OtherIncome <> existingSnapshot.OtherIncome
            || previousSnapshot.OpenTrades <> existingSnapshot.OpenTrades
            || previousSnapshot.MovementCounter <> existingSnapshot.MovementCounter

        if snapshotsDiffer then
            // Return corrected snapshot
            Some
                { existingSnapshot with
                    RealizedGains = previousSnapshot.RealizedGains
                    RealizedPercentage = previousSnapshot.RealizedPercentage
                    UnrealizedGains = previousSnapshot.UnrealizedGains
                    UnrealizedGainsPercentage = previousSnapshot.UnrealizedGainsPercentage
                    Invested = previousSnapshot.Invested
                    Commissions = previousSnapshot.Commissions
                    Fees = previousSnapshot.Fees
                    Deposited = previousSnapshot.Deposited
                    Withdrawn = previousSnapshot.Withdrawn
                    DividendsReceived = previousSnapshot.DividendsReceived
                    OptionsIncome = previousSnapshot.OptionsIncome
                    OtherIncome = previousSnapshot.OtherIncome
                    OpenTrades = previousSnapshot.OpenTrades
                    MovementCounter = previousSnapshot.MovementCounter }
        else
            // No correction needed
            None

    /// <summary>
    /// Reset all financial fields of an existing snapshot to zero/default values.
    /// This is SCENARIO H - when no movements and no previous snapshot exist, but existing snapshot is present.
    /// This should be rare but can occur during data cleanup or corrections.
    /// </summary>
    let resetSnapshot (existingSnapshot: BrokerFinancialSnapshot) : BrokerFinancialSnapshot =
        { existingSnapshot with
            RealizedGains = Money.FromAmount 0m
            RealizedPercentage = 0m
            UnrealizedGains = Money.FromAmount 0m
            UnrealizedGainsPercentage = 0m
            Invested = Money.FromAmount 0m
            Commissions = Money.FromAmount 0m
            Fees = Money.FromAmount 0m
            Deposited = Money.FromAmount 0m
            Withdrawn = Money.FromAmount 0m
            DividendsReceived = Money.FromAmount 0m
            OptionsIncome = Money.FromAmount 0m
            OtherIncome = Money.FromAmount 0m
            OpenTrades = false
            MovementCounter = 0 }
