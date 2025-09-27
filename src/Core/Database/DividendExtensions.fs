module internal DividendExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions
open Binnaculum.Core.SQL
open Binnaculum.Core.Patterns

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
            command.Parameters.AddWithValue(SQLParameterName.StartDate, startDate) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.EndDate, endDate) |> ignore
            let! dividends = Database.Do.readAll<Dividend>(command, Do.read)
            return dividends
        }

    static member getByTickerCurrencyAndDateRange(tickerId: int, currencyId: int, fromDate: string option, toDate: string) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- DividendsQuery.getByTickerCurrencyAndDateRange
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.StartDate, fromDate |> Option.defaultValue "1900-01-01") |> ignore
            command.Parameters.AddWithValue(SQLParameterName.EndDate, toDate) |> ignore
            let! dividends = Database.Do.readAll<Dividend>(command, Do.read)
            return dividends
        }

    static member getFilteredDividends(tickerId: int, currencyId: int, startDate: string, endDate: string) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- DividendsQuery.getFilteredDividends
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.StartDate, startDate) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.EndDate, endDate) |> ignore
            let! dividends = Database.Do.readAll<Dividend>(command, Do.read)
            return dividends
        }

    static member getCurrenciesByTickerAndDate(tickerId: int, date: string) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- DividendsQuery.getCurrenciesByTickerAndDate
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.Date, date) |> ignore
            let! reader = command.ExecuteReaderAsync()
            let mutable currencies = []
            while reader.Read() do
                let currencyId = reader.GetInt32(0)
                currencies <- currencyId :: currencies
            reader.Close()
            return currencies |> List.rev
        }

    static member getByBrokerAccountIdFromDate(brokerAccountId: int, startDate: DateTimePattern) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- DividendsQuery.getByBrokerAccountIdFromDate
        command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.TimeStamp, startDate.ToString()) |> ignore
        let! dividends = Database.Do.readAll<Dividend>(command, Do.read)
        return dividends
    }

    static member getByBrokerAccountIdForDate(brokerAccountId: int, targetDate: DateTimePattern) = task {
        let! command = Database.Do.createCommand()
        command.CommandText <- DividendsQuery.getByBrokerAccountIdForDate
        command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId) |> ignore
        command.Parameters.AddWithValue(SQLParameterName.TimeStamp, targetDate.ToString()) |> ignore
        let! dividends = Database.Do.readAll<Dividend>(command, Do.read)
        return dividends
    }

