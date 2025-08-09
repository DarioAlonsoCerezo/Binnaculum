namespace Binnaculum.Core.Storage

open System
open System.Threading.Tasks
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
    
    /// <summary>
    /// Creates missing snapshots for the specified dates.
    /// This ensures data continuity for cascade updates by filling gaps in the snapshot history.
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="missingDates">Set of dates that need snapshots created</param>
    /// <returns>List of newly created snapshots</returns>
    let private createAndGetMissingSnapshots(brokerAccountId: int, missingDates: Set<DateTimePattern>) = task {
        let mutable createdSnapshots = []

        // Sort dates to minimize jumps in time (improves cascade update efficiency)
        let sortedMissingDates = missingDates |> Set.toList |> List.sortBy (fun d -> d.Value)
        
        for date in sortedMissingDates do
            let! snapshot = getOrCreateSnapshot(brokerAccountId, date)
            createdSnapshots <- snapshot :: createdSnapshots
        
        return createdSnapshots
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
                let! missedSnapshots = createAndGetMissingSnapshots(brokerAccountId, missingSnapshotDates)
                let allSnapshots = futureSnapshots @ missedSnapshots |> List.sortBy (fun s -> s.Base.Date.Value)
                do! BrokerFinancialSnapshotManager.brokerAccountCascadeUpdate snapshot allSnapshots
            | true, false, true ->
                // Future movements exist, all snapshots present - standard cascade
                do! BrokerFinancialSnapshotManager.brokerAccountCascadeUpdate snapshot futureSnapshots
            | _ ->
                // Edge cases - default to cascade for safety
                do! BrokerFinancialSnapshotManager.brokerAccountCascadeUpdate snapshot futureSnapshots
            return()
        }

