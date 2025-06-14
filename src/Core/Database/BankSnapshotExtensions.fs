module internal BankSnapshotExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
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
    static member fill(bankSnapshot: BankSnapshot, command: SqliteCommand) =
        command.fillEntityAuditable<BankSnapshot>(
            [
                (SQLParameterName.Date, bankSnapshot.Base.Date.ToString());
                (SQLParameterName.BankId, bankSnapshot.BankId);
                (SQLParameterName.TotalBalance, bankSnapshot.TotalBalance.Value);
                (SQLParameterName.InterestEarned, bankSnapshot.InterestEarned.Value);
                (SQLParameterName.FeesPaid, bankSnapshot.FeesPaid.Value);
                (SQLParameterName.AccountCount, bankSnapshot.AccountCount);
            ], bankSnapshot)

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        let baseSnapshot = {
            Id = reader.getInt32 FieldName.Id
            Date = reader.getDateTimePattern FieldName.Date
            Audit = reader.getAudit()
        }
        {
            Base = baseSnapshot
            BankId = reader.getInt32 FieldName.BankId
            TotalBalance = reader.getMoney FieldName.TotalBalance
            InterestEarned = reader.getMoney FieldName.InterestEarned
            FeesPaid = reader.getMoney FieldName.FeesPaid
            AccountCount = reader.getInt32 FieldName.AccountCount
        }

    [<Extension>]
    static member save(bankSnapshot: BankSnapshot) = Database.Do.saveEntity bankSnapshot (fun t c -> t.fill c)
    
    [<Extension>]
    static member delete(bankSnapshot: BankSnapshot) = Database.Do.deleteEntity bankSnapshot

    static member getAll() = Database.Do.getAllEntities Do.read BankSnapshotQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id BankSnapshotQuery.getById

    static member getByBankId(bankId: int) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BankSnapshotQuery.getByBankId
        command.Parameters.AddWithValue(SQLParameterName.BankId, bankId) |> ignore
        let! result = Database.Do.readAll(command, Do.read)
        return result
    }

    static member getLatestByBankId(bankId: int) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BankSnapshotQuery.getLatestByBankId
        command.Parameters.AddWithValue(SQLParameterName.BankId, bankId) |> ignore
        let! result = Database.Do.read(command, Do.read)
        return result
    }

    static member getByBankIdAndDate(bankId: int, date: DateTimePattern) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BankSnapshotQuery.getByBankIdAndDate
        command.Parameters.AddWithValue(SQLParameterName.BankId, bankId) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.Date, date.ToString()) |> ignore
        let! result = Database.Do.read(command, Do.read)
        return result
    }

    static member getByDateRange(bankId: int, startDate: DateTimePattern, endDate: DateTimePattern) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BankSnapshotQuery.getByDateRange
        command.Parameters.AddWithValue(SQLParameterName.BankId, bankId) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.Date, startDate.ToString()) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.DateEnd, endDate.ToString()) |> ignore
        let! result = Database.Do.readAll(command, Do.read)
        return result
    }