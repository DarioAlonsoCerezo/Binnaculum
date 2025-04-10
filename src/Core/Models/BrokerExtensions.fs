module internal BrokerExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core.Database.TypeParser
open Binnaculum.Core
open Binnaculum.Core.SQL
open DataReaderExtensions
open CommandExtensions

    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member fill(broker: Broker, command: SqliteCommand) =
            command.fillParameters(
                [
                    ("@Id", broker.Id);
                    ("@Name", broker.Name);
                    ("@Image", broker.Image);
                    ("@SupportedBroker", fromSupportedBrokerToDatabase broker.SupportedBroker);
                ])

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.getInt32 "Id"
                Name = reader.getString "Name"
                Image = reader.getString "Image"
                SupportedBroker = reader.getString "SupportedBroker" |> fromDatabaseToSupportedBroker
            }

        [<Extension>]
        static member save(broker: Broker) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BrokerQuery.insert
            do! Database.Do.executeNonQuery(broker.fill command) |> Async.AwaitTask |> Async.Ignore
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