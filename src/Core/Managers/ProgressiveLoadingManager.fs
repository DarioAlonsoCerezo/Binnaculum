namespace Binnaculum.Core.Managers

open System.Threading.Tasks
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open BrokerFinancialSnapshotExtensions
open BrokerAccountExtensions
open BrokerMovementExtensions

/// <summary>
/// Progressive loading manager for phased app startup.
/// Loads critical data first for fast UI response, then loads secondary data in the background.
/// </summary>
module internal ProgressiveLoadingManager =
    
    /// <summary>
    /// Critical data loaded in Phase 1 (target: &lt;50ms).
    /// Contains minimal data needed to show the app UI.
    /// </summary>
    type CriticalData = {
        LatestSnapshot: BrokerFinancialSnapshot option
        AccountInfo: BrokerAccount option
    }
    
    /// <summary>
    /// Secondary data loaded in Phase 2 (target: &lt;150ms).
    /// Contains additional data that enhances the UI but is not required for initial display.
    /// </summary>
    type SecondaryData = {
        FirstPageMovements: BrokerMovement list
        TotalMovementCount: int
    }
    
    /// <summary>
    /// Load Phase 1: Critical data for fast startup.
    /// Loads only the latest snapshot and account info.
    /// Target: &lt;50ms on typical mobile devices.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <returns>Critical data needed for initial UI display</returns>
    let loadCriticalData (brokerAccountId: int) : Task<CriticalData> =
        task {
            let! snapshot = 
                BrokerFinancialSnapshotExtensions.Do.getLatestByBrokerAccountId(brokerAccountId)
            
            let! account = 
                BrokerAccountExtensions.Do.getById(brokerAccountId)
            
            return {
                LatestSnapshot = snapshot
                AccountInfo = account
            }
        }
    
    /// <summary>
    /// Load Phase 2: Secondary data for enhanced UI.
    /// Loads the first page of movements for quick access.
    /// Target: &lt;150ms on typical mobile devices.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="pageSize">Number of movements to load (default: 50)</param>
    /// <returns>Secondary data for UI enhancement</returns>
    let loadSecondaryData (brokerAccountId: int) (pageSize: int) : Task<SecondaryData> =
        task {
            let! movements = 
                BrokerMovementExtensions.Do.loadMovementsPaged(brokerAccountId, 0, pageSize)
            
            let! totalCount = 
                BrokerMovementExtensions.Do.getMovementCount(brokerAccountId)
            
            return {
                FirstPageMovements = movements
                TotalMovementCount = totalCount
            }
        }
    
    /// <summary>
    /// Load both phases sequentially.
    /// This is a convenience method for testing or non-progressive scenarios.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="pageSize">Number of movements to load in secondary phase</param>
    /// <returns>Tuple of critical and secondary data</returns>
    let loadAllPhases (brokerAccountId: int) (pageSize: int) : Task<CriticalData * SecondaryData> =
        task {
            let! critical = loadCriticalData brokerAccountId
            let! secondary = loadSecondaryData brokerAccountId pageSize
            return (critical, secondary)
        }
