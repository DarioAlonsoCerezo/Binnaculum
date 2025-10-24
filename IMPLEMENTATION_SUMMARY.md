# Resumable Import System - Implementation Summary

**Date**: 2025-10-24  
**Status**: Phases 1-2 Complete (Foundation Ready)  
**Next**: Phases 3-6 (Import Logic + Testing)

---

## ✅ What's Been Implemented

### Phase 1: Database Schema (COMPLETE)

**New SQL Query Modules:**
- `SQL/ImportSessionQuery.fs` - 13 operations for session management
- `SQL/ImportSessionChunkQuery.fs` - 8 operations for chunk tracking

**New Database Extensions:**
- `Database/ImportSessionExtensions.fs` - 9 CRUD functions
- `Database/ImportSessionChunkExtensions.fs` - 7 CRUD functions

**Database Models:**
- Added `ImportSession` type to `DatabaseModel.fs`
- Added `ImportSessionChunk` type to `DatabaseModel.fs`

**Schema Updates:**
- Modified `Database.fs` to create tables on startup
- Added indexes for performance

**File Hash Support:**
- Added `calculateFileHash()` to `CsvDateAnalyzer.fs`
- Added `FileHash` field to `DateAnalysis` type

### Phase 2: Session Management (COMPLETE)

**New Module:**
- `Import/ImportSessionManager.fs` - 11 public functions

**Key Functions:**
```fsharp
// Session Lifecycle
createSession: int -> string -> string -> DateAnalysis -> ChunkInfo list -> Task<int>
getActiveSession: int -> Task<ImportSession option>
getSessionById: int -> Task<ImportSession option>

// Chunk Management  
getChunks: int -> Task<ImportSessionChunk list>
getPendingChunks: int -> Task<ImportSessionChunk list>

// Progress Tracking (Transaction-Safe)
markChunkCompleted: int -> int -> int -> int64 -> SqliteTransaction -> Task<unit>

// Phase Transitions
updatePhase: int -> string -> Task<unit>
markPhase1Completed: int -> Task<unit>

// Phase 2 Tracking
markBrokerSnapshotsCompleted: int -> Task<unit>
markTickerSnapshotsCompleted: int -> Task<unit>

// Completion
completeSession: int -> Task<unit>
markSessionFailed: int -> string -> Task<unit>
markSessionCancelled: int -> Task<unit>

// Safety
validateFileHash: ImportSession -> string -> bool
```

---

## 📋 What Still Needs Implementation

### Phase 3: Two-Phase Import Manager (TODO)

**File to Create:** `Import/TwoPhaseImportManager.fs`

**Required Functions:**

