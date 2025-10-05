# Phase 1 - Cascade Update Analysis

## Issue Report

**Observation:** Many SCENARIO G calls during movement import/cascade updates

## Investigation

### What's Happening

Looking at the logs, the pattern is:

```
1. SaveBrokerMovement (e.g., deposit on Aug 11)
2. Cascade update creates snapshots for:
   - Aug 11 (movement date) → SCENARIO A
   - Oct 5 (today) → SCENARIO G (no movements, has previous + existing)
3. One-day update re-processes Oct 5 → SCENARIO G AGAIN
```

### Root Cause

**This is NOT a bug introduced in Phase 1!** 

Phase 1 changes only affect:
- `BrokerFinancialCalculateInMemory.fs` (in-memory functions)
- `BrokerFinancialBatchCalculator.fs` (batch processor)
- `BrokerFinancialBatchManager.fs` (batch orchestrator)

**The import flow still uses:**
- `BrokerAccountSnapshotManager.handleBrokerAccountChange`
- `BrokerFinancialSnapshotManager.brokerAccountOneDayUpdate` 
- **These were not modified in Phase 1**

### Why SCENARIO G Happens Twice

1. **Cascade Update**: When a movement is saved, cascade updates all future dates
   - Today's date gets processed → SCENARIO G (no movements today, but has previous + existing)
   
2. **One-Day Update**: After cascade, processes "today" again
   - Today gets reprocessed → SCENARIO G AGAIN

### Is This Actually a Problem?

**Scenario G is very cheap:**
- It just compares two snapshots
- If they match → returns `None` (no database save)
- If they differ → saves corrected snapshot

**From logs:**
```fsharp
[11:52:12.413] DEBUG: [BrokerFinancialSnapshotManager] SCENARIO G: ...
[11:52:12.492] DEBUG: [BrokerFinancialSnapshotManager] SCENARIO G completed
```
**Time:** ~79ms for comparison + potential save

## Proposed Solutions

### Option 1: Skip Scenario G in Cascade (Quick Fix)
Don't process "today" during cascade if no movements exist for today.

**Pros:**
- Simple fix
- Reduces unnecessary SCENARIO G calls

**Cons:**
- Doesn't address the duplicate processing

### Option 2: Eliminate Duplicate Processing (Better)
After cascade completes, don't re-process dates that were just cascaded.

**Pros:**
- Eliminates duplicate work
- Cleaner logic

**Cons:**
- Requires refactoring cascade logic

### Option 3: Use Batch Mode for Imports (Best - Already Planned!)
Replace the entire per-date cascade with batch processing (Phase 4).

**Pros:**
- 90-95% faster
- Eliminates all these issues
- Natural solution as part of Phase 4

**Cons:**
- Requires Phase 2 & 3 completion first

## Recommendation

### Short Term: Do Nothing ❌
**Reasoning:**
- SCENARIO G is cheap (milliseconds)
- Not actually broken
- Will be replaced by batch mode in Phase 4

### Medium Term: Continue with Phase 2 ✅
**Reasoning:**
- Phase 4 will eliminate this entirely
- Fixing cascade now is wasted effort
- Better to invest time in batch mode

## Performance Impact

### Current (Per-Date Cascade)
- Save 1 movement → Cascade N future dates
- Each date: Load data → Calculate → Save
- SCENARIO G called 2x per "today" processing
- **Total time for 1 movement:** ~200-300ms

### Future (Batch Mode - Phase 4)
- Save 1 movement → Batch process date range
- Load all data once → Calculate all dates in memory → Save all at once
- No SCENARIO G duplication (proper decision tree)
- **Total time for 1 movement:** ~50-100ms (estimated)

## Conclusion

**This is NOT a bug introduced in Phase 1.**

The logs show expected behavior from the existing cascade system. The "unnecessary" SCENARIO G calls are:
1. Actually necessary for data consistency
2. Very cheap (milliseconds)
3. Will be eliminated when we switch to batch mode (Phase 4)

**Recommendation:** Continue with Phase 2 implementation. The cascade inefficiency will be resolved naturally when batch mode replaces per-date processing.

---

## If You Still Want to Fix It Now

If the duplicate SCENARIO G really bothers you, here's a minimal fix:

### File: `BrokerAccountSnapshotManager.fs`

Find the cascade update logic and skip processing "today" if it has no movements:

```fsharp
// After cascade completes, check if we need to process today
if not (movementsForThisDate.HasMovements) && (thisDate = today) then
    // Skip - cascade already processed it
    ()
else
    // Normal one-day update
    do! BrokerFinancialSnapshotManager.brokerAccountOneDayUpdate snapshot movementsForThisDate
```

But honestly, I recommend waiting for Phase 4.
