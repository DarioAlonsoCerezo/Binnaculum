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
                    (SQLParameterName.Id, broker.Id);
                    (SQLParameterName.Name, broker.Name);
                    (SQLParameterName.Image, broker.Image);
                    (SQLParameterName.SupportedBroker, fromSupportedBrokerToDatabase broker.SupportedBroker);
                ])

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.getInt32 FieldName.Id
                Name = reader.getString FieldName.Name
                Image = reader.getString FieldName.Image
                SupportedBroker = reader.getString FieldName.SupportedBroker |> fromDatabaseToSupportedBroker
            }

        [<Extension>]
        static member save(broker: Broker) = Database.Do.saveEntity broker (fun b c -> b.fill c) 

        [<Extension>]
        static member delete(broker: Broker) = Database.Do.deleteEntity broker 
        
        static member getAll() = Database.Do.getAllEntities Do.read BrokerQuery.getAll
        
        static member getById(id: int) = Database.Do.getById Do.read id BrokerQuery.getById

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
            command.Parameters.AddWithValue(SQLParameterName.Name, name) |> ignore
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