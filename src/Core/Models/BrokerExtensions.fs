module internal BrokerExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core.Database.TypeParser
open Binnaculum.Core
open Binnaculum.Core.SQL

    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member fill(broker: Broker, command: SqliteCommand) =
            command.Parameters.AddWithValue("@Id", broker.Id) |> ignore
            command.Parameters.AddWithValue("@Name", broker.Name) |> ignore
            command.Parameters.AddWithValue("@Image", broker.Image) |> ignore
            command.Parameters.AddWithValue("@SupportedBroker", fromSupportedBrokerToDatabase broker.SupportedBroker) |> ignore
            command

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            let id = reader.GetInt32(reader.GetOrdinal("Id"))
            let name = reader.GetString(reader.GetOrdinal("Name"))
            let image = reader.GetString(reader.GetOrdinal("Image"))
            let supportedBroker = reader.GetString(reader.GetOrdinal("SupportedBroker")) |> fromDatabaseToSupportedBroker
            { 
                Id = id 
                Name = name 
                Image = image 
                SupportedBroker = supportedBroker 
            }

        [<Extension>]
        static member save(broker: Broker) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BrokerQuery.insert
            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        }
        
        static member getAll() = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BrokerQuery.getAll
            let! brokers = Database.Do.readAll<Broker>(command, Do.read)
            return brokers
        }

        //This list contains all supported brokers
        static member brokerList() =
            [
                { Id = 0; Name = Keys.Broker_IBKR; Image = Keys.Broker_Image_IBKR; SupportedBroker = SupportedBroker.IBKR }
                { Id = 0; Name = Keys.Broker_Tastytrade; Image = Keys.Broker_Image_Tastytrade; SupportedBroker = SupportedBroker.Tastytrade }
            ]

        // Check if a broker exists by name
        static member exists(name: string) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BrokerQuery.getByName
            command.Parameters.AddWithValue("@Name", name) |> ignore
            let! result = command.ExecuteScalarAsync() |> Async.AwaitTask
            return result <> null
        }

        // Insert brokers from the list if they do not exist
        static member insertIfNotExists() = task {
            for broker in Do.brokerList() do
                let! exists = Do.exists(broker.Name)
                if not exists then
                    do! Do.save(broker)
        }