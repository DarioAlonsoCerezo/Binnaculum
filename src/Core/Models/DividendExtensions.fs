module internal DividendExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions

    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member fill(dividend: Dividend, command: SqliteCommand) =
            command.fillParameters(
                [
                    ("@Id", dividend.Id);
                    ("@TimeStamp", dividend.TimeStamp);
                    ("@DividendAmount", dividend.DividendAmount);
                    ("@TickerId", dividend.TickerId);
                    ("@CurrencyId", dividend.CurrencyId);
                    ("@BrokerAccountId", dividend.BrokerAccountId);
                ])
            
        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.getInt32 "Id"
                TimeStamp = reader.getDateTime "TimeStamp"
                DividendAmount = reader.getDecimal "DividendAmount"
                TickerId = reader.getInt32 "TickerId"
                CurrencyId = reader.getInt32 "CurrencyId"
                BrokerAccountId = reader.getInt32 "BrokerAccountId"
            }

        [<Extension>]
        static member save(dividend: Dividend) = 
            Database.Do.saveEntity dividend (fun t c -> t.fill c) 

        [<Extension>]
        static member delete(dividend: Dividend) = Database.Do.deleteEntity dividend

        static member getAll() = Database.Do.getAllEntities Do.read

        static member getById(id: int) = Database.Do.getById id Do.read