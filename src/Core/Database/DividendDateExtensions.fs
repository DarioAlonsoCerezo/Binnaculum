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
                    ("@Id", dividendDate.Id);
                    ("@TimeStamp", dividendDate.TimeStamp);
                    ("@Amount", dividendDate.Amount);
                    ("@TickerId", dividendDate.TickerId);
                    ("@CurrencyId", dividendDate.CurrencyId);
                    ("@BrokerAccountId", dividendDate.BrokerAccountId);
                    ("@DividendCode", fromDividendDateCodeToDatabase dividendDate.DividendCode);
                ])
            
        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.getInt32 "Id"
                TimeStamp = reader.getDateTime "TimeStamp"
                Amount = reader.getDecimal "Amount"
                TickerId = reader.getInt32 "TickerId"
                CurrencyId = reader.getInt32 "CurrencyId"
                BrokerAccountId = reader.getInt32 "BrokerAccountId"
                DividendCode = reader.getString "DividendCode" |> fromDatabaseToDividendDateCode
            }

        [<Extension>]
        static member save(dividendDate: DividendDate) = Database.Do.saveEntity dividendDate (fun t c -> t.fill c) 

        [<Extension>]
        static member delete(dividendDate: DividendDate) = Database.Do.deleteEntity dividendDate

        static member getAll() = Database.Do.getAllEntities Do.read

        static member getById(id: int) = Database.Do.getById id Do.read