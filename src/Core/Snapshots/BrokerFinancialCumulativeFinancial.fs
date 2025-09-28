namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open BrokerFinancialSnapshotExtensions

module internal BrokerFinancialCumulativeFinancial =

    /// <summary>
    /// Creates a financial snapshot by combining calculated metrics with optional previous snapshot values.
    /// This function centralizes the logic for creating cumulative financial snapshots, reducing duplication
    /// across the three scenario methods (initial, new, and update).
    /// </summary>
    /// <param name="targetDate">The date for the new snapshot</param>
    /// <param name="currencyId">The currency ID for this snapshot</param>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="brokerAccountSnapshotId">The broker account snapshot ID to associate with</param>
    /// <param name="calculatedMetrics">The financial metrics calculated from movement data</param>
    /// <param name="previousSnapshot">Optional previous snapshot for cumulative calculations</param>
    /// <returns>Task that completes when the snapshot is calculated and saved</returns>
    let internal create
        (targetDate: DateTimePattern)
        (currencyId: int)
        (brokerAccountId: int)
        (brokerAccountSnapshotId: int)
        (calculatedMetrics: CalculatedFinancialMetrics)
        (previousSnapshot: BrokerFinancialSnapshot option)
        =
        task {
            System.Diagnostics.Debug.WriteLine(
                $"[BrokerFinancialCumulativeFinancial] Starting create for currency {currencyId}, date {targetDate}"
            )

            System.Diagnostics.Debug.WriteLine(
                $"[BrokerFinancialCumulativeFinancial] Calculated metrics - Deposited: {calculatedMetrics.Deposited.Value}, MovementCounter: {calculatedMetrics.MovementCounter}"
            )

            System.Diagnostics.Debug.WriteLine(
                $"[BrokerFinancialCumulativeFinancial] Has previous snapshot: {previousSnapshot.IsSome}"
            )

            // Calculate cumulative values by adding previous snapshot values (if any) to current metrics
            let cumulativeDeposited =
                match previousSnapshot with
                | Some prev ->
                    let result =
                        Money.FromAmount(prev.Deposited.Value + calculatedMetrics.Deposited.Value)

                    System.Diagnostics.Debug.WriteLine(
                        $"[BrokerFinancialCumulativeFinancial] Cumulative Deposited: previous {prev.Deposited.Value} + current {calculatedMetrics.Deposited.Value} = {result.Value}"
                    )

                    result
                | None ->
                    System.Diagnostics.Debug.WriteLine(
                        $"[BrokerFinancialCumulativeFinancial] No previous snapshot, using calculated Deposited: {calculatedMetrics.Deposited.Value}"
                    )

                    calculatedMetrics.Deposited

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
                | Some prev ->
                    Money.FromAmount(prev.DividendsReceived.Value + calculatedMetrics.DividendsReceived.Value)
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

            // Calculate unrealized gains from current positions (stocks) and add option unrealized gains
            let! (stockUnrealizedGains, stockUnrealizedGainsPercentage) =
                BrokerFinancialUnrealizedGains.calculateUnrealizedGains
                    calculatedMetrics.CurrentPositions
                    calculatedMetrics.CostBasisInfo
                    targetDate
                    currencyId

            // Combine stock and option unrealized gains
            let totalUnrealizedGains =
                Money.FromAmount(stockUnrealizedGains.Value + calculatedMetrics.OptionUnrealizedGains.Value)

            let unrealizedGainsPercentage =
                if cumulativeInvested.Value > 0m then
                    (totalUnrealizedGains.Value / cumulativeInvested.Value) * 100m
                else
                    0m

            System.Diagnostics.Debug.WriteLine(
                sprintf
                    "[BrokerFinancialCumulativeFinancial] Unrealized breakdown - Stock:%M (%%:%M) Options:%M Total:%M"
                    stockUnrealizedGains.Value
                    stockUnrealizedGainsPercentage
                    calculatedMetrics.OptionUnrealizedGains.Value
                    totalUnrealizedGains.Value
            )

            // Calculate realized percentage return
            let realizedPercentage =
                if cumulativeInvested.Value > 0m then
                    (cumulativeRealizedGains.Value / cumulativeInvested.Value) * 100m
                else
                    0m

            // Create the financial snapshot with all calculated values
            let newSnapshot =
                { Base = SnapshotManagerUtils.createBaseSnapshot targetDate
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

            System.Diagnostics.Debug.WriteLine(
                $"[BrokerFinancialCumulativeFinancial] Created snapshot to save - Deposited: {newSnapshot.Deposited.Value}, MovementCounter: {newSnapshot.MovementCounter}"
            )

            // Save the snapshot to database
            do! newSnapshot.save ()

            System.Diagnostics.Debug.WriteLine(
                $"[BrokerFinancialCumulativeFinancial] Snapshot saved successfully with ID: {newSnapshot.Base.Id}"
            )
        }
