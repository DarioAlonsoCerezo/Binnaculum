module internal DividendExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.SQL
open DataReaderExtensions

    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member fill(dividend: Dividend, command: SqliteCommand) =
            command.Parameters.AddWithValue("@Id", dividend.Id) |> ignore
            command.Parameters.AddWithValue("@TimeStamp", dividend.TimeStamp) |> ignore
            command.Parameters.AddWithValue("@DividendAmount", dividend.DividendAmount) |> ignore
            command.Parameters.AddWithValue("@TickerId", dividend.TickerId) |> ignore
            command.Parameters.AddWithValue("@CurrencyId", dividend.CurrencyId) |> ignore
            command.Parameters.AddWithValue("@BrokerAccountId", dividend.BrokerAccountId) |> ignore
            command

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.getInt32 "Id"
                TimeStamp = reader.getDateTime "TimeStamp"
                DividendAmount = reader.getDecimal "DividendAmount"
                TickerId = reader.getInt32 "TickerId"
                CurrencyId = reader.getInt32 "CurrencyId"
                BrokerAccountId = reader.getInt32 "BrokerAccountId"
            }

        [<Extension>]
        static member save(dividend: Dividend) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- 
                match dividend.Id with
                | 0 -> DividendsQuery.insert
                | _ -> DividendsQuery.update
            do! Database.Do.executeNonQuery(dividend.fill command) |> Async.AwaitTask |> Async.Ignore
        }

        [<Extension>]
        static member delete(dividend: Dividend) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- DividendsQuery.delete
            command.Parameters.AddWithValue("@Id", dividend.Id) |> ignore
            do! Database.Do.executeNonQuery(command) |> Async.AwaitTask |> Async.Ignore
        }

        static member getAll() = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- DividendsQuery.getAll
            let! dividends = Database.Do.readAll<Dividend>(command, Do.read)
            return dividends
        }

        static member getById(id: int) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- DividendsQuery.getAll
            command.Parameters.AddWithValue("@Id", id) |> ignore
            let! dividends = Database.Do.readAll<Dividend>(command, Do.read)
            return dividends |> List.tryHead
        }