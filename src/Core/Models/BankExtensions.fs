module internal BankExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions

    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member fill(bank: Bank, command: SqliteCommand) =
            command.fillParameters(
                [
                    ("@Id", bank.Id);
                    ("@Name", bank.Name);
                    ("@Image", bank.Image);
                ])

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.getInt32 "Id"
                Name = reader.getString "Name"
                Image = reader.getStringOrNone "Image"
            }

        [<Extension>]
        static member save(bank: Bank) = Database.Do.saveEntity bank (fun b c -> b.fill c)

        [<Extension>]
        static member delete(bank: Bank) = Database.Do.deleteEntity bank

        static member getAll() = Database.Do.getAllEntities Do.read

        static member getById(id: int) = Database.Do.getById id Do.read
        