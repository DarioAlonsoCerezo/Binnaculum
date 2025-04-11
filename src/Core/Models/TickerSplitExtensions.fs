module internal TickerSplitExtensions

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
        static member fill(tickerSplit: TickerSplit, command: SqliteCommand) =
            command.fillParameters(
                [
                    ("@Id", tickerSplit.Id);
                    ("@SplitDate", tickerSplit.SplitDate);
                    ("@TickerId", tickerSplit.TickerId);
                    ("@SplitRatio", tickerSplit.SplitFactor);
                ])

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.getInt32 "Id"
                SplitDate = reader.getDateTime "SplitDate"
                TickerId = reader.getInt32 "TickerId"
                SplitFactor = reader.getDecimal "SplitFactor"
            }

        [<Extension>]
        static member save(tickerSplit: TickerSplit) =
            Database.Do.saveEntity tickerSplit (fun t c -> t.fill c) TickerSplitQuery.insert TickerSplitQuery.update
        
        [<Extension>]
        static member delete(tickerSplit: TickerSplit) = 
            Database.Do.deleteEntity tickerSplit TickerSplitQuery.delete

        static member getAll() = 
            Database.Do.getAllEntities TickerSplitQuery.getAll Do.read

        static member getById(id: int) = 
            Database.Do.getById id TickerSplitQuery.getById Do.read