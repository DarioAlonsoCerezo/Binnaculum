# TickerSnapshot Batch Processing - Architecture Design

**Created**: October 5, 2025  
**Status**: ✅ APPROVED - Ready for Implementation  
**Phase**: 1.2 - Architecture Design

---

## 📐 Module Architecture

### Module Dependency Graph

```
┌──────────────────────────────────────────────────────┐
│ ImportManager.fs                                     │
│ (Integration Layer - calls batch processing)        │
└──────────────────────────────────────────────────────┘
                       │
                       ├─ processBatchedTickersForImport(brokerAccountId)
                       │
                       ▼
┌──────────────────────────────────────────────────────┐
│ TickerSnapshotBatchManager.fs                       │
│ (Orchestrator - coordinates 3 phases)               │
│                                                      │
│  PHASE 1: Load Data    ──────────┐                  │
│  PHASE 2: Calculate    ──────────┤                  │
│  PHASE 3: Persist      ──────────┘                  │
└──────────────────────────────────────────────────────┘
         │                  │                  │
         ▼                  ▼                  ▼
┌─────────────────┐ ┌────────────────┐ ┌──────────────────┐
│ TickerSnapshot  │ │ TickerSnapshot │ │ TickerSnapshot   │
│ BatchLoader.fs  │ │ BatchCalc.fs   │ │ BatchPersist.fs  │
│                 │ │                │ │                  │
│ - loadBaseline  │ │ - Context      │ │ - Single         │
│   Snapshots     │ │ - Calculator   │ │   Transaction    │
│ - loadTicker    │ │ - Chronological│ │ - Bulk Insert    │
│   Movements     │ │   Processing   │ │ - Rollback       │
│ - loadMarket    │ │ - In-Memory    │ │ - Cleanup        │
│   Prices        │ │   State        │ │                  │
└─────────────────┘ └────────────────┘ └──────────────────┘
         │                  │
         │                  ▼
         │          ┌──────────────────────┐
         │          │ TickerSnapshot       │
         │          │ CalculateInMemory.fs │
         └──────────┤                      │
                    │ - calculateNew       │
                    │ - calculateInitial   │
                    │ - updateExisting     │
                    │ - carryForward       │
                    └──────────────────────┘
```

### Compilation Order (Core.fsproj)

```fsharp
// Existing dependencies...
<Compile Include="Snapshots\SnapshotManagerUtils.fs" />

// NEW: Batch processing modules (BEFORE TickerSnapshotManager)
<Compile Include="Snapshots\TickerSnapshotBatchLoader.fs" />
<Compile Include="Snapshots\TickerSnapshotCalculateInMemory.fs" />
<Compile Include="Snapshots\TickerSnapshotBatchCalculator.fs" />
<Compile Include="Snapshots\TickerSnapshotBatchPersistence.fs" />

// Existing TickerSnapshotManager (can reference batch modules)
<Compile Include="Snapshots\TickerSnapshotManager.fs" />

// ... other snapshot managers ...

// NEW: Batch manager (AFTER TickerSnapshotManager)
<Compile Include="Snapshots\TickerSnapshotBatchManager.fs" />
```

---

## 📊 Data Structures

### 1. TickerMovementData (Batch Loader Output)

```fsharp
type TickerMovementData =
    {
        /// Trades grouped by ticker ID, currency ID, and date
        Trades: Map<(int * int * DateTimePattern), Trade list>
        /// Dividends grouped by ticker ID, currency ID, and date
        Dividends: Map<(int * int * DateTimePattern), Dividend list>
        /// Dividend taxes grouped by ticker ID, currency ID, and date
        DividendTaxes: Map<(int * int * DateTimePattern), DividendTax list>
        /// Option trades grouped by ticker ID, currency ID, and date
        OptionTrades: Map<(int * int * DateTimePattern), OptionTrade list>
    }
```

**Rationale**: Similar to `BrokerAccountMovementData` but keyed by ticker ID instead of just date, allowing multi-ticker batch processing.

### 2. TickerSnapshotBatchContext (Calculator Input)

```fsharp
type TickerSnapshotBatchContext =
    {
        /// Map of ticker ID to baseline TickerSnapshot (latest before processing range)
        BaselineTickerSnapshots: Map<int, TickerSnapshot>
        /// Map of (ticker ID, currency ID) to baseline TickerCurrencySnapshot
        BaselineCurrencySnapshots: Map<(int * int), TickerCurrencySnapshot>
        /// All movements grouped by ticker/currency/date
        MovementsByTickerCurrencyDate: TickerMovementData
        /// Map of (ticker ID, date) to market price
        MarketPrices: Map<(int * DateTimePattern), decimal>
        /// List of dates to process in chronological order
        DateRange: DateTimePattern list
        /// List of ticker IDs being processed
        TickerIds: int list
    }
```

