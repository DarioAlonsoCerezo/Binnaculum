namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open BrokerFinancialSnapshotExtensions

module internal BrokerFinancialCalculate =

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
            BrokerFinancialValidator.validatePreviousSnapshotCurrencyConsistency
                previousSnapshot
                currencyId
            
            // Calculate financial metrics from movements
            let calculatedMetrics = BrokerFinancialsMetricsFromMovements.calculate currencyMovements currencyId targetDate
            
            // Create new snapshot with previous snapshot as baseline
            do! BrokerFinancialCumulativeFinancial.create
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
            let calculatedMetrics = BrokerFinancialsMetricsFromMovements.calculate currencyMovements currencyId targetDate
            
            // Create initial snapshot without previous baseline (pass None for previousSnapshot)
            do! BrokerFinancialCumulativeFinancial.create 
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
            let calculatedMetrics = BrokerFinancialsMetricsFromMovements.calculate currencyMovements currencyId targetDate
            
            // Since there's no previous snapshot, we add the new movement metrics directly to the existing snapshot
            // The existing snapshot serves as the baseline (which represents the state before these new movements)
            let newDeposited = Money.FromAmount (existingSnapshot.Deposited.Value + calculatedMetrics.Deposited.Value)
            let newWithdrawn = Money.FromAmount (existingSnapshot.Withdrawn.Value + calculatedMetrics.Withdrawn.Value)
            let newInvested = Money.FromAmount (existingSnapshot.Invested.Value + calculatedMetrics.Invested.Value)
            let newRealizedGains = Money.FromAmount (existingSnapshot.RealizedGains.Value + calculatedMetrics.RealizedGains.Value)
            let newDividendsReceived = Money.FromAmount (existingSnapshot.DividendsReceived.Value + calculatedMetrics.DividendsReceived.Value)
            let newOptionsIncome = Money.FromAmount (existingSnapshot.OptionsIncome.Value + calculatedMetrics.OptionsIncome.Value)
            let newOtherIncome = Money.FromAmount (existingSnapshot.OtherIncome.Value + calculatedMetrics.OtherIncome.Value)
            let newCommissions = Money.FromAmount (existingSnapshot.Commissions.Value + calculatedMetrics.Commissions.Value)
            let newFees = Money.FromAmount (existingSnapshot.Fees.Value + calculatedMetrics.Fees.Value)
            let newMovementCounter = existingSnapshot.MovementCounter + calculatedMetrics.MovementCounter
            
            // Calculate unrealized gains from current positions (including both existing and new positions)
            let! (unrealizedGains, unrealizedGainsPercentage) = 
                BrokerFinancialUnrealizedGains.calculateUnrealizedGains calculatedMetrics.CurrentPositions calculatedMetrics.CostBasisInfo targetDate currencyId
            
            // Calculate realized percentage return
            let realizedPercentage = 
                if newInvested.Value > 0m then
                    (newRealizedGains.Value / newInvested.Value) * 100m
                else 
                    0m
            
            // Update the existing snapshot with the combined values
            // Keep the original ID and audit information to maintain data integrity
            let updatedSnapshot = {
                existingSnapshot with
                    MovementCounter = newMovementCounter
                    RealizedGains = newRealizedGains
                    RealizedPercentage = realizedPercentage
                    UnrealizedGains = unrealizedGains
                    UnrealizedGainsPercentage = unrealizedGainsPercentage
                    Invested = newInvested
                    Commissions = newCommissions
                    Fees = newFees
                    Deposited = newDeposited
                    Withdrawn = newWithdrawn
                    DividendsReceived = newDividendsReceived
                    OptionsIncome = newOptionsIncome
                    OtherIncome = newOtherIncome
                    OpenTrades = calculatedMetrics.HasOpenPositions
            }
            
            // Save the updated snapshot to database
            do! updatedSnapshot.save()
        }

