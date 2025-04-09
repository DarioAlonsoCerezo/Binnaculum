module internal DividendTaxExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.SQL
open Binnaculum.Core.Database.TypeParser

    [<Extension>]
    type Do() =

        [<Extension>]
        static member fill(dividendTax: DividendTax, command: SqliteCommand) =
            command.Parameters.AddWithValue("@Id", dividendTax.Id) |> ignore
            command.Parameters.AddWithValue("@TimeStamp", dividendTax.TimeStamp) |> ignore
            command.Parameters.AddWithValue("@Amount", dividendTax.Amount) |> ignore
            command.Parameters.AddWithValue("@TickerId", dividendTax.TickerId) |> ignore
            command.Parameters.AddWithValue("@CurrencyId", dividendTax.CurrencyId) |> ignore
            command.Parameters.AddWithValue("@BrokerAccountId", dividendTax.BrokerAccountId) |> ignore
            command

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            let id = reader.GetInt32(reader.GetOrdinal("Id"))
            let timeStamp = reader.GetDateTime(reader.GetOrdinal("TimeStamp"))
            let amount = reader.GetDecimal(reader.GetOrdinal("Amount"))
            let tickerId = reader.GetInt32(reader.GetOrdinal("TickerId"))
            let currencyId = reader.GetInt32(reader.GetOrdinal("CurrencyId"))
            let brokerAccountId = reader.GetInt32(reader.GetOrdinal("BrokerAccountId"))
            {
                Id = id
                TimeStamp = timeStamp
                Amount = amount
                TickerId = tickerId
                CurrencyId = currencyId
                BrokerAccountId = brokerAccountId
            }

        [<Extension>]
        static member save(dividendTax: DividendTax) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <-
                match dividendTax.Id with
                | 0 -> DividendTaxesQuery.insert
                | _ -> DividendTaxesQuery.update
            do! Database.Do.executeNonQuery(dividendTax.fill command) |> Async.AwaitTask |> Async.Ignore
        }

        [<Extension>]
        static member delete(dividendTax: DividendTax) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- DividendTaxesQuery.delete
            command.Parameters.AddWithValue("@Id", dividendTax.Id) |> ignore
            do! Database.Do.executeNonQuery(command) |> Async.AwaitTask |> Async.Ignore
        }

        static member getAll() = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- DividendTaxesQuery.getAll
            let! dividendTaxes = Database.Do.readAll<DividendTax>(command, Do.read)
            return dividendTaxes
        }

        static member getById(id: int) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- DividendTaxesQuery.getById
            command.Parameters.AddWithValue("@Id", id) |> ignore
            let! dividendTax = Database.Do.readAll<DividendTax>(command, Do.read)
            return dividendTax |> List.tryHead
        }