**Rationale**: Provides all data needed for pure in-memory calculations without database access.

### 3. TickerSnapshotBatchResult (Calculator Output)

```fsharp
type TickerSnapshotBatchResult =
    {
        /// List of (TickerSnapshot, TickerCurrencySnapshot list) tuples
        CalculatedSnapshots: (TickerSnapshot * TickerCurrencySnapshot list) list
        /// Processing metrics for monitoring
        ProcessingMetrics:
            {| TickersProcessed: int
               DatesProcessed: int
               MovementsProcessed: int
               SnapshotsCreated: int
               CalculationTimeMs: int64 |}
        /// Any errors encountered during processing
        Errors: string list
    }
```

**Rationale**: Matches BrokerFinancial pattern but adapted for ticker-specific data.

### 4. PersistenceMetrics (Persistence Output)

```fsharp
type PersistenceMetrics =
    {
        /// Number of TickerSnapshots saved
        TickerSnapshotsSaved: int
        /// Number of TickerCurrencySnapshots saved
        CurrencySnapshotsSaved: int
        /// Transaction execution time in milliseconds
        TransactionTimeMs: int64
    }
```

**Rationale**: Provides detailed metrics for performance monitoring.

---

## 🔄 Processing Flow

### Phase 1: Data Loading

```fsharp
module TickerSnapshotBatchLoader =
    
    // Load baseline snapshots (latest before import period)
    let loadBaselineSnapshots 
        (tickerIds: int list) 
        (beforeDate: DateTimePattern) 
        : Task<Map<int, TickerSnapshot> * Map<(int * int), TickerCurrencySnapshot>>
    
    // Load all movements for tickers in date range
    let loadTickerMovements 
        (tickerIds: int list) 
        (startDate: DateTimePattern) 
        (endDate: DateTimePattern) 
        : Task<TickerMovementData>
    
    // Load market prices for tickers in date range
    let loadMarketPrices 
        (tickerIds: int list) 
        (startDate: DateTimePattern) 
        (endDate: DateTimePattern) 
        : Task<Map<(int * DateTimePattern), decimal>>
```

**SQL Optimization**:
- Single query per movement type (no N+1 queries)
- `WHERE TickerId IN (@id1, @id2, ...) AND Date BETWEEN @start AND @end`
- Batch load all data upfront to avoid repeated database round trips

### Phase 2: Calculation Logic

```fsharp
module TickerSnapshotCalculateInMemory =
    
    // Pure calculation functions (no database I/O)
    let calculateNewSnapshot 
        (movements: TickerMovementsByDate)
        (previousSnapshot: TickerCurrencySnapshot option)
        (marketPrice: decimal)
        : TickerCurrencySnapshot
    
    let calculateInitialSnapshot 
        (movements: TickerMovementsByDate)
        (marketPrice: decimal)
        : TickerCurrencySnapshot
    
    let updateExistingSnapshot 
        (movements: TickerMovementsByDate)
        (previousSnapshot: TickerCurrencySnapshot)
        (existingSnapshot: TickerCurrencySnapshot)
        (marketPrice: decimal)
        : TickerCurrencySnapshot
    
    let carryForwardSnapshot 
        (previousSnapshot: TickerCurrencySnapshot)
        (newDate: DateTimePattern)
        (marketPrice: decimal)
        : TickerCurrencySnapshot
```

**Calculation Scenarios**:
- **Scenario A**: New movements + previous snapshot → Calculate cumulative values
- **Scenario B**: New movements + no previous → Calculate from zero
- **Scenario C**: New movements + previous + existing → Update existing with delta
- **Scenario D**: No movements + previous → Carry forward with price update

### Phase 3: Batch Persistence

```fsharp
module TickerSnapshotBatchPersistence =
    
    // Persist all snapshots in single transaction
    let persistBatchedSnapshots 
        (snapshots: (TickerSnapshot * TickerCurrencySnapshot list) list)
        : Task<Result<PersistenceMetrics, string>>
    
    // Delete existing + insert new (for force recalculation)
    let persistBatchedSnapshotsWithCleanup 
        (snapshots: (TickerSnapshot * TickerCurrencySnapshot list) list)
        (tickerIds: int list)
        (startDate: DateTimePattern)
        (endDate: DateTimePattern)
        : Task<Result<PersistenceMetrics, string>>
```

