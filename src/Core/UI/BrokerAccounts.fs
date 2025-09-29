namespace Binnaculum.Core.UI

open System.Threading.Tasks
open Binnaculum.Core.Database
open BrokerAccountSnapshotExtensions
open BrokerFinancialSnapshotExtensions
open Binnaculum.Core.Models
open Binnaculum.Core.DatabaseToModels
open Binnaculum.Core.Keys

/// <summary>
/// This module provides the public API for all BrokerAccount-related operations accessible from the UI layer.
/// It follows the established patterns of database access, model conversion, and follows the project's 
/// error handling conventions (let exceptions bubble up to the UI layer).
/// </summary>
module BrokerAccounts =
    
    /// <summary>
    /// Retrieves all snapshots for a specific BrokerAccount.
    /// Takes a brokerAccountId, queries the database for all snapshots associated with that account,
    /// and converts them to OverviewSnapshot models for UI consumption.
    /// Follows project conventions for error handling - exceptions bubble up to UI layer.
    /// </summary>
    /// <param name="brokerAccountId">The ID of the BrokerAccount to retrieve snapshots for</param>
    /// <returns>A list of OverviewSnapshot records representing all snapshots for the broker account</returns>
    let GetSnapshots(brokerAccountId: int) = task {
        // Get all broker account snapshots from database
        let! brokerAccountSnapshots = BrokerAccountSnapshotExtensions.Do.getByBrokerAccountId(brokerAccountId) |> Async.AwaitTask
        
        // Get the broker account model using fast lookup
        let brokerAccount = brokerAccountId.ToFastBrokerAccountById()
        
        // Convert each database snapshot to OverviewSnapshot
        let overviewSnapshotTasks = 
            brokerAccountSnapshots
            |> List.map (fun dbSnapshot -> task {
                // Get related financial snapshots for this broker account snapshot  
                let! financialSnapshots = BrokerFinancialSnapshotExtensions.Do.getByBrokerAccountIdAndDate(brokerAccountId, dbSnapshot.Base.Date) |> Async.AwaitTask
                
                // Convert to OverviewSnapshot using existing conversion function
                return dbSnapshot.brokerAccountSnapshotToOverviewSnapshot(financialSnapshots, brokerAccount)
            })
        
        let! overviewSnapshots = Task.WhenAll(overviewSnapshotTasks |> List.toArray)
        return overviewSnapshots |> Array.toList
    }