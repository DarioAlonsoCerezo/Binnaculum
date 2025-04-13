module internal TickerSplitExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions

[<Extension>]
type Do() =
    
    [<Extension>]
    static member fill(tickerSplit: TickerSplit, command: SqliteCommand) =
        command.fillEntityAuditable<TickerSplit>(
            [
                (SQLParameterName.SplitDate, tickerSplit.SplitDate);
                (SQLParameterName.TickerId, tickerSplit.TickerId);
                (SQLParameterName.SplitFactor, tickerSplit.SplitFactor);
            ])

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Id = reader.getInt32 FieldName.Id
            SplitDate = reader.getDateTimePattern FieldName.SplitDate
            TickerId = reader.getInt32 FieldName.TickerId
            SplitFactor = reader.getDecimal FieldName.SplitFactor
            Audit = reader.getAudit()
        }

    [<Extension>]
    static member save(tickerSplit: TickerSplit) =
        Database.Do.saveEntity tickerSplit (fun t c -> t.fill c)
    
    [<Extension>]
    static member delete(tickerSplit: TickerSplit) = Database.Do.deleteEntity tickerSplit

    static member getAll() = Database.Do.getAllEntities Do.read

    static member getById(id: int) = Database.Do.getById id Do.read