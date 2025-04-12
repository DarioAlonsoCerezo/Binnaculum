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
            command.fillParameters(
                [
                    ("@Id", trade.Id);
                    ("@TimeStamp", trade.TimeStamp);
                    ("@TickerId", trade.TickerId);
                    ("@BrokerAccountId", trade.BrokerAccountId);
                    ("@CurrencyId", trade.CurrencyId);
                    ("@Quantity", trade.Quantity);
                    ("@Price", trade.Price);
                    ("@Commission", trade.Commissions);
                    ("@Fees", trade.Fees);
                    ("@TradeCode", fromTradeCodeToDatabase trade.TradeCode);
                    ("@TradeType", fromTradeTypeToDatabase trade.TradeType);
                    ("@Notes", trade.Notes)
                ])

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.getInt32 "Id"
                TimeStamp = reader.getDateTime "TimeStamp"
                TickerId = reader.getInt32 "TickerId"
                BrokerAccountId = reader.getInt32 "BrokerAccountId"
                CurrencyId = reader.getInt32 "CurrencyId"
                Quantity = reader.getDecimal "Quantity"
                Price = reader.getDecimal "Price"
                Commissions = reader.getDecimal "Commissions"
                Fees = reader.getDecimal "Fees"
                TradeCode = reader.GetString(reader.GetOrdinal("TradeCode")) |> fromDatabaseToTradeCode
                TradeType = reader.GetString(reader.GetOrdinal("TradeType")) |> fromDatabaseToTradeType
                Notes = reader.getStringOrNone "Notes"
            }

        [<Extension>]
        static member save(trade: Trade) = Database.Do.saveEntity trade (fun t c -> t.fill c) 

        [<Extension>]
        static member delete(trade: Trade) = Database.Do.deleteEntity trade

        static member getAll() = Database.Do.getAllEntities Do.read

        static member getById(id: int) = Database.Do.getById id Do.read