**Transaction Strategy**:
```sql
BEGIN TRANSACTION;

-- Optional cleanup for force recalculation
DELETE FROM TickerCurrencySnapshots 
WHERE TickerSnapshotId IN (
    SELECT Id FROM TickerSnapshots 
    WHERE TickerId IN (@ids) AND Date BETWEEN @start AND @end
);

DELETE FROM TickerSnapshots 
WHERE TickerId IN (@ids) AND Date BETWEEN @start AND @end;

-- Bulk insert new snapshots
INSERT INTO TickerSnapshots (...) VALUES (...);  -- Batch insert
INSERT INTO TickerCurrencySnapshots (...) VALUES (...);  -- Batch insert

COMMIT;
```

---

## 🎯 API Contracts

### TickerSnapshotBatchManager (Public Entry Point)

```fsharp
module TickerSnapshotBatchManager =
    
    /// Request parameters for batch processing
    type BatchProcessingRequest =
        {
            TickerIds: int list
            StartDate: DateTimePattern
            EndDate: DateTimePattern
            ForceRecalculation: bool
        }
    
    /// Result of batch processing with detailed metrics
    type BatchProcessingResult =
        {
            Success: bool
            TickerSnapshotsSaved: int
            CurrencySnapshotsSaved: int
            TickersProcessed: int
            DatesProcessed: int
            MovementsProcessed: int
            LoadTimeMs: int64
            CalculationTimeMs: int64
            PersistenceTimeMs: int64
            TotalTimeMs: int64
            Errors: string list
        }
    
    /// Process all tickers affected by an import
    let processBatchedTickersForImport 
        (brokerAccountId: int) 
        : Task<BatchProcessingResult>
    
    /// Process specific tickers for date range (targeted updates)
    let processSingleTickerBatch 
        (request: BatchProcessingRequest) 
        : Task<BatchProcessingResult>
```

### ImportManager Integration Point

```fsharp
// In ImportManager.fs after ReactiveSnapshotManager.refreshAsync()

// PHASE 1: Refresh broker account snapshots (existing)
do! ReactiveSnapshotManager.refreshAsync ()

// PHASE 2: Process ticker snapshots in batch (NEW)
CoreLogger.logInfo "ImportManager" "Starting batch ticker snapshot processing..."
let! tickerBatchResult = 
    TickerSnapshotBatchManager.processBatchedTickersForImport(brokerAccount.Id)

if tickerBatchResult.Success then
    CoreLogger.logInfof 
        "ImportManager" 
        "Ticker snapshot batch processing completed: %d snapshots in %dms"
        tickerBatchResult.TickerSnapshotsSaved
        tickerBatchResult.TotalTimeMs
else
    CoreLogger.logWarningf 
        "ImportManager" 
        "Ticker snapshot batch processing had errors: %s"
        (tickerBatchResult.Errors |> String.concat "; ")

// PHASE 3: Refresh reactive collections (existing)
do! TickerSnapshotLoader.load()
```

---

## 🧮 Calculation Algorithm

### Chronological Processing with State Tracking

```fsharp
// Pseudocode for batch calculator
let calculateBatchedTickerSnapshots (context: TickerSnapshotBatchContext) =
    
    let mutable latestSnapshotsByTickerCurrency = 
        context.BaselineCurrencySnapshots // Start with baseline
    
    // Process each date in chronological order
    for date in context.DateRange do
        
        // Process each ticker
        for tickerId in context.TickerIds do
            
            // Get currencies for this ticker (from movements or default)
            let currencies = getRelevantCurrencies tickerId date context
            
            // Process each currency
            for currencyId in currencies do
                
                // Get movements for this ticker/currency/date
                let movements = getMovements tickerId currencyId date context
                
                // Get previous snapshot from in-memory state
                let previousSnapshot = 
                    latestSnapshotsByTickerCurrency.TryFind (tickerId, currencyId)
                
                // Get market price
                let marketPrice = 
                    context.MarketPrices.TryFind (tickerId, date)
                    |> Option.defaultValue 0m
                
                // Calculate new snapshot (pure function)
                let newSnapshot = 
                    match movements, previousSnapshot with
                    | Some mvts, Some prev -> 
                        calculateNewSnapshot mvts prev marketPrice
                    | Some mvts, None -> 
                        calculateInitialSnapshot mvts marketPrice
                    | None, Some prev -> 
                        carryForwardSnapshot prev date marketPrice
                    | None, None -> 
                        None // No snapshot needed
                
                // Update in-memory state
                match newSnapshot with
                | Some snapshot ->
                    latestSnapshotsByTickerCurrency <- 
                        latestSnapshotsByTickerCurrency.Add((tickerId, currencyId), snapshot)
                | None -> ()
    
    return calculatedSnapshots
```

