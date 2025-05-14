module internal BankAccountBalanceExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions
open Binnaculum.Core.Database.TypeParser
open Binnaculum.Core.SQL

[<Extension>]
type Do() =
    
    [<Extension>]
    static member fill(bankAccountBalance: BankAccountMovement, command: SqliteCommand) =
        command.fillEntityAuditable<BankAccountMovement>(
            [
                (SQLParameterName.TimeStamp, bankAccountBalance.TimeStamp);
                (SQLParameterName.Amount, bankAccountBalance.Amount.Value);
                (SQLParameterName.BankAccountId, bankAccountBalance.BankAccountId);
                (SQLParameterName.CurrencyId, bankAccountBalance.CurrencyId);
                (SQLParameterName.MovementType, fromBankMovementTypeToDatabase bankAccountBalance.MovementType);
            ], bankAccountBalance)

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Id = reader.getInt32 FieldName.Id
            TimeStamp = reader.getDateTimePattern FieldName.TimeStamp
            Amount = reader.getMoney FieldName.Amount
            BankAccountId = reader.getInt32 FieldName.BankAccountId
            CurrencyId = reader.getInt32 FieldName.CurrencyId
            MovementType = fromDatabaseToBankMovementType (reader.getString FieldName.MovementType)
            Audit = reader.getAudit()
        }

    [<Extension>]
    static member save(bankAccountBalance: BankAccountMovement) = Database.Do.saveEntity bankAccountBalance (fun t c -> t.fill c)
    
    [<Extension>]
    static member delete(bankAccountBalance: BankAccountMovement) = Database.Do.deleteEntity bankAccountBalance

    static member getAll() = Database.Do.getAllEntities Do.read BankAccountMovementsQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id BankAccountMovementsQuery.getById