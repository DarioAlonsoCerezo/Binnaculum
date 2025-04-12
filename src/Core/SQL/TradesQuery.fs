namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal TradesQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {Trades}
        (
            {Id} INTEGER PRIMARY KEY,
            {TimeStamp} TEXT NOT NULL,
            {TickerId} INTEGER NOT NULL,
            {BrokerAccountId} INTEGER NOT NULL,
            {CurrencyId} INTEGER NOT NULL,
            {Quantity} TEXT NOT NULL,
            {Price} TEXT NOT NULL,
            {Commissions} TEXT NOT NULL,
            {Fees} TEXT NOT NULL,
            {TradeCode} TEXT NOT NULL,
            {TradeType} TEXT NOT NULL,
            {Notes} TEXT
        )
        """

    let insert =
        $"""
        INSERT INTO {Trades}
        (
            {TimeStamp},
            {TickerId},
            {BrokerAccountId},
            {CurrencyId},
            {Quantity},
            {Price},
            {Commissions},
            {Fees},
            {TradeCode},
            {TradeType},
            {Notes}
        )
        VALUES
        (
            {SQLParameterName.TimeStamp},
            {SQLParameterName.TickerId},
            {SQLParameterName.BrokerAccountId},
            {SQLParameterName.CurrencyId},
            {SQLParameterName.Quantity},
            {SQLParameterName.Price},
            {SQLParameterName.Commissions},
            {SQLParameterName.Fees},
            {SQLParameterName.TradeCode},
            {SQLParameterName.TradeType},
            {SQLParameterName.Notes}
        )
        """

    let update =
        $"""
        UPDATE {Trades}
        SET
            {TimeStamp} = {SQLParameterName.TimeStamp},
            {TickerId} = {SQLParameterName.TickerId},
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId},
            {CurrencyId} = {SQLParameterName.CurrencyId},
            {Quantity} = {SQLParameterName.Quantity},
            {Price} = {SQLParameterName.Price},
            {Commissions} = {SQLParameterName.Commissions},
            {Fees} = {SQLParameterName.Fees},   
            {TradeCode} = {SQLParameterName.TradeCode},
            {TradeType} = {SQLParameterName.TradeType},
            {Notes} = {SQLParameterName.Notes}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {Trades}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {Trades}
        """

    let getById =
        $"""
        SELECT * FROM {Trades}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """