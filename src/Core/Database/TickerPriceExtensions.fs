module internal TickerPriceExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions
open Binnaculum.Core.SQL

[<Extension>]
type Do() =
    
    [<Extension>]
    static member fill(tickerPrice: TickerPrice, command: SqliteCommand) =
        command.fillEntityAuditable<TickerPrice>(
            [
                (SQLParameterName.PriceDate, tickerPrice.PriceDate);
                (SQLParameterName.TickerId, tickerPrice.TickerId);
                (SQLParameterName.Price, tickerPrice.Price);
                (SQLParameterName.CurrencyId, tickerPrice.CurrencyId);
            ], tickerPrice)

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Id = reader.getInt32 FieldName.Id
            PriceDate = reader.getDateTimePattern FieldName.PriceDate
            TickerId = reader.getInt32 FieldName.TickerId
            Price = reader.getMoney FieldName.Price
            CurrencyId = reader.getInt32 FieldName.CurrencyId
            Audit = reader.getAudit()
        }

    [<Extension>]
    static member save(tickerPrice: TickerPrice) =
        Database.Do.saveEntity tickerPrice (fun t c -> t.fill c)
    
    [<Extension>]
    static member delete(tickerPrice: TickerPrice) = Database.Do.deleteEntity tickerPrice 

    static member getAll() = Database.Do.getAllEntities Do.read TickerPriceQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id TickerPriceQuery.getById