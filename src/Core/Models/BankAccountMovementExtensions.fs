module internal BankAccountBalanceExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.SQL
open DataReaderExtensions
open CommandExtensions
open Binnaculum.Core.Database.TypeParser

    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member fill(bankAccountBalance: BankAccountMovement, command: SqliteCommand) =
            command.fillParameters(
                [
                    ("@Id", bankAccountBalance.Id);
                    ("@TimeStamp", bankAccountBalance.TimeStamp);
                    ("@Amount", bankAccountBalance.Amount);
                    ("@BankAccountId", bankAccountBalance.BankAccountId);
                    ("@CurrencyId", bankAccountBalance.CurrencyId);
                    ("@MovementType", fromBankMovementTypeToDatabase bankAccountBalance.MovementType);
                ])

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.getInt32 "Id"
                TimeStamp = reader.getDateTime "TimeStamp"
                Amount = reader.getDecimal "Amount"
                BankAccountId = reader.getInt32 "BankAccountId"
                CurrencyId = reader.getInt32 "CurrencyId"
                MovementType = fromDatabaseToBankMovementType (reader.getString "MovementType")
            }

        [<Extension>]
        static member save(bankAccountBalance: BankAccountMovement) =
            Database.Do.saveEntity bankAccountBalance (fun t c -> t.fill c) BankAccountMovementsQuery.insert BankAccountMovementsQuery.update
        
        [<Extension>]
        static member delete(bankAccountBalance: BankAccountMovement) = 
            Database.Do.deleteEntity bankAccountBalance BankAccountMovementsQuery.delete

        static member getAll() = 
            Database.Do.getAllEntities BankAccountMovementsQuery.getAll Do.read

        static member getById(id: int) = 
            Database.Do.getById id BankAccountMovementsQuery.getById Do.read