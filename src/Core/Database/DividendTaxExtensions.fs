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
        command.fillEntityAuditable<DividendTax>(
            [
                (SQLParameterName.TimeStamp, dividendTax.TimeStamp);
                (SQLParameterName.Amount, dividendTax.DividendTaxAmount);
                (SQLParameterName.TickerId, dividendTax.TickerId);
                (SQLParameterName.CurrencyId, dividendTax.CurrencyId);
                (SQLParameterName.BrokerAccountId, dividendTax.BrokerAccountId);
            ])
        
    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Id = reader.getInt32 FieldName.Id
            TimeStamp = reader.getDateTimePattern FieldName.TimeStamp
            DividendTaxAmount = reader.getMoney FieldName.Amount
            TickerId = reader.getInt32 FieldName.TickerId
            CurrencyId = reader.getInt32 FieldName.CurrencyId
            BrokerAccountId = reader.getInt32 FieldName.BrokerAccountId
            Audit = reader.getAudit()
        }

    [<Extension>]
    static member save(dividendTax: DividendTax) = Database.Do.saveEntity dividendTax (fun t c -> t.fill c) 

    [<Extension>]
    static member delete(dividendTax: DividendTax) = Database.Do.deleteEntity dividendTax

    static member getAll() = Database.Do.getAllEntities Do.read

    static member getById(id: int) = Database.Do.getById id Do.read