```fsharp
module TwoPhaseImportManager =
    
    // ========== PHASE 1: MOVEMENT PERSISTENCE ==========
    
    /// Parse movements for a specific chunk's date range
    let parseMovementsForChunk 
        (filePath: string) 
        (brokerType: SupportedBroker) 
        (chunk: ImportSessionChunk) 
        : Task<ParsedMovements> =
        // 1. Use existing TastytradeStatementParser or IBKRStatementParser
        // 2. Filter movements by chunk.StartDate to chunk.EndDate
        // 3. Return movements for this chunk only
    
    /// Process single chunk with atomic transaction
    let processChunkWithProgress 
        (session: ImportSession) 
        (chunk: ImportSessionChunk) 
        (brokerType: SupportedBroker)
        : Task<Result<int, string>> =
        task {
            let stopwatch = Stopwatch.StartNew()
            
            // 1. Parse movements for this chunk
            let! movements = parseMovementsForChunk(session.FilePath, brokerType, chunk)
            
            // 2. BEGIN TRANSACTION (CRITICAL!)
            use connection = Database.getConnection()
            use transaction = connection.BeginTransaction()
            
            try
                // 3. Insert movements using existing DatabasePersistence
                do! DatabasePersistence.persistMovements(movements, transaction)
                
                // 4. Mark chunk completed IN SAME TRANSACTION
                do! ImportSessionManager.markChunkCompleted(
                    session.Id,
                    chunk.ChunkNumber,
                    movements.Length,
                    stopwatch.ElapsedMilliseconds,
                    transaction
                )
                
                // 5. COMMIT (atomic: movements + progress)
                transaction.Commit()
                
                return Ok movements.Length
            with ex ->
                // ROLLBACK both movements and progress
                transaction.Rollback()
                return Error ex.Message
        }
    
    /// Run Phase 1: Persist all movements in chunks
    let runPhase1_PersistMovements 
        (session: ImportSession) 
        (chunks: ImportSessionChunk list)
        (brokerType: SupportedBroker)
        (cancellationToken: CancellationToken)
        : Task<Result<unit, string>> =
        task {
            for chunk in chunks do
                // Check cancellation
                if cancellationToken.IsCancellationRequested then
                    return Error "Import cancelled by user"
                
                // Process chunk
                let! result = processChunkWithProgress(session, chunk, brokerType)
                
                match result with
                | Ok count ->
                    CoreLogger.logInfof "TwoPhaseImportManager" 
                        "Chunk %d/%d completed: %d movements" 
                        chunk.ChunkNumber session.TotalChunks count
                    
                    // Update reactive UI (if needed)
                    // ReactiveImportManager.updateChunkProgress(...)
                    
                | Error err ->
                    CoreLogger.logError "TwoPhaseImportManager" 
                        $"Chunk {chunk.ChunkNumber} failed: {err}"
                    return Error err
            
            // All chunks done - transition to Phase 2
            do! ImportSessionManager.markPhase1Completed(session.Id)
            return Ok ()
        }
    
    // ========== PHASE 2: SNAPSHOT CALCULATION ==========
    
    /// Run Phase 2: Calculate all snapshots using existing batch managers
    let runPhase2_CalculateSnapshots 
        (session: ImportSession)
        (cancellationToken: CancellationToken)
        : Task<Result<unit, string>> =
        task {
            try
                // Step 1: Broker Financial Snapshots
                CoreLogger.logInfo "TwoPhaseImportManager" "Calculating broker financial snapshots..."
                
                let brokerRequest = {
                    BrokerAccountId = session.BrokerAccountId
                    StartDate = DateTimePattern.Parse(session.MinDate)
                    EndDate = DateTimePattern.Parse(session.MaxDate)
                    ForceRecalculation = false
                }
                
                let! brokerResult = BrokerFinancialBatchManager.processBatchedFinancials(brokerRequest)
                
                if not brokerResult.Success then
                    return Error $"Broker snapshots failed: {String.Join("; ", brokerResult.Errors)}"
                
                do! ImportSessionManager.markBrokerSnapshotsCompleted(session.Id)
                
                // Step 2: Ticker Snapshots
                CoreLogger.logInfo "TwoPhaseImportManager" "Calculating ticker snapshots..."
                
                // Get ticker IDs for this account
                let! tickerIds = getTickerIdsForAccount(session.BrokerAccountId)
                
                let tickerRequest = {
                    BrokerAccountId = Some session.BrokerAccountId
                    TickerIds = tickerIds
                    StartDate = DateTimePattern.Parse(session.MinDate)
                    EndDate = DateTimePattern.Parse(session.MaxDate)
                    ForceRecalculation = false
                }
                
                let! tickerResult = TickerSnapshotBatchManager.processBatchedTickers(tickerRequest)
                
                if not tickerResult.Success then
                    return Error $"Ticker snapshots failed: {String.Join("; ", tickerResult.Errors)}"
                
                do! ImportSessionManager.markTickerSnapshotsCompleted(session.Id)
                
                // Complete session
                do! ImportSessionManager.completeSession(session.Id)
                
                CoreLogger.logInfo "TwoPhaseImportManager" $"Session {session.Id} completed successfully"
                return Ok ()
                
            with ex ->
                CoreLogger.logError "TwoPhaseImportManager" $"Phase 2 failed: {ex.Message}"
                return Error ex.Message
        }
    
    // ========== MAIN ENTRY POINTS ==========
    
    /// Start new import from beginning
    let startImport 
        (brokerAccountId: int) 
        (brokerAccountName: string) 
        (filePath: string) 
        (brokerType: SupportedBroker)
        (cancellationToken: CancellationToken)
        : Task<Result<unit, string>> =
        task {
            try
                // 1. Analyze CSV
                let! analysis = CsvDateAnalyzer.analyzeCsvDates(filePath, brokerType)
                
                // 2. Create chunks
                let chunks = ChunkStrategy.createWeeklyChunks(analysis)
                
                // 3. Create session
                let! sessionId = ImportSessionManager.createSession(
                    brokerAccountId,
                    brokerAccountName,
                    filePath,
                    analysis,
                    chunks
                )
                
                // 4. Load session and chunks
                let! session = ImportSessionManager.getSessionById(sessionId)
                let! chunkRecords = ImportSessionManager.getPendingChunks(sessionId)
                
                match session with
                | Some s ->
                    // 5. Run Phase 1
                    let! phase1Result = runPhase1_PersistMovements(s, chunkRecords, brokerType, cancellationToken)
                    
                    match phase1Result with
                    | Ok () ->
                        // 6. Run Phase 2
                        return! runPhase2_CalculateSnapshots(s, cancellationToken)
                    | Error err -> return Error err
                    
                | None -> return Error "Failed to create session"
                
            with ex ->
                CoreLogger.logError "TwoPhaseImportManager" $"Import failed: {ex.Message}"
                return Error ex.Message
        }
    
    /// Resume existing import from current state
    let resumeImport 
        (sessionId: int) 
        (brokerType: SupportedBroker)
        (cancellationToken: CancellationToken)
        : Task<Result<unit, string>> =
        task {
            try
                // 1. Load session
                let! sessionOpt = ImportSessionManager.getSessionById(sessionId)
                
                match sessionOpt with
                | None -> return Error $"Session {sessionId} not found"
                | Some session ->
                    // 2. Validate file hash
                    if not (ImportSessionManager.validateFileHash(session, session.FilePath)) then
                        do! ImportSessionManager.markSessionFailed(sessionId, "File has been modified")
                        return Error "File has been modified since import started"
                    
                    // 3. Resume based on phase
                    match session.Phase with
                    | "Phase1_PersistingMovements" ->
                        // Resume Phase 1
                        let! pendingChunks = ImportSessionManager.getPendingChunks(sessionId)
                        let! phase1Result = runPhase1_PersistMovements(session, pendingChunks, brokerType, cancellationToken)
                        
                        match phase1Result with
                        | Ok () -> return! runPhase2_CalculateSnapshots(session, cancellationToken)
                        | Error err -> return Error err
                        
                    | "Phase2_CalculatingSnapshots" ->
                        // Resume Phase 2 (safe to re-run)
                        return! runPhase2_CalculateSnapshots(session, cancellationToken)
                        
                    | "Completed" ->
                        return Ok ()  // Already done
                        
                    | _ ->
                        return Error $"Unknown phase: {session.Phase}"
                        
            with ex ->
                CoreLogger.logError "TwoPhaseImportManager" $"Resume failed: {ex.Message}"
                return Error ex.Message
        }
```

