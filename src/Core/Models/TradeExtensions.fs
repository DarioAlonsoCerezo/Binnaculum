module internal TradeExtensions

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
        static member fill(trade: Trade, command: SqliteCommand) =
            command.Parameters.AddWithValue("@Id", trade.Id) |> ignore
            command.Parameters.AddWithValue("@TimeStamp", trade.TimeStamp) |> ignore
            command.Parameters.AddWithValue("@TickerId", trade.TickerId) |> ignore
            command.Parameters.AddWithValue("@BrokerAccountId", trade.BrokerAccountId) |> ignore
            command.Parameters.AddWithValue("@CurrencyId", trade.CurrencyId) |> ignore
            command.Parameters.AddWithValue("@Quantity", trade.Quantity) |> ignore
            command.Parameters.AddWithValue("@Price", trade.Price) |> ignore
            command.Parameters.AddWithValue("@Commission", trade.Commissions) |> ignore
            command.Parameters.AddWithValue("@Fees", trade.Fees) |> ignore
            command.Parameters.AddWithValue("@TradeCode", fromTradeCodeToDatabase trade.TradeCode) |> ignore
            command.Parameters.AddWithValue("@TradeType", fromTradeTypeToDatabase trade.TradeType) |> ignore
            command.Parameters.AddWithValue("@Notes", trade.Notes) |> ignore
            command


        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.getInt32 "Id"
                TimeStamp = reader.getDateTime "TimeStamp"
                TickerId = reader.getInt32 "TickerId"
                BrokerAccountId = reader.getInt32 "BrokerAccountId"
                CurrencyId = reader.getInt32 "CurrencyId"
                Quantity = reader.getDecimal "Quantity"
                Price = reader.getDecimal "Price"
                Commissions = reader.getDecimal "Commissions"
                Fees = reader.getDecimal "Fees"
                TradeCode = reader.GetString(reader.GetOrdinal("TradeCode")) |> fromDatabaseToTradeCode
                TradeType = reader.GetString(reader.GetOrdinal("TradeType")) |> fromDatabaseToTradeType
                Notes = reader.getStringOrNone "Notes"
            }

        [<Extension>]
        static member save(trade: Trade) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <-
                match trade.Id with
                | 0 -> TradesQuery.insert
                | _ -> TradesQuery.update
            do! Database.Do.executeNonQuery(trade.fill command) |> Async.AwaitTask |> Async.Ignore
        }

        [<Extension>]
        static member delete(trade: Trade) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TradesQuery.delete
            command.Parameters.AddWithValue("@Id", trade.Id) |> ignore
            do! Database.Do.executeNonQuery(command) |> Async.AwaitTask |> Async.Ignore
        }

        static member getAll() = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TradesQuery.getAll
            let! trades = Database.Do.readAll<Trade>(command, Do.read)
            return trades
        }

        static member getById(id: int) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TradesQuery.getById
            command.Parameters.AddWithValue("@Id", id) |> ignore
            let! trades = Database.Do.readAll<Trade>(command, Do.read)
            return trades |> List.tryHead
        }