# TickerSnapshot Batch Processing Implementation Plan

**Branch**: `feature/ticker-snapshot-batch-processing`  
**Created**: October 5, 2025  
**Status**: 🚧 IN PROGRESS - Phase 1  
**Target**: 90-95% performance improvement for TickerSnapshot calculations during import

---

## 📋 Executive Summary

### Current Problem
- **Issue**: TickerSnapshots created in database but calculations incomplete (Options=$0, Realized=$0)
- **Root Cause**: Per-date database I/O processing misses cumulative data and runs before movements are fully imported
- **Impact**: Incorrect financial metrics in ticker snapshots, making portfolio tracking unreliable

### Solution Approach
Mirror the successful `BrokerFinancialSnapshot` batch processing pattern:
1. **Load ALL data upfront** (baseline snapshots + movements + market prices)
2. **Calculate ALL snapshots in MEMORY** (no database round trips)
3. **Persist ALL results in SINGLE TRANSACTION** (atomic, fast)

### Expected Results
- ✅ Correct calculation of Options income and Realized gains
- ✅ 90-95% reduction in database I/O (based on BrokerFinancial results)
- ✅ Scalable for large imports (100+ tickers, 1000+ movements)
- ✅ Future-proof architecture for incremental enhancements

---

## 🎯 Success Criteria

### Test Case: Pfizer Options Import
**Current Results** (baseline - as of commit 1604120):
```
TickerSnapshots: Expected 2, Got 2 - ✅ PASS (structure created)
PFE Options: Expected $175.52, Got $0.00 - ❌ FAIL (calculation missing)
PFE Realized: Expected $175.52, Got $0.00 - ❌ FAIL (calculation missing)
```

**Target Results** (after batch processing implementation):
```
TickerSnapshots: Expected 2, Got 2 - ✅ PASS
PFE Options: Expected $175.52, Got $175.52 - ✅ PASS
PFE Realized: Expected $175.52, Got $175.52 - ✅ PASS
PFE TotalShares: Expected 0.00, Got 0.00 - ✅ PASS
PFE CostBasis: Expected $0.00, Got $0.00 - ✅ PASS
PFE Unrealized: Expected $0.00, Got $0.00 - ✅ PASS
```

### Performance Targets
- **Import Time**: <5 seconds for 10 tickers with 100 movements each
- **Database Queries**: Single batch load + single bulk save (vs. N×M queries currently)
- **Memory Usage**: Efficient in-memory processing (validated via performance tests)

---

## 📐 Architecture Overview

### Modular Component Design
```
┌─────────────────────────────────────────────────────────┐
│ ImportManager.fs                                        │
│ └─> TickerSnapshotBatchManager.processBatchedTickers   │
└─────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│ TickerSnapshotBatchManager.fs (Orchestrator)           │
│  PHASE 1: Load Data                                    │
│  PHASE 2: Calculate In-Memory                          │
│  PHASE 3: Persist Results                              │
└─────────────────────────────────────────────────────────┘
        │                      │                      │
        ▼                      ▼                      ▼
┌──────────────┐  ┌──────────────────────┐  ┌─────────────────┐
│ Batch Loader │  │ Batch Calculator     │  │ Batch Persister │
│              │  │                      │  │                 │
│ - Baseline   │  │ - Chronological      │  │ - Single        │
│   Snapshots  │  │   Processing         │  │   Transaction   │
│ - Movements  │  │ - In-Memory State    │  │ - Bulk Insert   │
│ - Prices     │  │ - Pure Calculations  │  │ - Rollback      │
└──────────────┘  └──────────────────────┘  └─────────────────┘
        │                      │                      │
        ▼                      ▼                      ▼
┌─────────────────────────────────────────────────────────┐
│ TickerSnapshotCalculateInMemory.fs                     │
│ (Pure calculation logic - no database I/O)             │
│  - calculateNewSnapshot                                │
│  - calculateInitialSnapshot                            │
│  - updateExistingSnapshot                              │
│  - carryForwardSnapshot                                │
└─────────────────────────────────────────────────────────┘
```

### Data Flow
```
CSV Import
    │
    ├─> Parse Transactions
    │
    ├─> Save to Database (movements, trades, options)
    │
    └─> Batch Processing (NEW - replaces per-date processing)
            │
            ├─> TickerSnapshotBatchLoader
            │       Load baseline snapshots (latest before import period)
            │       Load ALL movements (trades, dividends, options)
            │       Load market prices for date range
            │
            ├─> TickerSnapshotBatchCalculator
            │       Process dates chronologically
            │       Calculate ALL snapshots in memory
            │       Maintain cumulative state
            │
            └─> TickerSnapshotBatchPersistence
                    Delete existing snapshots (if ForceRecalculation)
                    Bulk insert all snapshots in single transaction
                    Rollback on error
```