### Phase 4: Resume Logic (TODO)

**Integration Points:**

```fsharp
// In App startup or broker account selection:
module AppStartup =
    
    /// Check for active imports on app launch
    let checkForActiveImports (brokerAccountId: int) : Task<unit> =
        task {
            let! activeSession = ImportSessionManager.getActiveSession(brokerAccountId)
            
            match activeSession with
            | Some session ->
                // Show UI prompt: "Resume import?"
                // If user confirms:
                //   do! TwoPhaseImportManager.resumeImport(session.Id, brokerType, cancellationToken)
                ()
            | None -> ()
        }
```

### Phase 5: Reactive Integration (TODO)

**Update ReactiveImportManager.fs:**

```fsharp
// Replace placeholder in ReactiveImportManager.startImport:
let startImport (brokerAccountId: int) (brokerAccountName: string) (filePath: string) =
    task {
        try
            let cts = new CancellationTokenSource()
            currentCancellationSource <- Some cts
            
            // Determine broker type from file or account
            let! brokerType = determineBrokerType(brokerAccountId, filePath)
            
            // Use TwoPhaseImportManager
            let! result = TwoPhaseImportManager.startImport(
                brokerAccountId,
                brokerAccountName,
                filePath,
                brokerType,
                cts.Token
            )
            
            match result with
            | Ok () ->
                updateState (Completed { /* summary */ })
            | Error err ->
                updateState (Failed err)
                
        with ex ->
            updateState (Failed ex.Message)
    }
```

