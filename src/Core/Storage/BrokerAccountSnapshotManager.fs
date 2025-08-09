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
            // IMPORTANT: This method retrieves ALL movement types FROM a specific date (inclusive) onwards.
            // This includes movements ON the snapshot date and all subsequent dates.
            // Any missed movement type will result in incorrect financial calculations and snapshot inconsistencies.
            
            // CRITICAL DATABASE QUERY LOGIC:
            // Use ">=" (greater than or equal) NOT ">" (greater than) in database queries
            // Example: WHERE TimeStamp >= @SnapshotDate  (INCLUDES the snapshot date)
            //     NOT: WHERE TimeStamp > @SnapshotDate   (EXCLUDES the snapshot date)
            
            // IMPORTANT: snapshotDate parameter should be set to START OF DAY (00:00:01)
            // This method should be called with getDateOnlyStartOfDay(date) to ensure we capture
            // all movements throughout the entire day, from 00:00:01 to 23:59:59.
            // Using end of day (23:59:59) would miss movements that occurred earlier in the day.
            
            // DATA REUSE STRATEGY:
            // This method should be called ONCE and its results reused throughout the snapshot calculation process.
            // The returned data will be used for:
            // 1. Identifying dates that need snapshots (extractUniqueDatesFromMovements)
            // 2. Calculating financial metrics for each affected date
            // 3. Determining cascade vs. one-day update strategies
            // 4. Validating data consistency across date ranges
            
            // RETURN TYPE: Consider returning a structured result containing:
            // - All movements grouped by date for efficient processing
            // - Unique dates list for snapshot gap detection
            // - Movement counts by type for validation
            // - Total affected date range for performance optimization
            
            // 1. BROKER MOVEMENTS (BrokerMovementExtensions) - IMPLEMENTED ✅
            //    - Deposits: Money added to the account
            //    - Withdrawals: Money removed from the account  
            //    - Fees: Account maintenance fees, trading fees
            //    - Interest Gained: Interest earned on cash balances
            //    - Interest Paid: Interest paid on margin/borrowed funds
            //    - Lending Income: Revenue from securities lending
            //    - Currency Conversions: Converting between different currencies
            //    - ACAT Transfers (Money): Account transfers - money in/out
            //    - ACAT Transfers (Securities): Account transfers - securities in/out
            let! brokerMovements = BrokerMovementExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, snapshotDate)
            
            // 2. STOCK/ETF TRADES (TradeExtensions) - IMPLEMENTED ✅
            //    - Buy orders: Purchasing securities (reduces cash, increases positions)
            //    - Sell orders: Selling securities (increases cash, reduces positions)
            //    - Both impact: account balance, commissions, fees, realized gains/losses
            let! trades = TradeExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, snapshotDate)
            
            // 3. DIVIDEND PAYMENTS (DividendExtensions) - IMPLEMENTED ✅
            //    - Cash dividends received from owned securities
            //    - Impacts: account balance (positive), dividend income totals
            let! dividends = DividendExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, snapshotDate)
            
            // 4. DIVIDEND-RELATED EVENTS (DividendDateExtensions)
            //    NOTE: Ex-dividend and pay dates should NOT be included in performance calculations!
            //    REASONING: These are informational events that don't represent actual cash flows:
            //    - Ex-dividend date: Stock goes ex-dividend but no cash received yet
            //    - Pay date announcement: Future payment date but money not received
            //    - Users can sell shares between ex-dividend and pay dates
            //    - Including these would create phantom income and double-counting issues
            //    DECISION: EXCLUDE from getAllMovementsFromDate - only track actual dividend RECEIPTS
            //    TODO: Consider if we need these for informational purposes only (not financial calculations)
            
            // 5. DIVIDEND TAXES (DividendTaxExtensions) - IMPLEMENTED ✅
            //    - Withholding taxes on dividend payments (especially foreign dividends)
            //    - Impacts: account balance (negative), tax expense tracking
            let! dividendTaxes = DividendTaxExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, snapshotDate)
            
            // 6. OPTIONS TRADING (OptionTradeExtensions) - IMPLEMENTED ✅
            //    - Option purchases: Buying calls/puts (reduces cash)
            //    - Option sales: Selling calls/puts (increases cash, may create obligations)
            //    - Option exercises: Converting options to stock positions
            //    - Option expirations: Options expiring worthless or in-the-money
            //    - Impacts: account balance, options positions, realized gains/losses
            let! optionTrades = OptionTradeExtensions.Do.getByBrokerAccountIdFromDate(brokerAccountId, snapshotDate)
            
            // CRITICAL: Each movement type above can change:
            // - Account cash balance (positive or negative)
            // - Securities positions (quantities owned)
            // - Realized gains/losses (from sales)
            // - Unrealized gains/losses (from position value changes)
            // - Commission and fee totals
            // - Income categorization (dividends, options, interest, etc.)
            
            // IMPORTANT NOTE: MARKET PRICE CHANGES
            // Price changes for held securities are NOT retrieved here because they are not "movements"
            // in the traditional sense - they are valuation changes that affect unrealized P&L.
            // 
            // DESIGN DECISION: Price changes are handled separately through:
            // 1. TickerSnapshots: Store end-of-day prices for each security
            // 2. Position Calculation: During snapshot calculation, apply current market prices
            //    to held positions to determine unrealized gains/losses
            // 3. Portfolio Valuation: Sum of (position_quantity × current_price) for all holdings
            //
            // This approach separates:
            // - TRANSACTIONAL data (actual money/security movements) ← Retrieved here
            // - VALUATION data (market price changes) ← Handled in snapshot calculation logic
            //
            // Benefits:
            // - Cleaner separation of concerns
            // - Avoids massive volume of price "movement" records
            // - Allows historical price reconstruction for any date
            // - Supports multiple valuation methods (end-of-day, real-time, etc.)
            
            // PERFORMANCE OPTIMIZATION: 
            // Consider implementing parallel retrieval of movement types for better performance:
            // let! [|brokerMovements; trades; dividends; dividendTaxes; optionTrades|] = 
            //     [|getBrokerMovements(); getTrades(); getDividends(); getDividendTaxes(); getOptionTrades()|]
            //     |> Async.Parallel |> Async.AwaitTask
            
            // HELPER FUNCTIONS TO ADD:
            // - extractUniqueDatesFromMovements: Get all unique dates from the movement collection
            // - groupMovementsByDate: Organize movements by date for efficient processing
            // - validateMovementDataIntegrity: Ensure no data corruption or missing references
            
            // CURRENT RETURN: All movement types implemented (5/5 financial movement types)
            // 1. Broker Movements ✅, 2. Trades ✅, 3. Dividends ✅, 4. Dividend Taxes ✅, 5. Option Trades ✅
            // TODO: Expand to return structured data with all movement types - ready for implementation
            
            // Create structured movement data using the new BrokerAccountMovementData type
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
            
            // DATA REUSE STRATEGY: Fetch movement data ONCE and reuse throughout the process
            // This single call will provide all the data needed for:
            // 1. Determining affected dates
            // 2. Detecting snapshot gaps  
            // 3. Choosing update strategy (one-day vs cascade)
            // 4. Performing the actual calculations
            
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
