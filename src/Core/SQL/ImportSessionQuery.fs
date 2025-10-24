namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core

module internal ImportSessionQuery =
    /// Creates the ImportSession table if it does not already exist.
    let createTable =
        """
        CREATE TABLE IF NOT EXISTS ImportSession
        (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            BrokerAccountId INTEGER NOT NULL,
            BrokerAccountName TEXT NOT NULL,
            FileName TEXT NOT NULL,
            FilePath TEXT NOT NULL,
            FileHash TEXT NOT NULL,
            State TEXT NOT NULL,
            Phase TEXT NOT NULL DEFAULT 'Phase1_PersistingMovements',
            TotalChunks INTEGER NOT NULL DEFAULT 0,
            ChunksCompleted INTEGER NOT NULL DEFAULT 0,
            MovementsPersisted INTEGER NOT NULL DEFAULT 0,
            BrokerSnapshotsCalculated INTEGER NOT NULL DEFAULT 0,
            TickerSnapshotsCalculated INTEGER NOT NULL DEFAULT 0,
            MinDate TEXT NOT NULL,
            MaxDate TEXT NOT NULL,
            TotalEstimatedMovements INTEGER NOT NULL DEFAULT 0,
            StartedAt TEXT NOT NULL,
            Phase1CompletedAt TEXT,
            Phase2StartedAt TEXT,
            CompletedAt TEXT,
            LastProgressUpdateAt TEXT,
            LastError TEXT,
            CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
            UpdatedAt TEXT NOT NULL DEFAULT (datetime('now')),
            FOREIGN KEY (BrokerAccountId) REFERENCES BrokerAccount(Id) ON DELETE CASCADE
        )
        """

    /// Create indexes for ImportSession table
    let createIndexes =
        [
            "CREATE INDEX IF NOT EXISTS idx_importsession_state ON ImportSession(State)"
            "CREATE INDEX IF NOT EXISTS idx_importsession_brokeraccount ON ImportSession(BrokerAccountId)"
            "CREATE INDEX IF NOT EXISTS idx_importsession_brokeraccount_state ON ImportSession(BrokerAccountId, State)"
        ]

    /// Insert a new import session
    let insert =
        """
        INSERT INTO ImportSession
        (
            BrokerAccountId,
            BrokerAccountName,
            FileName,
            FilePath,
            FileHash,
            State,
            Phase,
            TotalChunks,
            ChunksCompleted,
            MovementsPersisted,
            MinDate,
            MaxDate,
            TotalEstimatedMovements,
            StartedAt,
            LastProgressUpdateAt
        )
        VALUES
        (
            @BrokerAccountId,
            @BrokerAccountName,
            @FileName,
            @FilePath,
            @FileHash,
            @State,
            @Phase,
            @TotalChunks,
            @ChunksCompleted,
            @MovementsPersisted,
            @MinDate,
            @MaxDate,
            @TotalEstimatedMovements,
            @StartedAt,
            @LastProgressUpdateAt
        )
        """

    /// Get active session for a broker account
    let getActiveSession =
        """
        SELECT * FROM ImportSession
        WHERE BrokerAccountId = @BrokerAccountId
          AND State IN ('Analyzing', 'Phase1_PersistingMovements', 'Phase2_CalculatingSnapshots')
        ORDER BY Id DESC
        LIMIT 1
        """

    /// Get session by ID
    let getById =
        """
        SELECT * FROM ImportSession
        WHERE Id = @Id
        LIMIT 1
        """

    /// Update session phase
    let updatePhase =
        """
        UPDATE ImportSession
        SET Phase = @Phase,
            UpdatedAt = datetime('now'),
            LastProgressUpdateAt = datetime('now')
        WHERE Id = @Id
        """

    /// Mark Phase 1 completed
    let markPhase1Completed =
        """
        UPDATE ImportSession
        SET Phase = 'Phase2_CalculatingSnapshots',
            Phase1CompletedAt = datetime('now'),
            Phase2StartedAt = datetime('now'),
            UpdatedAt = datetime('now'),
            LastProgressUpdateAt = datetime('now')
        WHERE Id = @Id
        """

    /// Update chunk progress (increment counters)
    let updateChunkProgress =
        """
        UPDATE ImportSession
        SET ChunksCompleted = ChunksCompleted + 1,
            MovementsPersisted = MovementsPersisted + @MovementsCount,
            UpdatedAt = datetime('now'),
            LastProgressUpdateAt = datetime('now')
        WHERE Id = @Id
        """

    /// Mark Phase 2 broker snapshots completed
    let markBrokerSnapshotsCompleted =
        """
        UPDATE ImportSession
        SET BrokerSnapshotsCalculated = 1,
            UpdatedAt = datetime('now'),
            LastProgressUpdateAt = datetime('now')
        WHERE Id = @Id
        """

    /// Mark Phase 2 ticker snapshots completed
    let markTickerSnapshotsCompleted =
        """
        UPDATE ImportSession
        SET TickerSnapshotsCalculated = 1,
            UpdatedAt = datetime('now'),
            LastProgressUpdateAt = datetime('now')
        WHERE Id = @Id
        """

    /// Complete import session
    let completeSession =
        """
        UPDATE ImportSession
        SET State = 'Completed',
            CompletedAt = datetime('now'),
            UpdatedAt = datetime('now'),
            LastProgressUpdateAt = datetime('now')
        WHERE Id = @Id
        """

    /// Mark import as failed
    let markFailed =
        """
        UPDATE ImportSession
        SET State = 'Failed',
            LastError = @ErrorMessage,
            UpdatedAt = datetime('now')
        WHERE Id = @Id
        """

    /// Mark import as cancelled
    let markCancelled =
        """
        UPDATE ImportSession
        SET State = 'Cancelled',
            UpdatedAt = datetime('now')
        WHERE Id = @Id
        """
