module internal TradeExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.Database.TypeParser
open DataReaderExtensions
open CommandExtensions
open Binnaculum.Core.SQL
open OptionExtensions

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
                (SQLParameterName.Price, trade.Price.Value);
                (SQLParameterName.Commissions, trade.Commissions.Value);
                (SQLParameterName.Fees, trade.Fees.Value);
                (SQLParameterName.TradeCode, fromTradeCodeToDatabase trade.TradeCode);
                (SQLParameterName.TradeType, fromTradeTypeToDatabase trade.TradeType);
                (SQLParameterName.Notes, trade.Notes.ToDbValue())
            ], trade)

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        {
            Id = reader.getInt32 FieldName.Id
            TimeStamp = reader.getDateTimePattern FieldName.TimeStamp
            TickerId = reader.getInt32 FieldName.TickerId
            BrokerAccountId = reader.getInt32 FieldName.BrokerAccountId
            CurrencyId = reader.getInt32 FieldName.CurrencyId
            Quantity = reader.getDecimal FieldName.Quantity
            Price = reader.getMoney FieldName.Price
            Commissions = reader.getMoney FieldName.Commissions
            Fees = reader.getMoney FieldName.Fees
            TradeCode = reader.GetString(reader.GetOrdinal(FieldName.TradeCode)) |> fromDatabaseToTradeCode
            TradeType = reader.GetString(reader.GetOrdinal(FieldName.TradeType)) |> fromDatabaseToTradeType
            Notes = reader.getStringOrNone FieldName.Notes
            Audit = reader.getAudit()
        }

    [<Extension>]
    static member save(trade: Trade) = Database.Do.saveEntity trade (fun t c -> t.fill c) 

    [<Extension>]
    static member delete(trade: Trade) = Database.Do.deleteEntity trade

    static member getAll() = Database.Do.getAllEntities Do.read TradesQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id TradesQuery.getById