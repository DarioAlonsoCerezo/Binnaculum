module internal BrokerSnapshotExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions
open Binnaculum.Core.SQL
open Binnaculum.Core.Patterns
open System

[<Extension>]
type Do() =
    
    [<Extension>]
    static member fill(brokerSnapshot: BrokerSnapshot, command: SqliteCommand) =
        command.fillEntityAuditable<BrokerSnapshot>(
            [
                (SQLParameterName.Date, brokerSnapshot.Base.Date.ToString());
                (SQLParameterName.BrokerId, brokerSnapshot.BrokerId);
                (SQLParameterName.PortfoliosValue, brokerSnapshot.PortfoliosValue.Value);
                (SQLParameterName.RealizedGains, brokerSnapshot.RealizedGains.Value);
                (SQLParameterName.RealizedPercentage, brokerSnapshot.RealizedPercentage);
                (SQLParameterName.AccountCount, brokerSnapshot.AccountCount);
                (SQLParameterName.Invested, brokerSnapshot.Invested.Value);
                (SQLParameterName.Commissions, brokerSnapshot.Commissions.Value);
                (SQLParameterName.Fees, brokerSnapshot.Fees.Value);
            ], brokerSnapshot)

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
    static member save(brokerSnapshot: BrokerSnapshot) = Database.Do.saveEntity brokerSnapshot (fun bs c -> bs.fill c)

    [<Extension>]
    static member delete(brokerSnapshot: BrokerSnapshot) = Database.Do.deleteEntity brokerSnapshot

    static member getAll() = Database.Do.getAllEntities Do.read BrokerSnapshotQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id BrokerSnapshotQuery.getById

    static member getByBrokerId(brokerId: int) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BrokerSnapshotQuery.getByBrokerId
        command.Parameters.AddWithValue(SQLParameterName.BrokerId, brokerId) |> ignore
        let! result = Database.Do.readAll<BrokerSnapshot>(command, Do.read)
        return result
    }

    static member getLatestByBrokerId(brokerId: int) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BrokerSnapshotQuery.getLatestByBrokerId
        command.Parameters.AddWithValue(SQLParameterName.BrokerId, brokerId) |> ignore
        let! result = Database.Do.read<BrokerSnapshot>(command, Do.read)
        return result
    }

    static member getByBrokerIdAndDate(brokerId: int, date: DateTimePattern) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BrokerSnapshotQuery.getByBrokerIdAndDate
        command.Parameters.AddWithValue(SQLParameterName.BrokerId, brokerId) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.Date, date.ToString()) |> ignore
        let! result = Database.Do.read<BrokerSnapshot>(command, Do.read)
        return result
    }

    static member getByDateRange(brokerId: int, startDate: DateTimePattern, endDate: DateTimePattern) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BrokerSnapshotQuery.getByDateRange
        command.Parameters.AddWithValue(SQLParameterName.BrokerId, brokerId) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.Date, startDate.ToString()) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.DateEnd, endDate.ToString()) |> ignore
        let! result = Database.Do.readAll<BrokerSnapshot>(command, Do.read)
        return result
    }