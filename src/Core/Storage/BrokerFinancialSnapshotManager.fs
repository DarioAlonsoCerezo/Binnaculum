namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open SnapshotManagerUtils
open BrokerFinancialSnapshotExtensions

module internal BrokerFinancialSnapshotManager =    

    /// <summary>
    /// Resets all financial fields of an existing snapshot to zero/default values.
    /// Used for SCENARIO H: No movements, no previous snapshot, has existing snapshot.
    /// </summary>
    let private zeroOutFinancialSnapshot (existing: BrokerFinancialSnapshot) =
        task {
            let zeroedSnapshot = {
                existing with
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
                    MovementCounter = 0
            }
            do! zeroedSnapshot.save()
        }

    /// <summary>
    /// Implements SCENARIO E: Carries forward the previous financial snapshot to a new date when no movements exist and no existing snapshot is present.
    /// Creates a new snapshot with the same values as the previous snapshot, updating the date and BrokerAccountSnapshotId.
    /// </summary>
    let private carryForwardPreviousSnapshot
        (targetDate: DateTimePattern)
        (brokerAccountSnapshotId: int)
        (previous: BrokerFinancialSnapshot)
        =
        task {
            let carriedSnapshot = {
                previous with
                    Base = createBaseSnapshot targetDate
                    BrokerAccountSnapshotId = brokerAccountSnapshotId
            }
            do! carriedSnapshot.save()
        }

    /// <summary>
    /// Implements SCENARIO G: Validates and corrects an existing financial snapshot to match a previous snapshot if discrepancies are found.
    /// Used for consistency checks when no movements exist but both previous and existing snapshots are present for a currency and date.
    /// </summary>
    let private validateAndCorrectSnapshotConsistency
        (previous: BrokerFinancialSnapshot)
        (existing: BrokerFinancialSnapshot)
        =
        task {
            let snapshotsDiffer =
                previous.RealizedGains <> existing.RealizedGains ||
                previous.RealizedPercentage <> existing.RealizedPercentage ||
                previous.UnrealizedGains <> existing.UnrealizedGains ||
                previous.UnrealizedGainsPercentage <> existing.UnrealizedGainsPercentage ||
                previous.Invested <> existing.Invested ||
                previous.Commissions <> existing.Commissions ||
                previous.Fees <> existing.Fees ||
                previous.Deposited <> existing.Deposited ||
                previous.Withdrawn <> existing.Withdrawn ||
                previous.DividendsReceived <> existing.DividendsReceived ||
                previous.OptionsIncome <> existing.OptionsIncome ||
                previous.OtherIncome <> existing.OtherIncome ||
                previous.OpenTrades <> existing.OpenTrades ||
                previous.MovementCounter <> existing.MovementCounter
            if snapshotsDiffer then
                let correctedSnapshot = {
                    existing with
                        RealizedGains = previous.RealizedGains
                        RealizedPercentage = previous.RealizedPercentage
                        UnrealizedGains = previous.UnrealizedGains
                        UnrealizedGainsPercentage = previous.UnrealizedGainsPercentage
                        Invested = previous.Invested
                        Commissions = previous.Commissions
                        Fees = previous.Fees
                        Deposited = previous.Deposited
                        Withdrawn = previous.Withdrawn
                        DividendsReceived = previous.DividendsReceived
                        OptionsIncome = previous.OptionsIncome
                        OtherIncome = previous.OtherIncome
                        OpenTrades = previous.OpenTrades
                        MovementCounter = previous.MovementCounter
                }
                do! correctedSnapshot.save()
            // If no difference, do nothing
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
    let private calculateDirectSnapshotUpdate
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
            let calculatedMetrics = BrokerFinancialsMetricsFromMovements.calculate currencyMovements currencyId
            
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
    let private calculateInitialFinancialSnapshot
        (targetDate: DateTimePattern)
        (currencyId: int)
        (brokerAccountId: int)
        (brokerAccountSnapshotId: int)
        (currencyMovements: CurrencyMovementData)
        =
        task {
            // Calculate financial metrics from movements
            let calculatedMetrics = BrokerFinancialsMetricsFromMovements.calculate currencyMovements currencyId
            
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
    let private calculateNewFinancialSnapshot
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
            let calculatedMetrics = BrokerFinancialsMetricsFromMovements.calculate currencyMovements currencyId
            
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
    /// Validates and updates broker account snapshots for a given day, processing all necessary currency movements.
    /// This handles both new movements and adjustments to existing snapshots for accuracy.
    /// </summary>
    /// <param name="brokerAccountSnapshot">The broker account snapshot for the target date</param>
    /// <param name="movementData">The movement data for updating snapshots</param>
    /// <param name="previousSnapshot">The optional previous snapshot for baseline calculations</param>
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
            
            // Note: Financial snapshots are created within the scenario processing loop
            // unlike BrokerAccountSnapshotManager where account snapshots need pre-creation
            // Each scenario handles its own financial snapshot creation as needed
            // Existing snapshots are detected per-currency using List.tryFind during processing
            
            // ✅ Process each currency that needs attention (framework implemented)
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
                
                // 4.4. ✅ SCENARIO DECISION TREE (framework implemented, calculations implemented)
                match currencyMovementData, previousSnapshot, existingSnapshot with
                
                // SCENARIO A: New movements, has previous snapshot, no existing snapshot
                | Some movements, Some previous, None ->
                    do! calculateNewFinancialSnapshot targetDate currencyId brokerAccountId brokerAccountSnapshot.Base.Id movements previous
                
                // SCENARIO B: New movements, no previous snapshot, no existing snapshot  
                | Some movements, None, None ->
                    do! calculateInitialFinancialSnapshot targetDate currencyId brokerAccountId brokerAccountSnapshot.Base.Id movements
                
                // SCENARIO C: New movements, has previous snapshot, has existing snapshot
                | Some movements, Some previous, Some existing ->
                    do! BrokerFinancialUpdateExisting.update targetDate currencyId brokerAccountId brokerAccountSnapshot.Base.Id movements previous existing

                // SCENARIO D: New movements, no previous snapshot, has existing snapshot
                | Some movements, None, Some existing ->
                    do! calculateDirectSnapshotUpdate targetDate currencyId brokerAccountId brokerAccountSnapshot.Base.Id movements existing
                
                // SCENARIO E: No movements, has previous snapshot, no existing snapshot
                | None, Some previous, None ->
                    do! carryForwardPreviousSnapshot targetDate brokerAccountSnapshot.Base.Id previous
                
                // SCENARIO F: No movements, no previous snapshot, no existing snapshot
                | None, None, None ->
                    // ✅ No action needed - this currency has no history and no activity
                    // Skip processing for this currency
                    ()                
                // SCENARIO G: No movements, has previous snapshot, has existing snapshot
                | None, Some previous, Some existing ->
                    do! validateAndCorrectSnapshotConsistency previous existing
                // SCENARIO H: No movements, no previous snapshot, has existing snapshot  
                | None, None, Some existing ->
                    // Existing snapshot should be validated or cleaned up
                    do! zeroOutFinancialSnapshot existing
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
    
    let brokerAccountCascadeUpdate
        (currentBrokerAccountSnapshot: BrokerAccountSnapshot)
        (snapshotsToUpdate: BrokerAccountSnapshot list)
        (movementData: BrokerAccountMovementData)
        =
        task {
            // Validate input parameters using the reusable validation method
            BrokerFinancialValidator.validateSnapshotAndMovementData currentBrokerAccountSnapshot movementData
            
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