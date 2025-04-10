module internal TickerExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.SQL
open DataReaderExtensions

    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member fill(ticker: Ticker, command: SqliteCommand) =
            command.Parameters.AddWithValue("@Id", ticker.Id) |> ignore
            command.Parameters.AddWithValue("@Symbol", ticker.Symbol) |> ignore
            command.Parameters.AddWithValue("@Image", ticker.Image) |> ignore
            command.Parameters.AddWithValue("@Name", ticker.Name) |> ignore
            command

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            let id = reader.GetInt32(reader.GetOrdinal("Id"))
            let symbol = reader.GetString(reader.GetOrdinal("Symbol"))
            let image = reader.getStringOrNone "Image" 
            let name = reader.getStringOrNone "Name"
            { 
                Id = id 
                Symbol = symbol 
                Image = image 
                Name = name 
            }

        [<Extension>]
        static member save(ticker: Ticker) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- 
                match ticker.Id with
                | 0 -> TickersQuery.insert
                | _ -> TickersQuery.update
            do! Database.Do.executeNonQuery(ticker.fill command) |> Async.AwaitTask |> Async.Ignore
        }

        [<Extension>]
        static member delete(ticker: Ticker) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TickersQuery.delete
            command.Parameters.AddWithValue("@Id", ticker.Id) |> ignore
            do! Database.Do.executeNonQuery(command) |> Async.AwaitTask |> Async.Ignore
        }

        static member getAll() = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TickersQuery.getAll
            let! tickers = Database.Do.readAll<Ticker>(command, Do.read)
            return tickers
        }

        static member getById(id: int) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TickersQuery.getById
            command.Parameters.AddWithValue("@Id", id) |> ignore
            let! tikers = Database.Do.readAll<Ticker>(command, Do.read)
            return tikers |> List.tryHead
        }