module internal ImportSessionChunkExtensions

open System
open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Import
open Microsoft.Data.Sqlite
open Binnaculum.Core.Database.TypeParser
open Binnaculum.Core
open Binnaculum.Core.SQL
open DataReaderExtensions
open CommandExtensions

[<Extension>]
type Do() =

    [<Extension>]
    static member read(reader: SqliteDataReader) : ImportSessionChunk =
        { Id = reader.getInt32 "Id"
          ImportSessionId = reader.getInt32 "ImportSessionId"
          ChunkNumber = reader.getInt32 "ChunkNumber"
          StartDate = reader.getString "StartDate"
          EndDate = reader.getString "EndDate"
          EstimatedMovements = reader.getInt32 "EstimatedMovements"
          State = reader.getString "State"
          ActualMovements = reader.getInt32 "ActualMovements"
          StartedAt = reader.getStringOrNone "StartedAt"
          CompletedAt = reader.getStringOrNone "CompletedAt"
          DurationMs =
            let ordinal = reader.GetOrdinal("DurationMs")

            if reader.IsDBNull(ordinal) then
                None
            else
                Some(reader.GetInt64(ordinal))
          Error = reader.getStringOrNone "Error"
          Audit = reader.getAudit () }

    /// Create chunks for a session
    static member createChunks(sessionId: int, chunks: ChunkInfo list) =
        task {
            let! command = Database.Do.createCommand ()

            for chunk in chunks do
                command.CommandText <- ImportSessionChunkQuery.insert
                command.Parameters.Clear()

                command.Parameters.AddWithValue("@ImportSessionId", sessionId) |> ignore
                command.Parameters.AddWithValue("@ChunkNumber", chunk.ChunkNumber) |> ignore

                command.Parameters.AddWithValue(
                    "@StartDate",
                    chunk.StartDate.ToString("O", System.Globalization.CultureInfo.InvariantCulture)
                )
                |> ignore

                command.Parameters.AddWithValue(
                    "@EndDate",
                    chunk.EndDate.ToString("O", System.Globalization.CultureInfo.InvariantCulture)
                )
                |> ignore

                command.Parameters.AddWithValue("@EstimatedMovements", chunk.EstimatedMovements)
                |> ignore

                command.Parameters.AddWithValue("@State", "Pending") |> ignore

                do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }

    /// Get all chunks for a session
    static member getBySessionId(sessionId: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- ImportSessionChunkQuery.getBySessionId
            command.Parameters.AddWithValue("@ImportSessionId", sessionId) |> ignore

            return! Database.Do.readAll<ImportSessionChunk> (command, Do.read)
        }

    /// Get pending chunks for a session
    static member getPendingChunks(sessionId: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- ImportSessionChunkQuery.getPendingChunks
            command.Parameters.AddWithValue("@ImportSessionId", sessionId) |> ignore

            return! Database.Do.readAll<ImportSessionChunk> (command, Do.read)
        }

    /// Mark chunk as in progress
    static member markInProgress(chunkId: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- ImportSessionChunkQuery.markInProgress
            command.Parameters.AddWithValue("@Id", chunkId) |> ignore

            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }

    /// Mark chunk as completed
    static member markCompleted(sessionId: int, chunkNumber: int, actualMovements: int, durationMs: int64) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- ImportSessionChunkQuery.markCompleted
            command.Parameters.AddWithValue("@ImportSessionId", sessionId) |> ignore
            command.Parameters.AddWithValue("@ChunkNumber", chunkNumber) |> ignore
            command.Parameters.AddWithValue("@ActualMovements", actualMovements) |> ignore
            command.Parameters.AddWithValue("@DurationMs", durationMs) |> ignore

            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }

    /// Mark chunk as failed
    static member markFailed(chunkId: int, errorMessage: string) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- ImportSessionChunkQuery.markFailed
            command.Parameters.AddWithValue("@Id", chunkId) |> ignore
            command.Parameters.AddWithValue("@ErrorMessage", errorMessage) |> ignore

            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }

    /// Get chunk by session and chunk number
    static member getBySessionAndChunkNumber(sessionId: int, chunkNumber: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- ImportSessionChunkQuery.getBySessionAndChunkNumber
            command.Parameters.AddWithValue("@ImportSessionId", sessionId) |> ignore
            command.Parameters.AddWithValue("@ChunkNumber", chunkNumber) |> ignore

            let! chunks = Database.Do.readAll<ImportSessionChunk> (command, Do.read)
            return chunks |> List.tryHead
        }
