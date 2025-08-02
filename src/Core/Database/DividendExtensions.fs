module internal DividendExtensions

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
    static member fill(dividend: Dividend, command: SqliteCommand) =
        command.fillEntityAuditable<Dividend>(
            [
                (SQLParameterName.TimeStamp, dividend.TimeStamp.ToString());
                (SQLParameterName.DividendAmount, dividend.DividendAmount.Value);
                (SQLParameterName.TickerId, dividend.TickerId);
                (SQLParameterName.CurrencyId, dividend.CurrencyId);
                (SQLParameterName.BrokerAccountId, dividend.BrokerAccountId);
            ], dividend)
        
    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Id = reader.getInt32 FieldName.Id
            TimeStamp = reader.getDateTimePattern FieldName.TimeStamp
            DividendAmount = reader.getMoney FieldName.DividendAmount
            TickerId = reader.getInt32 FieldName.TickerId
            CurrencyId = reader.getInt32 FieldName.CurrencyId
            BrokerAccountId = reader.getInt32 FieldName.BrokerAccountId
            Audit = reader.getAudit()
        }

    [<Extension>]
    static member save(dividend: Dividend) = 
        Database.Do.saveEntity dividend (fun t c -> t.fill c) 

    [<Extension>]
    static member delete(dividend: Dividend) = Database.Do.deleteEntity dividend

    static member getAll() = Database.Do.getAllEntities Do.read DividendsQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id DividendsQuery.getById

    static member getBetweenDates(startDate: string, endDate: string) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- DividendsQuery.getBetweenDates
            command.Parameters.AddWithValue("@StartDate", startDate) |> ignore
            command.Parameters.AddWithValue("@EndDate", endDate) |> ignore
            let! dividends = Database.Do.readAll<Dividend>(command, Do.read)
            return dividends
        }

    static member getByTickerCurrencyAndDateRange(tickerId: int, currencyId: int, fromDate: string option, toDate: string) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- DividendsQuery.getByTickerCurrencyAndDateRange
            command.Parameters.AddWithValue("@TickerId", tickerId) |> ignore
            command.Parameters.AddWithValue("@CurrencyId", currencyId) |> ignore
            command.Parameters.AddWithValue("@FromDate", fromDate |> Option.defaultValue "1900-01-01") |> ignore
            command.Parameters.AddWithValue("@ToDate", toDate) |> ignore
            let! dividends = Database.Do.readAll<Dividend>(command, Do.read)
            return dividends
        }

    static member getFilteredDividends(tickerId: int, currencyId: int, startDate: string, endDate: string) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- DividendsQuery.getFilteredDividends
            command.Parameters.AddWithValue("@TickerId", tickerId) |> ignore
            command.Parameters.AddWithValue("@CurrencyId", currencyId) |> ignore
            command.Parameters.AddWithValue("@StartDate", startDate) |> ignore
            command.Parameters.AddWithValue("@EndDate", endDate) |> ignore
            let! dividends = Database.Do.readAll<Dividend>(command, Do.read)
            return dividends
        }