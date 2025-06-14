module internal BrokerSnapshotExtensions

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
    static member fill(snapshot: BrokerSnapshot, command: SqliteCommand) =
        command.fillEntityAuditable<BrokerSnapshot>(
            [
                (SQLParameterName.Date, snapshot.Base.Date.ToString());
                (SQLParameterName.BrokerId, snapshot.BrokerId);
                (SQLParameterName.PortfoliosValue, snapshot.PortfoliosValue.Value);
                (SQLParameterName.RealizedGains, snapshot.RealizedGains.Value);
                (SQLParameterName.RealizedPercentage, snapshot.RealizedPercentage);
                (SQLParameterName.AccountCount, snapshot.AccountCount);
                (SQLParameterName.Invested, snapshot.Invested.Value);
                (SQLParameterName.Commissions, snapshot.Commissions.Value);
                (SQLParameterName.Fees, snapshot.Fees.Value);
            ], snapshot)

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Base = {
                Id = reader.getInt32 FieldName.Id
                Date = reader.getDateTimePattern FieldName.Date
                Audit = reader.getAudit()
            }
            BrokerId = reader.getInt32 FieldName.BrokerId
            PortfoliosValue = reader.getMoney FieldName.PortfoliosValue
            RealizedGains = reader.getMoney FieldName.RealizedGains
            RealizedPercentage = reader.getDecimal FieldName.RealizedPercentage
            AccountCount = reader.getInt32 FieldName.AccountCount
            Invested = reader.getMoney FieldName.Invested
            Commissions = reader.getMoney FieldName.Commissions
            Fees = reader.getMoney FieldName.Fees
        }

    [<Extension>]
    static member save(snapshot: BrokerSnapshot) = Database.Do.saveEntity snapshot (fun s c -> s.fill c)
    
    [<Extension>]
    static member delete(snapshot: BrokerSnapshot) = Database.Do.deleteEntity snapshot

    static member getAll() = Database.Do.getAllEntities Do.read BrokerSnapshotQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id BrokerSnapshotQuery.getById
    
    static member getByBrokerId(brokerId: int) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BrokerSnapshotQuery.getByBrokerId
            command.Parameters.AddWithValue(SQLParameterName.BrokerId, brokerId) |> ignore
            let! snapshots = Database.Do.readAll<BrokerSnapshot>(command, Do.read)
            return snapshots
        }
        
    static member getLatestByBrokerId(brokerId: int) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BrokerSnapshotQuery.getLatestByBrokerId
            command.Parameters.AddWithValue(SQLParameterName.BrokerId, brokerId) |> ignore
            let! snapshot = Database.Do.read<BrokerSnapshot>(command, Do.read)
            return snapshot
        }
        
    static member getByBrokerIdAndDate(brokerId: int, date: DateTimePattern) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BrokerSnapshotQuery.getByBrokerIdAndDate
            command.Parameters.AddWithValue(SQLParameterName.BrokerId, brokerId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.Date, date.ToString()) |> ignore
            let! snapshot = Database.Do.read<BrokerSnapshot>(command, Do.read)
            return snapshot
        }
        
    static member getByDateRange(brokerId: int, startDate: DateTimePattern, endDate: DateTimePattern) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BrokerSnapshotQuery.getByDateRange
            command.Parameters.AddWithValue(SQLParameterName.BrokerId, brokerId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.Date, startDate.ToString()) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.DateEnd, endDate.ToString()) |> ignore
            let! snapshots = Database.Do.readAll<BrokerSnapshot>(command, Do.read)
            return snapshots
        }