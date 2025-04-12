﻿module internal BrokerAccountExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions

    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member fill(brokerAccount: BrokerAccount, command: SqliteCommand) =
            command.fillParameters(
                [
                    (SQLParameterName.Id, brokerAccount.Id);
                    (SQLParameterName.BrokerId, brokerAccount.BrokerId);
                    (SQLParameterName.AccountNumber, brokerAccount.AccountNumber);
                    (SQLParameterName.CreatedAt, brokerAccount.CreatedAt);
                    (SQLParameterName.UpdatedAt, brokerAccount.UpdatedAt);
                ])

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.getInt32 FieldName.Id
                BrokerId = reader.getInt32 FieldName.BrokerId
                AccountNumber = reader.getString FieldName.AccountNumber
                CreatedAt = reader.getDateTimePatternOrNone FieldName.CreatedAt
                UpdatedAt = reader.getDateTimePatternOrNone FieldName.UpdatedAt
            }

        [<Extension>]
        static member save(account: BrokerAccount) = Database.Do.saveEntity account (fun a c -> a.fill c) 

        [<Extension>]
        static member delete(account: BrokerAccount) = Database.Do.deleteEntity account

        static member getAll() = Database.Do.getAllEntities Do.read

        static member getById(id: int) = Database.Do.getById id Do.read