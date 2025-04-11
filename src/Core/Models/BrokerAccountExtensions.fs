module internal BrokerAccountExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.SQL
open DataReaderExtensions
open CommandExtensions

    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member fill(brokerAccount: BrokerAccount, command: SqliteCommand) =
            command.fillParameters(
                [
                    ("@Id", brokerAccount.Id);
                    ("@BrokerId", brokerAccount.BrokerId);
                    ("@AccountNumber", brokerAccount.AccountNumber);
                ])

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.getInt32 "Id"
                BrokerId = reader.getInt32 "BrokerId"
                AccountNumber = reader.getString "AccountNumber"
            }

        [<Extension>]
        static member save(account: BrokerAccount) = 
            Database.Do.saveEntity 
                account 
                (fun a c -> a.fill c) 
                BrokerAccountQuery.insert BrokerAccountQuery.update

        [<Extension>]
        static member delete(account: BrokerAccount) = 
            Database.Do.deleteEntity account BrokerAccountQuery.delete

        static member getAll() = 
            Database.Do.getAllEntities BrokerAccountQuery.getAll Do.read

        static member getById(id: int) = 
            Database.Do.getById id BrokerAccountQuery.getById Do.read