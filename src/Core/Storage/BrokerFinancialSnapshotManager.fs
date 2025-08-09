namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open SnapshotManagerUtils
open BrokerFinancialSnapshotExtensions
open BrokerMovementExtensions

module internal BrokerFinancialSnapshotManager =
    
    /// <summary>
    /// Validates the input parameters for broker account snapshot operations.
    /// Throws detailed exceptions if any validation fails, does nothing if all validations pass.
    /// </summary>
    /// <param name="brokerAccountSnapshot">The broker account snapshot to validate</param>
    /// <param name="movementData">The movement data to validate</param>
    let private validateSnapshotAndMovementData
        (brokerAccountSnapshot: BrokerAccountSnapshot)
        (movementData: BrokerAccountMovementData)
        =
        // Validate brokerAccountSnapshot parameters
        if brokerAccountSnapshot.Base.Id <= 0 then
            failwithf "Invalid broker account snapshot ID: %d. ID must be greater than 0." brokerAccountSnapshot.Base.Id
        
        if brokerAccountSnapshot.BrokerAccountId <= 0 then
            failwithf "Invalid broker account ID: %d. Broker account ID must be greater than 0." brokerAccountSnapshot.BrokerAccountId
        
        // Validate movementData parameters
        if movementData.BrokerAccountId <= 0 then
            failwithf "Invalid movement data broker account ID: %d. Must be greater than 0." movementData.BrokerAccountId
        
        if movementData.BrokerAccountId <> brokerAccountSnapshot.BrokerAccountId then
            failwithf "Movement data broker account ID (%d) does not match snapshot broker account ID (%d)." 
                movementData.BrokerAccountId brokerAccountSnapshot.BrokerAccountId
        
        if movementData.FromDate.Value > brokerAccountSnapshot.Base.Date.Value then
            failwithf "Movement data FromDate (%A) cannot be later than snapshot date (%A)." 
                movementData.FromDate.Value brokerAccountSnapshot.Base.Date.Value

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
            // Extract previous snapshot values as baseline for cumulative calculations
            // We need these totals to build upon for the new snapshot date
            let previousRealizedGains = previousSnapshot.RealizedGains
            let previousInvested = previousSnapshot.Invested
            let previousDeposited = previousSnapshot.Deposited
            let previousWithdrawn = previousSnapshot.Withdrawn
            let previousDividendsReceived = previousSnapshot.DividendsReceived
            let previousOptionsIncome = previousSnapshot.OptionsIncome
            let previousOtherIncome = previousSnapshot.OtherIncome
            let previousCommissions = previousSnapshot.Commissions
            let previousFees = previousSnapshot.Fees
            let previousMovementCounter = previousSnapshot.MovementCounter
            
            // Validate previous snapshot currency matches current currency
            if previousSnapshot.CurrencyId <> currencyId then
                failwithf "Previous snapshot currency (%d) does not match current currency (%d)" 
                    previousSnapshot.CurrencyId currencyId
            
            // Process broker movements using the new extension methods
            // These calculations provide clean, reusable financial aggregations
            let brokerMovementSummary = 
                currencyMovements.BrokerMovements
                |> FinancialCalculations.calculateFinancialSummary
            
            // Extract calculated values from broker movements
            let currentDeposited = brokerMovementSummary.TotalDeposited
            let currentWithdrawn = brokerMovementSummary.TotalWithdrawn
            let currentFees = brokerMovementSummary.TotalFees
            let currentCommissions = brokerMovementSummary.TotalCommissions
            let currentOtherIncome = brokerMovementSummary.TotalOtherIncome
            let currentInterestPaid = brokerMovementSummary.TotalInterestPaid
            let brokerMovementCount = brokerMovementSummary.MovementCount
            
            // Calculate currency conversion impact for this specific currency
            // This accounts for money gained (positive) or lost (negative) through conversions
            // Example: Converting $1000 USD to €900 EUR:
            // - USD currency: conversionImpact = -$1000 (money lost)
            // - EUR currency: conversionImpact = +€900 (money gained)
            let conversionImpact = 
                currencyMovements.BrokerMovements
                |> fun movements -> FinancialCalculations.calculateConversionImpact(movements, currencyId)
            
            // Process trade movements for investment tracking and realized gains calculation
            // Trades directly impact invested amounts, commissions, and realized gains when positions are closed
            // 📋 TODO: Calculate total invested amount (price * quantity + commissions + fees)
            // 📋 TODO: Add commissions to Commissions total
            // 📋 TODO: Add fees to Fees total
            // 📋 TODO: Update Invested total (cumulative investment)
            // 📋 TODO: Calculate realized gains for closed positions (sell trades)
            // 📋 TODO: Update RealizedGains total
            // 📋 TODO: Determine if OpenTrades status should be true (open positions exist)
            
            // Process dividend income from holdings
            // Dividends provide additional income beyond trading gains
            // 📋 TODO: Add dividend amounts to DividendsReceived total
            // 📋 TODO: Add to OtherIncome or keep separate dividend tracking
            // 📋 TODO: Handle dividend reinvestment scenarios if applicable
            
            // Process dividend tax withholdings
            // Tax withholdings reduce the net dividend income received
            // 📋 TODO: Add tax amounts to appropriate tax tracking field
            // 📋 TODO: Reduce net dividend income accordingly
            // 📋 TODO: Handle withholding tax vs. additional tax scenarios
            
            // Process options trading for premium income and options-related costs
            // Options can generate income (selling) or costs (buying) and affect realized gains
            // 📋 TODO: Add premium received to OptionsIncome (for sells)
            // 📋 TODO: Add premium paid to Invested or separate options cost
            // 📋 TODO: Handle options commissions and fees
            // 📋 TODO: Calculate realized gains for closed option positions
            // 📋 TODO: Track open option positions for OpenTrades status
            
            // Calculate cumulative values by combining previous snapshot with current movements
            // Most financial metrics are cumulative and build upon previous totals
            
            // Apply conversion impact to appropriate categories based on whether money was gained or lost
            let (adjustedDeposited, adjustedWithdrawn) = 
                if conversionImpact.Value >= 0m then
                    // Positive conversion impact: money was gained in this currency (e.g., USD -> EUR conversion)
                    (currentDeposited.Value + conversionImpact.Value, currentWithdrawn.Value)
                else
                    // Negative conversion impact: money was lost from this currency (e.g., EUR -> USD conversion)
                    (currentDeposited.Value, currentWithdrawn.Value + abs(conversionImpact.Value))
            
            let newDeposited = Money.FromAmount (previousDeposited.Value + adjustedDeposited)
            let newWithdrawn = Money.FromAmount (previousWithdrawn.Value + adjustedWithdrawn)
            let newCommissions = Money.FromAmount (previousCommissions.Value + currentCommissions.Value)
            let newFees = Money.FromAmount (previousFees.Value + currentFees.Value)
            let newOtherIncome = Money.FromAmount (previousOtherIncome.Value + currentOtherIncome.Value)
            
            // Handle interest paid (typically reduces other income or increases fees)
            let adjustedOtherIncome = Money.FromAmount (newOtherIncome.Value - currentInterestPaid.Value)
            
            // 📋 TODO: New Invested = Previous Invested + Current Period Investment (from trades)
            // 📋 TODO: New RealizedGains = Previous RealizedGains + Current Period Realized Gains (from trades)
            // 📋 TODO: New DividendsReceived = Previous DividendsReceived + Current Period Dividends
            // 📋 TODO: New OptionsIncome = Previous OptionsIncome + Current Period Options Income
            
            // Update movement counter to track activity level over time
            let newMovementCounter = previousMovementCounter + brokerMovementCount
            // Additional movements from trades, dividends, options will be added when those sections are implemented
            
            // Calculate unrealized gains which requires current market prices and position tracking
            // This is more complex as it requires knowing current positions and market values
            
            // Calculate current position quantities for each ticker held
            // Position tracking is essential for unrealized gains and portfolio valuation
            // 📋 TODO: Calculate current position quantities for each ticker
            // 📋 TODO: Determine cost basis for each position (FIFO, LIFO, or Average Cost)
            // 📋 TODO: Track position entry dates for tax calculation implications
            
            // Retrieve current market prices for portfolio valuation
            // Market prices are needed to determine current portfolio value vs. cost basis
            // 📋 TODO: Retrieve current market prices for all tickers with positions
            // 📋 TODO: Handle multi-currency price conversions if needed
            // 📋 TODO: Calculate market value of each position
            
            // Compute unrealized gains based on current market values vs. cost basis
            // 📋 TODO: UnrealizedGains = Total Market Value - Total Cost Basis
            // 📋 TODO: UnrealizedGainsPercentage = (UnrealizedGains / Total Cost Basis) * 100
            // 📋 TODO: Handle zero cost basis scenarios (free shares, etc.)
            
            // Calculate percentage metrics for performance analysis
            // Performance percentages help evaluate investment success over time
            
            // Calculate realized percentage return on invested capital
            // 📋 TODO: RealizedPercentage = (RealizedGains / Total Invested) * 100
            // 📋 TODO: Handle zero investment scenarios gracefully
            // 📋 TODO: Consider time-weighted vs. money-weighted return calculations
            
            // Calculate overall portfolio performance metrics
            // 📋 TODO: Calculate overall portfolio return percentage
            // 📋 TODO: Consider dividend yield impact on total return
            // 📋 TODO: Factor in currency conversion gains/losses if applicable
            
            // Create the final BrokerFinancialSnapshot with all calculated values
            // This snapshot represents the complete financial state for this currency on the target date
            let newSnapshot = {
                Base = createBaseSnapshot targetDate
                BrokerId = 0 // Set to 0 for account-level snapshots
                BrokerAccountId = brokerAccountId
                CurrencyId = currencyId
                MovementCounter = newMovementCounter
                BrokerSnapshotId = 0 // Set to 0 for account-level snapshots
                BrokerAccountSnapshotId = brokerAccountSnapshotId
                RealizedGains = previousRealizedGains // 📋 TODO: Update with calculated realized gains from trades
                RealizedPercentage = 0m // 📋 TODO: Calculate based on RealizedGains / Invested
                UnrealizedGains = Money.FromAmount 0m // 📋 TODO: Calculate from market prices
                UnrealizedGainsPercentage = 0m // 📋 TODO: Calculate based on UnrealizedGains / cost basis
                Invested = previousInvested // 📋 TODO: Update with current period investment from trades
                Commissions = newCommissions
                Fees = newFees
                Deposited = newDeposited
                Withdrawn = newWithdrawn
                DividendsReceived = previousDividendsReceived // 📋 TODO: Update with current dividends
                OptionsIncome = previousOptionsIncome // 📋 TODO: Update with current options income
                OtherIncome = adjustedOtherIncome
                OpenTrades = false // 📋 TODO: Determine from current positions
            }
            
            // Save the snapshot to database with proper error handling
            do! newSnapshot.save()
            
            // Validate and log the snapshot creation for monitoring
            // 📋 TODO: Validate calculated values are reasonable (no negative invested amounts, etc.)
            // 📋 TODO: Log snapshot creation for monitoring and debugging
            // 📋 TODO: Track calculation performance metrics if needed
            
            return()
        }
    
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
            // Validate input parameters using the reusable validation method
            validateSnapshotAndMovementData currentBrokerAccountSnapshot movementData
            
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
            // PHASE 1: INPUT VALIDATION & SETUP ✅ IMPLEMENTED
            // =================================================================
            
            // 1.1. ✅ Validate input parameters using the reusable validation method
            validateSnapshotAndMovementData brokerAccountSnapshot movementData
            
            // 1.2. ✅ Extract validated parameters for calculations
            let targetDate = brokerAccountSnapshot.Base.Date
            let brokerAccountId = brokerAccountSnapshot.BrokerAccountId
            
            // =================================================================
            // PHASE 2: RETRIEVE PREVIOUS FINANCIAL SNAPSHOTS ✅ IMPLEMENTED
            // =================================================================
            
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
            
            // =================================================================
            // PHASE 3: CURRENCY ANALYSIS & SNAPSHOT PLANNING ✅ IMPLEMENTED
            // =================================================================
            
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
            
            // =================================================================
            // PHASE 4: SCENARIO HANDLING & SNAPSHOT PROCESSING 📋 TODO
            // =================================================================
            
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
                
                // 4.4. ✅ SCENARIO DECISION TREE (framework implemented, calculations pending)
                match currencyMovementData, previousSnapshot, existingSnapshot with
                
                // SCENARIO A: New movements, has previous snapshot, no existing snapshot
                | Some movements, Some previous, None ->
                    do! calculateNewFinancialSnapshot targetDate currencyId brokerAccountId brokerAccountSnapshot.Base.Id movements previous
                
                // SCENARIO B: New movements, no previous snapshot, no existing snapshot  
                | Some movements, None, None ->
                    // Create initial financial snapshot for this currency
                    // This happens when first movement in a new currency occurs
                    // 📋 TODO: Implement initial calculation logic using only movements
                    // Scenario B represents the initiation of tracking for a new currency where
                    // no previous snapshots or movements exist in the system.
                    //
                    // This requires creating a new financial snapshot with all initial values set
                    // based on the default currency settings and the initial set of movements.
                    //
                    // Steps to implement:
                    // - Use the defaultFinancialSnapshot function to create a snapshot with the
                    //   provided targetDate, brokerAccountId, and other identifiers.
                    // - The snapshot should be populated with the default currency, which is typically
                    //   the primary currency for the broker account.
                    // - Since this is the first snapshot, all financial metrics (e.g., Invested,
                    //   RealizedGains) should be set to zero, and only the initial movement data should
                    //   be considered for any immediate calculations.
                    // - Save the new snapshot to the database.
                    //
                    // This scenario is straightforward as it involves default initialization without
                    // complex calculations or historical data considerations.
                    //
                    // Expected Outcome:
                    // - A new financial snapshot is created with default values and the initial currency
                    //   movements applied.
                    // - The snapshot reflects the state of the broker account for the new currency as of
                    //   the target date.
                    //
                    // This allows for immediate tracking of the new currency's financial performance
                    // from the point of initial movement.
                    // defaultFinancialSnapshot targetDate 0 brokerAccountId 0 brokerAccountSnapshot.Id
                    ()
                
                // SCENARIO C: New movements, has previous snapshot, has existing snapshot
                | Some movements, Some previous, Some existing ->
                    // Update existing snapshot by recalculating with new movements
                    // This can happen during data corrections or reprocessing
                    // 📋 TODO: Implement update logic combining movements, previous, and existing
                    // Scenario C involves updating an existing financial snapshot with new movements
                    // while considering already processed data in the existing snapshot.
                    //
                    // This requires intelligently merging the new movement data with the existing
                    // snapshot data to ensure accurate and up-to-date financial reporting.
                    //
                    // Steps to implement:
                    // - Retrieve the existing snapshot for the currency and identify the new movements
                    //   that have occurred since the last snapshot update.
                    // - For each new movement, update the relevant financial metrics in the existing
                    //   snapshot (e.g., adjust RealizedGains for realized trades, update Invested
                    //   amount, etc.).
                    // - Recalculate any derived metrics (e.g., UnrealizedGains, RealizedPercentage)
                    //   based on the updated financial data.
                    // - Save the updated snapshot back to the database, ensuring that it reflects the
                    //   latest state of the broker account.
                    //
                    // This scenario requires careful handling of each movement type and its impact on
                    // the financial metrics to avoid double-counting or incorrect calculations.
                    //
                    // Expected Outcome:
                    // - The existing financial snapshot is updated with the latest movement data,
                    //   reflecting the current state of the broker account.
                    // - All financial metrics are accurately recalculated and persisted.
                    //
                    // This ensures that the financial reporting for the currency is complete and
                    // up-to-date, considering all recent trading activities.
                    // TODO: Implement logic for updating existing snapshot with new movements
                    ()
                
                // SCENARIO D: New movements, no previous snapshot, has existing snapshot
                | Some movements, None, Some existing ->
                    // Update existing snapshot with new movements (rare edge case)
                    // 📋 TODO: Implement update logic using movements and existing snapshot
                    // Scenario D is an edge case where new movements are available for a currency
                    // that already has an existing snapshot, but no previous snapshot is found.
                    //
                    // This may occur in situations where movements are reprocessed or adjusted
                    // without a corresponding update to the previous snapshot.
                    //
                    // Steps to implement:
                    // - Retrieve the existing snapshot for the currency and the new movements data.
                    // - Update the existing snapshot's metrics (e.g., Deposited, Withdrawn) based on
                    //   the new movements.
                    // - Since there's no previous snapshot to compare against, ensure that all metric
                    //   updates are based solely on the new movement data.
                    // - Save the updated snapshot to the database.
                    //
                    // This scenario is relatively simple as it mainly involves updating the existing
                    // snapshot with the new data without the need for complex calculations or merges.
                    //
                    // Expected Outcome:
                    // - The existing financial snapshot is updated with the new movement data,
                    //   accurately reflecting the latest broker account state.
                    //
                    // This ensures that the financial information is current, although the absence of
                    // a previous snapshot may limit some historical calculations.
                    // TODO: Implement logic for updating existing snapshot with new movements only
                    ()
                
                // SCENARIO E: No movements, has previous snapshot, no existing snapshot
                | None, Some previous, None ->
                    // Carry forward previous snapshot values (no activity day)
                    // Create snapshot with same values as previous day but new date
                    // 📋 TODO: Implement carry-forward logic from previous snapshot
                    // Scenario E handles passive days where no new movements occur, but a snapshot
                    // for the previous day exists.
                    //
                    // The goal is to create a new snapshot that mirrors the previous day's snapshot,
                    // effectively "freezing" the portfolio's state for the day without any changes.
                    //
                    // Steps to implement:
                    // - Retrieve the previous snapshot for the currency.
                    // - Create a new snapshot using the same values as the previous snapshot, with
                    //   the exception of the date, which should be updated to the target date.
                    // - Save the new snapshot to the database.
                    //
                    // This scenario is straightforward as it primarily involves duplicating the
                    // previous snapshot with a new date.
                    //
                    // Expected Outcome:
                    // - A new financial snapshot is created with the same values as the previous snapshot,
                    //   indicating no changes in the portfolio for the currency.
                    //
                    // This is useful for maintaining accurate daily snapshots even when no trading
                    // activity occurs.
                    // previous.Base.Date.AddDays(1) |> createBaseSnapshot |> ignore
                    ()
                
                // SCENARIO F: No movements, no previous snapshot, no existing snapshot
                | None, None, None ->
                    // ✅ No action needed - this currency has no history and no activity
                    // Skip processing for this currency
                    ()
                
                // SCENARIO G: No movements, has previous snapshot, has existing snapshot
                | None, Some previous, Some existing ->
                    // Verify existing snapshot matches previous (data consistency check)
                    // 📋 TODO: Implement consistency validation and correction if needed
                    // Scenario G is a consistency check where both a previous snapshot and an existing
                    // snapshot are found for a currency, but no new movements have occurred.
                    //
                    // The existing snapshot should theoretically match the previous snapshot, and this
                    // scenario serves to validate that consistency.
                    //
                    // Steps to implement:
                    // - Compare the existing snapshot with the previous snapshot for the currency.
                    // - If discrepancies are found, investigate and correct the existing snapshot to match
                    //   the previous snapshot.
                    // - This may involve reverting erroneous changes or resetting the snapshot to a known
                    //   good state.
                    //
                    // Expected Outcome:
                    // - The existing snapshot is validated or corrected to ensure it matches the previous
                    //   snapshot.
                    //
                    // This ensures data integrity and consistency within the financial snapshot records.
                    // TODO: Implement validation logic to compare existing and previous snapshots
                    ()
                
                // SCENARIO H: No movements, no previous snapshot, has existing snapshot  
                | None, None, Some existing ->
                    // Existing snapshot should be validated or cleaned up
                    // This might indicate data inconsistency
                    // 📋 TODO: Implement validation and cleanup logic
                    // Scenario H deals with the potential orphaning of snapshots where an existing
                    // snapshot is found without a corresponding previous snapshot or new movements.
                    //
                    // This may occur due to errors in data processing or could indicate that the
                    // snapshot belongs to a now-inactive currency.
                    //
                    // Steps to implement:
                    // - Validate the existing snapshot to ensure it contains all necessary data and
                    //   makes sense in the current context.
                    // - If the snapshot is determined to be orphaned or invalid, remove it or archive
                    //   it as necessary to prevent confusion.
                    //
                    // Expected Outcome:
                    // - The existing snapshot is either validated and kept or cleaned up if found
                    //   unnecessary or erroneous.
                    //
                    // This helps maintain a clean and accurate set of financial snapshots for the
                    // broker accounts.
                    // TODO: Implement logic to validate or remove orphaned snapshots
                    ()
            
            // =================================================================
            // PHASE 5: FINANCIAL CALCULATION HELPERS 📋 TODO
            // =================================================================
            
            // 📋 5.1. Calculate cumulative values (RealizedGains, Invested, etc.)
            // These should accumulate from previous snapshot + current movements
            
            // 📋 5.2. Calculate period-specific values (Commissions, Fees for this date)
            // These should sum only movements from current date
            
            // 📋 5.3. Calculate portfolio metrics (UnrealizedGains, OpenTrades status)
            // These require current positions and market prices
            
            // 📋 5.4. Update MovementCounter (increment from previous or set based on activity)
            
            // =================================================================
            // PHASE 6: EDGE CASE HANDLING 📋 TODO
            // =================================================================
            
            // 📋 6.1. Handle currency conversion movements (multi-currency impact)
            // A single conversion movement affects two currencies simultaneously
            
            // 📋 6.2. Handle ACAT transfers (securities and money transfers)
            // These may not have direct cash impact but affect positions
            
            // 📋 6.3. Handle option exercises and assignments
            // These create complex position and cash flow changes
            
            // 📋 6.4. Handle dividend tax withholdings
            // These are negative cash flows that need proper categorization
            
            // 📋 6.5. Handle corporate actions (splits, mergers, spinoffs)
            // These affect position quantities and cost basis
            
            // =================================================================
            // PHASE 7: DATA VALIDATION & PERSISTENCE 📋 TODO
            // =================================================================
            
            // 📋 7.1. Validate all calculated snapshot values for consistency
            // - Ensure monetary values are reasonable
            // - Validate percentages are within expected ranges
            // - Check that cumulative values increase monotonically (where applicable)
            
            // 📋 7.2. Save all new/updated financial snapshots to database
            // Use batch operations if multiple currencies were processed
            
            // 📋 7.3. Log processing summary for monitoring and debugging
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