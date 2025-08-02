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
                (SQLParameterName.Price, tickerPrice.Price.Value);
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

    static member getPriceByDateOrPrevious(tickerId: int, priceDate: string) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TickerPriceQuery.getPriceByDateOrPrevious
            command.Parameters.AddWithValue("@TickerId", tickerId) |> ignore
            command.Parameters.AddWithValue("@PriceDate", priceDate) |> ignore
            let! fromDatabase = Database.Do.readAll<TickerPrice>(command, Do.read)
            return fromDatabase 
                |> List.tryHead 
                |> Option.map (fun p -> p.Price.Value) 
                |> Option.defaultValue 0m
        }

    static member getCurrenciesByTickerAndDate(tickerId: int, date: string) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TickerPriceQuery.getCurrenciesByTickerAndDate
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.Date, date) |> ignore
            use reader = command.ExecuteReader()
            let mutable currencies = []
            while reader.Read() do
                let currencyId = reader.GetInt32(0)
                currencies <- currencyId :: currencies
            return currencies |> List.rev
        }        