module internal BrokerFinancialSnapshotExtensions

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
    static member fill(snapshot: BrokerFinancialSnapshot, command: SqliteCommand) =
        command.fillEntityAuditable<BrokerFinancialSnapshot>(
            [
                (SQLParameterName.Date, snapshot.Base.Date.ToString());
                (SQLParameterName.CurrencyId, snapshot.CurrencyId);
                (SQLParameterName.MovementCounter, snapshot.MovementCounter);
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
        {
            Base = {
                Id = reader.getInt32 FieldName.Id
                Date = reader.getDateTimePattern FieldName.Date
                Audit = reader.getAudit()
            }
            CurrencyId = reader.getInt32 FieldName.CurrencyId
            MovementCounter = reader.getInt32 FieldName.MovementCounter
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
    static member save(snapshot: BrokerFinancialSnapshot) = Database.Do.saveEntity snapshot (fun s c -> s.fill c)

    [<Extension>]
    static member delete(snapshot: BrokerFinancialSnapshot) = Database.Do.deleteEntity snapshot

    static member getAll() = Database.Do.getAllEntities Do.read BrokerFinancialSnapshotQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id BrokerFinancialSnapshotQuery.getById

    static member getByCurrencyId(currencyId: int) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BrokerFinancialSnapshotQuery.getByCurrencyId
        command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId) |> ignore
        let! snapshots = Database.Do.readAll<BrokerFinancialSnapshot>(command, Do.read)
        return snapshots
    }

    static member getLatestByCurrencyId(currencyId: int) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BrokerFinancialSnapshotQuery.getLatestByCurrencyId
        command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId) |> ignore
        let! snapshots = Database.Do.readAll<BrokerFinancialSnapshot>(command, Do.read)
        return snapshots |> List.tryHead
    }

    static member getByCurrencyIdAndDate(currencyId: int, date: DateTimePattern) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BrokerFinancialSnapshotQuery.getByCurrencyIdAndDate
        command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.Date, date.ToString()) |> ignore
        let! snapshots = Database.Do.readAll<BrokerFinancialSnapshot>(command, Do.read)
        return snapshots |> List.tryHead
    }

    static member getByDateRange(currencyId: int, startDate: DateTimePattern, endDate: DateTimePattern) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BrokerFinancialSnapshotQuery.getByDateRange
        command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.Date, startDate.ToString()) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.DateEnd, endDate.ToString()) |> ignore
        let! snapshots = Database.Do.readAll<BrokerFinancialSnapshot>(command, Do.read)
        return snapshots
    }

    static member getByMovementCounter(movementCounter: int) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- BrokerFinancialSnapshotQuery.getByMovementCounter
        command.Parameters.AddWithValue(SQLParameterName.MovementCounter, movementCounter) |> ignore
        let! snapshots = Database.Do.readAll<BrokerFinancialSnapshot>(command, Do.read)
        return snapshots
    }