(*
================================================================================
PERFORMANCE OPTIMIZATION STRATEGIES FOR MOBILE DEVICES
================================================================================

CONTEXT: The current getAllMovementsFromDate() method loads all movements from a 
specific date onwards, which could cause memory issues on older mobile devices 
with large datasets (thousands of trades, dividends, options over years).

Since all Core methods run on background threads (via ReactiveUI architecture), 
these optimizations maintain the existing threading model while improving memory 
efficiency and performance on resource-constrained devices.

================================================================================
STRATEGY 1: CONSERVATIVE MEMORY LIMITS (IMMEDIATE IMPLEMENTATION)
================================================================================

Add safety limits to prevent OutOfMemoryExceptions:

let private getAllMovementsFromDateSafe(brokerAccountId, snapshotDate, maxRecords = 1000) = task {
    // Add LIMIT clauses to all database queries
    let! brokerMovements = BrokerMovementExtensions.Do.getByBrokerAccountIdFromDateLimited(brokerAccountId, snapshotDate, maxRecords / 5)
    let! trades = TradeExtensions.Do.getByBrokerAccountIdFromDateLimited(brokerAccountId, snapshotDate, maxRecords / 5)
    let! dividends = DividendExtensions.Do.getByBrokerAccountIdFromDateLimited(brokerAccountId, snapshotDate, maxRecords / 5)
    let! dividendTaxes = DividendTaxExtensions.Do.getByBrokerAccountIdFromDateLimited(brokerAccountId, snapshotDate, maxRecords / 5)
    let! optionTrades = OptionTradeExtensions.Do.getByBrokerAccountIdFromDateLimited(brokerAccountId, snapshotDate, maxRecords / 5)
    
    // Log warning if hitting limits for monitoring
    let totalRecords = brokerMovements.Length + trades.Length + dividends.Length + dividendTaxes.Length + optionTrades.Length
    if totalRecords >= maxRecords then
        System.Diagnostics.Debug.WriteLine($"Movement data truncated for account {brokerAccountId} - loaded {totalRecords}/{maxRecords}")
    
    return BrokerAccountMovementData.create snapshotDate brokerAccountId brokerMovements trades dividends dividendTaxes optionTrades
}

DATABASE QUERY CHANGES NEEDED:
- Add LIMIT parameter to all *Query.fs files (BrokerMovementQuery, TradesQuery, etc.)
- Add corresponding *Extensions.fs methods with limit parameters
- SQL: "SELECT * FROM table WHERE conditions ORDER BY TimeStamp LIMIT @MaxRecords"

================================================================================
STRATEGY 2: INTELLIGENT DATE WINDOWING (PERFORMANCE ENHANCEMENT)
================================================================================

Use smart date ranges instead of unlimited lookback:

let private getOptimalDateRange(brokerAccountId, targetDate) = task {
    // Check device memory and account activity to determine optimal window
    let! deviceMemoryMB = getAvailableMemoryMB()
    let! dailyAvgMovements = getAverageMovementsPerDay(brokerAccountId)
    
    let lookbackDays = 
        match deviceMemoryMB, dailyAvgMovements with
        | mem, avg when mem < 1000 && avg > 20 -> 30    // Low memory, high activity: 1 month
        | mem, avg when mem < 1000 && avg <= 20 -> 60   // Low memory, normal activity: 2 months  
        | mem, avg when mem >= 2000 && avg > 20 -> 180  // Good memory, high activity: 6 months
        | mem, avg when mem >= 2000 && avg <= 20 -> 365 // Good memory, normal activity: 1 year
        | _, _ -> 90                                     // Default: 3 months
    
    let startDate = targetDate.AddDays(-lookbackDays)
    let endDate = targetDate.AddDays(30) // 30-day forward buffer
    return (startDate, endDate)
}

let private getAllMovementsFromDateOptimized(brokerAccountId, snapshotDate) = task {
    let! (startDate, endDate) = getOptimalDateRange(brokerAccountId, snapshotDate)
    
    // Use date range instead of open-ended queries
    let! brokerMovements = BrokerMovementExtensions.Do.getByBrokerAccountIdDateRange(brokerAccountId, startDate, endDate)
    let! trades = TradeExtensions.Do.getByBrokerAccountIdDateRange(brokerAccountId, startDate, endDate)
    // ... etc for all movement types
    
    return BrokerAccountMovementData.create snapshotDate brokerAccountId brokerMovements trades dividends dividendTaxes optionTrades
}

SUPPORTING FUNCTIONS NEEDED:
- getAvailableMemoryMB(): Check device available memory
- getAverageMovementsPerDay(): Query historical movement frequency
- New *DateRange methods in Extensions (WHERE date BETWEEN start AND end)

================================================================================
STRATEGY 3: CHUNKED PROCESSING WITH STREAMING (ADVANCED OPTIMIZATION)
================================================================================

Process large datasets in chunks to avoid memory spikes:

type MovementChunk = {
    BrokerMovements: BrokerMovement list
    Trades: Trade list  
    Dividends: Dividend list
    DividendTaxes: DividendTax list
    OptionTrades: OptionTrade list
    HasMore: bool
    ChunkIndex: int
}

let private processMovementsInChunks(brokerAccountId, startDate, chunkSize = 200) = task {
    let mutable hasMore = true
    let mutable offset = 0
    let combinedResults = ResizeArray<BrokerAccountMovementData>()
    
    while hasMore do
        // Load chunk of movements
        let! chunk = getMovementChunk(brokerAccountId, startDate, offset, chunkSize)
        
        // Process chunk immediately to avoid accumulating memory
        let! processedChunk = processChunkToSnapshot(chunk)
        combinedResults.Add(processedChunk)
        
        hasMore <- chunk.HasMore
        offset <- offset + chunkSize
        
        // Force garbage collection on low-memory devices
        if isLowMemoryDevice() then
            System.GC.Collect()
            System.GC.WaitForPendingFinalizers()
    
    return combineChunkedResults(combinedResults.ToArray())
}

CHUNK PROCESSING BENEFITS:
- Memory usage stays constant regardless of total dataset size
- Can process unlimited data without OutOfMemoryExceptions  
- Progress reporting possible for large datasets
- Automatic garbage collection prevents memory accumulation

================================================================================
STRATEGY 4: MOVEMENT SUMMARY CACHING (HISTORICAL DATA OPTIMIZATION)
================================================================================

Use pre-computed summaries for distant historical data:

type MovementSummary = {
    CurrencyId: int
    TotalCashImpact: Money
    MovementCount: int  
    CommissionsFees: Money
    DividendIncome: Money
    OptionIncome: Money
    DateRange: DateTimePattern * DateTimePattern
    LastCalculated: DateTimePattern
}

let private getMovementDataWithSummaries(brokerAccountId, targetDate) = task {
    let cutoffDate = targetDate.AddMonths(-3) // 3 months of detailed data
    
    // Recent data: full detail for accurate calculations
    let! recentMovements = getAllMovementsFromDate(brokerAccountId, cutoffDate)
    
    // Historical data: pre-computed summaries only
    let! historicalSummaries = getMovementSummariesFromCache(brokerAccountId, targetDate.AddYears(-5), cutoffDate)
    
    return {
        RecentData = recentMovements
        HistoricalSummaries = historicalSummaries
        CombinedCashImpact = calculateTotalCashImpact(recentMovements, historicalSummaries)
    }
}

CACHING IMPLEMENTATION:
- Create MovementSummaries table for pre-computed aggregates
- Background job updates summaries monthly for older data
- Only load detailed movements for recent periods (1-3 months)
- Combine recent detail + historical summaries for total picture

================================================================================
STRATEGY 5: CURRENCY-SELECTIVE LOADING (MULTI-CURRENCY OPTIMIZATION)  
================================================================================

Load only currencies with recent activity:

let private getActiveMovementsOnly(brokerAccountId, startDate) = task {
    // First, get currency activity summary (lightweight query)
    let! activeCurrencies = getCurrenciesWithRecentActivity(brokerAccountId, startDate)
    
    // Skip currencies with no recent activity
    if activeCurrencies.Length = 0 then
        return BrokerAccountMovementData.empty(brokerAccountId, startDate)
    
    // Load movements only for active currencies
    let loadTasks = activeCurrencies |> List.map (fun currencyId ->
        task {
            let! brokerMovements = BrokerMovementExtensions.Do.getByBrokerAccountIdCurrency(brokerAccountId, startDate, currencyId)
            let! trades = TradeExtensions.Do.getByBrokerAccountIdCurrency(brokerAccountId, startDate, currencyId)
            // ... etc
            return (currencyId, brokerMovements, trades, dividends, dividendTaxes, optionTrades)
        }
    )
    
    let! currencyResults = Task.WhenAll(loadTasks)
    return combineCurrencyResults(currencyResults, brokerAccountId, startDate)
}

CURRENCY FILTERING BENEFITS:
- Dramatically reduces memory for accounts with historical currency conversions
- Skips loading EUR movements if user hasn't traded EUR in 6+ months
- Maintains multi-currency accuracy while improving performance

================================================================================
STRATEGY 6: MEMORY PRESSURE MONITORING (ADAPTIVE PROCESSING)
================================================================================

Automatically adapt processing based on device memory state:

type MemoryPressure = 
    | Low       // < 40% memory used
    | Medium    // 40-60% memory used  
    | High      // 60-80% memory used
    | Critical  // > 80% memory used

type ProcessingStrategy = {
    MaxRecords: int
    ChunkSize: int
    ForceGC: bool
    UseCache: bool
    LookbackDays: int
}

let private getProcessingStrategy() = 
    let memoryPressure = getCurrentMemoryPressure()
    match memoryPressure with
    | Critical -> { MaxRecords = 100; ChunkSize = 25; ForceGC = true; UseCache = true; LookbackDays = 14 }
    | High ->     { MaxRecords = 500; ChunkSize = 50; ForceGC = true; UseCache = true; LookbackDays = 30 }  
    | Medium ->   { MaxRecords = 1500; ChunkSize = 150; ForceGC = false; UseCache = false; LookbackDays = 90 }
    | Low ->      { MaxRecords = 5000; ChunkSize = 500; ForceGC = false; UseCache = false; LookbackDays = 365 }

let private getAllMovementsAdaptive(brokerAccountId, snapshotDate) = task {
    let strategy = getProcessingStrategy()
    
    match strategy.UseCache with
    | true -> return! getMovementDataWithSummaries(brokerAccountId, snapshotDate)
    | false when strategy.MaxRecords < 1000 -> return! getAllMovementsFromDateSafe(brokerAccountId, snapshotDate, strategy.MaxRecords)
    | false -> return! getAllMovementsFromDate(brokerAccountId, snapshotDate)
}

ADAPTIVE PROCESSING BENEFITS:
- Automatically handles memory pressure without user intervention
- Degrades gracefully on older devices (summaries vs full data)
- Maintains optimal performance on high-end devices
- Prevents crashes due to memory exhaustion

================================================================================
STRATEGY 7: BACKGROUND PRIORITIZATION (REACTIVE UI INTEGRATION)
================================================================================

Prioritize UI responsiveness over processing speed:

let private processSnapshotWithUIFeedback(brokerAccountId, targetDate) = task {
    // Start with cached/summary data for immediate UI response  
    let! quickSnapshot = createQuickSnapshotFromCache(brokerAccountId, targetDate)
    
    // Update UI immediately with approximate data
    // (ReactiveUI will handle thread marshalling to MainThread)
    publishIntermediateSnapshot(brokerAccountId, targetDate, quickSnapshot)
    
    // Process full accurate data in background
    let backgroundTask = Task.Run(fun () -> task {
        let! fullMovementData = getAllMovementsFromDate(brokerAccountId, targetDate)
        let! accurateSnapshot = createAccurateSnapshot(fullMovementData)  
        
        // Update UI when accurate calculation completes
        publishFinalSnapshot(brokerAccountId, targetDate, accurateSnapshot)
    })
    
    // Don't await background task - return quick result immediately
    return quickSnapshot
}

UI INTEGRATION PATTERN:
1. User triggers snapshot update
2. Quick approximate result returned immediately (< 100ms)
3. UI updates with "Calculating..." indicator
4. Background task completes accurate calculation (1-5 seconds)
5. UI updates again with final accurate data
6. Maintains ReactiveUI's existing ObserveOn(UiThread) pattern

================================================================================
STRATEGY 8: PROGRESSIVE DISCLOSURE (USER EXPERIENCE OPTIMIZATION)
================================================================================

Load data progressively based on user needs:

// Load only essential data initially
let private getEssentialMovementsOnly(brokerAccountId, targetDate) = task {
    let! recentBrokerMovements = BrokerMovementExtensions.Do.getByBrokerAccountIdFromDateLimited(brokerAccountId, targetDate, 50)
    let! recentTrades = TradeExtensions.Do.getByBrokerAccountIdFromDateLimited(brokerAccountId, targetDate, 50)  
    
    // Skip dividends, taxes, options initially - load on demand
    return BrokerAccountMovementData.createPartial(brokerAccountId, targetDate, recentBrokerMovements, recentTrades)
}

// Load additional data when user requests detailed analysis
let private expandMovementData(partialData: BrokerAccountMovementData) = task {
    let! dividends = DividendExtensions.Do.getByBrokerAccountIdFromDate(partialData.BrokerAccountId, partialData.FromDate)
    let! dividendTaxes = DividendTaxExtensions.Do.getByBrokerAccountIdFromDate(partialData.BrokerAccountId, partialData.FromDate)
    let! optionTrades = OptionTradeExtensions.Do.getByBrokerAccountIdFromDate(partialData.BrokerAccountId, partialData.FromDate)
    
    return BrokerAccountMovementData.expand(partialData, dividends, dividendTaxes, optionTrades)
}

PROGRESSIVE LOADING BENEFITS:
- Faster initial loading for casual users
- Full data available when needed for detailed analysis
- Reduces memory usage for simple portfolio viewing
- Maintains performance on all device types

================================================================================
IMPLEMENTATION PRIORITY RECOMMENDATIONS
================================================================================

IMMEDIATE (Phase 1): Strategy 1 - Conservative Memory Limits  
- Add LIMIT clauses to all database queries
- Implement safety maximums (500-1000 records)
- Add logging for monitoring truncation

SHORT TERM (Phase 2): Strategy 2 - Intelligent Date Windowing
- Implement device memory detection
- Add date range optimization
- Create optimal window calculation logic

MEDIUM TERM (Phase 3): Strategy 6 - Memory Pressure Monitoring
- Add adaptive processing based on device state
- Implement graceful degradation strategies  
- Create memory pressure detection system

LONG TERM (Phase 4): Strategies 4, 5, 7 - Advanced Optimizations
- Movement summary caching system
- Currency-selective loading
- Progressive disclosure with ReactiveUI integration

================================================================================
TESTING STRATEGY FOR MOBILE PERFORMANCE
================================================================================

LOW-END DEVICE SIMULATION:
- Test with memory limits (1GB available RAM)
- Simulate large datasets (5000+ movements)  
- Test with multiple currencies (5+ currencies)
- Validate graceful degradation under pressure

PERFORMANCE BENCHMARKS:
- Initial load time < 2 seconds (essential data)
- Memory usage < 50MB peak for large datasets
- UI responsiveness maintained during background processing
- No OutOfMemoryExceptions under stress testing

MONITORING METRICS:
- Track movement data truncation frequency
- Monitor memory pressure adaptation frequency
- Measure UI thread blocking time (should be < 16ms)
- Record background processing completion times

================================================================================

All strategies maintain compatibility with existing ReactiveUI architecture:
- Core processing stays on background threads
- UI updates use .ObserveOn(UiThread) 
- Task.Run pattern preserved for heavy operations
- No changes required to existing UI code

The performance optimizations are additive and can be implemented incrementally
without disrupting the current working system.
*)        