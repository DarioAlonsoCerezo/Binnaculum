namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core

module internal ImportSessionChunkQuery =
    /// Creates the ImportSessionChunk table if it does not already exist.
    let createTable =
        """
        CREATE TABLE IF NOT EXISTS ImportSessionChunk
        (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            ImportSessionId INTEGER NOT NULL,
            ChunkNumber INTEGER NOT NULL,
            StartDate TEXT NOT NULL,
            EndDate TEXT NOT NULL,
            EstimatedMovements INTEGER NOT NULL,
            State TEXT NOT NULL DEFAULT 'Pending',
            ActualMovements INTEGER DEFAULT 0,
            StartedAt TEXT,
            CompletedAt TEXT,
            DurationMs INTEGER,
            Error TEXT,
            CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
            UpdatedAt TEXT NOT NULL DEFAULT (datetime('now')),
            FOREIGN KEY (ImportSessionId) REFERENCES ImportSession(Id) ON DELETE CASCADE,
            UNIQUE(ImportSessionId, ChunkNumber)
        )
        """

    /// Create indexes for ImportSessionChunk table
    let createIndexes =
        [
            "CREATE INDEX IF NOT EXISTS idx_chunk_session ON ImportSessionChunk(ImportSessionId)"
            "CREATE INDEX IF NOT EXISTS idx_chunk_session_state ON ImportSessionChunk(ImportSessionId, State)"
        ]

    /// Insert a new chunk
    let insert =
        """
        INSERT INTO ImportSessionChunk
        (
            ImportSessionId,
            ChunkNumber,
            StartDate,
            EndDate,
            EstimatedMovements,
            State
        )
        VALUES
        (
            @ImportSessionId,
            @ChunkNumber,
            @StartDate,
            @EndDate,
            @EstimatedMovements,
            @State
        )
        """

    /// Get all chunks for a session
    let getBySessionId =
        """
        SELECT * FROM ImportSessionChunk
        WHERE ImportSessionId = @ImportSessionId
        ORDER BY ChunkNumber
        """

    /// Get pending chunks for a session
    let getPendingChunks =
        """
        SELECT * FROM ImportSessionChunk
        WHERE ImportSessionId = @ImportSessionId
          AND State IN ('Pending', 'Failed')
        ORDER BY ChunkNumber
        """

    /// Mark chunk as in progress
    let markInProgress =
        """
        UPDATE ImportSessionChunk
        SET State = 'InProgress',
            StartedAt = datetime('now'),
            UpdatedAt = datetime('now')
        WHERE Id = @Id
        """

    /// Mark chunk as completed
    let markCompleted =
        """
        UPDATE ImportSessionChunk
        SET State = 'Completed',
            ActualMovements = @ActualMovements,
            CompletedAt = datetime('now'),
            DurationMs = @DurationMs,
            UpdatedAt = datetime('now')
        WHERE ImportSessionId = @ImportSessionId
          AND ChunkNumber = @ChunkNumber
        """

    /// Mark chunk as failed
    let markFailed =
        """
        UPDATE ImportSessionChunk
        SET State = 'Failed',
            Error = @ErrorMessage,
            UpdatedAt = datetime('now')
        WHERE Id = @Id
        """

    /// Get chunk by session and chunk number
    let getBySessionAndChunkNumber =
        """
        SELECT * FROM ImportSessionChunk
        WHERE ImportSessionId = @ImportSessionId
          AND ChunkNumber = @ChunkNumber
        LIMIT 1
        """