---

## 🔍 Comparison with BrokerFinancialSnapshot

### Similarities (Reuse Patterns)

| Aspect | BrokerFinancial | TickerSnapshot |
|--------|----------------|----------------|
| **Module Count** | 5 modules | 5 modules |
| **Phase Pattern** | Load → Calculate → Persist | Load → Calculate → Persist |
| **Data Structures** | Context, Result, Metrics | Context, Result, Metrics |
| **Calculation Scenarios** | A, B, C, D, E, F, G, H | A, B, C, D |
| **Transaction Strategy** | Single transaction | Single transaction |
| **Logging Pattern** | Comprehensive metrics | Comprehensive metrics |

### Differences (Adaptations Needed)

| Aspect | BrokerFinancial | TickerSnapshot | Rationale |
|--------|----------------|----------------|-----------|
| **Primary Key** | Currency ID | Ticker ID + Currency ID | Tickers have multi-currency data |
| **Baseline Data** | Single currency snapshot | TickerSnapshot + multiple currencies | Hierarchical structure |
| **Movement Grouping** | By date only | By ticker + currency + date | Multi-ticker processing |
| **Price Lookup** | Not needed | Required (LatestPrice field) | Unrealized gains calculation |
| **Aggregation Level** | Broker account level | Ticker level | Different granularity |

---

## ⚡ Performance Optimizations

### Database Query Optimization

**Current (Per-Date)**:
```sql
-- For EACH ticker, for EACH date:
SELECT * FROM Trades WHERE TickerId = @id AND Date = @date;  -- N×M queries
SELECT * FROM Dividends WHERE TickerId = @id AND Date = @date;
SELECT * FROM OptionTrades WHERE TickerId = @id AND Date = @date;
-- Total: N tickers × M dates × 3 queries = N×M×3 queries
```

**Optimized (Batch)**:
```sql
-- Single query per movement type for ALL tickers and dates:
SELECT * FROM Trades 
WHERE TickerId IN (@id1, @id2, ..., @idN) 
AND Date BETWEEN @start AND @end;  -- 1 query

SELECT * FROM Dividends 
WHERE TickerId IN (@id1, @id2, ..., @idN) 
AND Date BETWEEN @start AND @end;  -- 1 query

SELECT * FROM OptionTrades 
WHERE TickerId IN (@id1, @id2, ..., @idN) 
AND Date BETWEEN @start AND @end;  -- 1 query

-- Total: 3 queries (regardless of N or M)
```

**Reduction**: From **N×M×3** to **3** queries → **99%+ reduction** for large imports

### Memory Management

- **Lazy Evaluation**: Process dates chronologically, don't hold all snapshots in memory
- **Chunked Processing**: Could add pagination for very large datasets (future enhancement)
- **Disposal**: Use `use` bindings for database connections

---

## ✅ Acceptance Criteria

### Task 1.2 Complete When:

- [x] Data structures defined (Context, Result, Metrics)
- [x] Module APIs documented with function signatures
- [x] Processing flow documented (3 phases)
- [x] Calculation algorithm pseudocode provided
- [x] Comparison with BrokerFinancial pattern documented
- [x] SQL optimization strategy defined
- [x] Integration points identified (ImportManager)
- [x] Compilation order specified (Core.fsproj)

### Ready for Task 1.3 (Implementation):

✅ All architectural decisions documented  
✅ Clear contracts between modules  
✅ Performance optimization strategy defined  
✅ No ambiguities in module responsibilities  

---

## 📝 Implementation Notes

### Key Decisions

1. **Multi-Ticker Processing**: Context supports multiple tickers for efficient import scenarios
2. **Hierarchical Structure**: TickerSnapshot contains TickerCurrencySnapshot list (not flattened)
3. **Market Price Required**: LatestPrice field needs market price lookup (unlike BrokerFinancial)
4. **Baseline Loading**: Must load both TickerSnapshot AND TickerCurrencySnapshots separately
5. **Transaction Scope**: All tickers in single transaction (rollback protection)

### Future Enhancements

- **Parallel Processing**: Process tickers in parallel (async.Parallel)
- **Incremental Updates**: Only recalculate affected tickers (delta detection)
- **Caching Layer**: Cache market prices for frequently accessed tickers
- **Progress Callbacks**: Real-time progress for large imports
- **Partial Success**: Allow partial success with error isolation per ticker

---

**Architecture Review**: ✅ APPROVED  
**Ready for Implementation**: ✅ YES  
**Next Task**: 1.3 - Create TickerSnapshotBatchLoader.fs  
**Estimated Implementation Time**: 6-8 hours (all 4 modules + integration)  
