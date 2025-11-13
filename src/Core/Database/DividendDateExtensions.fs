module internal DividendDateExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.Database.TypeParser
open DataReaderExtensions
open CommandExtensions
open Binnaculum.Core.SQL

[<Extension>]
type Do() =

    [<Extension>]
    static member fill(dividendDate: DividendDate, command: SqliteCommand) =
        command.fillEntityAuditable<DividendDate>(
            [
                (SQLParameterName.TimeStamp, dividendDate.TimeStamp.ToString());
                (SQLParameterName.Amount, dividendDate.Amount.Value);
                (SQLParameterName.TickerId, dividendDate.TickerId);
                (SQLParameterName.CurrencyId, dividendDate.CurrencyId);
                (SQLParameterName.BrokerAccountId, dividendDate.BrokerAccountId);
                (SQLParameterName.DividendCode, fromDividendDateCodeToDatabase dividendDate.DividendCode);
            ], dividendDate)
        
    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Id = reader.getInt32 FieldName.Id
            TimeStamp = reader.getDateTimePattern FieldName.TimeStamp
            Amount = reader.getMoney FieldName.Amount
            TickerId = reader.getInt32 FieldName.TickerId
            CurrencyId = reader.getInt32 FieldName.CurrencyId
            BrokerAccountId = reader.getInt32 FieldName.BrokerAccountId
            DividendCode = reader.getString FieldName.DividendCode |> fromDatabaseToDividendDateCode
            Audit = reader.getAudit()
        }

    [<Extension>]
    static member save(dividendDate: DividendDate) = Database.Do.saveEntity dividendDate (fun t c -> t.fill c) 

    [<Extension>]
    static member delete(dividendDate: DividendDate) = Database.Do.deleteEntity dividendDate

    static member getAll() = Database.Do.getAllEntities Do.read DividendDateQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id DividendDateQuery.getById

    /// <summary>
    /// Load dividend dates with pagination support for a specific broker account.
    /// Returns dividend dates ordered by TimeStamp DESC (most recent first).
    /// </summary>
    /// <param name="brokerAccountId">The broker account ID to filter by</param>
    /// <param name="pageNumber">Zero-based page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>List of dividend dates for the specified page</returns>
    static member loadDividendDatesPaged(brokerAccountId: int, pageNumber: int, pageSize: int) =
        task {
            let offset = pageNumber * pageSize
            let! command = Database.Do.createCommand ()
            command.CommandText <- DividendDateQuery.getByBrokerAccountIdPaged
            command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId) |> ignore
            command.Parameters.AddWithValue("@PageSize", pageSize) |> ignore
            command.Parameters.AddWithValue("@Offset", offset) |> ignore
            let! dividendDates = Database.Do.readAll<DividendDate> (command, Do.read)
            return dividendDates
        }