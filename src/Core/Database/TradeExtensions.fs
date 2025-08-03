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
                (SQLParameterName.TimeStamp, trade.TimeStamp.ToString());
                (SQLParameterName.TickerId, trade.TickerId);
                (SQLParameterName.BrokerAccountId, trade.BrokerAccountId);
                (SQLParameterName.CurrencyId, trade.CurrencyId);
                (SQLParameterName.Quantity, trade.Quantity);
                (SQLParameterName.Price, trade.Price.Value);
                (SQLParameterName.Commissions, trade.Commissions.Value);
                (SQLParameterName.Fees, trade.Fees.Value);
                (SQLParameterName.TradeCode, fromTradeCodeToDatabase trade.TradeCode);
                (SQLParameterName.TradeType, fromTradeTypeToDatabase trade.TradeType);
                (SQLParameterName.Leveraged, trade.Leveraged)
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
            Leveraged = reader.getDecimal FieldName.Leveraged
            Notes = reader.getStringOrNone FieldName.Notes
            Audit = reader.getAudit()
        }

    [<Extension>]
    static member save(trade: Trade) = Database.Do.saveEntity trade (fun t c -> t.fill c) 

    [<Extension>]
    static member delete(trade: Trade) = Database.Do.deleteEntity trade

    static member getAll() = Database.Do.getAllEntities Do.read TradesQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id TradesQuery.getById

    static member getBetweenDates(startDate: string, endDate: string) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TradesQuery.getBetweenDates
            command.Parameters.AddWithValue(SQLParameterName.StartDate, startDate) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.EndDate, endDate) |> ignore
            let! trades = Database.Do.readAll<Trade>(command, Do.read)
            return trades
        }

    static member getByTickerCurrencyAndDateRange(tickerId: int, currencyId: int, fromDate: string option, toDate: string) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TradesQuery.getByTickerCurrencyAndDateRange
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.StartDate, fromDate |> Option.defaultValue "1900-01-01") |> ignore
            command.Parameters.AddWithValue(SQLParameterName.EndDate, toDate) |> ignore
            let! trades = Database.Do.readAll<Trade>(command, Do.read)
            return trades
        }

    static member getFilteredTrades(tickerId: int, currencyId: int, startDate: string, endDate: string) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TradesQuery.getFilteredTrades
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.StartDate, startDate) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.EndDate, endDate) |> ignore
            let! trades = Database.Do.readAll<Trade>(command, Do.read)
            return trades
        }

    static member getCurrenciesByTickerAndDate(tickerId: int, date: string) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TradesQuery.getCurrenciesByTickerAndDate
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

    static member getDistinctCurrenciesByTickerAndDate(tickerId: int, date: string) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- TradesQuery.getDistinctCurrenciesByTickerAndDate
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.Date, date) |> ignore
            let! reader = command.ExecuteReaderAsync()
            let mutable currencies = []
            while reader.Read() do
                let currencyId = reader.GetInt32(0)
                currencies <- currencyId :: currencies
            reader.Close()
            return currencies |> List.distinct |> List.rev
        }

    static member getByBrokerAccountAndCurrency(brokerAccountId: int, currencyId: int, ?startDate: string, ?endDate: string) =
        task {
            let! command = Database.Do.createCommand()
            match startDate, endDate with
            | Some s, Some e ->
                command.CommandText <- TradesQuery.getByBrokerAccountAndCurrencyWithDates
                command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId) |> ignore
                command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId) |> ignore
                command.Parameters.AddWithValue(SQLParameterName.StartDate, s) |> ignore
                command.Parameters.AddWithValue(SQLParameterName.EndDate, e) |> ignore
            | _ ->
                command.CommandText <- TradesQuery.getByBrokerAccountAndCurrency
                command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId) |> ignore
                command.Parameters.AddWithValue(SQLParameterName.CurrencyId, currencyId) |> ignore
            let! trades = Database.Do.readAll<Trade>(command, Do.read)
            return trades
          }