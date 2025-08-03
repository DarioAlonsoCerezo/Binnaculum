module internal BankAccountExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions
open Binnaculum.Core.SQL
open OptionExtensions

[<Extension>]
type Do() =
    
    [<Extension>]
    static member fill(bankAccount: BankAccount, command: SqliteCommand) =
        command.fillEntityAuditable<BankAccount>(
            [
                (SQLParameterName.BankId, bankAccount.BankId);
                (SQLParameterName.Name, bankAccount.Name);
                (SQLParameterName.Description, bankAccount.Description.ToDbValue());
                (SQLParameterName.CurrencyId, bankAccount.CurrencyId);
            ], bankAccount)

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

    static member getAll() = Database.Do.getAllEntities Do.read BankAccountsQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id BankAccountsQuery.getById

    static member getByBankId(bankId: int) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BankAccountsQuery.getByBankId
            command.Parameters.AddWithValue(SQLParameterName.BankId, bankId) |> ignore
            let! accounts = Database.Do.readAll<BankAccount>(command, Do.read)
            return accounts
        }