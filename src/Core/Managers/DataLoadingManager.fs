namespace Binnaculum.Core.Managers

open System.Threading.Tasks
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open BrokerMovementExtensions
open BrokerFinancialSnapshotExtensions
open TickerCurrencySnapshotExtensions
open BrokerAccountExtensions

/// <summary>
/// Context-aware data loading manager for different app screens.
/// Provides optimized data loading strategies based on the specific context.
/// </summary>
module internal DataLoadingManager =
    
    /// <summary>
    /// Data structure for overview/dashboard screen.
    /// Contains only snapshot data without loading individual movements.
    /// </summary>
    type OverviewData = {
        BrokerSnapshot: BrokerFinancialSnapshot option
        AccountInfo: BrokerAccount option
    }
    
    /// <summary>
    /// Data structure for movement list screen with pagination support.
    /// </summary>
    type MovementListData = {
        Movements: BrokerMovement list
        TotalCount: int
        CurrentPage: int
        PageSize: int
        HasMore: bool
    }
    
    /// <summary>
    /// Load data for overview screen without loading movements.
    /// Optimized for fast startup by only loading pre-calculated snapshots.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <returns>Overview data containing snapshots and account info</returns>
    let loadOverviewData (brokerAccountId: int) : Task<OverviewData> =
        task {
            let! latestSnapshot = 
                BrokerFinancialSnapshotExtensions.Do.getLatestByBrokerAccountId(brokerAccountId)
            
            let! accountInfo = 
                BrokerAccountExtensions.Do.getById(brokerAccountId)
            
            return {
                BrokerSnapshot = latestSnapshot
                AccountInfo = accountInfo
            }
        }
    
    /// <summary>
    /// Load data for movement list screen with pagination.
    /// Returns only the requested page of movements.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="pageNumber">Zero-based page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Movement list data with pagination information</returns>
    let loadMovementListData 
        (brokerAccountId: int) 
        (pageNumber: int) 
        (pageSize: int) 
        : Task<MovementListData> =
        task {
            let! movements = 
                BrokerMovementExtensions.Do.loadMovementsPaged(brokerAccountId, pageNumber, pageSize)
            
            let! totalCount = 
                BrokerMovementExtensions.Do.getMovementCount(brokerAccountId)
            
            return {
                Movements = movements
                TotalCount = totalCount
                CurrentPage = pageNumber
                PageSize = pageSize
                HasMore = (pageNumber + 1) * pageSize < totalCount
            }
        }
    
    /// <summary>
    /// Load movements for a specific date (calendar view).
    /// Returns only movements that occurred on the specified date.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="date">The target date</param>
    /// <returns>List of movements for the specified date</returns>
    let loadMovementsForDate (brokerAccountId: int) (date: DateTimePattern) : Task<BrokerMovement list> =
        task {
            return! BrokerMovementExtensions.Do.getByBrokerAccountIdForDate(brokerAccountId, date)
        }
    
    /// <summary>
    /// Load movements within a date range (for calendar or reporting views).
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <returns>List of movements within the date range</returns>
    let loadMovementsInDateRange 
        (brokerAccountId: int) 
        (startDate: DateTimePattern) 
        (endDate: DateTimePattern) 
        : Task<BrokerMovement list> =
        task {
            return! BrokerMovementExtensions.Do.loadMovementsInDateRange(brokerAccountId, startDate, endDate)
        }
