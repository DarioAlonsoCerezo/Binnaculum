module internal TickerExtensions

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
        static member fill(ticker: Ticker, command: SqliteCommand) =
            command.fillParameters(
                [
                    ("@Id", ticker.Id);
                    ("@Symbol", ticker.Symbol);
                    ("@Image", ticker.Image);
                    ("@Name", ticker.Name);
                ])

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.getInt32 "Id"
                Symbol = reader.getString "Symbol"
                Image = reader.getStringOrNone "Image"
                Name = reader.getStringOrNone "Name"
            }

        [<Extension>]
        static member save(ticker: Ticker) =
            Database.Do.saveEntity ticker (fun t c -> t.fill c) TickersQuery.insert TickersQuery.update

        [<Extension>]
        static member delete(ticker: Ticker) = 
            Database.Do.deleteEntity ticker TickersQuery.delete

        static member getAll() = 
            Database.Do.getAllEntities TickersQuery.getAll Do.read

        static member getById(id: int) = 
            Database.Do.getById id TickersQuery.getById Do.read