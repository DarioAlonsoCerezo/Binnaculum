# TickerSnapshot Batch Processing Refactoring Checklist

**Task**: Phase 3.3b - Refactor for Actual Architecture  
**Estimated Time**: 2-3 hours  
**Current Status**: ⏳ NOT STARTED  
**Files to Modify**: 4 modules (~1,540 lines total)

---

## Quick Reference: Architectural Difference

### ❌ WRONG (BrokerFinancialSnapshot - what we assumed)
```fsharp
type BrokerFinancialSnapshot = {
    Base: BaseSnapshot
    MainCurrency: BrokerCurrencySnapshot      // Embedded
    OtherCurrencies: BrokerCurrencySnapshot list  // Embedded
}
```

### ✅ CORRECT (TickerSnapshot - what actually exists)
```fsharp
// Parent entity (flat structure)
type TickerSnapshot = {
    Base: BaseSnapshot
    TickerId: int
}

// Child entity (separate, linked via FK)
type TickerCurrencySnapshot = {
    Base: BaseSnapshot
    TickerId: int
    CurrencyId: int
    TickerSnapshotId: int  // Foreign key to parent
    // ... financial fields (TotalShares, CostBasis, Options, Realized, etc.)
}
```

---

## Module 1: TickerSnapshotBatchLoader.fs

**Estimated Time**: ~1 hour  
**Lines**: 350+  
**Priority**: HIGH (other modules depend on this)

### Changes Required

#### Type Signatures
- [ ] Change return type of `loadBaselineSnapshots`:
  ```fsharp
  // FROM:
  Async<Map<int, TickerSnapshot>>
  
  // TO:
  Async<Map<int, TickerSnapshot> * Map<(int * int), TickerCurrencySnapshot>>
  ```

