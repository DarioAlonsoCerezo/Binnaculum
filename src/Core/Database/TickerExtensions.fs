module internal TickerExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions
open Binnaculum.Core.SQL
open OptionExtensions
open Binnaculum.Core.Patterns
open System

[<Extension>]
type Do() =
    
    [<Extension>]
    static member fill(ticker: Ticker, command: SqliteCommand) =
        command.fillEntityAuditable<Ticker>(
            [
                (SQLParameterName.Symbol, ticker.Symbol);
                (SQLParameterName.Image, ticker.Image.ToDbValue());
                (SQLParameterName.Name, ticker.Name.ToDbValue());
            ], ticker)

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

    static member getAll() = Database.Do.getAllEntities Do.read TickersQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id TickersQuery.getById

    static member getBySymbol(symbol: string) : System.Threading.Tasks.Task<Ticker> = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- TickersQuery.getByTicker
        command.Parameters.AddWithValue(SQLParameterName.Symbol, symbol) |> ignore
        let! ticker = Database.Do.read<Ticker>(command, Do.read)
        match ticker with
        | Some existingTicker -> 
            return existingTicker
        | None ->
            // Create new ticker with symbol and default values
            let audit = { CreatedAt = Some(DateTimePattern.FromDateTime(DateTime.Now)); UpdatedAt = None; }
            let newTicker = { Id = 0; Symbol = symbol; Image = None; Name = None; Audit = audit; }
            do! newTicker.save() |> Async.AwaitTask |> Async.Ignore
            
            // Retrieve the saved ticker with the assigned ID
            let! command2 = Database.Do.createCommand()
            command2.CommandText <- TickersQuery.getByTicker
            command2.Parameters.AddWithValue(SQLParameterName.Symbol, symbol) |> ignore
            let! savedTicker = Database.Do.read<Ticker>(command2, Do.read)
            match savedTicker with
            | Some ticker -> return ticker
            | None -> return failwithf "Failed to retrieve newly created ticker with symbol %s" symbol
    }

    static member tickerList() = 
        let audit = { CreatedAt = Some(DateTimePattern.FromDateTime(DateTime.Now)); UpdatedAt = None; }
        [
            { Id = 0; Symbol = "SPY"; Image = Some("spy.png"); Name = Some("SPDR S&P 500 ETF Trust"); Audit = audit; }
        ]

    static member exists(symbol: string) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- TickersQuery.getByTicker
        command.Parameters.AddWithValue(SQLParameterName.Symbol, symbol) |> ignore
        let! result = command.ExecuteScalarAsync() |> Async.AwaitTask
        return result <> null
    }
    
    static member insertIfNotExists() = task {
        let audit = { CreatedAt = Some(DateTimePattern.FromDateTime(DateTime.Now)); UpdatedAt = None; }
        let spy = { Id = 0; Symbol = "SPY"; Image = Some("spy.png"); Name = Some("SPDR S&P 500 ETF Trust"); Audit = audit; }
        let! exists = Do.exists(spy.Symbol)
        if not exists then
            do! spy.save()
    }