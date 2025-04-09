module internal TradeExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.SQL
open Binnaculum.Core.Database.TypeParser

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
            let id = reader.GetInt32(reader.GetOrdinal("Id"))
            let timeStamp = reader.GetDateTime(reader.GetOrdinal("TimeStamp"))
            let tickerId = reader.GetInt32(reader.GetOrdinal("TickerId"))
            let brokerAccountId = reader.GetInt32(reader.GetOrdinal("BrokerAccountId"))
            let currencyId = reader.GetInt32(reader.GetOrdinal("CurrencyId"))
            let quantity = reader.GetDecimal(reader.GetOrdinal("Quantity"))
            let price = reader.GetDecimal(reader.GetOrdinal("Price"))
            let commissions = reader.GetDecimal(reader.GetOrdinal("Commissions"))
            let fees = reader.GetDecimal(reader.GetOrdinal("Fees"))
            let tradeCode = reader.GetString(reader.GetOrdinal("TradeCode")) |> fromDatabaseToTradeCode
            let tradeType = reader.GetString(reader.GetOrdinal("TradeType")) |> fromDatabaseToTradeType
            let notes = Database.Do.getStringOrDefault(reader, "Notes")
            {
                Id = id
                TimeStamp = timeStamp
                TickerId = tickerId
                BrokerAccountId = brokerAccountId
                CurrencyId = currencyId
                Quantity = quantity
                Price = price
                Commissions = commissions
                Fees = fees
                TradeCode = tradeCode
                TradeType = tradeType
                Notes = notes
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