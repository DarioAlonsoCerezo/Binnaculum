module internal OptionTradeExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core.Database.TypeParser
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions

    [<Extension>]
    type Do() =

        [<Extension>]
        static member fill(optionTrade: OptionTrade, command: SqliteCommand) =
            command.fillParameters(
                [
                    ("@Id", optionTrade.Id);
                    ("@TimeStamp", optionTrade.TimeStamp);
                    ("@ExpirationDate", optionTrade.ExpirationDate);
                    ("@Premium", optionTrade.Premium);
                    ("@NetPremium", optionTrade.NetPremium);
                    ("@TickerId", optionTrade.TickerId);
                    ("@BrokerAccountId", optionTrade.BrokerAccountId);
                    ("@CurrencyId", optionTrade.CurrencyId);
                    ("@OptionType", fromOptionTypeToDatabase optionTrade.OptionType);
                    ("@Code", fromOptionCodeToDatabase optionTrade.Code);
                    ("@Strike", optionTrade.Strike);
                    ("@Commissions", optionTrade.Commissions);
                    ("@Fees", optionTrade.Fees);
                    ("@IsOpen", optionTrade.IsOpen);
                    ("@ClosedWith", optionTrade.ClosedWith)
                ])
            
        [<Extension>]
        static member read(reader: SqliteDataReader) =
            { 
                Id = reader.getInt32 "Id" 
                TimeStamp = reader.getDateTime "TimeStamp"
                ExpirationDate = reader.getDateTime "ExpirationDate"
                Premium = reader.getDecimal "Premium"
                NetPremium = reader.getDecimal "NetPremium"
                TickerId = reader.getInt32 "TickerId"
                BrokerAccountId = reader.getInt32 "BrokerAccountId"
                CurrencyId = reader.getInt32 "CurrencyId"
                OptionType = reader.getString "OptionType" |> fromDatabaseToOptionType
                Code = reader.getString "Code" |> fromDatabaseToOptionCode
                Strike = reader.getDecimal "Strike"
                Commissions = reader.getDecimal "Commissions"
                Fees = reader.getDecimal "Fees"
                IsOpen = reader.getBoolean "IsOpen"
                ClosedWith = reader.getIntOrNone "ClosedWith"
            }

        [<Extension>]
        static member save(optionTrade: OptionTrade) = Database.Do.saveEntity optionTrade (fun t c -> t.fill c) 

        [<Extension>]
        static member delete(optionTrade: OptionTrade) = Database.Do.deleteEntity optionTrade

        static member getAll() = Database.Do.getAllEntities Do.read

        static member getById(id: int) = Database.Do.getById id Do.read