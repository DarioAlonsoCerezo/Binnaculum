module internal InvestmentOverviewSnapshotExtensions

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
    static member fill(snapshot: InvestmentOverviewSnapshot, command: SqliteCommand) =
        command.fillEntityAuditable<InvestmentOverviewSnapshot>(
            [
                (SQLParameterName.Date, snapshot.Base.Date.ToString());
                (SQLParameterName.PortfoliosValue, snapshot.PortfoliosValue.Value);
                (SQLParameterName.RealizedGains, snapshot.RealizedGains.Value);
                (SQLParameterName.RealizedPercentage, snapshot.RealizedPercentage);
                (SQLParameterName.Invested, snapshot.Invested.Value);
                (SQLParameterName.Commissions, snapshot.Commissions.Value);
                (SQLParameterName.Fees, snapshot.Fees.Value);
            ], snapshot)

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Base = {
                Id = reader.getInt32 FieldName.Id
                Date = DateTimePattern.Parse(reader.getString FieldName.Date)
                Audit = reader.getAudit()
            }
            PortfoliosValue = reader.getMoney FieldName.PortfoliosValue
            RealizedGains = reader.getMoney FieldName.RealizedGains
            RealizedPercentage = reader.getDecimal FieldName.RealizedPercentage
            Invested = reader.getMoney FieldName.Invested
            Commissions = reader.getMoney FieldName.Commissions
            Fees = reader.getMoney FieldName.Fees
        }

    [<Extension>]
    static member save(snapshot: InvestmentOverviewSnapshot) = Database.Do.saveEntity snapshot (fun s c -> s.fill c)

    [<Extension>]
    static member delete(snapshot: InvestmentOverviewSnapshot) = Database.Do.deleteEntity snapshot

    static member getAll() = Database.Do.getAllEntities Do.read InvestmentOverviewSnapshotQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id InvestmentOverviewSnapshotQuery.getById

    static member getByDate(date: DateTimePattern) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- InvestmentOverviewSnapshotQuery.getByDate
        command.Parameters.AddWithValue(SQLParameterName.Date, date.ToString()) |> ignore
        let! snapshots = Database.Do.readAll<InvestmentOverviewSnapshot>(command, Do.read)
        return snapshots |> List.tryHead
    }

    static member getLatest() = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- InvestmentOverviewSnapshotQuery.getLatest
        let! snapshots = Database.Do.readAll<InvestmentOverviewSnapshot>(command, Do.read)
        return snapshots |> List.tryHead
    }

    static member getByDateRange(startDate: DateTimePattern, endDate: DateTimePattern) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- InvestmentOverviewSnapshotQuery.getByDateRange
        command.Parameters.AddWithValue(SQLParameterName.Date, startDate.ToString()) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.DateEnd, endDate.ToString()) |> ignore
        let! snapshots = Database.Do.readAll<InvestmentOverviewSnapshot>(command, Do.read)
        return snapshots
    }