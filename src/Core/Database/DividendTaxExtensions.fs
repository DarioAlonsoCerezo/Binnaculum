module internal DividendTaxExtensions

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
    static member fill(dividendTax: DividendTax, command: SqliteCommand) =
        command.fillEntityAuditable<DividendTax> (
            [ (SQLParameterName.TimeStamp, dividendTax.TimeStamp.ToString())
              (SQLParameterName.DividendTaxAmount, dividendTax.DividendTaxAmount.Value)
              (SQLParameterName.TickerId, dividendTax.TickerId)
              (SQLParameterName.CurrencyId, dividendTax.CurrencyId)
              (SQLParameterName.BrokerAccountId, dividendTax.BrokerAccountId) ],
            dividendTax
        )

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        { Id = reader.getInt32 FieldName.Id
          TimeStamp = reader.getDateTimePattern FieldName.TimeStamp
          DividendTaxAmount = reader.getMoney FieldName.DividendTaxAmount
          TickerId = reader.getInt32 FieldName.TickerId
          CurrencyId = reader.getInt32 FieldName.CurrencyId
          BrokerAccountId = reader.getInt32 FieldName.BrokerAccountId
          Audit = reader.getAudit () }

    [<Extension>]
    static member save(dividendTax: DividendTax) =
        Database.Do.saveEntity dividendTax (fun t c -> t.fill c)

    [<Extension>]
    static member delete(dividendTax: DividendTax) = Database.Do.deleteEntity dividendTax

    static member getAll() =
        Database.Do.getAllEntities Do.read DividendTaxesQuery.getAll

    static member getById(id: int) =
        Database.Do.getById Do.read id DividendTaxesQuery.getById

    static member getBetweenDates(startDate: string, endDate: string) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- DividendTaxesQuery.getBetweenDates
            command.Parameters.AddWithValue(SQLParameterName.StartDate, startDate) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.EndDate, endDate) |> ignore
            let! dividendTaxes = Database.Do.readAll<DividendTax> (command, Do.read)
            return dividendTaxes
        }

    static member getByTickerCurrencyAndDateRange
        (tickerId: int, currencyId: int, fromDate: string option, toDate: string)
        =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- DividendTaxesQuery.getByTickerCurrencyAndDateRange
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore

            command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId)
            |> ignore

            command.Parameters.AddWithValue(SQLParameterName.StartDate, fromDate |> Option.defaultValue "1900-01-01")
            |> ignore

            command.Parameters.AddWithValue(SQLParameterName.EndDate, toDate) |> ignore
            let! dividendTaxes = Database.Do.readAll<DividendTax> (command, Do.read)
            return dividendTaxes
        }

    static member getFilteredDividendTaxes(tickerId: int, currencyId: int, startDate: string, endDate: string) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- DividendTaxesQuery.getFilteredDividendTaxes
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore

            command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId)
            |> ignore

            command.Parameters.AddWithValue(SQLParameterName.StartDate, startDate) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.EndDate, endDate) |> ignore
            let! dividendTaxes = Database.Do.readAll<DividendTax> (command, Do.read)
            return dividendTaxes
        }

    static member getCurrenciesByTickerAndDate(tickerId: int, date: string) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- DividendTaxesQuery.getCurrenciesByTickerAndDate
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

    static member getByBrokerAccountIdFromDate(brokerAccountId: int, startDate: DateTimePattern) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- DividendTaxesQuery.getByBrokerAccountIdFromDate

            command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId)
            |> ignore

            command.Parameters.AddWithValue(SQLParameterName.TimeStamp, startDate.ToString())
            |> ignore

            let! dividendTaxes = Database.Do.readAll<DividendTax> (command, Do.read)
            return dividendTaxes
        }

    static member getByBrokerAccountIdForDate(brokerAccountId: int, targetDate: DateTimePattern) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- DividendTaxesQuery.getByBrokerAccountIdForDate

            command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId)
            |> ignore

            command.Parameters.AddWithValue(SQLParameterName.TimeStamp, targetDate.ToString())
            |> ignore

            let! dividendTaxes = Database.Do.readAll<DividendTax> (command, Do.read)
            return dividendTaxes
        }

    static member getByTickerIdFromDate(tickerId: int, startDate: DateTimePattern) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- DividendTaxesQuery.getByTickerIdFromDate
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore

            command.Parameters.AddWithValue(SQLParameterName.TimeStamp, startDate.ToString())
            |> ignore

            let! dividendTaxes = Database.Do.readAll<DividendTax> (command, Do.read)
            return dividendTaxes
        }