#### Line-by-Line Fixes
- [ ] **Line 85**: Remove `.MainCurrency` access (doesn't exist)
- [ ] **Line 88**: Remove `.OtherCurrencies` access (doesn't exist)
- [ ] **Line 97**: Change `.Currency` to `.CurrencyId` (correct field name)
- [ ] **Lines 101, 185-209**: Fix array↔list type conversions

#### Logic Changes
- [ ] Separate queries for loading:
  ```fsharp
  // Load TickerSnapshot separately
  let! baseSnapshots = TickerSnapshotExtensions.getLatestBeforeDate(tickerId, beforeDate)
  
  // Load TickerCurrencySnapshot separately (by TickerSnapshotId FK)
  let! currencySnapshots = TickerCurrencySnapshotExtensions.getByTickerSnapshotId(baseSnapshotId)
  ```

- [ ] Build two maps instead of hierarchy:
  ```fsharp
  let tickerSnapshotMap = ... // Map<int, TickerSnapshot>
  let currencySnapshotMap = ... // Map<(tickerId * currencyId), TickerCurrencySnapshot>
  return (tickerSnapshotMap, currencySnapshotMap)
  ```

#### Validation
- [ ] No references to `.MainCurrency` or `.OtherCurrencies`
- [ ] Returns tuple of separate maps
- [ ] Type annotations clear (no ambiguity errors)

---

## Module 2: TickerSnapshotCalculateInMemory.fs

**Estimated Time**: ~45 minutes  
**Lines**: 450+  
**Priority**: HIGH (calculator depends on this)

### Changes Required

#### Function Signatures
- [ ] Update all calculation functions to return `TickerCurrencySnapshot` (not hierarchy):
  ```fsharp
  // FROM:
  calculateNewSnapshot : ... -> TickerSnapshot
  
  // TO:
  calculateNewSnapshot : ... -> TickerCurrencySnapshot
  ```

#### Record Construction
- [ ] Use correct SnapshotsModel.TickerCurrencySnapshot fields:
  ```fsharp
  {
      Base = { Id = 0; Date = date; BrokerAccountId = accountId }
      TickerId = tickerId
      CurrencyId = currencyId
      TickerSnapshotId = 0  // Will be set during persistence
      TotalShares = calculatedShares
      Weight = calculatedWeight
      CostBasis = calculatedCostBasis
      RealCost = calculatedRealCost
      Dividends = calculatedDividends
      Options = calculatedOptions
      TotalIncomes = calculatedIncomes
      Unrealized = calculatedUnrealized
      Realized = calculatedRealized
      Performance = calculatedPerformance
      LatestPrice = marketPrice
      OpenTrades = hasOpenTrades
  }
  ```

#### Logic Changes
- [ ] Remove hierarchy creation (no embedding of children)
- [ ] Remove any MainCurrency/OtherCurrencies logic
- [ ] Keep pure calculation logic (good design, no changes needed)

#### Validation
- [ ] All functions return `TickerCurrencySnapshot`
- [ ] Record fields match SnapshotsModel definition exactly
- [ ] No hierarchy construction

---

## Module 3: TickerSnapshotBatchCalculator.fs

**Estimated Time**: ~45 minutes  
**Lines**: 370+  
**Priority**: MEDIUM (depends on Module 1 & 2)

### Changes Required

#### Type Definitions
- [ ] Update `TickerSnapshotBatchContext`:
  ```fsharp
  type TickerSnapshotBatchContext = {
      TickerSnapshots: Map<int, TickerSnapshot>  // Separate
      CurrencySnapshots: Map<(int * int), TickerCurrencySnapshot>  // Separate
      Movements: TickerMovementsByDate
      MarketPrices: Map<(int * int * DateTime), MarketPrice>
      // ... other fields
  }
  ```

- [ ] Update `TickerSnapshotBatchResult`:
  ```fsharp
  type TickerSnapshotBatchResult = {
      TickerSnapshots: TickerSnapshot list  // Separate list
      CurrencySnapshots: TickerCurrencySnapshot list  // Separate list
      Metrics: CalculationMetrics
      Errors: string list
  }
  ```

#### Core Function Changes
- [ ] Rewrite `calculateBatchedTickerSnapshots`:
  ```fsharp
  let calculateBatchedTickerSnapshots context =
      let mutable tickerSnapshots = []
      let mutable currencySnapshots = []
      let mutable latestState = context.CurrencySnapshots
      
      // Process dates chronologically (keep this logic!)
      for date in context.Dates do
          for tickerId in context.TickerIds do
              // Create TickerSnapshot (simple entity)
              let tickerSnapshot = {
                  Base = { Id = 0; Date = date; BrokerAccountId = accountId }
                  TickerId = tickerId
              }
              tickerSnapshots <- tickerSnapshot :: tickerSnapshots
              
              // Calculate TickerCurrencySnapshots (multiple per ticker)
              for currencyId in relevantCurrencies do
                  let currencySnapshot = calculateNewSnapshot(...)
                  currencySnapshots <- currencySnapshot :: currencySnapshots
                  latestState <- latestState.Add((tickerId, currencyId), currencySnapshot)
      
      {
          TickerSnapshots = List.rev tickerSnapshots
          CurrencySnapshots = List.rev currencySnapshots
          Metrics = ...
          Errors = []
      }
  ```

#### Validation
- [ ] Chronological processing preserved (critical for cumulative calculations)
- [ ] In-memory state tracking preserved (good design)
- [ ] Returns separate lists instead of hierarchy

---

## Module 4: TickerSnapshotBatchPersistence.fs

**Estimated Time**: ~30 minutes  
**Lines**: 370+  
**Priority**: LOW (final module in chain)

### Changes Required

#### Core Function Rewrite
- [ ] Rewrite `persistBatchedSnapshots` for two-phase save:
  ```fsharp
  let persistBatchedSnapshots (snapshots: TickerSnapshotBatchResult) =
      async {
          // PHASE 1: Save parent TickerSnapshots, get database IDs
          let! savedTickerSnapshots = 
              snapshots.TickerSnapshots
              |> List.map (fun ts -> TickerSnapshotExtensions.save ts)
              |> Async.Sequential
          
          // Build lookup: (tickerId, date) -> database ID
          let snapshotIdLookup = 
              savedTickerSnapshots
              |> List.map (fun ts -> ((ts.TickerId, ts.Base.Date), ts.Base.Id))
              |> Map.ofList
          
          // PHASE 2: Update TickerCurrencySnapshots with correct FKs
          let currencySnapshotsWithFKs =
              snapshots.CurrencySnapshots
              |> List.map (fun cs ->
                  let parentId = snapshotIdLookup.[(cs.TickerId, cs.Base.Date)]
                  { cs with TickerSnapshotId = parentId }
              )
          
          // PHASE 3: Save child TickerCurrencySnapshots
          let! savedCurrencySnapshots =
              currencySnapshotsWithFKs
              |> List.map (fun cs -> TickerCurrencySnapshotExtensions.save cs)
              |> Async.Sequential
          
          return {
              TickerSnapshotsSaved = savedTickerSnapshots.Length
              CurrencySnapshotsSaved = savedCurrencySnapshots.Length
              // ... metrics
          }
      }
  ```

#### Deduplication Logic
- [ ] Check for existing TickerSnapshots by (tickerId, date)
- [ ] Check for existing TickerCurrencySnapshots by (tickerId, currencyId, date)
- [ ] Preserve database IDs when updating
- [ ] Handle both entity types separately

#### Validation
- [ ] Foreign key assignment correct (TickerSnapshotId)
- [ ] Deduplication works for both entity types
- [ ] Transaction management preserved
- [ ] PersistenceMetrics reports both entity types

---

## Post-Refactoring Validation

### Compilation
- [ ] Run: `dotnet build src/Core/Core.fsproj`
- [ ] Expected: 0 errors (down from ~50)
- [ ] All 5 batch modules compile successfully

### Type Checking
- [ ] No `.MainCurrency` / `.OtherCurrencies` references anywhere
- [ ] All TickerCurrencySnapshot fields match SnapshotsModel definition
- [ ] Foreign key fields present (TickerSnapshotId)
- [ ] Return types correct (separate entities, not hierarchy)

### Logic Verification
- [ ] Chronological processing preserved in calculator
- [ ] In-memory state tracking preserved in calculator
- [ ] Parallel loading preserved in loader
- [ ] Transaction management preserved in persistence
- [ ] Error handling preserved in all modules

### Integration Check
- [ ] TickerSnapshotBatchManager.fs still compiles (orchestrator unchanged)
- [ ] Database extensions still work (no changes needed)
- [ ] Core.fsproj order still correct

---

## Common Pitfalls to Avoid

### 1. Type Ambiguity
**Problem**: FS3566 errors due to overlapping field names  
**Solution**: Use explicit type annotations
```fsharp
// Bad (ambiguous)
let snapshot = { Base = base; TickerId = id }

// Good (explicit)
let snapshot: TickerCurrencySnapshot = { Base = base; TickerId = id; ... }
```

### 2. Array vs List Confusion
**Problem**: F# expects lists but arrays provided  
**Solution**: Use `List.ofArray` or change to list literals
```fsharp
// Bad
let items = [| item1; item2 |]

// Good
let items = [ item1; item2 ]
```

### 3. Missing Fields
**Problem**: Record construction with incomplete fields  
**Solution**: Always include ALL required fields from SnapshotsModel
```fsharp
// Reference the actual type definition
type TickerCurrencySnapshot = {
    Base: BaseSnapshot
    TickerId: int
    CurrencyId: int
    TickerSnapshotId: int  // Don't forget FK!
    // ... all 11 financial fields
}
```

### 4. Foreign Key Assignment
**Problem**: TickerSnapshotId = 0 causes FK constraint violations  
**Solution**: Always update FK after parent save
```fsharp
// Save parent first
let! parentSnapshot = save tickerSnapshot
// Then update child with parent's ID
let childWithFK = { currencySnapshot with TickerSnapshotId = parentSnapshot.Base.Id }
```

---

## Success Criteria

### Before Refactoring
- ❌ ~50 compilation errors
- ❌ Modules assume hierarchical structure
- ❌ Cannot integrate with ImportManager

### After Refactoring
- ✅ 0 compilation errors
- ✅ Modules work with flat+FK structure
- ✅ Ready for ImportManager integration (Task 3.5)
- ✅ Ready for Pfizer test (Task 4.1)

---

**Next Task After Completion**: Task 3.4 - Build and Validate Compilation  
**Following Task**: Task 3.5 - ImportManager Integration  
**End Goal**: Pfizer test passes with correct Options ($175.52) and Realized ($175.52) values
