namespace Binnaculum.Core.Import

open System
open System.Threading.Tasks
open Microsoft.Data.Sqlite
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Logging
open ImportSessionExtensions
open ImportSessionChunkExtensions

/// <summary>
/// Session management for resumable imports.
/// Handles creation, tracking, and updating of import sessions and chunks.
/// </summary>
module internal ImportSessionManager =

    /// <summary>
    /// Create a new import session with all chunks.
    /// Returns the session ID.
    /// </summary>
    let createSession
        (brokerAccountId: int)
        (brokerAccountName: string)
        (filePath: string)
        (analysis: DateAnalysis)
        (chunks: ChunkInfo list)
        : Task<int> =
        task {
            try
                CoreLogger.logInfof
                    "ImportSessionManager"
                    "Creating import session for account %s with %d chunks (%d estimated movements)"
                    brokerAccountName
                    chunks.Length
                    analysis.TotalMovements

                // Create session record
                let fileName = System.IO.Path.GetFileName(filePath)
                
                let! sessionId =
                    ImportSessionExtensions.Do.createSession(
                        brokerAccountId,
                        brokerAccountName,
                        fileName,
                        filePath,
                        analysis.FileHash,
                        chunks.Length,
                        analysis.TotalMovements,
                        analysis.MinDate,
                        analysis.MaxDate
                    )

                // Create all chunk records
                do! ImportSessionChunkExtensions.Do.createChunks(sessionId, chunks)

                CoreLogger.logInfof
                    "ImportSessionManager"
                    "Import session %d created successfully with %d chunks"
                    sessionId
                    chunks.Length

                return sessionId
            with ex ->
                CoreLogger.logError "ImportSessionManager" $"Failed to create import session: {ex.Message}"
                return raise ex
        }

    /// <summary>
    /// Get active session for a broker account.
    /// Returns None if no active session exists.
    /// </summary>
    let getActiveSession (brokerAccountId: int) : Task<ImportSession option> =
        task {
            try
                let! session = ImportSessionExtensions.Do.getActiveSession(brokerAccountId)

                match session with
                | Some s ->
                    CoreLogger.logInfof
                        "ImportSessionManager"
                        "Found active session %d for account %d in phase %s"
                        s.Id
                        brokerAccountId
                        s.Phase
                | None ->
                    CoreLogger.logDebug "ImportSessionManager" $"No active session for account {brokerAccountId}"

                return session
            with ex ->
                CoreLogger.logError "ImportSessionManager" $"Failed to get active session: {ex.Message}"
                return None
        }

    /// <summary>
    /// Get session by ID.
    /// </summary>
    let getSessionById (sessionId: int) : Task<ImportSession option> =
        task {
            try
                return! ImportSessionExtensions.Do.getById(sessionId)
            with ex ->
                CoreLogger.logError "ImportSessionManager" $"Failed to get session {sessionId}: {ex.Message}"
                return None
        }

    /// <summary>
    /// Get all chunks for a session.
    /// </summary>
    let getChunks (sessionId: int) : Task<ImportSessionChunk list> =
        task {
            try
                return! ImportSessionChunkExtensions.Do.getBySessionId(sessionId)
            with ex ->
                CoreLogger.logError "ImportSessionManager" $"Failed to get chunks for session {sessionId}: {ex.Message}"
                return []
        }

    /// <summary>
    /// Get pending chunks for a session (state = Pending or Failed).
    /// Used for resume functionality.
    /// </summary>
    let getPendingChunks (sessionId: int) : Task<ImportSessionChunk list> =
        task {
            try
                let! pendingChunks = ImportSessionChunkExtensions.Do.getPendingChunks(sessionId)

                CoreLogger.logInfof
                    "ImportSessionManager"
                    "Found %d pending chunks for session %d"
                    pendingChunks.Length
                    sessionId

                return pendingChunks
            with ex ->
                CoreLogger.logError "ImportSessionManager" $"Failed to get pending chunks for session {sessionId}: {ex.Message}"
                return []
        }

    /// <summary>
    /// Mark chunk as completed - MUST be called within a transaction.
    /// Updates both chunk state and session progress counters atomically.
    /// </summary>
    let markChunkCompleted
        (sessionId: int)
        (chunkNumber: int)
        (actualMovements: int)
        (durationMs: int64)
        (transaction: SqliteTransaction)
        : Task<unit> =
        task {
            try
                // Update chunk record
                do! ImportSessionChunkExtensions.Do.markCompleted(
                    sessionId,
                    chunkNumber,
                    actualMovements,
                    durationMs,
                    transaction
                )

                // Update session progress
                do! ImportSessionExtensions.Do.updateChunkProgress(
                    sessionId,
                    actualMovements,
                    transaction
                )

                // Note: No logging here since this is called within a transaction
                // Logging will be done by the caller after commit
            with ex ->
                // Still no logging - let the exception bubble up to trigger transaction rollback
                raise ex
        }

    /// <summary>
    /// Update session phase.
    /// </summary>
    let updatePhase (sessionId: int) (newPhase: string) : Task<unit> =
        task {
            try
                do! ImportSessionExtensions.Do.updatePhase(sessionId, newPhase)

                CoreLogger.logInfof
                    "ImportSessionManager"
                    "Session %d phase updated to %s"
                    sessionId
                    newPhase
            with ex ->
                CoreLogger.logError "ImportSessionManager" $"Failed to update phase for session {sessionId}: {ex.Message}"
                raise ex
        }

    /// <summary>
    /// Mark Phase 1 completed and transition to Phase 2.
    /// </summary>
    let markPhase1Completed (sessionId: int) : Task<unit> =
        task {
            try
                do! ImportSessionExtensions.Do.markPhase1Completed(sessionId)

                CoreLogger.logInfo "ImportSessionManager" $"Session {sessionId} Phase 1 completed, transitioning to Phase 2"
            with ex ->
                CoreLogger.logError "ImportSessionManager" $"Failed to mark Phase 1 completed for session {sessionId}: {ex.Message}"
                raise ex
        }

    /// <summary>
    /// Mark Phase 2 broker snapshots step completed.
    /// </summary>
    let markBrokerSnapshotsCompleted (sessionId: int) : Task<unit> =
        task {
            try
                do! ImportSessionExtensions.Do.markBrokerSnapshotsCompleted(sessionId)

                CoreLogger.logInfo "ImportSessionManager" $"Session {sessionId} broker snapshots calculated"
            with ex ->
                CoreLogger.logError "ImportSessionManager" $"Failed to mark broker snapshots completed for session {sessionId}: {ex.Message}"
                raise ex
        }

    /// <summary>
    /// Mark Phase 2 ticker snapshots step completed.
    /// </summary>
    let markTickerSnapshotsCompleted (sessionId: int) : Task<unit> =
        task {
            try
                do! ImportSessionExtensions.Do.markTickerSnapshotsCompleted(sessionId)

                CoreLogger.logInfo "ImportSessionManager" $"Session {sessionId} ticker snapshots calculated"
            with ex ->
                CoreLogger.logError "ImportSessionManager" $"Failed to mark ticker snapshots completed for session {sessionId}: {ex.Message}"
                raise ex
        }

    /// <summary>
    /// Complete import session successfully.
    /// </summary>
    let completeSession (sessionId: int) : Task<unit> =
        task {
            try
                do! ImportSessionExtensions.Do.completeSession(sessionId)

                CoreLogger.logInfo "ImportSessionManager" $"Session {sessionId} completed successfully"
            with ex ->
                CoreLogger.logError "ImportSessionManager" $"Failed to complete session {sessionId}: {ex.Message}"
                raise ex
        }

    /// <summary>
    /// Mark session as failed with error message.
    /// </summary>
    let markSessionFailed (sessionId: int) (errorMessage: string) : Task<unit> =
        task {
            try
                do! ImportSessionExtensions.Do.markFailed(sessionId, errorMessage)

                CoreLogger.logError "ImportSessionManager" $"Session {sessionId} marked as failed: {errorMessage}"
            with ex ->
                CoreLogger.logError "ImportSessionManager" $"Failed to mark session {sessionId} as failed: {ex.Message}"
                raise ex
        }

    /// <summary>
    /// Mark session as cancelled.
    /// </summary>
    let markSessionCancelled (sessionId: int) : Task<unit> =
        task {
            try
                do! ImportSessionExtensions.Do.markCancelled(sessionId)

                CoreLogger.logInfo "ImportSessionManager" $"Session {sessionId} marked as cancelled"
            with ex ->
                CoreLogger.logError "ImportSessionManager" $"Failed to mark session {sessionId} as cancelled: {ex.Message}"
                raise ex
        }

    /// <summary>
    /// Validate file hash matches session.
    /// Returns true if file hash matches, false otherwise.
    /// </summary>
    let validateFileHash (session: ImportSession) (currentFilePath: string) : bool =
        try
            let currentHash = CsvDateAnalyzer.calculateFileHash(currentFilePath)

            if currentHash = session.FileHash then
                CoreLogger.logInfo "ImportSessionManager" $"File hash validation passed for session {session.Id}"
                true
            else
                CoreLogger.logWarning
                    "ImportSessionManager"
                    $"File hash mismatch for session {session.Id}: expected {session.FileHash}, got {currentHash}"
                false
        with ex ->
            CoreLogger.logError "ImportSessionManager" $"Failed to validate file hash for session {session.Id}: {ex.Message}"
            false
