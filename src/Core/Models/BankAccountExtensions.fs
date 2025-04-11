module internal BankAccountExtensions

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
        static member fill(bankAccount: BankAccount, command: SqliteCommand) =
            command.fillParameters(
                [
                    ("@Id", bankAccount.Id);
                    ("@BankId", bankAccount.BankId);
                    ("@Name", bankAccount.Name);
                    ("@Description", bankAccount.Description);
                    ("@CurrencyId", bankAccount.CurrencyId);
                ])

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.getInt32 "Id"
                BankId = reader.getInt32 "BankId"
                Name = reader.getString "Name"
                Description = reader.getStringOrNone "Description"
                CurrencyId = reader.getInt32 "CurrencyId"
            }

        [<Extension>]
        static member save(bankAccount: BankAccount) =
            Database.Do.saveEntity bankAccount (fun t c -> t.fill c) BankAccountsQuery.insert BankAccountsQuery.update
        
        [<Extension>]
        static member delete(bankAccount: BankAccount) = 
            Database.Do.deleteEntity bankAccount BankAccountsQuery.delete

        static member getAll() = 
            Database.Do.getAllEntities BankAccountsQuery.getAll Do.read

        static member getById(id: int) = 
            Database.Do.getById id BankAccountsQuery.getById Do.read