/// <summary>
/// Financial calculation extension methods for Dividend collections.
/// These methods provide reusable calculation logic for dividend income tracking and financial snapshot processing.
/// </summary>
[<Extension>]
type DividendCalculations() =

    /// <summary>
    /// Calculates the total dividend income received from all dividend payments.
    /// This represents the gross dividend income before any tax withholdings.
    /// </summary>
    /// <param name="dividends">List of dividends to analyze</param>
    /// <returns>Total dividend income as Money</returns>
    [<Extension>]
    static member calculateTotalDividendIncome(dividends: Dividend list) =
        dividends
        |> List.sumBy (fun dividend -> dividend.DividendAmount.Value)
        |> Money.FromAmount

    /// <summary>
    /// Calculates dividend income grouped by ticker symbol.
    /// Useful for understanding which stocks are the biggest dividend contributors.
    /// </summary>
    /// <param name="dividends">List of dividends to analyze</param>
    /// <returns>Map of ticker ID to total dividend income for that ticker</returns>
    [<Extension>]
    static member calculateDividendsByTicker(dividends: Dividend list) =
        dividends
        |> List.groupBy (fun dividend -> dividend.TickerId)
        |> List.map (fun (tickerId, tickerDividends) ->
            let totalIncome = 
                tickerDividends 
                |> List.sumBy (fun d -> d.DividendAmount.Value)
                |> Money.FromAmount
            (tickerId, totalIncome))
        |> Map.ofList

    /// <summary>
    /// Counts the total number of dividend payments.
    /// This can be used for MovementCounter calculations in financial snapshots.
    /// </summary>
    /// <param name="dividends">List of dividends to count</param>
    /// <returns>Total number of dividend payments as integer</returns>
    [<Extension>]
    static member calculateDividendCount(dividends: Dividend list) =
        dividends.Length

    /// <summary>
    /// Filters dividends by currency ID.
    /// </summary>
    /// <param name="dividends">List of dividends to filter</param>
    /// <param name="currencyId">The currency ID to filter by</param>
    /// <returns>Filtered list of dividends for the specified currency</returns>
    [<Extension>]
    static member filterByCurrency(dividends: Dividend list, currencyId: int) =
        dividends
        |> List.filter (fun dividend -> dividend.CurrencyId = currencyId)

    /// <summary>
    /// Filters dividends by ticker ID.
    /// </summary>
    /// <param name="dividends">List of dividends to filter</param>
    /// <param name="tickerId">The ticker ID to filter by</param>
    /// <returns>Filtered list of dividends for the specified ticker</returns>
    [<Extension>]
    static member filterByTicker(dividends: Dividend list, tickerId: int) =
        dividends
        |> List.filter (fun dividend -> dividend.TickerId = tickerId)

    /// <summary>
    /// Gets all unique currency IDs involved in the dividends.
    /// </summary>
    /// <param name="dividends">List of dividends to analyze</param>
    /// <returns>Set of unique currency IDs</returns>
    [<Extension>]
    static member getUniqueCurrencyIds(dividends: Dividend list) =
        dividends 
        |> List.map (fun dividend -> dividend.CurrencyId)
        |> Set.ofList

    /// <summary>
    /// Gets all unique ticker IDs that paid dividends.
    /// </summary>
    /// <param name="dividends">List of dividends to analyze</param>
    /// <returns>Set of unique ticker IDs</returns>
    [<Extension>]
    static member getUniqueTickerIds(dividends: Dividend list) =
        dividends 
        |> List.map (fun dividend -> dividend.TickerId)
        |> Set.ofList

    /// <summary>
    /// Calculates dividend income for a specific date range.
    /// Useful for period-specific dividend calculations.
    /// </summary>
    /// <param name="dividends">List of dividends to analyze</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <returns>Total dividend income in the specified date range</returns>
    [<Extension>]
    static member calculateDividendIncomeInDateRange(dividends: Dividend list, startDate: DateTimePattern, endDate: DateTimePattern) =
        dividends
        |> List.filter (fun dividend -> 
            dividend.TimeStamp.Value >= startDate.Value && dividend.TimeStamp.Value <= endDate.Value)
        |> List.sumBy (fun dividend -> dividend.DividendAmount.Value)
        |> Money.FromAmount

    /// <summary>
    /// Calculates average dividend per payment.
    /// Useful for dividend yield and frequency analysis.
    /// </summary>
    /// <param name="dividends">List of dividends to analyze</param>
    /// <returns>Average dividend amount per payment as Money</returns>
    [<Extension>]
    static member calculateAverageDividend(dividends: Dividend list) =
        if dividends.IsEmpty then
            Money.FromAmount 0m
        else
            dividends
            |> List.sumBy (fun dividend -> dividend.DividendAmount.Value)
            |> fun total -> total / (decimal dividends.Length)
            |> Money.FromAmount

    /// <summary>
    /// Calculates a comprehensive dividend summary for dividends.
    /// Returns a record with all major dividend metrics calculated.
    /// </summary>
    /// <param name="dividends">List of dividends to analyze</param>
    /// <param name="currencyId">Optional currency ID to filter calculations by</param>
    /// <returns>Dividend summary record with calculated totals</returns>
    [<Extension>]
    static member calculateDividendSummary(dividends: Dividend list, ?currencyId: int) =
        let relevantDividends = 
            match currencyId with
            | Some id -> dividends.filterByCurrency(id)
            | None -> dividends
        
        {|
            TotalDividendIncome = relevantDividends.calculateTotalDividendIncome()
            DividendsByTicker = relevantDividends.calculateDividendsByTicker()
            AverageDividend = relevantDividends.calculateAverageDividend()
            DividendCount = relevantDividends.calculateDividendCount()
            UniqueCurrencies = relevantDividends.getUniqueCurrencyIds()
            UniqueTickers = relevantDividends.getUniqueTickerIds()
        |}