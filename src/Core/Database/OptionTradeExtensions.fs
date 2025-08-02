module internal OptionTradeExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core.Database.TypeParser
open Binnaculum.Core
open DataReaderExtensions
open CommandExtensions
open Binnaculum.Core.SQL
open OptionExtensions

[<Extension>]
type Do() =

    [<Extension>]
    static member fill(optionTrade: OptionTrade, command: SqliteCommand) =
        command.fillEntityAuditable<OptionTrade>(
            [
                (SQLParameterName.TimeStamp, optionTrade.TimeStamp.ToString());
                (SQLParameterName.ExpirationDate, optionTrade.ExpirationDate.ToString());
                (SQLParameterName.Premium, optionTrade.Premium.Value);
                (SQLParameterName.NetPremium, optionTrade.NetPremium.Value);
                (SQLParameterName.TickerId, optionTrade.TickerId);
                (SQLParameterName.BrokerAccountId, optionTrade.BrokerAccountId);
                (SQLParameterName.CurrencyId, optionTrade.CurrencyId);
                (SQLParameterName.OptionType, fromOptionTypeToDatabase optionTrade.OptionType);
                (SQLParameterName.Code, fromOptionCodeToDatabase optionTrade.Code);
                (SQLParameterName.Strike, optionTrade.Strike.Value);
                (SQLParameterName.Commissions, optionTrade.Commissions.Value);
                (SQLParameterName.Fees, optionTrade.Fees.Value);
                (SQLParameterName.IsOpen, optionTrade.IsOpen);
                (SQLParameterName.ClosedWith, optionTrade.ClosedWith.ToDbValue())
                (SQLParameterName.Multiplier, optionTrade.Multiplier)
                (SQLParameterName.Notes, optionTrade.Notes.ToDbValue())
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
            Multiplier = reader.getDecimal FieldName.Multiplier
            Notes = reader.getStringOrNone FieldName.Notes
            Audit = reader.getAudit()
        }

    [<Extension>]
    static member save(optionTrade: OptionTrade) = Database.Do.saveEntity optionTrade (fun t c -> t.fill c) 

    [<Extension>]
    static member delete(optionTrade: OptionTrade) = Database.Do.deleteEntity optionTrade

    static member getAll() = Database.Do.getAllEntities Do.read OptionsQuery.getAll

    static member getById(id: int) = Database.Do.getById Do.read id OptionsQuery.getById

    static member getBetweenDates(startDate: string, endDate: string) =
        task {
            let! command = Database.Do.createCommand()
            command.CommandText <- OptionsQuery.getBetweenDates
            command.Parameters.AddWithValue(SQLParameterName.StartDate, startDate) |> ignore
            command.Parameters.AddWithValue(SQLParameterName.EndDate, endDate) |> ignore
            let! optionTrades = Database.Do.readAll<OptionTrade>(command, Do.read)
            return optionTrades
        }