# Resumable Import System - Implementation Plan

**Project**: Binnaculum - Two-Phase Resumable Import with Progress Tracking  
**Created**: 2025-10-24  
**Status**: 🚧 In Progress  
**GitHub Issue**: [#420](https://github.com/DarioAlonsoCerezo/Binnaculum/issues/420)

---

## 📋 Implementation Progress

### Phase 1: Database Schema 📝 TODO
- [ ] Create `ImportSession` table schema
- [ ] Create `ImportSessionChunk` table schema
- [ ] Add database indexes
- [ ] Create SQL query modules
- [ ] Create database extension modules
- [ ] Complete Database.fs integration
- [ ] Add files to Core.fsproj
- [ ] Build and test database schema

### Phase 2: Session Management 📝 TODO
- [ ] Create `ImportSessionManager.fs`
- [ ] Implement session CRUD operations
- [ ] Implement chunk CRUD operations
- [ ] Add file hash calculation to `CsvDateAnalyzer`
- [ ] Test session management

### Phase 3: Phase 1 Implementation (Movement Persistence) 📝 TODO
- [ ] Create `TwoPhaseImportManager.fs`
- [ ] Implement chunked movement persistence
- [ ] Implement atomic transaction boundaries
- [ ] Implement progress updates (atomic with movements)
- [ ] Add error handling and retry logic
- [ ] Test Phase 1 flow

### Phase 4: Phase 2 Implementation (Snapshot Calculation) 📝 TODO
- [ ] Integrate existing `BrokerFinancialBatchManager`
- [ ] Integrate existing `TickerSnapshotBatchManager`
- [ ] Track Phase 2 sub-step completion
- [ ] Test Phase 2 flow

### Phase 5: Resume Logic 📝 TODO
- [ ] Implement session resume detection
- [ ] Implement Phase 1 resume (from pending chunks)
- [ ] Implement Phase 2 resume (recalculate snapshots)
- [ ] Add file hash validation on resume
- [ ] Test resume scenarios

### Phase 6: Reactive Integration 📝 TODO
- [ ] Update `ReactiveImportManager.fs`
- [ ] Integrate two-phase flow with reactive progress
- [ ] Add cancellation support
- [ ] Add pause/resume UI state tracking
- [ ] Test reactive updates

### Phase 7: MAUI Lifecycle Integration 📝 TODO
- [ ] Add app lifecycle hooks (OnSleep/OnResume)
- [ ] Implement graceful pause on background
- [ ] Implement auto-resume on foreground
- [ ] Add user confirmation for resume
- [ ] Test app lifecycle scenarios

### Phase 8: Testing & Validation 📝 TODO
- [ ] Unit tests for session management
- [ ] Integration tests for Phase 1
- [ ] Integration tests for Phase 2
- [ ] Resume scenario tests
- [ ] Performance tests (large imports)
- [ ] Memory pressure tests
- [ ] App lifecycle tests

---

## 🎯 Core Concept

### Two-Phase Import Strategy

```
PHASE 1: Persist Movements (Chunked for Memory Safety)
======================================================
├─ Parse CSV in chunks (max 2000 movements per chunk)
├─ Persist movements to database (transactional)
├─ Save progress after each chunk (atomic with movements)
└─ All movements in database before Phase 2 starts

PHASE 2: Calculate Snapshots (Using Existing Batch Logic)
==========================================================
├─ Use existing BrokerFinancialBatchManager
├─ Use existing TickerSnapshotBatchManager
├─ Calculate all snapshots from movements in database
└─ Track completion flags in ImportSession

RESUME: Can Resume at Any Phase
================================
├─ Phase 1 interrupted → Resume from next pending chunk
├─ Phase 2 interrupted → Re-run snapshot calculation (safe)
└─ File validated by hash before resume
```

### Why Two Phases?

1. **Separation of Concerns**: CSV parsing separate from snapshot calculations
2. **Reuse Existing Code**: Phase 2 uses proven batch managers (no changes needed!)
3. **Better Resumability**: Clear phase boundaries for interruption handling
4. **Memory Safety**: Phase 1 chunks keep memory usage under 5MB per chunk

---

## 🗄️ Database Schema

### Table 1: ImportSession

Tracks overall import session state and progress.

```sql
CREATE TABLE ImportSession (
    -- Primary Key
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    
    -- Import Context
    BrokerAccountId INTEGER NOT NULL,
    BrokerAccountName TEXT NOT NULL,
    FileName TEXT NOT NULL,
    FilePath TEXT NOT NULL,
    FileHash TEXT NOT NULL,  -- MD5/SHA256 to detect file changes
    
    -- State Tracking
    State TEXT NOT NULL,  -- 'Analyzing', 'Phase1_PersistingMovements', 'Phase2_CalculatingSnapshots', 'Completed', 'Failed', 'Cancelled'
    Phase TEXT NOT NULL DEFAULT 'Phase1_PersistingMovements',
    
    -- Phase 1 Progress
    TotalChunks INTEGER NOT NULL DEFAULT 0,
    ChunksCompleted INTEGER NOT NULL DEFAULT 0,
    MovementsPersisted INTEGER NOT NULL DEFAULT 0,
    
    -- Phase 2 Progress Flags
    BrokerSnapshotsCalculated INTEGER NOT NULL DEFAULT 0,  -- 0 or 1
    TickerSnapshotsCalculated INTEGER NOT NULL DEFAULT 0,  -- 0 or 1
    
    -- Date Range
    MinDate TEXT NOT NULL,
    MaxDate TEXT NOT NULL,
    TotalEstimatedMovements INTEGER NOT NULL DEFAULT 0,
    
    -- Timestamps
    StartedAt TEXT NOT NULL,
    Phase1CompletedAt TEXT,
    Phase2StartedAt TEXT,
    CompletedAt TEXT,
    LastProgressUpdateAt TEXT,
    
    -- Error Tracking
    LastError TEXT,
    
    -- Metadata
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    
    FOREIGN KEY (BrokerAccountId) REFERENCES BrokerAccount(Id) ON DELETE CASCADE
);

-- Indexes for performance
CREATE INDEX idx_importsession_state ON ImportSession(State);
CREATE INDEX idx_importsession_brokeraccount ON ImportSession(BrokerAccountId);
CREATE INDEX idx_importsession_brokeraccount_state ON ImportSession(BrokerAccountId, State);
```

**Key Fields Explained**:
- `FileHash`: MD5/SHA256 of CSV file - validates file hasn't changed on resume
- `State` vs `Phase`: State is user-facing, Phase tracks internal processing step
- `ChunksCompleted` / `TotalChunks`: Progress tracking for UI (e.g., "5/10 chunks")
- `MovementsPersisted`: Running total of movements saved (for progress percentage)
- `BrokerSnapshotsCalculated` / `TickerSnapshotsCalculated`: Phase 2 sub-step flags

### Table 2: ImportSessionChunk

Tracks individual chunk processing state and results.

```sql
CREATE TABLE ImportSessionChunk (
    -- Primary Key
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ImportSessionId INTEGER NOT NULL,
    ChunkNumber INTEGER NOT NULL,
    
    -- Chunk Definition
    StartDate TEXT NOT NULL,
    EndDate TEXT NOT NULL,
    EstimatedMovements INTEGER NOT NULL,
    
    -- State Tracking
    State TEXT NOT NULL DEFAULT 'Pending',  -- 'Pending', 'InProgress', 'Completed', 'Failed'
    
    -- Results
    ActualMovements INTEGER DEFAULT 0,
    
    -- Timing
    StartedAt TEXT,
    CompletedAt TEXT,
    DurationMs INTEGER,
    
    -- Error Tracking
    Error TEXT,
    
    -- Metadata
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UpdatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    
    FOREIGN KEY (ImportSessionId) REFERENCES ImportSession(Id) ON DELETE CASCADE,
    UNIQUE(ImportSessionId, ChunkNumber)
);

-- Indexes for performance
CREATE INDEX idx_chunk_session ON ImportSessionChunk(ImportSessionId);
CREATE INDEX idx_chunk_session_state ON ImportSessionChunk(ImportSessionId, State);
```

**Key Fields Explained**:
- `ChunkNumber`: Sequential (1, 2, 3...) for ordering
- `StartDate` / `EndDate`: Date range this chunk covers (e.g., 2025-01-01 to 2025-01-07)
- `EstimatedMovements`: From date analysis, actual may differ
- `ActualMovements`: Real count after processing (for verification)
- `DurationMs`: Performance tracking per chunk

---

## 🔄 Import Flow Diagram

### Complete Import Flow

```
┌─────────────────────────────────────────────────────────┐
│ START IMPORT                                            │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ 1. ANALYZE CSV (Fast, No DB Changes)                   │
│    - CsvDateAnalyzer.analyzeCsvDates()                  │
│    - Parse dates only (lightweight)                     │
│    - Count movements per date                           │
│    - Calculate file hash (MD5/SHA256)                   │
│    Result: DateAnalysis { TotalMovements, MinDate... } │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ 2. CREATE CHUNKS (In-Memory, No DB Changes)            │
│    - ChunkStrategy.createWeeklyChunks()                 │
│    - Split into weekly chunks (adaptive sizing)         │
│    - Max 2000 movements per chunk (mobile-safe)         │
│    Result: List<ChunkInfo>                              │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ 3. CREATE SESSION IN DATABASE                          │
│    - INSERT INTO ImportSession (...)                    │
│    - INSERT INTO ImportSessionChunk (...) for all chunks│
│    - State = 'Phase1_PersistingMovements'               │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ PHASE 1: PERSIST MOVEMENTS (Chunked)                   │
│                                                         │
│ FOR EACH CHUNK:                                         │
│   ┌───────────────────────────────────────────┐       │
│   │ 1. Parse movements from CSV for chunk     │       │
│   │ 2. BEGIN TRANSACTION                       │       │
│   │    ├─ INSERT movements (BrokerMovement,   │       │
│   │    │   OptionTrade, etc.)                  │       │
│   │    ├─ UPDATE ImportSessionChunk            │       │
│   │    │    SET State='Completed'              │       │
│   │    │        ActualMovements=X              │       │
│   │    │        CompletedAt=now                │       │
│   │    ├─ UPDATE ImportSession                 │       │
│   │    │    SET ChunksCompleted++              │       │
│   │    │        MovementsPersisted+=X          │       │
│   │    │        LastProgressUpdateAt=now       │       │
│   │ 3. COMMIT (Atomic: Movements + Progress!)  │       │
│   └───────────────────────────────────────────┘       │
│   4. Release memory ✅                                  │
│   5. Update reactive UI progress                        │
│                                                         │
│   🛑 SAFE CANCELLATION POINT                           │
│      - All completed chunks saved in DB                 │
│      - Can resume from next pending chunk               │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ PHASE 1 COMPLETE                                        │
│ UPDATE ImportSession                                    │
│    SET Phase='Phase2_CalculatingSnapshots'              │
│        State='Phase2_CalculatingSnapshots'              │
│        Phase1CompletedAt=now                            │
│        Phase2StartedAt=now                              │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ PHASE 2: CALCULATE SNAPSHOTS (Existing Batch Logic)    │
│                                                         │
│ Step 1: Calculate Broker Snapshots                     │
│    - BrokerFinancialBatchManager.processRecentImports() │
│    - UPDATE ImportSession                               │
│         SET BrokerSnapshotsCalculated=1                 │
│             LastProgressUpdateAt=now                    │
│                                                         │
│ Step 2: Calculate Ticker Snapshots + Operations        │
│    - TickerSnapshotBatchManager.processRecentImports()  │
│    - UPDATE ImportSession                               │
│         SET TickerSnapshotsCalculated=1                 │
│             LastProgressUpdateAt=now                    │
│                                                         │
│ 🛑 SAFE CANCELLATION POINT                             │
│    - All movements already in DB                        │
│    - Just re-run Phase 2 on resume (idempotent)         │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ IMPORT COMPLETE                                         │
│ UPDATE ImportSession                                    │
│    SET State='Completed'                                │
│        CompletedAt=now                                  │
└─────────────────────────────────────────────────────────┘
```

### Resume Flow

```
┌─────────────────────────────────────────────────────────┐
│ APP RESUMES (After Background/Crash)                    │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ 1. CHECK FOR ACTIVE IMPORTS                            │
│    SELECT * FROM ImportSession                          │
│    WHERE BrokerAccountId = ?                            │
│      AND State IN ('Phase1...', 'Phase2...')            │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ 2. LOAD SESSION + CHUNKS                               │
│    SELECT * FROM ImportSession WHERE Id = ?             │
│    SELECT * FROM ImportSessionChunk WHERE SessionId = ? │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ 3. VALIDATE FILE (Hash Check)                          │
│    - Calculate current file hash                        │
│    - Compare with session.FileHash                      │
│    - If different: ABORT (file changed)                 │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ 4. RESUME BASED ON PHASE                               │
│                                                         │
│ IF Phase = 'Phase1_PersistingMovements':               │
│   ├─ Get pending chunks (State='Pending' or 'Failed')  │
│   ├─ Continue processing from first pending chunk       │
│   ├─ Complete Phase 1                                   │
│   └─ Proceed to Phase 2                                 │
│                                                         │
│ IF Phase = 'Phase2_CalculatingSnapshots':              │
│   ├─ All movements already in DB ✅                     │
│   ├─ Check BrokerSnapshotsCalculated flag              │
│   ├─ Check TickerSnapshotsCalculated flag              │
│   ├─ Re-run incomplete steps                            │
│   └─ Complete                                           │
│                                                         │
│ IF Phase = 'Completed':                                │
│   └─ Nothing to do (already finished)                   │
└─────────────────────────────────────────────────────────┘
```

---

## 🛡️ Transaction Boundaries - CRITICAL!

### ⚠️ MOST IMPORTANT RULE: Atomic Progress Updates

**The Problem:**
If movements are saved in one transaction and progress in another, a crash between them causes duplicate data on resume.

**❌ WRONG: Separate Transactions (Will Cause Duplicates)**
```fsharp
// Transaction 1: Save movements
do! persistMovements(movements)  // COMMIT happens here

// App crashes here! 💥

// Transaction 2: Save progress (NEVER HAPPENS)
do! markChunkCompleted(chunkId)
```

**Result on Resume:**
- Chunk still marked "Pending" in database
- Chunk re-processed → movements inserted again
- **DUPLICATE DATA!** 🔴

**✅ CORRECT: Single Atomic Transaction**
```fsharp
use transaction = Database.beginTransaction()

// Both movements AND progress in SAME transaction
do! persistMovements(movements, transaction)
do! markChunkCompleted(chunkId, transaction)

// Commit both together (atomic)
transaction.Commit()

// App crashes here? 💥
// BOTH rolled back automatically → safe retry ✅
```

**Result on Resume:**
- Transaction automatically rolled back
- Chunk still marked "Pending"
- Chunk safely re-processed → no duplicates ✅

### Why This Is Critical

```
Scenario: Processing Chunk 5 (500 movements)

❌ Wrong Approach (Separate Transactions):
├─ Transaction 1: INSERT 500 movements → COMMIT
├─ App crashes here 💥
├─ Transaction 2: UPDATE chunk progress → NEVER HAPPENS
├─ On resume: Chunk 5 still marked "Pending"
└─ Result: Re-process chunk 5 → 1000 movements in DB! 🔴

✅ Correct Approach (Atomic Transaction):
├─ Transaction: BEGIN
│   ├─ INSERT 500 movements
│   └─ UPDATE chunk progress to "Completed"
├─ App crashes here 💥
├─ Transaction: AUTOMATIC ROLLBACK
├─ On resume: Chunk 5 still marked "Pending"
└─ Result: Re-process chunk 5 → 500 movements in DB! ✅
```

### Implementation Example

```fsharp
/// Process single chunk with atomic transaction
let processChunkWithProgress (session: ImportSession) (chunk: ImportSessionChunk) =
    task {
        // 1. Parse movements from CSV for this chunk
        let movements = parseMovementsForChunk(session.FilePath, chunk)
        
        // 2. BEGIN TRANSACTION (CRITICAL!)
        use connection = Database.getConnection()
        use transaction = connection.BeginTransaction()
        
        try
            // 3. Insert movements (within transaction)
            do! DatabasePersistence.persistMovements(movements, transaction)
            
            // 4. Mark chunk completed (within SAME transaction)
            do! ImportSessionManager.markChunkCompleted(
                session.Id, 
                chunk.ChunkNumber, 
                movements.Length,
                transaction)  // ← Pass transaction!
            
            // 5. COMMIT (atomic: both movements and progress saved together)
            transaction.Commit()
            
            CoreLogger.logInfo "ChunkProcessor" 
                $"✅ Chunk {chunk.ChunkNumber} committed: {movements.Length} movements"
            
            return Ok movements.Length
            
        with ex ->
            // 6. ROLLBACK (both movements and progress rolled back)
            transaction.Rollback()
            
            CoreLogger.logError "ChunkProcessor" 
                $"❌ Chunk {chunk.ChunkNumber} failed, rolled back: {ex.Message}"
            
            return Error ex.Message
    }
```

---

## 📊 Key Database Operations

### 1. Create Import Session

```sql
-- Insert session
INSERT INTO ImportSession (
    BrokerAccountId,
    BrokerAccountName,
    FileName,
    FilePath,
    FileHash,
    State,
    Phase,
    TotalChunks,
    MinDate,
    MaxDate,
    TotalEstimatedMovements,
    StartedAt
) VALUES (
    @accountId,
    @accountName,
    @fileName,
    @filePath,
    @fileHash,
    'Analyzing',
    'Phase1_PersistingMovements',
    @totalChunks,
    @minDate,
    @maxDate,
    @totalMovements,
    datetime('now')
);

-- Get session ID
SELECT last_insert_rowid();

-- Insert all chunks
INSERT INTO ImportSessionChunk (
    ImportSessionId,
    ChunkNumber,
    StartDate,
    EndDate,
    EstimatedMovements,
    State
) VALUES (?, ?, ?, ?, ?, 'Pending');
-- Repeat for each chunk
```

### 2. Mark Chunk Completed (Within Transaction)

```sql
-- Update chunk state
UPDATE ImportSessionChunk 
SET State = 'Completed',
    ActualMovements = @movementCount,
    CompletedAt = datetime('now'),
    DurationMs = (julianday('now') - julianday(StartedAt)) * 86400000,
    UpdatedAt = datetime('now')
WHERE Id = @chunkId;

-- Update session progress (SAME TRANSACTION!)
UPDATE ImportSession 
SET ChunksCompleted = ChunksCompleted + 1,
    MovementsPersisted = MovementsPersisted + @movementCount,
    LastProgressUpdateAt = datetime('now'),
    UpdatedAt = datetime('now')
WHERE Id = @sessionId;
```

### 3. Transition to Phase 2

```sql
UPDATE ImportSession 
SET Phase = 'Phase2_CalculatingSnapshots',
    State = 'Phase2_CalculatingSnapshots',
    Phase1CompletedAt = datetime('now'),
    Phase2StartedAt = datetime('now'),
    UpdatedAt = datetime('now')
WHERE Id = @sessionId;
```

### 4. Mark Phase 2 Sub-Step Completed

```sql
-- After broker snapshots calculated
UPDATE ImportSession 
SET BrokerSnapshotsCalculated = 1,
    LastProgressUpdateAt = datetime('now'),
    UpdatedAt = datetime('now')
WHERE Id = @sessionId;

-- After ticker snapshots calculated
UPDATE ImportSession 
SET TickerSnapshotsCalculated = 1,
    LastProgressUpdateAt = datetime('now'),
    UpdatedAt = datetime('now')
WHERE Id = @sessionId;
```

### 5. Mark Import Completed

```sql
UPDATE ImportSession 
SET State = 'Completed',
    CompletedAt = datetime('now'),
    UpdatedAt = datetime('now')
WHERE Id = @sessionId;
```

### 6. Load Session for Resume

```sql
-- Get active session for account
SELECT * FROM ImportSession 
WHERE BrokerAccountId = @accountId 
  AND State IN ('Phase1_PersistingMovements', 'Phase2_CalculatingSnapshots')
LIMIT 1;

-- Get all chunks for session
SELECT * FROM ImportSessionChunk
WHERE ImportSessionId = @sessionId
ORDER BY ChunkNumber;

-- Get pending chunks only (for resume)
SELECT * FROM ImportSessionChunk
WHERE ImportSessionId = @sessionId
  AND State IN ('Pending', 'Failed')
ORDER BY ChunkNumber;
```

---

## 📁 File Structure

### Files to Create 📝

```
src/Core/
├─ SQL/
│  ├─ ImportSessionQuery.fs              📝 TODO
│  └─ ImportSessionChunkQuery.fs         📝 TODO
│
├─ Database/
│  ├─ ImportSessionExtensions.fs         📝 TODO
│  └─ ImportSessionChunkExtensions.fs    📝 TODO
│
├─ Import/
│  ├─ ImportSessionManager.fs            📝 TODO
│  └─ TwoPhaseImportManager.fs           📝 TODO
```

### Files to Update 🔧

```
src/Core/
├─ Database/
│  └─ Database.fs                        🔧 UPDATE (add table creation)
│
├─ Import/
│  ├─ CsvDateAnalyzer.fs                 🔧 UPDATE (add file hash)
│  └─ ReactiveImportManager.fs           🔧 UPDATE (integrate two-phase)
│
└─ Core.fsproj                           🔧 UPDATE (add new files)
```

### Existing Files (Already Implemented - DO NOT MODIFY) ✅

```
src/Core/Import/
├─ ImportState.fs                        ✅ State types (ChunkProgress, etc.)
├─ ChunkStrategy.fs                      ✅ Chunking logic
└─ CsvDateAnalyzer.fs                    ✅ CSV date parsing (needs hash added)

src/Core/Snapshots/
├─ BrokerFinancialBatchManager.fs        ✅ Phase 2 - Broker snapshots
└─ TickerSnapshotBatchManager.fs         ✅ Phase 2 - Ticker snapshots
```

---

## 🔧 Implementation Guide

### Phase 1: Complete Database Schema

**Status**: 📝 TODO (0/8 tasks done)

**Tasks**:
1. Update `Database.fs` to create tables
2. Add files to `Core.fsproj`
3. Build and verify

**File: `src/Core/Database/Database.fs`**

Find the `createTables` function and add:

```fsharp
let createTables () =
    task {
        // ... existing tables ...
        
        // Add new import session tables
        do! ImportSessionExtensions.createImportSessionTable()
        do! ImportSessionChunkExtensions.createImportSessionChunkTable()
    }
```

**File: `src/Core/Core.fsproj`**

Add new files in dependency order:

```xml
<!-- SQL Queries -->
<Compile Include="SQL\ImportSessionQuery.fs" />
<Compile Include="SQL\ImportSessionChunkQuery.fs" />

<!-- Database Extensions -->
<Compile Include="Database\ImportSessionExtensions.fs" />
<Compile Include="Database\ImportSessionChunkExtensions.fs" />

<!-- Import Managers (add later in Phase 2) -->
<Compile Include="Import\ImportSessionManager.fs" />
<Compile Include="Import\TwoPhaseImportManager.fs" />
```

**Build and Test**:
```bash
dotnet build src/Core/Core.fsproj
dotnet test src/Tests/Core.Tests/Core.Tests.fsproj
```

---

### Phase 2: Session Management

**File: `src/Core/Import/ImportSessionManager.fs`**

Create session management module:

```fsharp
namespace Binnaculum.Core.Import

open System
open System.Threading.Tasks
open Microsoft.Data.Sqlite
open Binnaculum.Core.Database
open Binnaculum.Core.Logging

/// <summary>
/// Import session management with database persistence.
/// Handles session lifecycle and chunk tracking.
/// </summary>
module ImportSessionManager =
    
    /// Create new import session with chunks
    let createSession 
        (accountId: int) 
        (accountName: string)
        (filePath: string) 
        (fileHash: string)
        (analysis: DateAnalysis) 
        (chunks: ChunkInfo list) 
        : Task<int> =
        task {
            // 1. Insert ImportSession
            let! sessionId = ImportSessionExtensions.insertSession(
                accountId, accountName, filePath, fileHash, analysis)
            
            // 2. Insert all chunks
            for i, chunk in chunks |> List.indexed do
                do! ImportSessionChunkExtensions.insertChunk(sessionId, i + 1, chunk)
            
            CoreLogger.logInfo "ImportSessionManager" 
                $"✅ Created session {sessionId} with {chunks.Length} chunks"
            
            return sessionId
        }
    
    /// Get active session for account
    let getActiveSession (accountId: int) : Task<ImportSession option> =
        task {
            return! ImportSessionExtensions.getActiveSession(accountId)
        }
    
    /// Get session by ID
    let getSessionById (sessionId: int) : Task<ImportSession> =
        task {
            return! ImportSessionExtensions.getSessionById(sessionId)
        }
    
    /// Mark chunk completed (MUST be called within transaction)
    let markChunkCompleted 
        (sessionId: int) 
        (chunkNumber: int) 
        (movementsCount: int)
        (transaction: SqliteTransaction) 
        : Task<unit> =
        task {
            // Update chunk state (within transaction)
            do! ImportSessionChunkExtensions.markChunkCompleted(
                sessionId, chunkNumber, movementsCount, transaction)
            
            // Update session progress (within SAME transaction)
            do! ImportSessionExtensions.incrementProgress(
                sessionId, movementsCount, transaction)
        }
    
    /// Update session phase
    let updateSessionPhase (sessionId: int) (newPhase: string) : Task<unit> =
        task {
            do! ImportSessionExtensions.updatePhase(sessionId, newPhase)
        }
    
    /// Mark Phase 2 step completed
    let markPhase2StepCompleted (sessionId: int) (stepName: string) : Task<unit> =
        task {
            do! ImportSessionExtensions.markPhase2Step(sessionId, stepName)
        }
    
    /// Complete import session
    let completeSession (sessionId: int) : Task<unit> =
        task {
            do! ImportSessionExtensions.markCompleted(sessionId)
        }
    
    /// Get pending chunks for resume
    let getPendingChunks (sessionId: int) : Task<ImportSessionChunk list> =
        task {
            return! ImportSessionChunkExtensions.getPendingChunks(sessionId)
        }
    
    /// Get all chunks for session
    let getChunks (sessionId: int) : Task<ImportSessionChunk list> =
        task {
            return! ImportSessionChunkExtensions.getChunks(sessionId)
        }
```

**Add File Hash to CsvDateAnalyzer**:

```fsharp
// In CsvDateAnalyzer.fs
open System.Security.Cryptography
open System.IO

/// Calculate MD5 hash of file
let calculateFileHash (filePath: string) : string =
    use md5 = MD5.Create()
    use stream = File.OpenRead(filePath)
    let hash = md5.ComputeHash(stream)
    BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()

// Update DateAnalysis type
type DateAnalysis = {
    MinDate: DateTime
    MaxDate: DateTime
    TotalMovements: int
    MovementsByDate: Map<DateOnly, int>
    UniqueDates: DateOnly list
    FileHash: string  // ADD THIS
}
```

---

### Phase 3: Two-Phase Import Manager

**File: `src/Core/Import/TwoPhaseImportManager.fs`**

Main import orchestrator:

```fsharp
namespace Binnaculum.Core.Import

open System
open System.Threading.Tasks
open Binnaculum.Core.Database
open Binnaculum.Core.Logging

/// <summary>
/// Two-phase import manager with resumable chunk processing.
/// Phase 1: Persist movements (chunked)
/// Phase 2: Calculate snapshots (batch)
/// </summary>
module TwoPhaseImportManager =
    
    /// Process single chunk with atomic transaction
    let processChunkWithProgress 
        (session: ImportSession) 
        (chunk: ImportSessionChunk) 
        : Task<Result<int, string>> =
        task {
            try
                // 1. Parse movements from CSV
                let movements = parseMovementsForChunk(session.FilePath, chunk)
                
                // 2. BEGIN TRANSACTION
                use connection = Database.getConnection()
                use transaction = connection.BeginTransaction()
                
                try
                    // 3. Insert movements (within transaction)
                    do! DatabasePersistence.persistMovements(movements, transaction)
                    
                    // 4. Mark chunk completed (within SAME transaction)
                    do! ImportSessionManager.markChunkCompleted(
                        session.Id, 
                        chunk.ChunkNumber, 
                        movements.Length,
                        transaction)
                    
                    // 5. COMMIT (atomic: movements + progress)
                    transaction.Commit()
                    
                    CoreLogger.logInfo "TwoPhaseImport" 
                        $"✅ Chunk {chunk.ChunkNumber}/{session.TotalChunks}: {movements.Length} movements"
                    
                    return Ok movements.Length
                    
                with ex ->
                    // ROLLBACK both movements and progress
                    transaction.Rollback()
                    return Error ex.Message
                    
            with ex ->
                return Error ex.Message
        }
    
    /// Run Phase 1: Persist all movements
    let runPhase1_PersistMovements 
        (session: ImportSession) 
        (chunks: ImportSessionChunk list) 
        : Task<unit> =
        task {
            CoreLogger.logInfo "Phase1" 
                $"Starting Phase 1: {chunks.Length} chunks to process"
            
            for chunk in chunks do
                // TODO: Check cancellation token
                
                let! result = processChunkWithProgress(session, chunk)
                
                match result with
                | Ok count ->
                    // Update reactive UI here
                    ()
                | Error msg ->
                    CoreLogger.logError "Phase1" $"Chunk {chunk.ChunkNumber} failed: {msg}"
            
            // Transition to Phase 2
            do! ImportSessionManager.updateSessionPhase(session.Id, "Phase2_CalculatingSnapshots")
            
            CoreLogger.logInfo "Phase1" "✅ Phase 1 complete"
        }
    
    /// Run Phase 2: Calculate snapshots
    let runPhase2_CalculateSnapshots (session: ImportSession) : Task<unit> =
        task {
            CoreLogger.logInfo "Phase2" "Starting Phase 2: Calculating snapshots"
            
            // Step 1: Broker snapshots
            do! BrokerFinancialBatchManager.processRecentImports(session.BrokerAccountId)
            do! ImportSessionManager.markPhase2StepCompleted(session.Id, "BrokerSnapshots")
            
            // Step 2: Ticker snapshots
            let! tickerIds = getTickerIdsForAccount(session.BrokerAccountId)
            do! TickerSnapshotBatchManager.processRecentImports(session.BrokerAccountId, tickerIds)
            do! ImportSessionManager.markPhase2StepCompleted(session.Id, "TickerSnapshots")
            
            // Complete
            do! ImportSessionManager.completeSession(session.Id)
            
            CoreLogger.logInfo "Phase2" "✅ Phase 2 complete"
        }
    
    /// Start new import
    let startImport 
        (accountId: int) 
        (accountName: string) 
        (filePath: string) 
        : Task<unit> =
        task {
            // 1. Analyze CSV
            let! analysis = CsvDateAnalyzer.analyzeCsvDates(filePath, brokerType)
            
            // 2. Create chunks
            let chunks = ChunkStrategy.createWeeklyChunks(analysis)
            
            // 3. Create session
            let! sessionId = ImportSessionManager.createSession(
                accountId, accountName, filePath, analysis.FileHash, analysis, chunks)
            
            let! session = ImportSessionManager.getSessionById(sessionId)
            let! chunkList = ImportSessionManager.getChunks(sessionId)
            
            // 4. Run Phase 1
            do! runPhase1_PersistMovements(session, chunkList)
            
            // 5. Run Phase 2
            do! runPhase2_CalculateSnapshots(session)
        }
    
    /// Resume existing import
    let resumeImport (sessionId: int) : Task<unit> =
        task {
            // 1. Load session
            let! session = ImportSessionManager.getSessionById(sessionId)
            
            // 2. Validate file hash
            let currentHash = CsvDateAnalyzer.calculateFileHash(session.FilePath)
            if currentHash <> session.FileHash then
                failwith "File has been modified since import started"
            
            // 3. Resume based on phase
            match session.Phase with
            | "Phase1_PersistingMovements" ->
                let! pendingChunks = ImportSessionManager.getPendingChunks(sessionId)
                do! runPhase1_PersistMovements(session, pendingChunks)
                do! runPhase2_CalculateSnapshots(session)
                
            | "Phase2_CalculatingSnapshots" ->
                do! runPhase2_CalculateSnapshots(session)
                
            | "Completed" ->
                CoreLogger.logInfo "Resume" "Import already completed"
                
            | _ ->
                failwith $"Unknown phase: {session.Phase}"
        }
```

---

## 🧪 Testing Strategy

### Unit Tests

Create `ImportSessionTests.fs`:

```fsharp
[<Test>]
let ``createSession creates session and chunks`` () =
    task {
        // Arrange
        let analysis = createTestAnalysis()
        let chunks = createTestChunks()
        
        // Act
        let! sessionId = ImportSessionManager.createSession(
            1, "Test Account", "test.csv", "hash123", analysis, chunks)
        
        // Assert
        let! session = ImportSessionManager.getSessionById(sessionId)
        let! savedChunks = ImportSessionManager.getChunks(sessionId)
        
        Assert.AreEqual("Phase1_PersistingMovements", session.Phase)
        Assert.AreEqual(chunks.Length, savedChunks.Length)
    }

[<Test>]
let ``markChunkCompleted is atomic with movements`` () =
    task {
        // This tests the CRITICAL atomic transaction requirement
        
        use connection = Database.getConnection()
        use transaction = connection.BeginTransaction()
        
        // Insert test movements
        do! insertTestMovements(transaction)
        
        // Mark chunk completed
        do! ImportSessionManager.markChunkCompleted(sessionId, 1, 100, transaction)
        
        // Rollback (simulate crash)
        transaction.Rollback()
        
        // Verify both movements AND progress were rolled back
        let! movements = getMovements()
        let! session = ImportSessionManager.getSessionById(sessionId)
        
        Assert.AreEqual(0, movements.Length)  // Movements rolled back
        Assert.AreEqual(0, session.ChunksCompleted)  // Progress rolled back
    }
```

### Integration Tests

```fsharp
[<Test>]
let ``complete import flow processes all chunks`` () =
    task {
        // Act
        do! TwoPhaseImportManager.startImport(accountId, accountName, csvFilePath)
        
        // Assert
        let! movements = getMovements(accountId)
        Assert.IsTrue(movements.Length > 0)
        
        let! session = ImportSessionManager.getActiveSession(accountId)
        Assert.IsNone(session)  // No active session (completed)
    }

[<Test>]
let ``resume from Phase 1 continues from pending chunks`` () =
    task {
        // Arrange: Create partially completed session
        let! sessionId = createPartialSession(chunksCompleted = 3, totalChunks = 10)
        
        // Act
        do! TwoPhaseImportManager.resumeImport(sessionId)
        
        // Assert
        let! session = ImportSessionManager.getSessionById(sessionId)
        Assert.AreEqual(10, session.ChunksCompleted)
        Assert.AreEqual("Completed", session.State)
    }
```

---

## 📊 Performance Targets

- **Memory per chunk**: < 5MB
- **Peak memory during import**: < 20MB total
- **Import speed**: 1000 movements/second minimum
- **Database write per chunk**: < 100ms
- **Chunk processing**: < 500ms per chunk (excluding DB I/O)

---

## ⚠️ Common Pitfalls

1. **❌ Separate Transactions**: Progress MUST be in same transaction as movements
2. **❌ File Path Changes**: Always validate file hash on resume
3. **❌ Memory Leaks**: Release movement data after each chunk
4. **❌ Ignoring Cancellation**: Check cancellation token before each chunk
5. **❌ Blocking UI**: Use Task-based async, never block on UI thread
6. **❌ Missing Indexes**: Database queries will be slow without proper indexes

---

## 🔗 References

### Related Code
- `BrokerFinancialBatchManager.fs` - Phase 2 broker snapshot logic
- `TickerSnapshotBatchManager.fs` - Phase 2 ticker snapshot logic
- `ChunkStrategy.fs` - Chunking algorithm (already implemented)
- `ImportState.fs` - State types (already implemented)

### Documentation
- GitHub Issue: [#420](https://github.com/DarioAlonsoCerezo/Binnaculum/issues/420)
- Project Guidelines: `.github/copilot-instructions.md`
- F# Guidelines: `.github/instructions/fsharp-core.instructions.md`

---

**Last Updated**: 2025-10-24  
**Status**: Ready for implementation (all tasks pending)  
**Next Step**: Start Phase 1 - Database Schema
