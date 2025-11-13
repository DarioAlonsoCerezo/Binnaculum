module internal TickerSplitExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions
open Binnaculum.Core.SQL

[<Extension>]
type Do() =

    [<Extension>]
    static member fill(tickerSplit: TickerSplit, command: SqliteCommand) =
        command.fillEntityAuditable<TickerSplit> (
            [ (SQLParameterName.SplitDate, tickerSplit.SplitDate.ToString())
              (SQLParameterName.TickerId, tickerSplit.TickerId)
              (SQLParameterName.SplitFactor, tickerSplit.SplitFactor) ],
            tickerSplit
        )

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        { Id = reader.getInt32 FieldName.Id
          SplitDate = reader.getDateTimePattern FieldName.SplitDate
          TickerId = reader.getInt32 FieldName.TickerId
          SplitFactor = reader.getDecimal FieldName.SplitFactor
          Audit = reader.getAudit () }

    [<Extension>]
    static member save(tickerSplit: TickerSplit) =
        Database.Do.saveEntity tickerSplit (fun t c -> t.fill c)

    [<Extension>]
    static member delete(tickerSplit: TickerSplit) = Database.Do.deleteEntity tickerSplit

    static member getAll() =
        Database.Do.getAllEntities Do.read TickerSplitQuery.getAll

    static member getById(id: int) =
        Database.Do.getById Do.read id TickerSplitQuery.getById

    static member getBetweenDates(startDate: string, endDate: string) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- TickerSplitQuery.getBetweenDates
            command.Parameters.AddWithValue("@StartDate", startDate) |> ignore
            command.Parameters.AddWithValue("@EndDate", endDate) |> ignore
            let! splits = Database.Do.readAll<TickerSplit> (command, Do.read)
            return splits
        }
