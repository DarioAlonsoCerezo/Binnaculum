module internal OptionTradeExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core.Database.TypeParser
open Binnaculum.Core
open Binnaculum.Core.SQL
open DataReaderExtensions

    [<Extension>]
    type Do() =

        [<Extension>]
        static member fill(optionTrade: OptionTrade, command: SqliteCommand) =
            command.Parameters.AddWithValue("@Id", optionTrade.Id) |> ignore
            command.Parameters.AddWithValue("@TimeStamp", optionTrade.TimeStamp) |> ignore
            command.Parameters.AddWithValue("@ExpirationDate", optionTrade.ExpirationDate) |> ignore
            command.Parameters.AddWithValue("@Premium", optionTrade.Premium) |> ignore
            command.Parameters.AddWithValue("@NetPremium", optionTrade.NetPremium) |> ignore
            command.Parameters.AddWithValue("@TickerId", optionTrade.TickerId) |> ignore
            command.Parameters.AddWithValue("@BrokerAccountId", optionTrade.BrokerAccountId) |> ignore
            command.Parameters.AddWithValue("@CurrencyId", optionTrade.CurrencyId) |> ignore
            command.Parameters.AddWithValue("@OptionType", fromOptionTypeToDatabase optionTrade.OptionType) |> ignore
            command.Parameters.AddWithValue("@Code", fromOptionCodeToDatabase optionTrade.Code) |> ignore
            command.Parameters.AddWithValue("@Strike", optionTrade.Strike) |> ignore
            command.Parameters.AddWithValue("@Commissions", optionTrade.Commissions) |> ignore
            command.Parameters.AddWithValue("@Fees", optionTrade.Fees) |> ignore
            command.Parameters.AddWithValue("@IsOpen", optionTrade.IsOpen) |> ignore
            command.Parameters.AddWithValue("@ClosedWith", optionTrade.ClosedWith) |> ignore
            command

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            { 
                Id = reader.getInt32 "Id" 
                TimeStamp = reader.getDateTime "TimeStamp"
                ExpirationDate = reader.getDateTime "ExpirationDate"
                Premium = reader.getDecimal "Premium"
                NetPremium = reader.getDecimal "NetPremium"
                TickerId = reader.getInt32 "TickerId"
                BrokerAccountId = reader.getInt32 "BrokerAccountId"
                CurrencyId = reader.getInt32 "CurrencyId"
                OptionType = reader.getString "OptionType" |> fromDatabaseToOptionType
                Code = reader.getString "Code" |> fromDatabaseToOptionCode
                Strike = reader.getDecimal "Strike"
                Commissions = reader.getDecimal "Commissions"
                Fees = reader.getDecimal "Fees"
                IsOpen = reader.getBoolean "IsOpen"
                ClosedWith = reader.getIntOrNone "ClosedWith"
            }

        [<Extension>]
        static member save(optionTrade: OptionTrade) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- 
                match optionTrade.Id with
                | 0 -> OptionsQuery.insert
                | _ -> OptionsQuery.update
            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }

        [<Extension>]
        static member delete(optionTrade: OptionTrade) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- OptionsQuery.delete
            command.Parameters.AddWithValue("@Id", optionTrade.Id) |> ignore
            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }

        static member getAll() = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- OptionsQuery.getAll
            let! optionTrades = Database.Do.readAll<OptionTrade>(command, Do.read)
            return optionTrades
        }

        static member getById(id: int) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- OptionsQuery.getById
            command.Parameters.AddWithValue("@Id", id) |> ignore
            let! optionTrade = Database.Do.readAll<OptionTrade>(command, Do.read)
            return optionTrade |> List.tryHead
        }