namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open BrokerFinancialSnapshotExtensions

module internal BrokerFinancialUpdateExisting =
    /// <summary>
    /// Updates an existing financial snapshot with new movements while considering previous snapshot values.
    /// This is used for SCENARIO C: when new movements are added to a date that already has a snapshot,
    /// requiring a recalculation that combines previous baseline + all movements (existing + new).
    /// This ensures data consistency during corrections, reprocessing, or late movement additions.
    /// </summary>
    /// <param name="targetDate">The date for the snapshot to update</param>
    /// <param name="currencyId">The currency ID for this snapshot</param>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="brokerAccountSnapshotId">The broker account snapshot ID to associate with</param>
    /// <param name="currencyMovements">The currency-specific movements (includes both new and existing movements)</param>
    /// <param name="previousSnapshot">The previous financial snapshot for baseline calculations</param>
    /// <param name="existingSnapshot">The existing snapshot that needs to be updated</param>
    /// <returns>Task that completes when the snapshot is updated and saved</returns>
    let internal update
        (targetDate: DateTimePattern)
        (currencyId: int)
        (brokerAccountId: int)
        (brokerAccountSnapshotId: int)
        (currencyMovements: CurrencyMovementData)
        (previousSnapshot: BrokerFinancialSnapshot)
        (existingSnapshot: BrokerFinancialSnapshot)
        =
        task {
            
            BrokerFinancialValidator.validateFinancialSnapshotsConsistency 
                currencyId 
                brokerAccountId 
                targetDate 
                previousSnapshot 
                existingSnapshot
            
            // Calculate financial metrics from ALL movements for this date
            // The currencyMovements parameter should contain both existing and new movements
            // This ensures we don't miss any previously processed movements during the update
            let calculatedMetrics = BrokerFinancialsMetricsFromMovements.calculate currencyMovements currencyId
            
            // Update the existing snapshot using the recalculated metrics
            do! BrokerFinancialSnapshotUpdateExistingWithMetrics.update
                    existingSnapshot 
                    targetDate 
                    currencyId 
                    calculatedMetrics 
                    previousSnapshot
        }
