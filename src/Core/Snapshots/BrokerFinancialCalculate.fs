namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns
open BrokerFinancialSnapshotExtensions

module internal BrokerFinancialCalculate =

    /// <summary>
    /// Builds an updated financial snapshot for SCENARIO D using freshly calculated metrics.
    /// This helper is intentionally factored out so we can validate the replacement logic without hitting the database.
    /// </summary>
    /// <param name="existingSnapshot">The existing snapshot record to update</param>
    /// <param name="calculatedMetrics">Freshly calculated metrics for the target date</param>
    /// <param name="stockUnrealizedGains">Unrealized gains from current stock positions</param>
    /// <returns>Snapshot record with fields replaced by the recalculated metrics</returns>
    let internal applyDirectSnapshotMetrics
        (existingSnapshot: BrokerFinancialSnapshot)
        (calculatedMetrics: CalculatedFinancialMetrics)
        (stockUnrealizedGains: Money)
        =
        let totalUnrealizedGains =
            Money.FromAmount(stockUnrealizedGains.Value + calculatedMetrics.OptionUnrealizedGains.Value)

        let unrealizedPercentage =
            if calculatedMetrics.Invested.Value > 0m then
                (totalUnrealizedGains.Value / calculatedMetrics.Invested.Value) * 100m
            else
                0m

        let realizedPercentage =
            if calculatedMetrics.Invested.Value > 0m then
                (calculatedMetrics.RealizedGains.Value / calculatedMetrics.Invested.Value)
                * 100m
            else
                0m

        { existingSnapshot with
            MovementCounter = calculatedMetrics.MovementCounter
            RealizedGains = calculatedMetrics.RealizedGains
            RealizedPercentage = realizedPercentage
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
    /// Applies direct snapshot metrics while optionally preserving realized gains when no closing activity occurred.
    /// This prevents historical realized totals from being reset to zero during Scenario D recalculations that only adjust deposits/fees.
    /// </summary>
    let internal applyDirectSnapshotMetricsWithPreservation
        (currencyMovements: CurrencyMovementData)
        (existingSnapshot: BrokerFinancialSnapshot)
        (calculatedMetrics: CalculatedFinancialMetrics)
        (stockUnrealizedGains: Money)
        =
        let updatedSnapshot =
            applyDirectSnapshotMetrics existingSnapshot calculatedMetrics stockUnrealizedGains

        let hasRealizedTradeActivity =
            currencyMovements.Trades
            |> List.exists (fun trade ->
                trade.TradeCode = TradeCode.SellToClose
                || trade.TradeCode = TradeCode.BuyToClose)

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

        if shouldPreserveRealized then
            System.Diagnostics.Debug.WriteLine(
                "[BrokerFinancialCalculate] Preserving existing realized gains during direct snapshot update because recalculated value is zero and no realized-closing activity was detected."
            )

            { updatedSnapshot with
                RealizedGains = existingSnapshot.RealizedGains
                RealizedPercentage = existingSnapshot.RealizedPercentage }
        else
            updatedSnapshot

    /// <summary>
    /// Calculates a new financial snapshot based on currency movements and previous snapshot values.
    /// This is used for SCENARIO A: the most common case where we have new movements for a currency
    /// with existing historical data, creating a new snapshot for the target date.
    /// </summary>
    /// <param name="targetDate">The date for the new snapshot</param>
    /// <param name="currencyId">The currency ID for this snapshot</param>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="brokerAccountSnapshotId">The broker account snapshot ID to associate with</param>
    /// <param name="currencyMovements">The currency-specific movements for calculations</param>
    /// <param name="previousSnapshot">The previous financial snapshot for cumulative calculations</param>
    /// <returns>Task that completes when the new snapshot is calculated and saved</returns>
    let internal newFinancialSnapshot
        (targetDate: DateTimePattern)
        (currencyId: int)
        (brokerAccountId: int)
        (brokerAccountSnapshotId: int)
        (currencyMovements: CurrencyMovementData)
        (previousSnapshot: BrokerFinancialSnapshot)
        =
        task {
            BrokerFinancialValidator.validatePreviousSnapshotCurrencyConsistency previousSnapshot currencyId

            // Calculate financial metrics from movements
            let calculatedMetrics =
                BrokerFinancialsMetricsFromMovements.calculate currencyMovements currencyId targetDate

            System.Diagnostics.Debug.WriteLine(
                $"[BrokerFinancialCalculate] Scenario A metrics - Currency:{currencyId} Date:{targetDate.Value} Realized:{calculatedMetrics.RealizedGains.Value} OptionsIncome:{calculatedMetrics.OptionsIncome.Value} Invested:{calculatedMetrics.Invested.Value} Movements:{calculatedMetrics.MovementCounter}"
            )

            // Create new snapshot with previous snapshot as baseline
            do!
                BrokerFinancialCumulativeFinancial.create
                    targetDate
                    currencyId
                    brokerAccountId
                    brokerAccountSnapshotId
                    calculatedMetrics
                    (Some previousSnapshot)
        }

    /// <summary>
    /// Creates an initial financial snapshot from movement data without requiring previous snapshots.
    /// This is used for SCENARIO B: when first movements occur in a new currency with no history.
    /// All financial metrics are calculated solely from the provided movement data.
    /// </summary>
    /// <param name="targetDate">The date for the new snapshot</param>
    /// <param name="currencyId">The currency ID for this snapshot</param>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="brokerAccountSnapshotId">The broker account snapshot ID to associate with</param>
    /// <param name="currencyMovements">The currency-specific movements for calculations</param>
    /// <returns>Task that completes when the initial snapshot is calculated and saved</returns>
    let internal initialFinancialSnapshot
        (targetDate: DateTimePattern)
        (currencyId: int)
        (brokerAccountId: int)
        (brokerAccountSnapshotId: int)
        (currencyMovements: CurrencyMovementData)
        =
        task {
            // Calculate financial metrics from movements
            let calculatedMetrics =
                BrokerFinancialsMetricsFromMovements.calculate currencyMovements currencyId targetDate

            System.Diagnostics.Debug.WriteLine(
                $"[BrokerFinancialCalculate] Scenario B metrics - Currency:{currencyId} Date:{targetDate.Value} Realized:{calculatedMetrics.RealizedGains.Value} OptionsIncome:{calculatedMetrics.OptionsIncome.Value} Invested:{calculatedMetrics.Invested.Value} Movements:{calculatedMetrics.MovementCounter}"
            )

            // Create initial snapshot without previous baseline (pass None for previousSnapshot)
            do!
                BrokerFinancialCumulativeFinancial.create
                    targetDate
                    currencyId
                    brokerAccountId
                    brokerAccountSnapshotId
                    calculatedMetrics
                    None
        }

    /// <summary>
    /// Updates an existing financial snapshot directly with new movements without a previous snapshot baseline.
    /// This is used for SCENARIO D: when new movements exist for a currency with an existing snapshot,
    /// but no previous snapshot is found. The existing snapshot itself serves as the baseline.
    /// This edge case may occur during data reprocessing, corrections, or when historical data is incomplete.
    /// </summary>
    /// <param name="targetDate">The date for the snapshot to update</param>
    /// <param name="currencyId">The currency ID for this snapshot</param>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="brokerAccountSnapshotId">The broker account snapshot ID to associate with</param>
    /// <param name="currencyMovements">The currency-specific movements for calculations</param>
    /// <param name="existingSnapshot">The existing snapshot to update directly</param>
    /// <returns>Task that completes when the snapshot is updated and saved</returns>
    let internal directSnapshotUpdate
        (targetDate: DateTimePattern)
        (currencyId: int)
        (brokerAccountId: int)
        (brokerAccountSnapshotId: int)
        (currencyMovements: CurrencyMovementData)
        (existingSnapshot: BrokerFinancialSnapshot)
        =
        task {
            BrokerFinancialValidator.validateExistingSnapshotConsistency
                existingSnapshot
                currencyId
                brokerAccountId
                targetDate

            // Calculate financial metrics from new movements
            let calculatedMetrics =
                BrokerFinancialsMetricsFromMovements.calculate currencyMovements currencyId targetDate

            System.Diagnostics.Debug.WriteLine(
                $"[BrokerFinancialCalculate] Scenario D metrics - Currency:{currencyId} Date:{targetDate.Value} Realized:{calculatedMetrics.RealizedGains.Value} OptionsIncome:{calculatedMetrics.OptionsIncome.Value} Invested:{calculatedMetrics.Invested.Value} Movements:{calculatedMetrics.MovementCounter}"
            )

            // Since there's no previous snapshot, the existing snapshot represents the same date we're recalculating.
            // Reprocessing should replace the stored values with the newly calculated metrics rather than accumulate again.
            // Calculate unrealized gains from current positions (including both existing and new positions)
            let! (unrealizedGains, _) =
                BrokerFinancialUnrealizedGains.calculateUnrealizedGains
                    calculatedMetrics.CurrentPositions
                    calculatedMetrics.CostBasisInfo
                    targetDate
                    currencyId

            let updatedSnapshot =
                applyDirectSnapshotMetricsWithPreservation
                    currencyMovements
                    existingSnapshot
                    calculatedMetrics
                    unrealizedGains

            do! updatedSnapshot.save ()
        }