---

## 🗂️ Implementation Phases

### **Phase 1: Foundation & Analysis** 
**Status**: 🔄 IN PROGRESS  
**Duration**: 1-2 hours  
**Commits**: TBD

#### Tasks
- [x] **Task 1.1**: Analyze Current TickerSnapshot Architecture vs BrokerFinancialSnapshot Pattern
  - **Status**: ✅ COMPLETED
  - **Notes**: Documented similarities, differences, and root cause of calculation issues
  - **Key Findings**:
    - TickerSnapshotManager uses per-date database I/O (inefficient)
    - handleNewTicker creates empty snapshots before movements imported
    - BrokerFinancialBatchManager pattern proven to reduce I/O by 90-95%

- [ ] **Task 1.2**: Design TickerSnapshot Batch Processing Architecture
  - **Status**: ⏳ NOT STARTED
  - **Deliverables**:
    - Module dependency diagram
    - Data structure definitions
    - API contracts for each module

- [ ] **Task 1.3**: Create TickerSnapshot Data Loading Module
  - **Status**: ⏳ NOT STARTED
  - **File**: `src/Core/Snapshots/TickerSnapshotBatchLoader.fs`
  - **Dependencies**: None (first module in batch chain)
  - **Functions**:
    - `loadBaselineSnapshots: int list -> DateTimePattern -> Map<int, TickerSnapshot * TickerCurrencySnapshot list>`
    - `loadTickerMovements: int list -> DateTimePattern -> DateTimePattern -> TickerMovementData`
    - `loadMarketPrices: int list -> DateTimePattern -> DateTimePattern -> Map<(int * DateTimePattern), decimal>`
  - **Validation**: Unit tests for SQL query optimization

#### Acceptance Criteria
- [ ] Architecture document created with component diagrams
- [ ] TickerSnapshotBatchLoader implemented with optimized SQL
- [ ] Unit tests passing for data loading functions
- [ ] Code review completed

#### Blockers & Risks
- None identified yet

---

### **Phase 2: Core Calculation Logic**
**Status**: ⏳ NOT STARTED  
**Duration**: 2-3 hours  
**Commits**: TBD

#### Tasks
- [ ] **Task 2.1**: Extract TickerSnapshot Calculation Logic to In-Memory Module
  - **Status**: ⏳ NOT STARTED
  - **File**: `src/Core/Snapshots/TickerSnapshotCalculateInMemory.fs`
  - **Dependencies**: TickerSnapshotManager (extract from)
  - **Functions**:
    - `calculateNewSnapshot: TickerMovementData -> TickerCurrencySnapshot option -> TickerCurrencySnapshot`
    - `calculateInitialSnapshot: TickerMovementData -> TickerCurrencySnapshot`
    - `updateExistingSnapshot: TickerMovementData -> TickerCurrencySnapshot -> TickerCurrencySnapshot -> TickerCurrencySnapshot`
    - `carryForwardSnapshot: TickerCurrencySnapshot -> DateTimePattern -> TickerCurrencySnapshot`
  - **Validation**: All calculation scenarios tested with known inputs/outputs

- [ ] **Task 2.2**: Implement TickerSnapshot Batch Calculator
  - **Status**: ⏳ NOT STARTED
  - **File**: `src/Core/Snapshots/TickerSnapshotBatchCalculator.fs`
  - **Dependencies**: TickerSnapshotCalculateInMemory
  - **Context**: `BatchCalculationContext { BaselineSnapshots, MovementsByTickerCurrencyDate, MarketPrices, DateRange }`
  - **Result**: `BatchCalculationResult { CalculatedSnapshots, Metrics, Errors }`
  - **Validation**: Integration tests with multi-ticker scenarios

