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
    static member fill(snapshot: BankAccountSnapshot, command: SqliteCommand) =
        command.fillEntityAuditable<BankAccountSnapshot>(
            [
                (SQLParameterName.Date, snapshot.Base.Date.ToString());
                (SQLParameterName.BankAccountId, snapshot.BankAccountId);
                (SQLParameterName.Balance, snapshot.Balance.Value);
                (SQLParameterName.CurrencyId, snapshot.CurrencyId);
                (SQLParameterName.InterestEarned, snapshot.InterestEarned.Value);
                (SQLParameterName.FeesPaid, snapshot.FeesPaid.Value);
            ], snapshot)

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
    static member save(snapshot: BankAccountSnapshot) = Database.Do.saveEntity snapshot (fun s c -> s.fill c)
    
    [<Extension>]
    static member delete(snapshot: BankAccountSnapshot) = Database.Do.deleteEntity snapshot

    static member getAll() = Database.Do.getAllEntities Do.read BankAccountSnapshotQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id BankAccountSnapshotQuery.getById
    
    static member getByBankAccountId(bankAccountId: int) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BankAccountSnapshotQuery.getByBankAccountId
            command.Parameters.AddWithValue(SQLParameterName.BankAccountId, bankAccountId) |> ignore
            let! snapshots = Database.Do.readAll<BankAccountSnapshot>(command, Do.read)
            return snapshots
        }
        
    static member getLatestByBankAccountId(bankAccountId: int) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BankAccountSnapshotQuery.getLatestByBankAccountId
            command.Parameters.AddWithValue(SQLParameterName.BankAccountId, bankAccountId) |> ignore
            let! snapshot = Database.Do.read<BankAccountSnapshot>(command, Do.read)
            return snapshot
        }
        
    static member getByCurrencyId(currencyId: int) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BankAccountSnapshotQuery.getByCurrencyId
            command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId) |> ignore
            let! snapshots = Database.Do.readAll<BankAccountSnapshot>(command, Do.read)
            return snapshots
        }
        
    static member getByBankAccountIdAndDate(bankAccountId: int, date: DateTimePattern) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BankAccountSnapshotQuery.getByBankAccountIdAndDate
            command.Parameters.AddWithValue(SQLParameterName.BankAccountId, bankAccountId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.Date, date.ToString()) |> ignore
            let! snapshot = Database.Do.read<BankAccountSnapshot>(command, Do.read)
            return snapshot
        }
        
    static member getByDateRange(bankAccountId: int, startDate: DateTimePattern, endDate: DateTimePattern) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BankAccountSnapshotQuery.getByDateRange
            command.Parameters.AddWithValue(SQLParameterName.BankAccountId, bankAccountId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.Date, startDate.ToString()) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.DateEnd, endDate.ToString()) |> ignore
            let! snapshots = Database.Do.readAll<BankAccountSnapshot>(command, Do.read)
            return snapshots
        }

    static member getLatestBeforeDateByBankAccountId(bankAccountId: int, date: DateTimePattern) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BankAccountSnapshotQuery.getLatestBeforeDateByBankAccountId
            command.Parameters.AddWithValue(SQLParameterName.BankAccountId, bankAccountId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.Date, date.ToString()) |> ignore
            let! snapshot = Database.Do.read<BankAccountSnapshot>(command, Do.read)
            return snapshot
        }