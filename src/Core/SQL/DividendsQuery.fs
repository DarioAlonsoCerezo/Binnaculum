namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal DividendsQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {Dividends}
        (
            {Id} INTEGER PRIMARY KEY,
            {TimeStamp} TEXT NOT NULL,
            {DividendAmount} TEXT NOT NULL,
            {TickerId} INTEGER NOT NULL,
            {CurrencyId} INTEGER NOT NULL,
            {BrokerAccountId} INTEGER NOT NULL
        )
        """

    let insert =
        $"""
        INSERT INTO {Dividends}
        (
            {TimeStamp},
            {DividendAmount},
            {TickerId},
            {CurrencyId},
            {BrokerAccountId}
        )
        VALUES
        (
            {SQLParameterName.TimeStamp},
            {SQLParameterName.DividendAmount},
            {SQLParameterName.TickerId},
            {SQLParameterName.CurrencyId},
            {SQLParameterName.BrokerAccountId}
        )
        """

    let update =
        $"""
        UPDATE {Dividends}
        SET
            {TimeStamp} = {SQLParameterName.TimeStamp},
            {DividendAmount} = {SQLParameterName.DividendAmount},
            {TickerId} = {SQLParameterName.TickerId},
            {CurrencyId} = {SQLParameterName.CurrencyId},
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {Dividends}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {Dividends}
        """

    let getById =
        $"""
        SELECT * FROM {Dividends}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """