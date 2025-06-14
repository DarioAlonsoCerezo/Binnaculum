module internal BankAccountSnapshotExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.SnapshotsModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions
open Binnaculum.Core.SQL
open Binnaculum.Core.Patterns

[<Extension>]
type Do() =
    
    [<Extension>]
    static member fill(bankAccountSnapshot: BankAccountSnapshot, command: SqliteCommand) =
        command.fillEntityAuditable<BankAccountSnapshot>(
            [
                (SQLParameterName.Date, bankAccountSnapshot.Base.Date.ToString());
                (SQLParameterName.BankAccountId, bankAccountSnapshot.BankAccountId);
                (SQLParameterName.Balance, bankAccountSnapshot.Balance.Value);
                (SQLParameterName.CurrencyId, bankAccountSnapshot.CurrencyId);
                (SQLParameterName.InterestEarned, bankAccountSnapshot.InterestEarned.Value);
                (SQLParameterName.FeesPaid, bankAccountSnapshot.FeesPaid.Value);
            ], bankAccountSnapshot)

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Base = {
                Id = reader.getInt32 FieldName.Id
                Date = reader.getDateTimePattern FieldName.Date
                Audit = reader.getAudit()
            }
            BankAccountId = reader.getInt32 FieldName.BankAccountId
            Balance = reader.getMoney FieldName.Balance
            CurrencyId = reader.getInt32 FieldName.CurrencyId
            InterestEarned = reader.getMoney FieldName.InterestEarned
            FeesPaid = reader.getMoney FieldName.FeesPaid
        }

    [<Extension>]
    static member save(bankAccountSnapshot: BankAccountSnapshot) = Database.Do.saveEntity bankAccountSnapshot (fun t c -> t.fill c)

    [<Extension>]
    static member delete(bankAccountSnapshot: BankAccountSnapshot) = Database.Do.deleteEntity bankAccountSnapshot

    static member getAll() = Database.Do.getAllEntities Do.read BankAccountSnapshotQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id BankAccountSnapshotQuery.getById

    static member getByBankAccountId(bankAccountId: int) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BankAccountSnapshotQuery.getByBankAccountId
        command.Parameters.AddWithValue(SQLParameterName.BankAccountId, bankAccountId) |> ignore
        return! Database.Do.readAll(command, Do.read)
    }

    static member getLatestByBankAccountId(bankAccountId: int) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BankAccountSnapshotQuery.getLatestByBankAccountId
        command.Parameters.AddWithValue(SQLParameterName.BankAccountId, bankAccountId) |> ignore
        let! result = Database.Do.read(command, Do.read)
        return result
    }

    static member getByCurrencyId(currencyId: int) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BankAccountSnapshotQuery.getByCurrencyId
        command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId) |> ignore
        return! Database.Do.readAll(command, Do.read)
    }

    static member getByBankAccountIdAndDate(bankAccountId: int, date: DateTimePattern) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BankAccountSnapshotQuery.getByBankAccountIdAndDate
        command.Parameters.AddWithValue(SQLParameterName.BankAccountId, bankAccountId) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.Date, date.ToString()) |> ignore
        let! result = Database.Do.read(command, Do.read)
        return result
    }

    static member getByDateRange(bankAccountId: int, startDate: DateTimePattern, endDate: DateTimePattern) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BankAccountSnapshotQuery.getByDateRange
        command.Parameters.AddWithValue(SQLParameterName.BankAccountId, bankAccountId) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.Date, startDate.ToString()) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.DateEnd, endDate.ToString()) |> ignore
        return! Database.Do.readAll(command, Do.read)
    }