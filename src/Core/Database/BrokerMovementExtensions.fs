module internal BrokerMovementExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.Database.TypeParser
open DataReaderExtensions
open CommandExtensions

[<Extension>]
type Do() =
    
    [<Extension>]
    static member fill(brokerMovement: BrokerMovement, command: SqliteCommand) =
        command.fillEntityAuditable<BrokerMovement>(
            [
                (SQLParameterName.TimeStamp, brokerMovement.TimeStamp.ToString());
                (SQLParameterName.Amount, brokerMovement.Amount);
                (SQLParameterName.CurrencyId, brokerMovement.CurrencyId);
                (SQLParameterName.BrokerAccountId, brokerMovement.BrokerAccountId);
                (SQLParameterName.Commissions, brokerMovement.Commissions);
                (SQLParameterName.Fees, brokerMovement.Fees);
                (SQLParameterName.MovementType, fromMovementTypeToDatabase brokerMovement.MovementType);
            ])

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Id = reader.getInt32 FieldName.Id
            TimeStamp = reader.getDateTimePattern FieldName.TimeStamp
            Amount = reader.getMoney FieldName.Amount
            CurrencyId = reader.getInt32 FieldName.CurrencyId
            BrokerAccountId = reader.getInt32 FieldName.BrokerAccountId
            Commissions = reader.getMoney FieldName.Commissions
            Fees = reader.getMoney FieldName.Fees
            MovementType = reader.getString FieldName.MovementType |> fromDataseToMovementType
            Audit = reader.getAudit()        
        }

    [<Extension>]
    static member save(brokerMovement: BrokerMovement) = Database.Do.saveEntity brokerMovement (fun t c -> t.fill c) 

    [<Extension>]
    static member delete(brokerMovement: BrokerMovement) = Database.Do.deleteEntity brokerMovement

    static member getAll() = Database.Do.getAllEntities Do.read

    static member getById(id: int) = Database.Do.getById id Do.read
