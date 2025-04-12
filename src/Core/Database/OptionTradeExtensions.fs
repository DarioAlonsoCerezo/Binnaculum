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
                (SQLParameterName.Id, optionTrade.Id);
                (SQLParameterName.TimeStamp, optionTrade.TimeStamp);
                (SQLParameterName.ExpirationDate, optionTrade.ExpirationDate);
                (SQLParameterName.Premium, optionTrade.Premium);
                (SQLParameterName.NetPremium, optionTrade.NetPremium);
                (SQLParameterName.TickerId, optionTrade.TickerId);
                (SQLParameterName.BrokerAccountId, optionTrade.BrokerAccountId);
                (SQLParameterName.CurrencyId, optionTrade.CurrencyId);
                (SQLParameterName.OptionType, fromOptionTypeToDatabase optionTrade.OptionType);
                (SQLParameterName.Code, fromOptionCodeToDatabase optionTrade.Code);
                (SQLParameterName.Strike, optionTrade.Strike);
                (SQLParameterName.Commissions, optionTrade.Commissions);
                (SQLParameterName.Fees, optionTrade.Fees);
                (SQLParameterName.IsOpen, optionTrade.IsOpen);
                (SQLParameterName.ClosedWith, optionTrade.ClosedWith)
            ])
            
    [<Extension>]
    static member read(reader: SqliteDataReader) =
        { 
            Id = reader.getInt32 FieldName.Id 
            TimeStamp = reader.getDateTime FieldName.TimeStamp
            ExpirationDate = reader.getDateTime FieldName.ExpirationDate
            Premium = reader.getDecimal FieldName.Premium
            NetPremium = reader.getDecimal FieldName.NetPremium
            TickerId = reader.getInt32 FieldName.TickerId
            BrokerAccountId = reader.getInt32 FieldName.BrokerAccountId
            CurrencyId = reader.getInt32 FieldName.CurrencyId
            OptionType = reader.getString FieldName.OptionType |> fromDatabaseToOptionType
            Code = reader.getString FieldName.Code |> fromDatabaseToOptionCode
            Strike = reader.getDecimal FieldName.Strike
            Commissions = reader.getDecimal FieldName.Commissions
            Fees = reader.getDecimal FieldName.Fees
            IsOpen = reader.getBoolean FieldName.IsOpen
            ClosedWith = reader.getIntOrNone FieldName.ClosedWith
        }

    [<Extension>]
    static member save(optionTrade: OptionTrade) = Database.Do.saveEntity optionTrade (fun t c -> t.fill c) 

    [<Extension>]
    static member delete(optionTrade: OptionTrade) = Database.Do.deleteEntity optionTrade

    static member getAll() = Database.Do.getAllEntities Do.read

    static member getById(id: int) = Database.Do.getById id Do.read