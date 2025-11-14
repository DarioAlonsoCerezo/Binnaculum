module internal ImportSessionExtensions

open System
open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.SQL
open DataReaderExtensions

[<Extension>]
type Do() =

    [<Extension>]
    static member read(reader: SqliteDataReader) : ImportSession =
        { Id = reader.getInt32 "Id"
          BrokerAccountId = reader.getInt32 "BrokerAccountId"
          BrokerAccountName = reader.getString "BrokerAccountName"
          FileName = reader.getString "FileName"
          FilePath = reader.getString "FilePath"
          FileHash = reader.getString "FileHash"
          State = reader.getString "State"
          Phase = reader.getString "Phase"
          TotalChunks = reader.getInt32 "TotalChunks"
          ChunksCompleted = reader.getInt32 "ChunksCompleted"
          MovementsPersisted = reader.getInt32 "MovementsPersisted"
          BrokerSnapshotsCalculated = reader.getInt32 "BrokerSnapshotsCalculated"
          TickerSnapshotsCalculated = reader.getInt32 "TickerSnapshotsCalculated"
          MinDate = reader.getString "MinDate"
          MaxDate = reader.getString "MaxDate"
          TotalEstimatedMovements = reader.getInt32 "TotalEstimatedMovements"
          StartedAt = reader.getString "StartedAt"
          Phase1CompletedAt = reader.getStringOrNone "Phase1CompletedAt"
          Phase2StartedAt = reader.getStringOrNone "Phase2StartedAt"
          CompletedAt = reader.getStringOrNone "CompletedAt"
          LastProgressUpdateAt = reader.getStringOrNone "LastProgressUpdateAt"
          LastError = reader.getStringOrNone "LastError"
          Audit = reader.getAudit () }

    /// Create a new import session with chunks
    static member createSession
        (
            brokerAccountId: int,
            brokerAccountName: string,
            fileName: string,
            filePath: string,
            fileHash: string,
            totalChunks: int,
            totalEstimatedMovements: int,
            minDate: DateTime,
            maxDate: DateTime
        ) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- ImportSessionQuery.insert

            command.Parameters.AddWithValue("@BrokerAccountId", brokerAccountId) |> ignore

            command.Parameters.AddWithValue("@BrokerAccountName", brokerAccountName)
            |> ignore

            command.Parameters.AddWithValue("@FileName", fileName) |> ignore
            command.Parameters.AddWithValue("@FilePath", filePath) |> ignore
            command.Parameters.AddWithValue("@FileHash", fileHash) |> ignore

            command.Parameters.AddWithValue("@State", "Phase1_PersistingMovements")
            |> ignore

            command.Parameters.AddWithValue("@Phase", "Phase1_PersistingMovements")
            |> ignore

            command.Parameters.AddWithValue("@TotalChunks", totalChunks) |> ignore
            command.Parameters.AddWithValue("@ChunksCompleted", 0) |> ignore
            command.Parameters.AddWithValue("@MovementsPersisted", 0) |> ignore

            command.Parameters.AddWithValue("@MinDate", minDate.ToString("yyyy-MM-dd HH:mm:ss"))
            |> ignore

            command.Parameters.AddWithValue("@MaxDate", maxDate.ToString("yyyy-MM-dd HH:mm:ss"))
            |> ignore

            command.Parameters.AddWithValue("@TotalEstimatedMovements", totalEstimatedMovements)
            |> ignore

            command.Parameters.AddWithValue("@StartedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            |> ignore

            command.Parameters.AddWithValue("@LastProgressUpdateAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
            |> ignore

            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore

            // Get the last inserted row ID
            command.CommandText <- "SELECT last_insert_rowid()"
            let! result = command.ExecuteScalarAsync() |> Async.AwaitTask
            return Convert.ToInt32(result)
        }

    /// Get active session for a broker account
    static member getActiveSession(brokerAccountId: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- ImportSessionQuery.getActiveSession
            command.Parameters.AddWithValue("@BrokerAccountId", brokerAccountId) |> ignore

            let! sessions = Database.Do.readAll<ImportSession> (command, Do.read)
            return sessions |> List.tryHead
        }

    /// Get session by ID
    static member getById(sessionId: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- ImportSessionQuery.getById
            command.Parameters.AddWithValue("@Id", sessionId) |> ignore

            let! sessions = Database.Do.readAll<ImportSession> (command, Do.read)
            return sessions |> List.tryHead
        }

    /// Update session phase
    static member updatePhase(sessionId: int, phase: string) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- ImportSessionQuery.updatePhase
            command.Parameters.AddWithValue("@Id", sessionId) |> ignore
            command.Parameters.AddWithValue("@Phase", phase) |> ignore

            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }

    /// Mark Phase 1 completed
    static member markPhase1Completed(sessionId: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- ImportSessionQuery.markPhase1Completed
            command.Parameters.AddWithValue("@Id", sessionId) |> ignore

            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }

    /// Update chunk progress
    static member updateChunkProgress(sessionId: int, movementsCount: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- ImportSessionQuery.updateChunkProgress
            command.Parameters.AddWithValue("@Id", sessionId) |> ignore
            command.Parameters.AddWithValue("@MovementsCount", movementsCount) |> ignore

            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }

    /// Mark broker snapshots calculated
    static member markBrokerSnapshotsCompleted(sessionId: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- ImportSessionQuery.markBrokerSnapshotsCompleted
            command.Parameters.AddWithValue("@Id", sessionId) |> ignore

            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }

    /// Mark ticker snapshots calculated
    static member markTickerSnapshotsCompleted(sessionId: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- ImportSessionQuery.markTickerSnapshotsCompleted
            command.Parameters.AddWithValue("@Id", sessionId) |> ignore

            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }

    /// Complete import session
    static member completeSession(sessionId: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- ImportSessionQuery.completeSession
            command.Parameters.AddWithValue("@Id", sessionId) |> ignore

            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }

    /// Mark session as failed
    static member markFailed(sessionId: int, errorMessage: string) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- ImportSessionQuery.markFailed
            command.Parameters.AddWithValue("@Id", sessionId) |> ignore
            command.Parameters.AddWithValue("@ErrorMessage", errorMessage) |> ignore

            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }

    /// Mark session as cancelled
    static member markCancelled(sessionId: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- ImportSessionQuery.markCancelled
            command.Parameters.AddWithValue("@Id", sessionId) |> ignore

            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }
