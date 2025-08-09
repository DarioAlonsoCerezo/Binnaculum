namespace Binnaculum.Core.Storage

open System
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open BrokerAccountSnapshotExtensions
open Binnaculum.Core.Storage.SnapshotManagerUtils

/// <summary>
/// Handles creation, updating, and recalculation of BrokerAccountSnapshots.
/// Enhanced with multi-currency support for per-currency detail rows.
/// 
/// This module exposes only two public entry points:
/// - handleBrokerAccountChange: For handling changes to existing broker accounts
/// - handleNewBrokerAccount: For initializing snapshots for new broker accounts
/// 
/// All other functionality is internal to maintain proper encapsulation and prevent misuse.
/// </summary>
module internal BrokerAccountSnapshotManager =

    let private getOrCreateSnapshot(brokerAccountId: int, snapshotDate: DateTimePattern) = task {
        
        // Check if a snapshot already exists for this broker account on the given date
        let! existingSnapshot = BrokerAccountSnapshotExtensions.Do.getByBrokerAccountIdAndDate(brokerAccountId, snapshotDate)
        match existingSnapshot with
        | Some snapshot -> 
            return snapshot // If it exists, return it
        | None ->
            let newSnapshot = {
                Base = createBaseSnapshot snapshotDate
                BrokerAccountId = brokerAccountId
            }
            do! newSnapshot.save()

            let! createdSnapshot = BrokerAccountSnapshotExtensions.Do.getByBrokerAccountIdAndDate(brokerAccountId, snapshotDate)
            match createdSnapshot with
            | Some snapshot -> return snapshot
            | None -> 
                failwithf "Failed to create default snapshot for broker account %d on date %A" brokerAccountId snapshotDate
                return { Base = createBaseSnapshot snapshotDate; BrokerAccountId = brokerAccountId }
    }  
    
    let private getAllMovementsFromDate(brokerAccountId, snapshotDate) =
        task {
            let! brokerMovements = BrokerMovementExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, snapshotDate)
            
            let! trades = TradeExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, snapshotDate)
            
            let! dividends = DividendExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, snapshotDate)
            
            let! dividendTaxes = DividendTaxExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, snapshotDate)
            
            let! optionTrades = OptionTradeExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, snapshotDate)
            
            return BrokerAccountMovementData.create 
                snapshotDate 
                brokerAccountId 
                brokerMovements 
                trades 
                dividends 
                dividendTaxes 
                optionTrades
        }

    let private getAllSnapshotsAfterDate(brokerAccountId, snapshotDate) =
        task {
            return! BrokerAccountSnapshotExtensions
                        .Do
                        .getBrokerAccountSnapshotsAfterDate(brokerAccountId, snapshotDate)
        }

    let private extractDatesFromSnapshots(snapshots: BrokerAccountSnapshot list) =
        snapshots
        |> List.map (fun s -> s.Base.Date)
        |> Set.ofList

    /// <summary>
    /// Handles snapshot updates when a new BrokerAccount is created
    /// Creates snapshots for the current day
    /// </summary>
    let handleNewBrokerAccount (brokerAccount: BrokerAccount) =
        task {
            let snapshotDate = getDateOnlyFromDateTime DateTime.Now
            let! snapshot = getOrCreateSnapshot(brokerAccount.Id, snapshotDate)
            do! BrokerFinancialSnapshotManager.setupInitialFinancialSnapshotForBrokerAccount snapshot  
        }

    /// <summary>
    /// Public API for handling broker account changes with multi-currency support.
    /// Automatically determines whether to use one-day or cascade update based on the date:
    /// This is the recommended entry point for triggering snapshot updates after account changes.
    /// </summary>
    /// <param name="accountId">The broker account ID that changed</param>
    /// <param name="date">The date of the change</param>
    /// <returns>Task that completes when the appropriate update strategy finishes</returns>
    let handleBrokerAccountChange (brokerAccountId: int, date: DateTimePattern) =
        task {
            let snapshotDate = getDateOnly date
            let! snapshot = getOrCreateSnapshot(brokerAccountId, snapshotDate)
            
            // 1. Get all movements FROM this date onwards (inclusive) - using START OF DAY to capture entire day
            let movementRetrievalDate = getDateOnlyStartOfDay date
            let! allMovementsFromDate = getAllMovementsFromDate(brokerAccountId, movementRetrievalDate)
            let! futureSnapshots = getAllSnapshotsAfterDate(brokerAccountId, snapshotDate)
            
            // 2. Extract affected dates from movement data (reuse the same data)
            let datesWithMovements = allMovementsFromDate.UniqueDates
            let datesWithSnapshots = extractDatesFromSnapshots(futureSnapshots)
            let missingSnapshotDates = Set.difference datesWithMovements datesWithSnapshots
            
            // 3. Decision logic using the pre-fetched data
            match allMovementsFromDate.HasMovements, futureSnapshots.IsEmpty, missingSnapshotDates.IsEmpty with
            | false, true, _ -> 
                // No future activity - simple one-day update
                do! BrokerFinancialSnapshotManager.brokerAccountOneDayUpdate snapshot
            | true, _, false ->
                // Future movements exist with missing snapshots - create missing snapshots then cascade
                //let! missedSnapshots = createAndGetMissingSnapshots(brokerAccountId, missingSnapshotDates)
                //do! BrokerFinancialSnapshotManager.brokerAccountCascadeUpdate snapshot (futureSnapshots @ missedSnapshots) allMovementsFromDate
                printfn "TODO: Implement cascade update with missing snapshots for account %d" brokerAccountId
            | true, false, true ->
                // Future movements exist, all snapshots present - standard cascade
                do! BrokerFinancialSnapshotManager.brokerAccountCascadeUpdate snapshot futureSnapshots
            | _ ->
                // Edge cases - default to cascade for safety
                do! BrokerFinancialSnapshotManager.brokerAccountCascadeUpdate snapshot futureSnapshots
            return()
        }
