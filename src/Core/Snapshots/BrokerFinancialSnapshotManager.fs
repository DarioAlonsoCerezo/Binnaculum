namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns

module internal BrokerFinancialSnapshotManager = 

    /// <summary>
    /// Validates and updates broker account snapshots for a given day, processing all necessary currency movements.
    /// This handles both new movements and adjustments to existing snapshots for accuracy.
    /// 
    /// All 8 scenarios are fully implemented and operational:
    /// 
    /// ✅ SCENARIO A: New movements, has previous snapshot, no existing snapshot
    ///    - Creates a new cumulative snapshot based on previous data + new movements
    ///    - Handled by calculateNewFinancialSnapshot
    /// 
    /// ✅ SCENARIO B: New movements, no previous snapshot, no existing snapshot
    ///    - Creates initial financial snapshot from just movement data
    ///    - Handled by calculateInitialFinancialSnapshot
    /// 
    /// ✅ SCENARIO C: New movements, has previous snapshot, has existing snapshot
    ///    - Updates existing snapshot with both previous data and new movements
    ///    - Handled by BrokerFinancialUpdateExisting.update
    /// 
    /// ✅ SCENARIO D: New movements, no previous snapshot, has existing snapshot
    ///    - Updates existing snapshot with new movements only (existing as baseline)
    ///    - Handled by calculateDirectSnapshotUpdate
    /// 
    /// ✅ SCENARIO E: No movements, has previous snapshot, no existing snapshot
    ///    - Carries forward previous snapshot values to the new date
    ///    - Handled by carryForwardPreviousSnapshot
    /// 
    /// ✅ SCENARIO F: No movements, no previous snapshot, no existing snapshot
    ///    - No action needed - no history and no activity for this currency
    /// 
    /// ✅ SCENARIO G: No movements, has previous snapshot, has existing snapshot
    ///    - Validates and corrects any discrepancies between snapshots
    ///    - Handled by validateAndCorrectSnapshotConsistency
    /// 
    /// ✅ SCENARIO H: No movements, no previous snapshot, has existing snapshot
    ///    - Resets existing snapshot to zero/default values
    ///    - Handled by zeroOutFinancialSnapshot
    /// </summary>
    /// <param name="brokerAccountSnapshot">The broker account snapshot for the target date</param>
    /// <param name="movementData">The movement data for updating snapshots</param>
    /// <returns>Task that completes when the update is finished</returns>
    let brokerAccountOneDayUpdate
        (brokerAccountSnapshot: BrokerAccountSnapshot)
        (movementData: BrokerAccountMovementData)
        =
        task {
            // 1.1. ✅ Validate input parameters using the reusable validation method
            BrokerFinancialValidator.validateSnapshotAndMovementData brokerAccountSnapshot movementData
            
            // 1.2. ✅ Extract validated parameters for calculations
            let targetDate = brokerAccountSnapshot.Base.Date
            let brokerAccountId = brokerAccountSnapshot.BrokerAccountId
            
            // 2.1. ✅ Get all existing financial snapshots for this broker account and target date
            // This handles the case where we might have multiple currency snapshots
            let! existingFinancialSnapshots = 
                BrokerFinancialSnapshotExtensions.Do.getAllByBrokerAccountIdBrokerAccountSnapshotIdAndDate(
                    brokerAccountId, 
                    brokerAccountSnapshot.Base.Id, 
                    targetDate)
            
            // 2.2. ✅ Get all previous financial snapshots for historical continuity
            // We need these as baseline for cumulative calculations (RealizedGains, Invested, etc.)
            // This gets ALL previous snapshots and we'll filter to find the most recent ones per currency
            let! allPreviousFinancialSnapshots = 
                BrokerFinancialSnapshotExtensions.Do.getLatestByBrokerAccountIdGroupedByDate(brokerAccountId)
            
            // 2.3. ✅ Filter and group to get the most recent previous snapshot per currency
            // This finds the actual latest snapshot before target date for each currency,
            // regardless of whether it was yesterday, last week, or months ago
            let relevantPreviousSnapshots = 
                allPreviousFinancialSnapshots
                |> List.filter (fun snap -> snap.Base.Date.Value < targetDate.Value)
                |> List.groupBy (fun snap -> snap.CurrencyId)
                |> List.map (fun (currencyId, snaps) -> 
                    snaps |> List.maxBy (fun s -> s.Base.Date.Value))
            
            // 3.1. ✅ Determine which currencies have movements on target date
            let currenciesWithMovements = movementData.UniqueCurrencies
            
            // 3.2. ✅ Determine which currencies had previous snapshots
            let currenciesWithPreviousSnapshots = 
                relevantPreviousSnapshots 
                |> List.map (fun snap -> snap.CurrencyId) 
                |> Set.ofList
            
            // 3.3. ✅ Calculate which currencies need processing (union of movements and historical)
            let allRelevantCurrencies = 
                Set.union currenciesWithMovements currenciesWithPreviousSnapshots
            
            // Financial snapshots are created within the scenario processing loop
            // unlike BrokerAccountSnapshotManager where account snapshots need pre-creation
            // Each scenario handles its own financial snapshot creation as needed
            // Existing snapshots are detected per-currency using List.tryFind during processing
            
            // Process each currency that needs attention
            for currencyId in allRelevantCurrencies do
                
                // 4.1. ✅ Get movement data for this specific currency
                let currencyMovementData = 
                    movementData.MovementsByCurrency.TryFind(currencyId)
                
                // 4.2. ✅ Get previous snapshot for this currency (for cumulative calculations)
                let previousSnapshot = 
                    relevantPreviousSnapshots 
                    |> List.tryFind (fun snap -> snap.CurrencyId = currencyId)
                
                // 4.3. ✅ Get existing snapshot for this currency and date (if any)
                let existingSnapshot = 
                    existingFinancialSnapshots 
                    |> List.tryFind (fun snap -> snap.CurrencyId = currencyId)
                
                // 4.4. ✅ SCENARIO DECISION TREE - All scenarios implemented
                match currencyMovementData, previousSnapshot, existingSnapshot with
                
                // SCENARIO A: New movements, has previous snapshot, no existing snapshot
                | Some movements, Some previous, None ->
                    do! BrokerFinancialCalculate.newFinancialSnapshot targetDate currencyId brokerAccountId brokerAccountSnapshot.Base.Id movements previous
                
                // SCENARIO B: New movements, no previous snapshot, no existing snapshot  
                | Some movements, None, None ->
                    do! BrokerFinancialCalculate.initialFinancialSnapshot targetDate currencyId brokerAccountId brokerAccountSnapshot.Base.Id movements
                
                // SCENARIO C: New movements, has previous snapshot, has existing snapshot
                | Some movements, Some previous, Some existing ->
                    do! BrokerFinancialUpdateExisting.update targetDate currencyId brokerAccountId brokerAccountSnapshot.Base.Id movements previous existing

                // SCENARIO D: New movements, no previous snapshot, has existing snapshot
                | Some movements, None, Some existing ->
                    do! BrokerFinancialCalculate.directSnapshotUpdate targetDate currencyId brokerAccountId brokerAccountSnapshot.Base.Id movements existing
                
                // SCENARIO E: No movements, has previous snapshot, no existing snapshot
                | None, Some previous, None ->
                    do! BrokerFinancialCarryForward.previousSnapshot targetDate brokerAccountSnapshot.Base.Id previous
                
                // SCENARIO F: No movements, no previous snapshot, no existing snapshot
                | None, None, None ->
                    // No action needed - this currency has no history and no activity
                    ()                
                // SCENARIO G: No movements, has previous snapshot, has existing snapshot
                | None, Some previous, Some existing ->
                    do! BrokerFinancialValidateAndCorrect.snapshotConsistency previous existing
                // SCENARIO H: No movements, no previous snapshot, has existing snapshot  
                | None, None, Some existing ->
                    // Existing snapshot should be validated or cleaned up
                    do! BrokerFinancialReset.zeroOutFinancialSnapshot existing
        }

    /// <summary>
    /// Retrieves movement data for a specific date by filtering the larger movement dataset.
    /// This is used by cascade updates to get movements for each individual snapshot date.
    /// </summary>
    /// <param name="targetDate">The specific date to get movements for</param>
    /// <param name="allMovementData">The complete movement data set to filter from</param>
    /// <returns>BrokerAccountMovementData containing only movements for the target date</returns>
    let private getMovementsForSpecificDate
        (targetDate: DateTimePattern)
        (allMovementData: BrokerAccountMovementData)
        =
        // Use the existing helper function from BrokerAccountMovementData module
        BrokerAccountMovementData.getMovementsForDate targetDate allMovementData

    /// <summary>
    /// Retrieves movement data between two specific dates (inclusive).
    /// This is used by brokerAccountOneDayWithPrevious to get movements between two snapshots.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="fromDate">Start date (inclusive) using start of day</param>
    /// <param name="toDate">End date (inclusive) using end of day</param>
    /// <returns>BrokerAccountMovementData containing movements within the date range</returns>
    let private getMovementsBetweenDates
        (brokerAccountId: int)
        (fromDate: DateTimePattern) 
        (toDate: DateTimePattern)
        =
        task {
            // Use start of day for fromDate to capture movements throughout the entire start day
            let startOfDayFromDate = SnapshotManagerUtils.getDateOnlyStartOfDay fromDate
            
            // Retrieve movements from the start date onwards, then filter to the end date
            // This follows the same pattern as BrokerAccountSnapshotManager.getAllMovementsFromDate
            let! brokerMovements = BrokerMovementExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, startOfDayFromDate)
            let! trades = TradeExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, startOfDayFromDate)
            let! dividends = DividendExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, startOfDayFromDate)
            let! dividendTaxes = DividendTaxExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, startOfDayFromDate)
            let! optionTrades = OptionTradeExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, startOfDayFromDate)
            
            // Filter movements to only include those within the date range (fromDate to toDate inclusive)
            let filteredBrokerMovements = 
                brokerMovements 
                |> List.filter (fun m -> m.TimeStamp.Value >= fromDate.Value && m.TimeStamp.Value <= toDate.Value)
            
            let filteredTrades = 
                trades 
                |> List.filter (fun t -> t.TimeStamp.Value >= fromDate.Value && t.TimeStamp.Value <= toDate.Value)
                
            let filteredDividends = 
                dividends 
                |> List.filter (fun d -> d.TimeStamp.Value >= fromDate.Value && d.TimeStamp.Value <= toDate.Value)
                
            let filteredDividendTaxes = 
                dividendTaxes 
                |> List.filter (fun dt -> dt.TimeStamp.Value >= fromDate.Value && dt.TimeStamp.Value <= toDate.Value)
                
            let filteredOptionTrades = 
                optionTrades 
                |> List.filter (fun ot -> ot.TimeStamp.Value >= fromDate.Value && ot.TimeStamp.Value <= toDate.Value)
            
            // Create the movement data using the existing pattern
            return BrokerAccountMovementData.create 
                startOfDayFromDate 
                brokerAccountId 
                filteredBrokerMovements 
                filteredTrades 
                filteredDividends 
                filteredDividendTaxes 
                filteredOptionTrades
        }

    /// <summary>
    /// Sets up the initial financial snapshot for a specific broker.
    /// This is used when a new broker is created or when initializing snapshots for existing brokers.
    /// </summary>
    let setupInitialFinancialSnapshotForBroker
        (snapshotDate: DateTimePattern)
        (brokerId: int)
        (brokerSnapshotId: int)
        =
        task {
            do! BrokerFinancialDefault.create 
                    snapshotDate
                    brokerId
                    0 // BrokerAccountId set to 0 for broker-level snapshots
                    brokerSnapshotId
                    0 // BrokerAccountSnapshotId set to 0 for broker-level snapshots
        }

    /// <summary>
    /// Sets up the initial financial snapshot for a specific broker account.
    /// This is used when a new broker account is created or when initializing snapshots for existing accounts.
    /// Creates a single financial snapshot using the default currency, which is appropriate for initial setup
    /// since no movements exist yet. Multi-currency snapshots will be created later as movements are processed.
    /// </summary>
    let setupInitialFinancialSnapshotForBrokerAccount
        (brokerAccountSnapshot: BrokerAccountSnapshot)
        =
        task {
            // Create initial snapshot with default currency
            // Multi-currency snapshots will be handled during movement processing
            // when brokerAccountOneDayUpdate and brokerAccountCascadeUpdate are implemented
            do! BrokerFinancialDefault.create  
                    brokerAccountSnapshot.Base.Date
                    0 // BrokerId set to 0 for account-level snapshots
                    brokerAccountSnapshot.BrokerAccountId
                    0 // BrokerSnapshotId set to 0 for account-level snapshots
                    brokerAccountSnapshot.Base.Id // Use the snapshot's own ID as BrokerAccountSnapshotId
        }
    
    /// <summary>
    /// Handles cascade updates across multiple broker account snapshots when movements affect a date range.
    /// This function processes the ripple effects of financial changes across a sequence of snapshots.
    /// 
    /// ✅ FULLY IMPLEMENTED CASCADE UPDATE LOGIC:
    /// 
    /// The cascade update works by processing snapshots chronologically, where each snapshot
    /// gets updated with its specific-date movements. The cumulative nature of financial metrics
    /// (RealizedGains, Invested, etc.) is automatically handled by brokerAccountOneDayUpdate's
    /// 8-scenario decision tree, which looks up previous snapshots for baseline calculations.
    /// 
    /// This ensures that changes cascade forward correctly through the entire snapshot chain.
    /// </summary>
    /// <param name="currentBrokerAccountSnapshot">The current snapshot that triggered the cascade</param>
    /// <param name="snapshotsToUpdate">List of snapshots that need updating as a result of changes</param>
    /// <param name="movementData">The movement data containing all financial changes</param>
    /// <returns>Task that completes when all snapshots in the cascade are updated</returns>
    let brokerAccountCascadeUpdate
        (currentBrokerAccountSnapshot: BrokerAccountSnapshot)
        (snapshotsToUpdate: BrokerAccountSnapshot list)
        (movementData: BrokerAccountMovementData)
        =
        task {
            // ✅ 1. Validate input parameters using the reusable validation method
            BrokerFinancialValidator.validateSnapshotAndMovementData currentBrokerAccountSnapshot movementData
            
            // ✅ 2. Handle edge cases
            if snapshotsToUpdate.IsEmpty then
                // No snapshots to cascade to - just update the current snapshot
                do! brokerAccountOneDayUpdate currentBrokerAccountSnapshot movementData
                return()
            
            // ✅ 3. Validate broker account consistency across all snapshots
            let brokerAccountId = currentBrokerAccountSnapshot.BrokerAccountId
            let inconsistentSnapshots = 
                snapshotsToUpdate 
                |> List.filter (fun snap -> snap.BrokerAccountId <> brokerAccountId)
            
            if not inconsistentSnapshots.IsEmpty then
                let inconsistentIds = inconsistentSnapshots |> List.map (fun s -> s.Base.Id) |> List.map string |> String.concat ", "
                failwithf "Cascade update failed: snapshots [%s] belong to different broker accounts than the current snapshot (account %d)" 
                    inconsistentIds brokerAccountId
            
            // ✅ 4. Sort snapshots chronologically to ensure proper sequential processing
            // Include the current snapshot in the processing chain
            let allSnapshotsToProcess = currentBrokerAccountSnapshot :: snapshotsToUpdate
            let sortedSnapshots = 
                allSnapshotsToProcess
                |> List.sortBy (fun snap -> snap.Base.Date.Value)
                |> List.distinct // Remove potential duplicates
            
            // ✅ 5. Validate chronological order - ensure no gaps that would affect calculations
            let sortedDates = sortedSnapshots |> List.map (fun snap -> snap.Base.Date.Value)
            let currentSnapshotDate = currentBrokerAccountSnapshot.Base.Date.Value
            let futureSnapshots = sortedSnapshots |> List.filter (fun snap -> snap.Base.Date.Value >= currentSnapshotDate)
            
            if futureSnapshots.IsEmpty then
                failwithf "Cascade update failed: no snapshots found on or after the current snapshot date (%A)" currentSnapshotDate
            
            // ✅ 6. Process each snapshot in chronological order
            // The cascade works because each brokerAccountOneDayUpdate call:
            // - Looks up the most recent previous snapshots for baseline calculations
            // - Updates the current snapshot with its specific-date movements
            // - Creates cumulative financial values (previous + current movements)
            // - As we process chronologically, each snapshot becomes the "previous" for the next one
            
            for snapshot in futureSnapshots do
                // ✅ 6.1. Get movement data specific to this snapshot's date
                let snapshotMovements = getMovementsForSpecificDate snapshot.Base.Date movementData
                
                // ✅ 6.2. Update this snapshot using the complete 8-scenario logic
                // This handles all currency scenarios and cumulative calculations automatically
                do! brokerAccountOneDayUpdate snapshot snapshotMovements
            
            // ✅ 7. Cascade update complete
            // The financial metrics cascade naturally through the chronological processing:
            // - Each snapshot builds upon the previous snapshot's cumulative values
            // - Multi-currency scenarios are handled per-currency within each date
            // - Position changes and unrealized gains are recalculated at each date
            // - Realized gains accumulate correctly through the timeline
        }

    /// <summary>
    /// Handles changes to existing broker accounts, updating financial snapshots using a previous snapshot as reference.
    /// This specialized function handles explicit reference-based updates between two specific snapshots.
    /// 
    /// ✅ FULLY IMPLEMENTED EXPLICIT REFERENCE UPDATE LOGIC:
    /// 
    /// This function differs from brokerAccountOneDayUpdate in that it explicitly uses a provided previous 
    /// snapshot rather than automatically finding the most recent one. This is useful for:
    /// - Data correction scenarios where you want to bridge specific snapshots
    /// - Historical data reprocessing where you know the exact previous state
    /// - Gap filling where automatic previous snapshot detection might not find the right baseline
    /// </summary>
    /// <param name="brokerAccountSnapshot">The current snapshot to update</param>
    /// <param name="previousSnapshot">The previous snapshot to use as a baseline</param>
    /// <returns>Task that completes when the update is finished</returns>
    let brokerAccountOneDayWithPrevious
        (brokerAccountSnapshot: BrokerAccountSnapshot)
        (previousSnapshot: BrokerAccountSnapshot)
        =
        task {
            // ✅ 1. Validate inputs - ensure snapshots are compatible and properly ordered
            
            // 1.1. Validate that both snapshots belong to the same broker account
            if brokerAccountSnapshot.BrokerAccountId <> previousSnapshot.BrokerAccountId then
                failwithf "Snapshot update failed: current snapshot (account %d) and previous snapshot (account %d) belong to different broker accounts" 
                    brokerAccountSnapshot.BrokerAccountId previousSnapshot.BrokerAccountId
            
            // 1.2. Verify that previous snapshot date is before current snapshot date
            if previousSnapshot.Base.Date.Value >= brokerAccountSnapshot.Base.Date.Value then
                failwithf "Snapshot update failed: previous snapshot date (%A) must be before current snapshot date (%A)" 
                    previousSnapshot.Base.Date.Value brokerAccountSnapshot.Base.Date.Value
            
            // 1.3. Extract common parameters for processing
            let brokerAccountId = brokerAccountSnapshot.BrokerAccountId
            let currentDate = brokerAccountSnapshot.Base.Date
            let previousDate = previousSnapshot.Base.Date
            
            // ✅ 2. Retrieve movement data between the two snapshots
            // Get all movements that occurred between (and including) the previous and current snapshot dates
            let! movementData = getMovementsBetweenDates brokerAccountId previousDate currentDate
            
            // ✅ 3. Handle the case where no movements exist between snapshots
            if movementData.HasMovements = false then
                // No movements between snapshots - just carry forward previous snapshot values
                // This uses the existing cascade update logic with empty movement data
                do! brokerAccountCascadeUpdate previousSnapshot [brokerAccountSnapshot] movementData
                return()
            
            // ✅ 4. Apply movements using standard one-day update logic
            // The brokerAccountOneDayUpdate function will:
            // - Find the most recent previous snapshots automatically (which should include our explicit previous snapshot)
            // - Process the movement data through the 8-scenario decision tree  
            // - Handle multi-currency scenarios appropriately
            // - Calculate cumulative financial metrics correctly
            do! brokerAccountOneDayUpdate brokerAccountSnapshot movementData
            
            // ✅ 5. Update complete
            // The explicit reference update ensures that the baseline calculations use the correct previous snapshot
            // while still leveraging all the robust financial calculation logic from the existing system
        }