/// <summary>
/// Financial calculation extension methods for DividendTax collections.
/// These methods provide reusable calculation logic for dividend tax withholding tracking and financial snapshot processing.
/// </summary>
[<Extension>]
type DividendTaxCalculations() =

    /// <summary>
    /// Calculates the total dividend tax withholdings paid.
    /// This represents tax deducted from dividend payments before they reach the investor.
    /// </summary>
    /// <param name="dividendTaxes">List of dividend taxes to analyze</param>
    /// <returns>Total dividend tax withholdings as Money</returns>
    [<Extension>]
    static member calculateTotalTaxWithheld(dividendTaxes: DividendTax list) =
        dividendTaxes
        |> List.sumBy (fun tax -> tax.DividendTaxAmount.Value)
        |> Money.FromAmount

    /// <summary>
    /// Calculates dividend tax withholdings grouped by ticker symbol.
    /// Useful for understanding tax impact by individual stocks.
    /// </summary>
    /// <param name="dividendTaxes">List of dividend taxes to analyze</param>
    /// <returns>Map of ticker ID to total tax withheld for that ticker</returns>
    [<Extension>]
    static member calculateTaxesByTicker(dividendTaxes: DividendTax list) =
        dividendTaxes
        |> List.groupBy (fun tax -> tax.TickerId)
        |> List.map (fun (tickerId, tickerTaxes) ->
            let totalTax =
                tickerTaxes
                |> List.sumBy (fun t -> t.DividendTaxAmount.Value)
                |> Money.FromAmount

            (tickerId, totalTax))
        |> Map.ofList

    /// <summary>
    /// Counts the total number of dividend tax withholding events.
    /// This can be used for MovementCounter calculations in financial snapshots.
    /// </summary>
    /// <param name="dividendTaxes">List of dividend taxes to count</param>
    /// <returns>Total number of tax withholding events as integer</returns>
    [<Extension>]
    static member calculateTaxEventCount(dividendTaxes: DividendTax list) = dividendTaxes.Length

    /// <summary>
    /// Filters dividend taxes by currency ID.
    /// </summary>
    /// <param name="dividendTaxes">List of dividend taxes to filter</param>
    /// <param name="currencyId">The currency ID to filter by</param>
    /// <returns>Filtered list of dividend taxes for the specified currency</returns>
    [<Extension>]
    static member filterByCurrency(dividendTaxes: DividendTax list, currencyId: int) =
        dividendTaxes |> List.filter (fun tax -> tax.CurrencyId = currencyId)

    /// <summary>
    /// Filters dividend taxes by ticker ID.
    /// </summary>
    /// <param name="dividendTaxes">List of dividend taxes to filter</param>
    /// <param name="tickerId">The ticker ID to filter by</param>
    /// <returns>Filtered list of dividend taxes for the specified ticker</returns>
    [<Extension>]
    static member filterByTicker(dividendTaxes: DividendTax list, tickerId: int) =
        dividendTaxes |> List.filter (fun tax -> tax.TickerId = tickerId)

    /// <summary>
    /// Gets all unique currency IDs involved in dividend tax withholdings.
    /// </summary>
    /// <param name="dividendTaxes">List of dividend taxes to analyze</param>
    /// <returns>Set of unique currency IDs</returns>
    [<Extension>]
    static member getUniqueCurrencyIds(dividendTaxes: DividendTax list) =
        dividendTaxes |> List.map (fun tax -> tax.CurrencyId) |> Set.ofList

    /// <summary>
    /// Gets all unique ticker IDs that had dividend tax withholdings.
    /// </summary>
    /// <param name="dividendTaxes">List of dividend taxes to analyze</param>
    /// <returns>Set of unique ticker IDs</returns>
    [<Extension>]
    static member getUniqueTickerIds(dividendTaxes: DividendTax list) =
        dividendTaxes |> List.map (fun tax -> tax.TickerId) |> Set.ofList

    /// <summary>
    /// Calculates dividend tax withholdings for a specific date range.
    /// Useful for period-specific tax calculations.
    /// </summary>
    /// <param name="dividendTaxes">List of dividend taxes to analyze</param>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive)</param>
    /// <returns>Total tax withheld in the specified date range</returns>
    [<Extension>]
    static member calculateTaxWithheldInDateRange
        (dividendTaxes: DividendTax list, startDate: DateTimePattern, endDate: DateTimePattern)
        =
        dividendTaxes
        |> List.filter (fun tax -> tax.TimeStamp.Value >= startDate.Value && tax.TimeStamp.Value <= endDate.Value)
        |> List.sumBy (fun tax -> tax.DividendTaxAmount.Value)
        |> Money.FromAmount

    /// <summary>
    /// Calculates effective tax rate when provided with corresponding dividend data.
    /// Useful for understanding the overall dividend tax burden.
    /// </summary>
    /// <param name="dividendTaxes">List of dividend taxes to analyze</param>
    /// <param name="dividends">Corresponding dividend payments</param>
    /// <returns>Effective tax rate as percentage (0-100)</returns>
    [<Extension>]
    static member calculateEffectiveTaxRate(dividendTaxes: DividendTax list, dividends: Dividend list) =
        let totalTax = dividendTaxes.calculateTotalTaxWithheld().Value
        let totalDividends = dividends |> List.sumBy (fun d -> d.DividendAmount.Value)

        if totalDividends > 0m then
            (totalTax / (totalDividends + totalTax)) * 100m
        else
            0m

    /// <summary>
    /// Calculates a comprehensive dividend tax summary.
    /// Returns a record with all major dividend tax metrics calculated.
    /// </summary>
    /// <param name="dividendTaxes">List of dividend taxes to analyze</param>
    /// <param name="currencyId">Optional currency ID to filter calculations by</param>
    /// <returns>Dividend tax summary record with calculated totals</returns>
    [<Extension>]
    static member calculateDividendTaxSummary(dividendTaxes: DividendTax list, ?currencyId: int) =
        let relevantTaxes =
            match currencyId with
            | Some id -> dividendTaxes.filterByCurrency (id)
            | None -> dividendTaxes

        {| TotalTaxWithheld = relevantTaxes.calculateTotalTaxWithheld ()
           TaxesByTicker = relevantTaxes.calculateTaxesByTicker ()
           TaxEventCount = relevantTaxes.calculateTaxEventCount ()
           UniqueCurrencies = relevantTaxes.getUniqueCurrencyIds ()
           UniqueTickers = relevantTaxes.getUniqueTickerIds () |}
