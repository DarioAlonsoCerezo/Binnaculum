module internal DividendTaxExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions

    [<Extension>]
    type Do() =

        [<Extension>]
        static member fill(dividendTax: DividendTax, command: SqliteCommand) =
            command.fillParameters(
                [
                    ("@Id", dividendTax.Id);
                    ("@TimeStamp", dividendTax.TimeStamp);
                    ("@Amount", dividendTax.Amount);
                    ("@TickerId", dividendTax.TickerId);
                    ("@CurrencyId", dividendTax.CurrencyId);
                    ("@BrokerAccountId", dividendTax.BrokerAccountId);
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
            }

        [<Extension>]
        static member save(dividendTax: DividendTax) = Database.Do.saveEntity dividendTax (fun t c -> t.fill c) 

        [<Extension>]
        static member delete(dividendTax: DividendTax) = Database.Do.deleteEntity dividendTax

        static member getAll() = Database.Do.getAllEntities Do.read

        static member getById(id: int) = Database.Do.getById id Do.read