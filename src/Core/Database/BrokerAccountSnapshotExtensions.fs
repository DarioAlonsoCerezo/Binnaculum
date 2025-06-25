module internal BrokerAccountSnapshotExtensions

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
    static member fill(snapshot: BrokerAccountSnapshot, command: SqliteCommand) =
        command.fillEntityAuditable<BrokerAccountSnapshot>(
            [
                (SQLParameterName.Date, snapshot.Base.Date.ToString());
                (SQLParameterName.BrokerAccountId, snapshot.BrokerAccountId);
                (SQLParameterName.PortfolioValue, snapshot.PortfolioValue.Value);
                (SQLParameterName.RealizedGains, snapshot.RealizedGains.Value);
                (SQLParameterName.RealizedPercentage, snapshot.RealizedPercentage);
                (SQLParameterName.UnrealizedGains, snapshot.UnrealizedGains.Value);
                (SQLParameterName.UnrealizedGainsPercentage, snapshot.UnrealizedGainsPercentage);
                (SQLParameterName.Invested, snapshot.Invested.Value);
                (SQLParameterName.Commissions, snapshot.Commissions.Value);
                (SQLParameterName.Fees, snapshot.Fees.Value);
                (SQLParameterName.Deposited, snapshot.Deposited.Value);
                (SQLParameterName.Withdrawn, snapshot.Withdrawn.Value);
                (SQLParameterName.DividendsReceived, snapshot.DividendsReceived.Value);
                (SQLParameterName.OptionsIncome, snapshot.OptionsIncome.Value);
                (SQLParameterName.OtherIncome, snapshot.OtherIncome.Value);
                (SQLParameterName.OpenTrades, snapshot.OpenTrades);
            ], snapshot)

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        let baseSnapshot = {
            Id = reader.getInt32 FieldName.Id
            Date = reader.getDateTimePattern FieldName.Date
            Audit = reader.getAudit()
        }
        {
            Base = baseSnapshot
            BrokerAccountId = reader.getInt32 FieldName.BrokerAccountId
            PortfolioValue = reader.getMoney FieldName.PortfolioValue
            RealizedGains = reader.getMoney FieldName.RealizedGains
            RealizedPercentage = reader.getDecimal FieldName.RealizedPercentage
            UnrealizedGains = reader.getMoney FieldName.UnrealizedGains
            UnrealizedGainsPercentage = reader.getDecimal FieldName.UnrealizedGainsPercentage
            Invested = reader.getMoney FieldName.Invested
            Commissions = reader.getMoney FieldName.Commissions
            Fees = reader.getMoney FieldName.Fees
            Deposited = reader.getMoney FieldName.Deposited
            Withdrawn = reader.getMoney FieldName.Withdrawn
            DividendsReceived = reader.getMoney FieldName.DividendsReceived
            OptionsIncome = reader.getMoney FieldName.OptionsIncome
            OtherIncome = reader.getMoney FieldName.OtherIncome
            OpenTrades = reader.getBoolean FieldName.OpenTrades
        }

    [<Extension>]
    static member save(snapshot: BrokerAccountSnapshot) = Database.Do.saveEntity snapshot (fun s c -> s.fill c)

    [<Extension>]
    static member delete(snapshot: BrokerAccountSnapshot) = Database.Do.deleteEntity snapshot

    static member getAll() = Database.Do.getAllEntities Do.read BrokerAccountSnapshotQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id BrokerAccountSnapshotQuery.getById

    static member getByBrokerAccountId(brokerAccountId: int) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BrokerAccountSnapshotQuery.getByBrokerAccountId
        command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId) |> ignore
        let! snapshots = Database.Do.readAll<BrokerAccountSnapshot>(command, Do.read)
        return snapshots
    }

    static member getLatestByBrokerAccountId(brokerAccountId: int) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BrokerAccountSnapshotQuery.getLatestByBrokerAccountId
        command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId) |> ignore
        let! snapshots = Database.Do.readAll<BrokerAccountSnapshot>(command, Do.read)
        return snapshots |> List.tryHead
    }

    static member getByBrokerAccountIdAndDate(brokerAccountId: int, date: DateTimePattern) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BrokerAccountSnapshotQuery.getByBrokerAccountIdAndDate
        command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.Date, date.ToString()) |> ignore
        let! snapshots = Database.Do.readAll<BrokerAccountSnapshot>(command, Do.read)
        return snapshots |> List.tryHead
    }

    static member getByDateRange(brokerAccountId: int, startDate: DateTimePattern, endDate: DateTimePattern) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BrokerAccountSnapshotQuery.getByDateRange
        command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.Date, startDate.ToString()) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.DateEnd, endDate.ToString()) |> ignore
        let! snapshots = Database.Do.readAll<BrokerAccountSnapshot>(command, Do.read)
        return snapshots
    }