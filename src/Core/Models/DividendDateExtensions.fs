module internal DividendDateExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.SQL
open Binnaculum.Core.Database.TypeParser
open DataReaderExtensions

    [<Extension>]
    type Do() =

        [<Extension>]
        static member fill(dividendDate: DividendDate, command: SqliteCommand) =
            command.Parameters.AddWithValue("@TimeStamp", dividendDate.TimeStamp) |> ignore
            command.Parameters.AddWithValue("@Amount", dividendDate.Amount) |> ignore
            command.Parameters.AddWithValue("@TickerId", dividendDate.TickerId) |> ignore
            command.Parameters.AddWithValue("@CurrencyId", dividendDate.CurrencyId) |> ignore
            command.Parameters.AddWithValue("@BrokerAccountId", dividendDate.BrokerAccountId) |> ignore
            command.Parameters.AddWithValue("@DividendCode", fromDividendDateCodeToDatabase dividendDate.DividendCode) |> ignore
            command

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.getInt32 "Id"
                TimeStamp = reader.getDateTime "TimeStamp"
                Amount = reader.getDecimal "Amount"
                TickerId = reader.getInt32 "TickerId"
                CurrencyId = reader.getInt32 "CurrencyId"
                BrokerAccountId = reader.getInt32 "BrokerAccountId"
                DividendCode = reader.getString "DividendCode" |> fromDatabaseToDividendDateCode
            }

        [<Extension>]
        static member save(dividendDate: DividendDate) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <-
                match dividendDate.Id with
                | 0 -> DividendDateQuery.insert
                | _ -> DividendDateQuery.update
            do! Database.Do.executeNonQuery(dividendDate.fill command) |> Async.AwaitTask |> Async.Ignore
        }

        [<Extension>]
        static member delete(dividendDate: DividendDate) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- DividendDateQuery.delete
            command.Parameters.AddWithValue("@Id", dividendDate.Id) |> ignore
            do! Database.Do.executeNonQuery(command) |> Async.AwaitTask |> Async.Ignore
        }

        static member getAll() = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- DividendDateQuery.getAll
            let! dividendDates = Database.Do.readAll<DividendDate>(command, Do.read)
            return dividendDates
        }

        static member getById(id: int) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- DividendDateQuery.getById
            command.Parameters.AddWithValue("@Id", id) |> ignore
            let! dividendDate = Database.Do.readAll<DividendDate>(command, Do.read)
            return dividendDate |> List.tryHead
        }