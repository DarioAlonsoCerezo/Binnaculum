namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open BrokerFinancialSnapshotExtensions

module internal BrokerFinancialSnapshotUpdateExistingWithMetrics =


    /// <summary>
    /// Updates an existing financial snapshot with recalculated values.
    /// This function handles the specific logic for updating existing snapshots while maintaining
    /// database identity and audit information.
    /// </summary>
    /// <param name="existingSnapshot">The existing snapshot to update</param>
    /// <param name="targetDate">The date for the snapshot</param>
    /// <param name="currencyId">The currency ID for this snapshot</param>
    /// <param name="calculatedMetrics">The financial metrics calculated from movement data</param>
    /// <param name="previousSnapshot">The previous snapshot for baseline calculations</param>
    /// <returns>Task that completes when the snapshot is updated and saved</returns>
    let internal update
        (existingSnapshot: BrokerFinancialSnapshot)
        (targetDate: DateTimePattern)
        (currencyId: int)
        (calculatedMetrics: CalculatedFinancialMetrics)
        (previousSnapshot: BrokerFinancialSnapshot)
        =
        task {
            // Calculate cumulative values using previous snapshot as baseline
            let newDeposited = Money.FromAmount (previousSnapshot.Deposited.Value + calculatedMetrics.Deposited.Value)
            let newWithdrawn = Money.FromAmount (previousSnapshot.Withdrawn.Value + calculatedMetrics.Withdrawn.Value)
            let newInvested = Money.FromAmount (previousSnapshot.Invested.Value + calculatedMetrics.Invested.Value)
            let newRealizedGains = Money.FromAmount (previousSnapshot.RealizedGains.Value + calculatedMetrics.RealizedGains.Value)
            let newDividendsReceived = Money.FromAmount (previousSnapshot.DividendsReceived.Value + calculatedMetrics.DividendsReceived.Value)
            let newOptionsIncome = Money.FromAmount (previousSnapshot.OptionsIncome.Value + calculatedMetrics.OptionsIncome.Value)
            let newOtherIncome = Money.FromAmount (previousSnapshot.OtherIncome.Value + calculatedMetrics.OtherIncome.Value)
            let newCommissions = Money.FromAmount (previousSnapshot.Commissions.Value + calculatedMetrics.Commissions.Value)
            let newFees = Money.FromAmount (previousSnapshot.Fees.Value + calculatedMetrics.Fees.Value)
            let newMovementCounter = previousSnapshot.MovementCounter + calculatedMetrics.MovementCounter
            
            // Calculate unrealized gains from current positions (stocks) and add option unrealized gains
            let! (stockUnrealizedGains, stockUnrealizedGainsPercentage) = 
                BrokerFinancialUnrealizedGains.calculateUnrealizedGains calculatedMetrics.CurrentPositions calculatedMetrics.CostBasisInfo targetDate currencyId
            
            // Combine stock and option unrealized gains
            let totalUnrealizedGains = Money.FromAmount (stockUnrealizedGains.Value + calculatedMetrics.OptionUnrealizedGains.Value)
            let unrealizedGainsPercentage = 
                if newInvested.Value > 0m then
                    (totalUnrealizedGains.Value / newInvested.Value) * 100m
                else 
                    0m
            
            // Calculate realized percentage return
            let realizedPercentage = 
                if newInvested.Value > 0m then
                    (newRealizedGains.Value / newInvested.Value) * 100m
                else 
                    0m
            
            // Update the existing snapshot with recalculated values
            // Keep the original ID and audit information to maintain data integrity
            let updatedSnapshot = {
                existingSnapshot with
                    MovementCounter = newMovementCounter
                    RealizedGains = newRealizedGains
                    RealizedPercentage = realizedPercentage
                    UnrealizedGains = totalUnrealizedGains
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

