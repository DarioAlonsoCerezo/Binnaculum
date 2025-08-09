module internal BrokerMovementExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.Database.TypeParser
open DataReaderExtensions
open CommandExtensions
open OptionExtensions
open Binnaculum.Core.SQL
open Binnaculum.Core.Patterns

[<Extension>]
type Do() =
    
    [<Extension>]
    static member fill(brokerMovement: BrokerMovement, command: SqliteCommand) =
        command.fillEntityAuditable<BrokerMovement>(
            [
                (SQLParameterName.TimeStamp, brokerMovement.TimeStamp.ToString());
                (SQLParameterName.Amount, brokerMovement.Amount.Value);
                (SQLParameterName.CurrencyId, brokerMovement.CurrencyId);
                (SQLParameterName.BrokerAccountId, brokerMovement.BrokerAccountId);
                (SQLParameterName.Commissions, brokerMovement.Commissions.Value);
                (SQLParameterName.Fees, brokerMovement.Fees.Value);
                (SQLParameterName.MovementType, fromMovementTypeToDatabase brokerMovement.MovementType);
                (SQLParameterName.Notes, brokerMovement.Notes.ToDbValue());
                (SQLParameterName.FromCurrencyId, brokerMovement.FromCurrencyId.ToDbValue());
                (SQLParameterName.AmountChanged, (brokerMovement.AmountChanged |> Option.map (fun m -> m.Value)).ToDbValue());
                (SQLParameterName.TickerId, brokerMovement.TickerId.ToDbValue());
                (SQLParameterName.Quantity, brokerMovement.Quantity.ToDbValue());
            ], brokerMovement)

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
            Notes = reader.getStringOrNone FieldName.Notes
            FromCurrencyId = reader.getIntOrNone FieldName.FromCurrencyId
            AmountChanged = reader.getMoneyOrNone FieldName.AmountChanged
            TickerId = reader.getIntOrNone FieldName.TickerId
            Quantity = reader.getDecimalOrNone FieldName.Quantity
            Audit = reader.getAudit()        
        }

    [<Extension>]
    static member save(brokerMovement: BrokerMovement) = Database.Do.saveEntity brokerMovement (fun t c -> t.fill c) 

    [<Extension>]
    static member delete(brokerMovement: BrokerMovement) = Database.Do.deleteEntity brokerMovement

    static member getAll() = Database.Do.getAllEntities Do.read BrokerMovementQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id BrokerMovementQuery.getById

    static member getByBrokerAccountIdUntilDate(brokerAccountId: int, endDate: DateTimePattern) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BrokerMovementQuery.getByBrokerAccountIdAndDateRange
        command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.DateEnd, endDate.ToString()) |> ignore
        let! movements = Database.Do.readAll<BrokerMovement>(command, Do.read)
        return movements
    }

    static member getByBrokerAccountIdFromDate(brokerAccountId: int, startDate: DateTimePattern) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BrokerMovementQuery.getByBrokerAccountIdFromDate
        command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.TimeStamp, startDate.ToString()) |> ignore
        let! movements = Database.Do.readAll<BrokerMovement>(command, Do.read)
        return movements
    }