- [ ] **Task 2.3**: Implement TickerSnapshot Batch Persistence
  - **Status**: ⏳ NOT STARTED
  - **File**: `src/Core/Snapshots/TickerSnapshotBatchPersistence.fs`
  - **Dependencies**: TickerSnapshotBatchCalculator (result consumer)
  - **Functions**:
    - `persistBatchedSnapshots: (TickerSnapshot * TickerCurrencySnapshot list) list -> Task<Result<PersistenceMetrics, string>>`
    - `persistBatchedSnapshotsWithCleanup: (TickerSnapshot * TickerCurrencySnapshot list) list -> int list -> DateTimePattern -> DateTimePattern -> Task<Result<PersistenceMetrics, string>>`
  - **Validation**: Transaction rollback tests, error handling tests

#### Acceptance Criteria
- [ ] All calculation scenarios implemented (A-H like BrokerFinancial)
- [ ] Batch calculator processes multiple tickers efficiently
- [ ] Persistence handles errors gracefully with rollback
- [ ] Unit tests passing for all calculation functions
- [ ] Integration tests passing for multi-ticker scenarios

#### Blockers & Risks
- Risk: F# compilation order dependencies (mitigated by proper .fsproj ordering)

---

### **Phase 3: Integration & Orchestration**
**Status**: ⏳ NOT STARTED  
**Duration**: 2-3 hours  
**Commits**: TBD

#### Tasks
- [ ] **Task 3.1**: Create TickerSnapshot Batch Manager Orchestrator
  - **Status**: ⏳ NOT STARTED
  - **File**: `src/Core/Snapshots/TickerSnapshotBatchManager.fs`
  - **Dependencies**: All Phase 2 modules
  - **Entry Points**:
    - `processBatchedTickersForImport: int -> Task<BatchProcessingResult>` (for import scenarios)
    - `processSingleTickerBatch: int -> DateTimePattern -> DateTimePattern -> Task<BatchProcessingResult>` (for targeted updates)
  - **Phases**: PHASE 1 Load → PHASE 2 Calculate → PHASE 3 Persist
  - **Validation**: End-to-end import tests with logging validation

- [ ] **Task 3.2**: Update Core.fsproj with New Modules in Correct Compilation Order
  - **Status**: ⏳ NOT STARTED
  - **File**: `src/Core/Core.fsproj`
  - **Order**:
    1. `TickerSnapshotBatchLoader.fs` (line ~116, before TickerSnapshotManager)
    2. `TickerSnapshotCalculateInMemory.fs` (line ~117, before TickerSnapshotManager)
    3. `TickerSnapshotBatchCalculator.fs` (line ~118, before TickerSnapshotManager)
    4. `TickerSnapshotBatchPersistence.fs` (line ~119, before TickerSnapshotManager)
    5. `TickerSnapshotManager.fs` (line ~120, existing)
    6. `TickerSnapshotBatchManager.fs` (line ~135, after TickerSnapshotManager)
  - **Validation**: F# project builds successfully without dependency errors

- [ ] **Task 3.3**: Integrate TickerSnapshot Batch Processing into ImportManager
  - **Status**: ⏳ NOT STARTED
  - **File**: `src/Core/Import/ImportManager.fs`
  - **Changes**:
    - After `ReactiveSnapshotManager.refreshAsync()` (line ~247)
    - Add: `do! TickerSnapshotBatchManager.processBatchedTickersForImport(brokerAccount.Id)`
    - Keep: `do! TickerSnapshotLoader.load()` (refreshes Collections)
  - **Logging**: Add performance metrics logging (load/calc/persist times)
  - **Validation**: Pfizer test passes with correct calculations

- [ ] **Task 3.4**: Update DatabasePersistence.getOrCreateTickerId Strategy
  - **Status**: ⏳ NOT STARTED
  - **File**: `src/Core/Import/DatabasePersistence.fs`
  - **Decision Required**: Remove or keep `handleNewTicker` call at line 298?
    - **Option A**: Remove (let batch processing handle all snapshots)
    - **Option B**: Keep for non-import ticker creation scenarios
    - **Recommendation**: TBD after analysis
  - **Validation**: Document decision rationale in code comments

#### Acceptance Criteria
- [ ] TickerSnapshotBatchManager coordinates all phases successfully
- [ ] Core.fsproj compiles with correct F# dependency order
- [ ] ImportManager calls batch processing at appropriate point
- [ ] DatabasePersistence strategy documented and implemented
- [ ] All integration points tested

#### Blockers & Risks
- Risk: Backward compatibility with existing snapshot workflows (mitigated by gradual rollout)

---

### **Phase 4: Testing & Validation**
**Status**: ⏳ NOT STARTED  
**Duration**: 2-3 hours  
**Commits**: TBD

