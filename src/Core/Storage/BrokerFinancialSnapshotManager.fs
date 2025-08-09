namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open SnapshotManagerUtils
open BrokerFinancialSnapshotExtensions

module internal BrokerFinancialSnapshotManager =
    
    let private defaultFinancialSnapshot
        (snapshotDate: DateTimePattern)
        (brokerId: int)
        (brokerAccountId: int)
        (brokerSnapshotId: int)
        (brokerAccountSnapshotId: int)
        =
        task {
            let! currencyId = getDefaultCurrency()
            let snapshot = {
                Base = createBaseSnapshot snapshotDate
                BrokerId = brokerId
                BrokerAccountId = brokerAccountId
                CurrencyId = currencyId
                MovementCounter = 0
                BrokerSnapshotId = brokerSnapshotId
                BrokerAccountSnapshotId = brokerAccountSnapshotId
                RealizedGains = Money.FromAmount 0m
                RealizedPercentage = 0m
                UnrealizedGains = Money.FromAmount 0m
                UnrealizedGainsPercentage = 0m
                Invested = Money.FromAmount 0m
                Commissions = Money.FromAmount 0m
                Fees = Money.FromAmount 0m
                Deposited = Money.FromAmount 0m
                Withdrawn = Money.FromAmount 0m
                DividendsReceived = Money.FromAmount 0m
                OptionsIncome = Money.FromAmount 0m
                OtherIncome = Money.FromAmount 0m
                OpenTrades = false
            }
            do! snapshot.save()
        }

    let brokerAccountCascadeUpdate
        (currentBrokerAccountSnapshot: BrokerAccountSnapshot)
        (snapshotsToUpdate: BrokerAccountSnapshot list)
        (movementData: BrokerAccountMovementData)
        =
        task {
            //TODO: Use the movement data for cascade calculations
            // Now has access to movementData.BrokerMovements, movementData.Trades, etc.
            // for cascade calculations across multiple snapshots without additional database calls
            return()
        }

    /// <summary>
    /// Handles changes to existing broker accounts, updating snapshots using a previous date.
    /// This function is used for one-day updates where the previous snapshot is used to calculate changes.
    /// </summary>
    let brokerAccountOneDayWithPrevious
        (brokerAccountSnapshot: BrokerAccountSnapshot)
        (previousSnapshot: BrokerAccountSnapshot)
        =
        task {
            //TODO
            return()
        }

    /// <summary>
    /// Handles changes to existing broker accounts, updating snapshots as necessary.
    /// This function performs a one-day update using the provided movement data.
    /// </summary>
    let brokerAccountOneDayUpdate
        (brokerAccountSnapshot: BrokerAccountSnapshot)
        (movementData: BrokerAccountMovementData)
        =
        task {
            // =================================================================
            // PHASE 1: INPUT VALIDATION & SETUP
            // =================================================================
            
            // 1.1. Validate input parameters
            // - Ensure brokerAccountSnapshot has valid ID and Date
            // - Ensure movementData is not null/empty structure
            // - Validate that movementData.FromDate <= brokerAccountSnapshot.Base.Date
            
            // 1.2. Extract target date from snapshot for calculations
            let targetDate = brokerAccountSnapshot.Base.Date
            let brokerAccountId = brokerAccountSnapshot.BrokerAccountId
            
            // =================================================================
            // PHASE 2: RETRIEVE PREVIOUS FINANCIAL SNAPSHOTS
            // =================================================================
            
            // 2.1. Get all existing financial snapshots for this broker account and target date
            // This handles the case where we might have multiple currency snapshots
            let! existingFinancialSnapshots = 
                BrokerFinancialSnapshotExtensions.Do.getAllByBrokerAccountIdBrokerAccountSnapshotIdAndDate(
                    brokerAccountId, 
                    brokerAccountSnapshot.Base.Id, 
                    targetDate)
            
            // 2.2. Get previous day's financial snapshots for historical continuity
            // We need these as baseline for cumulative calculations (RealizedGains, Invested, etc.)
            let previousDate = DateTimePattern.FromDateTime(targetDate.Value.AddDays(-1.0))
            let! previousFinancialSnapshots = 
                BrokerFinancialSnapshotExtensions.Do.getLatestByBrokerAccountIdGroupedByDate(brokerAccountId)
            
            // 2.3. Filter previous snapshots to get the most recent ones before target date
            let relevantPreviousSnapshots = 
                previousFinancialSnapshots
                |> List.filter (fun snap -> snap.Base.Date.Value < targetDate.Value)
                |> List.groupBy (fun snap -> snap.CurrencyId)
                |> List.map (fun (currencyId, snaps) -> 
                    snaps |> List.maxBy (fun s -> s.Base.Date.Value))
            
            // =================================================================
            // PHASE 3: CURRENCY ANALYSIS & SNAPSHOT PLANNING
            // =================================================================
            
            // 3.1. Determine which currencies have movements on target date
            let currenciesWithMovements = movementData.UniqueCurrencies
            
            // 3.2. Determine which currencies had previous snapshots
            let currenciesWithPreviousSnapshots = 
                relevantPreviousSnapshots 
                |> List.map (fun snap -> snap.CurrencyId) 
                |> Set.ofList
            
            // 3.3. Determine which currencies already have snapshots for target date
            let currenciesWithExistingSnapshots = 
                existingFinancialSnapshots 
                |> List.map (fun snap -> snap.CurrencyId) 
                |> Set.ofList
            
            // 3.4. Calculate which currencies need new snapshots created
            let allRelevantCurrencies = 
                Set.union currenciesWithMovements currenciesWithPreviousSnapshots
            
            let currenciesNeedingNewSnapshots = 
                Set.difference allRelevantCurrencies currenciesWithExistingSnapshots
            
            // =================================================================
            // PHASE 4: SCENARIO HANDLING & SNAPSHOT PROCESSING
            // =================================================================
            
            // Process each currency that needs attention
            for currencyId in allRelevantCurrencies do
                
                // 4.1. Get movement data for this specific currency
                let currencyMovementData = 
                    movementData.MovementsByCurrency.TryFind(currencyId)
                
                // 4.2. Get previous snapshot for this currency (for cumulative calculations)
                let previousSnapshot = 
                    relevantPreviousSnapshots 
                    |> List.tryFind (fun snap -> snap.CurrencyId = currencyId)
                
                // 4.3. Get existing snapshot for this currency and date (if any)
                let existingSnapshot = 
                    existingFinancialSnapshots 
                    |> List.tryFind (fun snap -> snap.CurrencyId = currencyId)
                
                // 4.4. SCENARIO DECISION TREE
                match currencyMovementData, previousSnapshot, existingSnapshot with
                
                // SCENARIO A: New movements, has previous snapshot, no existing snapshot
                | Some movements, Some previous, None ->
                    // Calculate new financial snapshot based on movements + previous values
                    // This is the most common scenario for active trading currencies
                    // TODO: Implement calculation logic using movements and previous snapshot
                    ()
                
                // SCENARIO B: New movements, no previous snapshot, no existing snapshot  
                | Some movements, None, None ->
                    // Create initial financial snapshot for this currency
                    // This happens when first movement in a new currency occurs
                    // TODO: Implement initial calculation logic using only movements
                    ()
                
                // SCENARIO C: New movements, has previous snapshot, has existing snapshot
                | Some movements, Some previous, Some existing ->
                    // Update existing snapshot by recalculating with new movements
                    // This can happen during data corrections or reprocessing
                    // TODO: Implement update logic combining movements, previous, and existing
                    ()
                
                // SCENARIO D: New movements, no previous snapshot, has existing snapshot
                | Some movements, None, Some existing ->
                    // Update existing snapshot with new movements (rare edge case)
                    // TODO: Implement update logic using movements and existing snapshot
                    ()
                
                // SCENARIO E: No movements, has previous snapshot, no existing snapshot
                | None, Some previous, None ->
                    // Carry forward previous snapshot values (no activity day)
                    // Create snapshot with same values as previous day but new date
                    // TODO: Implement carry-forward logic from previous snapshot
                    ()
                
                // SCENARIO F: No movements, no previous snapshot, no existing snapshot
                | None, None, None ->
                    // No action needed - this currency has no history and no activity
                    // Skip processing for this currency
                    ()
                
                // SCENARIO G: No movements, has previous snapshot, has existing snapshot
                | None, Some previous, Some existing ->
                    // Verify existing snapshot matches previous (data consistency check)
                    // TODO: Implement consistency validation and correction if needed
                    ()
                
                // SCENARIO H: No movements, no previous snapshot, has existing snapshot  
                | None, None, Some existing ->
                    // Existing snapshot should be validated or cleaned up
                    // This might indicate data inconsistency
                    // TODO: Implement validation and cleanup logic
                    ()
            
            // =================================================================
            // PHASE 5: FINANCIAL CALCULATION HELPERS (TODO: IMPLEMENT)
            // =================================================================
            
            // 5.1. Calculate cumulative values (RealizedGains, Invested, etc.)
            // These should accumulate from previous snapshot + current movements
            
            // 5.2. Calculate period-specific values (Commissions, Fees for this date)
            // These should sum only movements from current date
            
            // 5.3. Calculate portfolio metrics (UnrealizedGains, OpenTrades status)
            // These require current positions and market prices
            
            // 5.4. Update MovementCounter (increment from previous or set based on activity)
            
            // =================================================================
            // PHASE 6: EDGE CASE HANDLING
            // =================================================================
            
            // 6.1. Handle currency conversion movements (multi-currency impact)
            // A single conversion movement affects two currencies simultaneously
            
            // 6.2. Handle ACAT transfers (securities and money transfers)
            // These may not have direct cash impact but affect positions
            
            // 6.3. Handle option exercises and assignments
            // These create complex position and cash flow changes
            
            // 6.4. Handle dividend tax withholdings
            // These are negative cash flows that need proper categorization
            
            // 6.5. Handle corporate actions (splits, mergers, spinoffs)
            // These affect position quantities and cost basis
            
            // =================================================================
            // PHASE 7: DATA VALIDATION & PERSISTENCE
            // =================================================================
            
            // 7.1. Validate all calculated snapshot values for consistency
            // - Ensure monetary values are reasonable
            // - Validate percentages are within expected ranges
            // - Check that cumulative values increase monotonically (where applicable)
            
            // 7.2. Save all new/updated financial snapshots to database
            // Use batch operations if multiple currencies were processed
            
            // 7.3. Log processing summary for monitoring and debugging
            // Include counts of processed currencies and any edge cases encountered
            
            return()
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
            do! defaultFinancialSnapshot 
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
            do! defaultFinancialSnapshot 
                    brokerAccountSnapshot.Base.Date
                    0 // BrokerId set to 0 for account-level snapshots
                    brokerAccountSnapshot.BrokerAccountId
                    0 // BrokerSnapshotId set to 0 for account-level snapshots
                    brokerAccountSnapshot.Base.Id // Use the snapshot's own ID as BrokerAccountSnapshotId
        }