### Phase 6: Testing (TODO)

**Unit Tests to Add:**

```fsharp
[<Test>]
let ``createSession creates session and chunks`` () =
    task {
        // Test session creation
    }

[<Test>]
let ``markChunkCompleted is atomic with movements`` () =
    task {
        // Test transaction rollback
    }

[<Test>]
let ``validateFileHash detects file changes`` () =
    // Test file hash validation
```

**Integration Tests:**

```fsharp
[<Test>]
let ``complete import flow processes all chunks`` () =
    task {
        // End-to-end test
    }

[<Test>]
let ``resume from Phase 1 continues from pending chunks`` () =
    task {
        // Resume scenario test
    }
```

---

## 🎯 Critical Implementation Notes

### 1. Atomic Transaction Pattern

**ALWAYS use this pattern for chunk processing:**

```fsharp
use transaction = connection.BeginTransaction()
try
    do! persistMovements(movements, transaction)
    do! ImportSessionManager.markChunkCompleted(sessionId, chunkNum, count, durationMs, transaction)
    transaction.Commit()
with ex ->
    transaction.Rollback()
    raise ex
```

### 2. File Hash Validation

**ALWAYS validate before resume:**

```fsharp
if not (ImportSessionManager.validateFileHash(session, session.FilePath)) then
    do! ImportSessionManager.markSessionFailed(sessionId, "File modified")
    failwith "File has been modified"
```

### 3. Memory Management

**Release memory after each chunk:**

```fsharp
for chunk in chunks do
    let! movements = parseMovementsForChunk(...)
    do! persistChunk(movements, ...)
    // movements goes out of scope here - GC can collect
```

### 4. Cancellation Handling

**Check before each chunk:**

```fsharp
for chunk in chunks do
    if cancellationToken.IsCancellationRequested then
        return Error "Cancelled"
    // Process chunk
```

---

## 📊 Performance Targets

- **Memory per chunk**: < 5MB
- **Peak memory**: < 20MB
- **Import speed**: > 1000 movements/sec
- **DB write per chunk**: < 100ms
- **Chunk processing**: < 500ms (excluding DB)

---

## 🔗 Key Dependencies

**Existing Modules to Use:**
- `CsvDateAnalyzer` - Date analysis and file hash
- `ChunkStrategy` - Chunk creation
- `TastytradeStatementParser` - Tastytrade CSV parsing
- `IBKRStatementParser` - IBKR CSV parsing
- `DatabasePersistence` - Movement persistence
- `BrokerFinancialBatchManager` - Phase 2 broker snapshots
- `TickerSnapshotBatchManager` - Phase 2 ticker snapshots

**No Changes Needed To:**
- Existing parsers
- Existing batch managers
- Existing database schema (except 2 new tables)
- Existing snapshot logic

---

## ✅ Definition of Done

- [x] Phase 1: Database schema complete
- [x] Phase 2: Session management complete
- [ ] Phase 3: Two-phase import manager
- [ ] Phase 4: Resume logic
- [ ] Phase 5: Reactive integration
- [ ] Phase 6: Testing
- [ ] All tests pass
- [ ] Performance targets met
- [ ] Documentation updated

---

**Current Status**: Foundational infrastructure complete (931 lines). Ready for Phase 3-6 implementation.

**Estimated Remaining Effort**: 2-3 days for experienced F# developer