#### Tasks
- [ ] **Task 4.1**: Build and Validate Core Project Compilation
  - **Status**: ⏳ NOT STARTED
  - **Command**: `dotnet build src/Core/Core.fsproj`
  - **Expected**: Build completes in 13-14 seconds without errors
  - **Validation**: No F# compilation order issues, all modules compile

- [ ] **Task 4.2**: Build and Validate Android Test Project
  - **Status**: ⏳ NOT STARTED
  - **Command**: `dotnet build src/Tests/Core.Platform.MauiTester/Core.Platform.MauiTester.csproj -f net9.0-android`
  - **Expected**: Build completes in ~3 minutes without errors
  - **Validation**: MAUI integration successful

- [ ] **Task 4.3**: Deploy and Run Pfizer Test - Verify TickerSnapshot Calculations
  - **Status**: ⏳ NOT STARTED
  - **Platform**: Android emulator/device
  - **Test**: `ReactivePfizerImportIntegrationTest`
  - **Expected Results**:
    - TickerSnapshots count: 2 ✅ PASS
    - PFE Options: $175.52 ✅ PASS (currently $0.00)
    - PFE Realized: $175.52 ✅ PASS (currently $0.00)
    - PFE TotalShares: 0.00 ✅ PASS
    - PFE CostBasis: $0.00 ✅ PASS
    - PFE Unrealized: $0.00 ✅ PASS
  - **Logging**: Verify batch processing logs show proper data loading, calculation, persistence

- [ ] **Task 4.4**: Performance Testing and Optimization Validation
  - **Status**: ⏳ NOT STARTED
  - **Tests**: `BrokerFinancialSnapshotManagerPerformanceTests`
  - **Validation**:
    - Existing broker snapshot performance not degraded
    - Import time improvements documented
    - Database I/O reduction measured (target: 90-95%)
  - **Deliverable**: Before/after performance metrics document

#### Acceptance Criteria
- [ ] All builds successful (Core + MauiTester)
- [ ] Pfizer test passes with 100% validation success
- [ ] Performance tests show expected improvements
- [ ] No regressions in existing snapshot functionality
- [ ] Batch processing logs demonstrate correct flow

#### Blockers & Risks
- Risk: Performance degradation in other areas (mitigated by comprehensive testing)
- Risk: Platform-specific issues (Android/iOS differences)

---

### **Phase 5: Documentation & Completion**
**Status**: ⏳ NOT STARTED  
**Duration**: 1-2 hours  
**Commits**: TBD

#### Tasks
- [ ] **Task 5.1**: Create Technical Documentation
  - **Status**: ⏳ NOT STARTED
  - **File**: `docs/ticker-snapshot-batch-processing.md`
  - **Sections**:
    - Architecture decisions and rationale
    - Performance characteristics and benchmarks
    - Batch vs per-date processing scenarios
    - Future enhancement opportunities
    - Troubleshooting guide
  - **Validation**: Documentation peer-reviewed

- [ ] **Task 5.2**: Update Copilot Instructions
  - **Status**: ⏳ NOT STARTED
  - **File**: `.github/copilot-instructions.md`
  - **Updates**:
    - Add TickerSnapshot batch processing patterns
    - Document module dependency order
    - Include performance optimization guidelines
  - **Validation**: Instructions clear and actionable

- [ ] **Task 5.3**: Code Review and Cleanup
  - **Status**: ⏳ NOT STARTED
  - **Tasks**:
    - Remove unused logging statements
    - Ensure consistent code formatting
    - Verify all TODOs addressed or documented
    - Review error messages for clarity
  - **Validation**: Code review checklist completed

- [ ] **Task 5.4**: Merge to Main Branch
  - **Status**: ⏳ NOT STARTED
  - **Prerequisites**:
    - All phases completed
    - All tests passing
    - Documentation complete
    - Code review approved
  - **Command**: `git merge feature/ticker-snapshot-batch-processing`
  - **Validation**: CI/CD pipeline passes

#### Acceptance Criteria
- [ ] Technical documentation complete and accurate
- [ ] Copilot instructions updated
- [ ] Code review approved
- [ ] Feature branch merged to main
- [ ] Release notes prepared

#### Blockers & Risks
- None identified

---

## 📊 Progress Tracking

