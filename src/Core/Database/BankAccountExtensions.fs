module internal BankAccountExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions

[<Extension>]
type Do() =
    
    [<Extension>]
    static member fill(bankAccount: BankAccount, command: SqliteCommand) =
        command.fillEntityAuditable<BankAccount>(
            [
                (SQLParameterName.BankId, bankAccount.BankId);
                (SQLParameterName.Name, bankAccount.Name);
                (SQLParameterName.Description, bankAccount.Description);
                (SQLParameterName.CurrencyId, bankAccount.CurrencyId);
            ])

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Id = reader.getInt32 FieldName.Id
            BankId = reader.getInt32 FieldName.BankId
            Name = reader.getString FieldName.Name
            Description = reader.getStringOrNone FieldName.Description
            CurrencyId = reader.getInt32 FieldName.CurrencyId
            Audit = reader.getAudit()
        }

    [<Extension>]
    static member save(bankAccount: BankAccount) = Database.Do.saveEntity bankAccount (fun t c -> t.fill c) 
    
    [<Extension>]
    static member delete(bankAccount: BankAccount) = Database.Do.deleteEntity bankAccount

    static member getAll() = Database.Do.getAllEntities Do.read

    static member getById(id: int) = Database.Do.getById id Do.read