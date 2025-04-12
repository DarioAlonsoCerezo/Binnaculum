module internal DividendDateExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.Database.TypeParser
open DataReaderExtensions
open CommandExtensions

[<Extension>]
type Do() =

    [<Extension>]
    static member fill(dividendDate: DividendDate, command: SqliteCommand) =
        command.fillParameters(
            [
                (SQLParameterName.Id, dividendDate.Id);
                (SQLParameterName.TimeStamp, dividendDate.TimeStamp);
                (SQLParameterName.Amount, dividendDate.Amount);
                (SQLParameterName.TickerId, dividendDate.TickerId);
                (SQLParameterName.CurrencyId, dividendDate.CurrencyId);
                (SQLParameterName.BrokerAccountId, dividendDate.BrokerAccountId);
                (SQLParameterName.DividendCode, fromDividendDateCodeToDatabase dividendDate.DividendCode);
            ])
        
    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Id = reader.getInt32 FieldName.Id
            TimeStamp = reader.getDateTime FieldName.TimeStamp
            Amount = reader.getDecimal FieldName.Amount
            TickerId = reader.getInt32 FieldName.TickerId
            CurrencyId = reader.getInt32 FieldName.CurrencyId
            BrokerAccountId = reader.getInt32 FieldName.BrokerAccountId
            DividendCode = reader.getString FieldName.DividendCode |> fromDatabaseToDividendDateCode
        }

    [<Extension>]
    static member save(dividendDate: DividendDate) = Database.Do.saveEntity dividendDate (fun t c -> t.fill c) 

    [<Extension>]
    static member delete(dividendDate: DividendDate) = Database.Do.deleteEntity dividendDate

    static member getAll() = Database.Do.getAllEntities Do.read

    static member getById(id: int) = Database.Do.getById id Do.read