### Overall Progress
```
Phase 1: Foundation & Analysis        [██░░░░░░░░] 20% (1/3 tasks)
Phase 2: Core Calculation Logic       [░░░░░░░░░░]  0% (0/3 tasks)
Phase 3: Integration & Orchestration  [░░░░░░░░░░]  0% (0/4 tasks)
Phase 4: Testing & Validation         [░░░░░░░░░░]  0% (0/4 tasks)
Phase 5: Documentation & Completion   [░░░░░░░░░░]  0% (0/4 tasks)

Total Progress: [██░░░░░░░░] 5% (1/18 tasks)
```

### Git History
| Commit | Date | Description | Tests Passing |
|--------|------|-------------|---------------|
| 1604120 | Oct 5, 2025 | feat: Add TickerSnapshot creation during import and collection refresh | ✅ Partial (structure created, calculations pending) |

### Metrics
| Metric | Baseline (Current) | Target | Actual | Status |
|--------|-------------------|--------|--------|--------|
| PFE Options Calculation | $0.00 ❌ | $175.52 | TBD | ⏳ Pending |
| PFE Realized Calculation | $0.00 ❌ | $175.52 | TBD | ⏳ Pending |
| Database Queries (import) | N×M queries | <10 queries | TBD | ⏳ Pending |
| Import Time (10 tickers) | TBD | <5 seconds | TBD | ⏳ Pending |

---

## 🔄 Update Log

### October 5, 2025
- ✅ Created feature branch `feature/ticker-snapshot-batch-processing`
- ✅ Committed baseline changes (TickerSnapshot creation + collection refresh)
- ✅ Created comprehensive tracking document
- ✅ Completed Task 1.1 (Architecture Analysis)
- 🎯 **Current Focus**: Phase 1 - Foundation & Analysis

### [Future Updates Will Be Added Here]

---

## 🚀 Next Steps

### Immediate Actions (Next Session)
1. **Complete Task 1.2**: Design TickerSnapshot Batch Processing Architecture
   - Create module dependency diagrams
   - Define data structures (BatchCalculationContext, BatchCalculationResult)
   - Document API contracts

2. **Start Task 1.3**: Create TickerSnapshotBatchLoader.fs
   - Implement SQL batch queries
   - Add unit tests for data loading

### Decision Points Requiring Input
- **DatabasePersistence Strategy**: Keep or remove `handleNewTicker` call during ticker creation?
- **Backward Compatibility**: Support both batch and per-date modes, or full cutover?
- **Error Handling**: Rollback strategy when batch processing fails mid-import?

### Questions for Review
1. Should we implement a feature flag to toggle between batch and per-date processing?
2. Do we need to support partial batch processing (e.g., only certain tickers)?
3. What's the priority: performance optimization or feature completeness?

---

## 📚 References

### Related Documents
- `docs/batch-financial-calculations-optimization.md` - BrokerFinancial pattern (proven success)
- `.github/copilot-instructions.md` - Project coding guidelines
- `BROKER_ACCOUNT_TEST_IMPLEMENTATION.md` - Test patterns and strategies

### Key Code Files
- `src/Core/Snapshots/BrokerFinancialBatchManager.fs` - Reference implementation
- `src/Core/Snapshots/TickerSnapshotManager.fs` - Current per-date implementation
- `src/Core/Import/ImportManager.fs` - Integration point
- `src/Tests/Core.Platform.MauiTester/TestCases/ReactivePfizerImportIntegrationTest.cs` - Validation test

### Performance Baselines
- BrokerFinancial batch processing: **90-95% I/O reduction**
- Current TickerSnapshot: **N×M database queries** (N tickers, M dates)
- Target: **<10 total queries** for any import size

---

## ✅ Definition of Done

### Feature Complete When:
- [ ] All 18 tasks completed across 5 phases
- [ ] Pfizer test passes with 100% validation (all 6 PFE fields correct)
- [ ] Performance tests show ≥90% database I/O reduction
- [ ] No regressions in existing functionality
- [ ] Documentation complete and peer-reviewed
- [ ] Code review approved
- [ ] Feature branch merged to main
- [ ] Release notes prepared

### Success Indicators:
- ✅ TickerSnapshot calculations accurate ($175.52 Options, $175.52 Realized for PFE)
- ✅ Import performance scalable (handles 100+ tickers efficiently)
- ✅ Future-proof architecture (easy to add features like parallel processing)
- ✅ Maintainable codebase (clear separation of concerns, well-documented)

---

**Last Updated**: October 5, 2025, 18:30 UTC  
**Next Review**: After Phase 1 completion  
**Branch**: `feature/ticker-snapshot-batch-processing`  
**Assignee**: GitHub Copilot Agent  
