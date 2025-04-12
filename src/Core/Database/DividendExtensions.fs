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
                (SQLParameterName.Id, dividend.Id);
                (SQLParameterName.TimeStamp, dividend.TimeStamp);
                (SQLParameterName.DividendAmount, dividend.DividendAmount);
                (SQLParameterName.TickerId, dividend.TickerId);
                (SQLParameterName.CurrencyId, dividend.CurrencyId);
                (SQLParameterName.BrokerAccountId, dividend.BrokerAccountId);
            ])
        
    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Id = reader.getInt32 FieldName.Id
            TimeStamp = reader.getDateTime FieldName.TimeStamp
            DividendAmount = reader.getDecimal FieldName.DividendAmount
            TickerId = reader.getInt32 FieldName.TickerId
            CurrencyId = reader.getInt32 FieldName.CurrencyId
            BrokerAccountId = reader.getInt32 FieldName.BrokerAccountId
        }

    [<Extension>]
    static member save(dividend: Dividend) = 
        Database.Do.saveEntity dividend (fun t c -> t.fill c) 

    [<Extension>]
    static member delete(dividend: Dividend) = Database.Do.deleteEntity dividend

    static member getAll() = Database.Do.getAllEntities Do.read

    static member getById(id: int) = Database.Do.getById id Do.read