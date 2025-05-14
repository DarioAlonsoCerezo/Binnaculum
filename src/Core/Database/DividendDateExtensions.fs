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
                (SQLParameterName.TimeStamp, dividendDate.TimeStamp);
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