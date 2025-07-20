module internal TickerCurrencySnapshotExxtensions

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
    static member fill(snapshot: TickerCurrencySnapshot, command: SqliteCommand) =
        command.fillEntityAuditable<TickerCurrencySnapshot>(
            [
                (SQLParameterName.Date, snapshot.Base.Date.ToString())
                (SQLParameterName.TickerId, snapshot.TickerId)
                (SQLParameterName.CurrencyId, snapshot.CurrencyId)
                (SQLParameterName.TickerSnapshotId, snapshot.TickerSnapshotId)
                (SQLParameterName.TotalShares, snapshot.TotalShares)
                (SQLParameterName.Weight, snapshot.Weight)
                (SQLParameterName.CostBasis, snapshot.CostBasis.Value)
                (SQLParameterName.RealCost, snapshot.RealCost.Value)
                (SQLParameterName.Dividends, snapshot.Dividends.Value)
                (SQLParameterName.Options, snapshot.Options.Value)
                (SQLParameterName.TotalIncomes, snapshot.TotalIncomes.Value)
                (SQLParameterName.Unrealized, snapshot.Unrealized.Value)
                (SQLParameterName.Realized, snapshot.Realized.Value)
                (SQLParameterName.Performance, snapshot.Performance)
                (SQLParameterName.LatestPrice, snapshot.LatestPrice.Value)
                (SQLParameterName.OpenTrades, if snapshot.OpenTrades then 1 else 0)
            ], snapshot)

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Base = {
                Id = reader.getInt32 FieldName.Id
                Date = reader.getDateTimePattern FieldName.Date
                Audit = reader.getAudit()
            }
            TickerId = reader.getInt32 FieldName.TickerId
            CurrencyId = reader.getInt32 FieldName.CurrencyId
            TickerSnapshotId = reader.getInt32 FieldName.TickerSnapshotId
            TotalShares = reader.getDecimal FieldName.TotalShares
            Weight = reader.getDecimal FieldName.Weight
            CostBasis = reader.getMoney FieldName.CostBasis
            RealCost = reader.getMoney FieldName.RealCost
            Dividends = reader.getMoney FieldName.Dividends
            Options = reader.getMoney FieldName.Options
            TotalIncomes = reader.getMoney FieldName.TotalIncomes
            Unrealized = reader.getMoney FieldName.Unrealized
            Realized = reader.getMoney FieldName.Realized
            Performance = reader.getDecimal FieldName.Performance
            LatestPrice = reader.getMoney FieldName.LatestPrice
            OpenTrades = reader.getInt32 FieldName.OpenTrades <> 0
        }

    [<Extension>]
    static member save(snapshot: TickerCurrencySnapshot) = Database.Do.saveEntity snapshot (fun t c -> t.fill c)

    [<Extension>]
    static member delete(snapshot: TickerCurrencySnapshot) = Database.Do.deleteEntity snapshot

    static member getAll() = Database.Do.getAllEntities Do.read TickerCurrencySnapshotQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id TickerCurrencySnapshotQuery.getById

    static member getByTickerId(tickerId: int) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TickerCurrencySnapshotQuery.getByTickerId
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            let! snapshots = Database.Do.readAll<TickerCurrencySnapshot>(command, Do.read)
            return snapshots
        }

    static member getLatestByTickerId(tickerId: int) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TickerCurrencySnapshotQuery.getLatestByTickerId
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            let! snapshot = Database.Do.read<TickerCurrencySnapshot>(command, Do.read)
            return snapshot
        }

    static member getByTickerIdAndDate(tickerId: int, date: Binnaculum.Core.Patterns.DateTimePattern) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TickerCurrencySnapshotQuery.getByTickerIdAndDate
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.Date, date.ToString()) |> ignore
            let! snapshot = Database.Do.read<TickerCurrencySnapshot>(command, Do.read)
            return snapshot
        }

    static member getTickerCurrencySnapshotsByDateRange(tickerId: int, startDate: Binnaculum.Core.Patterns.DateTimePattern, endDate: Binnaculum.Core.Patterns.DateTimePattern) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TickerCurrencySnapshotQuery.getTickerCurrencySnapshotsByDateRange
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.Date, startDate.ToString()) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.DateEnd, endDate.ToString()) |> ignore
            let! snapshots = Database.Do.readAll<TickerCurrencySnapshot>(command, Do.read)
            return snapshots
        }



