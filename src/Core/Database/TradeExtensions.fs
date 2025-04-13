module internal TradeExtensions

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
    static member fill(trade: Trade, command: SqliteCommand) =
        command.fillEntityAuditable<Trade>(
            [
                (SQLParameterName.TimeStamp, trade.TimeStamp);
                (SQLParameterName.TickerId, trade.TickerId);
                (SQLParameterName.BrokerAccountId, trade.BrokerAccountId);
                (SQLParameterName.CurrencyId, trade.CurrencyId);
                (SQLParameterName.Quantity, trade.Quantity);
                (SQLParameterName.Price, trade.Price);
                (SQLParameterName.Commissions, trade.Commissions);
                (SQLParameterName.Fees, trade.Fees);
                (SQLParameterName.TradeCode, fromTradeCodeToDatabase trade.TradeCode);
                (SQLParameterName.TradeType, fromTradeTypeToDatabase trade.TradeType);
                (SQLParameterName.Notes, trade.Notes)
            ])

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Id = reader.getInt32 FieldName.Id
            TimeStamp = reader.getDateTimePattern FieldName.TimeStamp
            TickerId = reader.getInt32 FieldName.TickerId
            BrokerAccountId = reader.getInt32 FieldName.BrokerAccountId
            CurrencyId = reader.getInt32 FieldName.CurrencyId
            Quantity = reader.getDecimal FieldName.Quantity
            Price = reader.getDecimal FieldName.Price
            Commissions = reader.getDecimal FieldName.Commissions
            Fees = reader.getDecimal FieldName.Fees
            TradeCode = reader.GetString(reader.GetOrdinal(FieldName.TradeCode)) |> fromDatabaseToTradeCode
            TradeType = reader.GetString(reader.GetOrdinal(FieldName.TradeType)) |> fromDatabaseToTradeType
            Notes = reader.getStringOrNone FieldName.Notes
            Audit = reader.getAudit()
        }

    [<Extension>]
    static member save(trade: Trade) = Database.Do.saveEntity trade (fun t c -> t.fill c) 

    [<Extension>]
    static member delete(trade: Trade) = Database.Do.deleteEntity trade

    static member getAll() = Database.Do.getAllEntities Do.read

    static member getById(id: int) = Database.Do.getById id Do.read