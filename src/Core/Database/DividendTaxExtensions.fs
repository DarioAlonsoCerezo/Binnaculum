module internal DividendTaxExtensions

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
    static member fill(dividendTax: DividendTax, command: SqliteCommand) =
        command.fillEntityAuditable<DividendTax>(
            [
                (SQLParameterName.TimeStamp, dividendTax.TimeStamp.ToString());
                (SQLParameterName.DividendTaxAmount, dividendTax.DividendTaxAmount.Value);
                (SQLParameterName.TickerId, dividendTax.TickerId);
                (SQLParameterName.CurrencyId, dividendTax.CurrencyId);
                (SQLParameterName.BrokerAccountId, dividendTax.BrokerAccountId);
            ], dividendTax)
        
    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Id = reader.getInt32 FieldName.Id
            TimeStamp = reader.getDateTimePattern FieldName.TimeStamp
            DividendTaxAmount = reader.getMoney FieldName.DividendTaxAmount
            TickerId = reader.getInt32 FieldName.TickerId
            CurrencyId = reader.getInt32 FieldName.CurrencyId
            BrokerAccountId = reader.getInt32 FieldName.BrokerAccountId
            Audit = reader.getAudit()
        }

    [<Extension>]
    static member save(dividendTax: DividendTax) = Database.Do.saveEntity dividendTax (fun t c -> t.fill c) 

    [<Extension>]
    static member delete(dividendTax: DividendTax) = Database.Do.deleteEntity dividendTax

    static member getAll() = Database.Do.getAllEntities Do.read DividendTaxesQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id DividendTaxesQuery.getById

    static member getBetweenDates(startDate: string, endDate: string) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- DividendTaxesQuery.getBetweenDates
            command.Parameters.AddWithValue(SQLParameterName.StartDate, startDate) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.EndDate, endDate) |> ignore
            let! dividendTaxes = Database.Do.readAll<DividendTax>(command, Do.read)
            return dividendTaxes
        }

    static member getByTickerCurrencyAndDateRange(tickerId: int, currencyId: int, fromDate: string option, toDate: string) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- DividendTaxesQuery.getByTickerCurrencyAndDateRange
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.StartDate, fromDate |> Option.defaultValue "1900-01-01") |> ignore
            command.Parameters.AddWithValue(SQLParameterName.EndDate, toDate) |> ignore
            let! dividendTaxes = Database.Do.readAll<DividendTax>(command, Do.read)
            return dividendTaxes
        }

    static member getFilteredDividendTaxes(tickerId: int, currencyId: int, startDate: string, endDate: string) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- DividendTaxesQuery.getFilteredDividendTaxes
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.StartDate, startDate) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.EndDate, endDate) |> ignore
            let! dividendTaxes = Database.Do.readAll<DividendTax>(command, Do.read)
            return dividendTaxes
        }