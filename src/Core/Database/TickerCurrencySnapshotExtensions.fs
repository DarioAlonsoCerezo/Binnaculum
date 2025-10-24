module internal TickerCurrencySnapshotExtensions

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
        command.fillEntityAuditable<TickerCurrencySnapshot> (
            [ (SQLParameterName.Date, snapshot.Base.Date.ToString())
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
              (SQLParameterName.Commissions, snapshot.Commissions.Value)
              (SQLParameterName.Fees, snapshot.Fees.Value) ],
            snapshot
        )

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        { Base =
            { Id = reader.getInt32 FieldName.Id
              Date = reader.getDateTimePattern FieldName.Date
              Audit = reader.getAudit () }
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
          Commissions = reader.getMoney FieldName.Commissions
          Fees = reader.getMoney FieldName.Fees }

    [<Extension>]
    static member save(snapshot: TickerCurrencySnapshot) =
        Database.Do.saveEntity snapshot (fun t c -> t.fill c)

    [<Extension>]
    static member delete(snapshot: TickerCurrencySnapshot) = Database.Do.deleteEntity snapshot

    static member getById(id: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- TickerCurrencySnapshotQuery.getById
            command.Parameters.AddWithValue(SQLParameterName.Id, id) |> ignore
            let! snapshot = Database.Do.read<TickerCurrencySnapshot> (command, Do.read)
            return snapshot
        }

    static member getAllByTickerIdAndDate(tickerId: int, date: Binnaculum.Core.Patterns.DateTimePattern) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- TickerCurrencySnapshotQuery.getAllByTickerIdAndDate
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore

            command.Parameters.AddWithValue(SQLParameterName.Date, date.ToString())
            |> ignore

            let! snapshots = Database.Do.readAll<TickerCurrencySnapshot> (command, Do.read)
            return snapshots
        }

    static member getAllByTickerIdAfterDate(tickerId: int, date: Binnaculum.Core.Patterns.DateTimePattern) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- TickerCurrencySnapshotQuery.getAllByTickerIdAfterDate
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore

            command.Parameters.AddWithValue(SQLParameterName.Date, date.ToString())
            |> ignore

            let! snapshots = Database.Do.readAll<TickerCurrencySnapshot> (command, Do.read)
            return snapshots
        }

    static member getAllByTickerSnapshotId(tickerSnapshotId: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- TickerCurrencySnapshotQuery.getAllByTickerSnapshotId

            command.Parameters.AddWithValue(SQLParameterName.TickerSnapshotId, tickerSnapshotId)
            |> ignore

            let! snapshots = Database.Do.readAll<TickerCurrencySnapshot> (command, Do.read)
            return snapshots
        }
