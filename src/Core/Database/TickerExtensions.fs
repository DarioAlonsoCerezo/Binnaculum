module internal TickerExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions

[<Extension>]
type Do() =
    
    [<Extension>]
    static member fill(ticker: Ticker, command: SqliteCommand) =
        command.fillEntityAuditable<Ticker>(
            [
                (SQLParameterName.Symbol, ticker.Symbol);
                (SQLParameterName.Image, ticker.Image);
                (SQLParameterName.Name, ticker.Name);
            ])

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Id = reader.getInt32 FieldName.Id
            Symbol = reader.getString FieldName.Symbol
            Image = reader.getStringOrNone FieldName.Image
            Name = reader.getStringOrNone FieldName.Name
            Audit = reader.getAudit()
        }

    [<Extension>]
    static member save(ticker: Ticker) = Database.Do.saveEntity ticker (fun t c -> t.fill c)

    [<Extension>]
    static member delete(ticker: Ticker) = Database.Do.deleteEntity ticker

    static member getAll() = Database.Do.getAllEntities Do.read

    static member getById(id: int) = Database.Do.getById id Do.read