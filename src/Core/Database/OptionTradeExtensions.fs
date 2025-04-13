module internal OptionTradeExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core.Database.TypeParser
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions
open Binnaculum.Core.SQL

[<Extension>]
type Do() =

    [<Extension>]
    static member fill(optionTrade: OptionTrade, command: SqliteCommand) =
        command.fillEntityAuditable<OptionTrade>(
            [
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
            ], optionTrade)
            
    [<Extension>]
    static member read(reader: SqliteDataReader) =
        { 
            Id = reader.getInt32 FieldName.Id 
            TimeStamp = reader.getDateTimePattern FieldName.TimeStamp
            ExpirationDate = reader.getDateTimePattern FieldName.ExpirationDate
            Premium = reader.getMoney FieldName.Premium
            NetPremium = reader.getMoney FieldName.NetPremium
            TickerId = reader.getInt32 FieldName.TickerId
            BrokerAccountId = reader.getInt32 FieldName.BrokerAccountId
            CurrencyId = reader.getInt32 FieldName.CurrencyId
            OptionType = reader.getString FieldName.OptionType |> fromDatabaseToOptionType
            Code = reader.getString FieldName.Code |> fromDatabaseToOptionCode
            Strike = reader.getMoney FieldName.Strike
            Commissions = reader.getMoney FieldName.Commissions
            Fees = reader.getMoney FieldName.Fees
            IsOpen = reader.getBoolean FieldName.IsOpen
            ClosedWith = reader.getIntOrNone FieldName.ClosedWith
            Audit = reader.getAudit()
        }

    [<Extension>]
    static member save(optionTrade: OptionTrade) = Database.Do.saveEntity optionTrade (fun t c -> t.fill c) 

    [<Extension>]
    static member delete(optionTrade: OptionTrade) = Database.Do.deleteEntity optionTrade

    static member getAll() = Database.Do.getAllEntities Do.read OptionsQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id OptionsQuery.getById