module internal BrokerAccountExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.SQL

    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member fill(brokerAccount: BrokerAccount, command: SqliteCommand) =
            command.Parameters.AddWithValue("@Id", brokerAccount.Id) |> ignore
            command.Parameters.AddWithValue("@BrokerId", brokerAccount.BrokerId) |> ignore
            command.Parameters.AddWithValue("@AccountNumber", brokerAccount.AccountNumber) |> ignore
            command

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            let id = reader.GetInt32(reader.GetOrdinal("Id"))
            let brokerId = reader.GetInt32(reader.GetOrdinal("BrokerId"))
            let accountNumber = reader.GetString(reader.GetOrdinal("AccountNumber"))
            { 
                Id = id 
                BrokerId = brokerId 
                AccountNumber = accountNumber 
            }

        [<Extension>]
        static member readAll(reader: SqliteDataReader) =
            let mutable resultList = []
            while reader.Read() do
                let brokerAccount = Do.read reader
                resultList <- brokerAccount :: resultList
            resultList

        [<Extension>]
        static member save(account: BrokerAccount) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- 
                match account.Id with
                | 0 -> BrokerAccountQuery.insert
                | _ -> BrokerAccountQuery.update
            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }

        [<Extension>]
        static member delete(account: BrokerAccount) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BrokerAccountQuery.delete
            command.Parameters.AddWithValue("@Id", account.Id) |> ignore
            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }

        static member getAll() = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BrokerAccountQuery.getAll
            let! brokerAccounts = Database.Do.readAll<BrokerAccount>(command, Do.read)
            return brokerAccounts
        }

        static member getById(id: int) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BrokerAccountQuery.getById
            command.Parameters.AddWithValue("@Id", id) |> ignore
            let! brokerAccounts = Database.Do.readAll<BrokerAccount>(command, Do.read)
            return brokerAccounts |> List.tryHead
        }