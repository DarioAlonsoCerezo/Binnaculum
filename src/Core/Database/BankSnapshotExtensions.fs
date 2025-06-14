module internal BankSnapshotExtensions

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
    static member fill(snapshot: BankSnapshot, command: SqliteCommand) =
        command.fillEntityAuditable<BankSnapshot>(
            [
                (SQLParameterName.Date, snapshot.Base.Date.ToString());
                (SQLParameterName.BankId, snapshot.BankId);
                (SQLParameterName.TotalBalance, snapshot.TotalBalance.Value);
                (SQLParameterName.InterestEarned, snapshot.InterestEarned.Value);
                (SQLParameterName.FeesPaid, snapshot.FeesPaid.Value);
                (SQLParameterName.AccountCount, snapshot.AccountCount);
            ], snapshot)

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Base = {
                Id = reader.getInt32 FieldName.Id
                Date = reader.getDateTimePattern FieldName.Date
                Audit = reader.getAudit()
            }
            BankId = reader.getInt32 FieldName.BankId
            TotalBalance = reader.getMoney FieldName.TotalBalance
            InterestEarned = reader.getMoney FieldName.InterestEarned
            FeesPaid = reader.getMoney FieldName.FeesPaid
            AccountCount = reader.getInt32 FieldName.AccountCount
        }

    [<Extension>]
    static member save(snapshot: BankSnapshot) = Database.Do.saveEntity snapshot (fun s c -> s.fill c)
    
    [<Extension>]
    static member delete(snapshot: BankSnapshot) = Database.Do.deleteEntity snapshot

    static member getAll() = Database.Do.getAllEntities Do.read BankSnapshotQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id BankSnapshotQuery.getById
    
    static member getByBankId(bankId: int) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BankSnapshotQuery.getByBankId
            command.Parameters.AddWithValue(SQLParameterName.BankId, bankId) |> ignore
            let! snapshots = Database.Do.readAll<BankSnapshot>(command, Do.read)
            return snapshots
        }
        
    static member getLatestByBankId(bankId: int) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BankSnapshotQuery.getLatestByBankId
            command.Parameters.AddWithValue(SQLParameterName.BankId, bankId) |> ignore
            let! snapshot = Database.Do.read<BankSnapshot>(command, Do.read)
            return snapshot
        }
        
    static member getByBankIdAndDate(bankId: int, date: DateTimePattern) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BankSnapshotQuery.getByBankIdAndDate
            command.Parameters.AddWithValue(SQLParameterName.BankId, bankId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.Date, date.ToString()) |> ignore
            let! snapshot = Database.Do.read<BankSnapshot>(command, Do.read)
            return snapshot
        }
        
    static member getByDateRange(bankId: int, startDate: DateTimePattern, endDate: DateTimePattern) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BankSnapshotQuery.getByDateRange
            command.Parameters.AddWithValue(SQLParameterName.BankId, bankId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.Date, startDate.ToString()) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.DateEnd, endDate.ToString()) |> ignore
            let! snapshots = Database.Do.readAll<BankSnapshot>(command, Do.read)
            return snapshots
        }