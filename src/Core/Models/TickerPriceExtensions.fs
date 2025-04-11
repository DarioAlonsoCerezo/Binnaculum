module internal TickerPriceExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.SQL
open DataReaderExtensions
open CommandExtensions

    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member fill(tickerPrice: TickerPrice, command: SqliteCommand) =
            command.fillParameters(
                [
                    ("@Id", tickerPrice.Id);
                    ("@PriceDate", tickerPrice.PriceDate);
                    ("@TickerId", tickerPrice.TickerId);
                    ("@Price", tickerPrice.Price);
                    ("@CurrencyId", tickerPrice.CurrencyId);
                ])

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.getInt32 "Id"
                PriceDate = reader.getDateTime "PriceDate"
                TickerId = reader.getInt32 "TickerId"
                Price = reader.getDecimal "Price"
                CurrencyId = reader.getInt32 "CurrencyId"
            }

        [<Extension>]
        static member save(tickerPrice: TickerPrice) =
            Database.Do.saveEntity 
                tickerPrice 
                (fun t c -> t.fill c) 
                TickerPriceQuery.insert TickerPriceQuery.update
        
        [<Extension>]
        static member delete(tickerPrice: TickerPrice) = 
            Database.Do.deleteEntity tickerPrice TickerPriceQuery.delete

        static member getAll() = 
            Database.Do.getAllEntities TickerPriceQuery.getAll Do.read

        static member getById(id: int) = 
            Database.Do.getById id TickerPriceQuery.getById Do.read