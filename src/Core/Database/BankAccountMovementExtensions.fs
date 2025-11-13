module internal BankAccountBalanceExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions
open Binnaculum.Core.Database.TypeParser
open Binnaculum.Core.SQL
open Binnaculum.Core.Patterns

[<Extension>]
type Do() =
    
    [<Extension>]
    static member fill(bankAccountBalance: BankAccountMovement, command: SqliteCommand) =
        command.fillEntityAuditable<BankAccountMovement>(
            [
                (SQLParameterName.TimeStamp, bankAccountBalance.TimeStamp.ToString());
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

    static member getByBankAccountIdAndDateTo(bankAccountId: int, date: DateTimePattern) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BankAccountMovementsQuery.getByBankAccountIdAndDateTo
            command.Parameters.AddWithValue(SQLParameterName.BankAccountId, bankAccountId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.TimeStamp, date.ToString()) |> ignore
            let! movements = Database.Do.readAll<BankAccountMovement>(command, Do.read)
            return movements
        }

    static member getByBankAccountIdAndDateRange(bankAccountId: int, fromDate: DateTimePattern, toDate: DateTimePattern) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BankAccountMovementsQuery.getByBankAccountIdAndDateRange
            command.Parameters.AddWithValue(SQLParameterName.BankAccountId, bankAccountId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.TimeStampStart, fromDate.ToString()) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.TimeStampEnd, toDate.ToString()) |> ignore
            let! movements = Database.Do.readAll<BankAccountMovement>(command, Do.read)
            return movements
        }

    /// <summary>
    /// Load bank account movements with pagination support for a specific bank account.
    /// Returns movements ordered by TimeStamp DESC (most recent first).
    /// </summary>
    /// <param name="bankAccountId">The bank account ID to filter by</param>
    /// <param name="pageNumber">Zero-based page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>List of bank account movements for the specified page</returns>
    static member loadBankMovementsPaged(bankAccountId: int, pageNumber: int, pageSize: int) =
        task {
            let offset = pageNumber * pageSize
            let! command = Database.Do.createCommand ()
            command.CommandText <- BankAccountMovementsQuery.getByBankAccountIdPaged
            command.Parameters.AddWithValue(SQLParameterName.BankAccountId, bankAccountId) |> ignore
            command.Parameters.AddWithValue("@PageSize", pageSize) |> ignore
            command.Parameters.AddWithValue("@Offset", offset) |> ignore
            let! movements = Database.Do.readAll<BankAccountMovement> (command, Do.read)
            return movements
        }