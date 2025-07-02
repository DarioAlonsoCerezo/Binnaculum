module internal BankExtensions

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
    static member fill(bank: Bank, command: SqliteCommand) =
        command.fillEntityAuditable<Bank>(
            [
                (SQLParameterName.Name, bank.Name);
                (SQLParameterName.Image, bank.Image.ToDbValue());
            ], bank)

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Id = reader.getInt32 FieldName.Id
            Name = reader.getString FieldName.Name
            Image = reader.getStringOrNone FieldName.Image
            Audit = reader.getAudit()
        }

    [<Extension>]
    static member save(bank: Bank) = Database.Do.saveEntity bank (fun b c -> b.fill c)

    [<Extension>]
    static member delete(bank: Bank) = Database.Do.deleteEntity bank

    static member getAll() = Database.Do.getAllEntities Do.read BankQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id BankQuery.getById

    static member getLatest() = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BankQuery.getLatest
        let! banks = Database.Do.readAll<Bank>(command, Do.read)
        return banks |> List.tryHead
    }