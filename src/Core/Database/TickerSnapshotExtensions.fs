module internal TickerSnapshotExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.SnapshotsModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions
open Binnaculum.Core.SQL

[<Extension>]
type Do() =
    
    [<Extension>]
    static member fill(tickerSnapshot: TickerSnapshot, command: SqliteCommand) =
        command.fillEntityAuditable<TickerSnapshot>(
            [
                (SQLParameterName.Date, tickerSnapshot.Base.Date.ToString());
                (SQLParameterName.TickerId, tickerSnapshot.TickerId)
            ], tickerSnapshot)

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Base = {
                Id = reader.getInt32 FieldName.Id
                Date = reader.getDateTimePattern FieldName.Date
                Audit = reader.getAudit()
            }
            TickerId = reader.getInt32 FieldName.TickerId
        }

    [<Extension>]
    static member save(tickerSnapshot: TickerSnapshot) = Database.Do.saveEntity tickerSnapshot (fun t c -> t.fill c)
    
    [<Extension>]
    static member delete(tickerSnapshot: TickerSnapshot) = Database.Do.deleteEntity tickerSnapshot

    static member getAll() = Database.Do.getAllEntities Do.read TickerSnapshotQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id TickerSnapshotQuery.getById
    
    static member getByTickerId(tickerId: int) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TickerSnapshotQuery.getByTickerId
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            let! snapshots = Database.Do.readAll<TickerSnapshot>(command, Do.read)
            return snapshots
        }
        
    static member getLatestByTickerId(tickerId: int) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TickerSnapshotQuery.getLatestByTickerId
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            let! snapshot = Database.Do.read<TickerSnapshot>(command, Do.read)
            return snapshot
        }
        
    static member getByTickerIdAndDate(tickerId: int, date: Binnaculum.Core.Patterns.DateTimePattern) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TickerSnapshotQuery.getByTickerIdAndDate
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.Date, date.ToString()) |> ignore
            let! snapshot = Database.Do.read<TickerSnapshot>(command, Do.read)
            return snapshot
        }
        
    static member getTickerSnapshotsByDateRange(tickerId: int, startDate: Binnaculum.Core.Patterns.DateTimePattern, endDate: Binnaculum.Core.Patterns.DateTimePattern) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TickerSnapshotQuery.getTickerSnapshotsByDateRange
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.Date, startDate.ToString()) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.DateEnd, endDate.ToString()) |> ignore
            let! snapshots = Database.Do.readAll<TickerSnapshot>(command, Do.read)
            return snapshots
        }