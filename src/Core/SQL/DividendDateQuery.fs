namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal DividendDateQuery =
    let createTable = 
        $"""
        CREATE TABLE IF NOT EXISTS {DividendDates}
        (
            {Id} INTEGER PRIMARY KEY,
            {TimeStamp} TEXT NOT NULL,
            {Amount} TEXT NOT NULL,
            {TickerId} INTEGER NOT NULL,
            {CurrencyId} INTEGER NOT NULL,
            {BrokerAccountId} INTEGER NOT NULL,
            {DividendCode} TEXT NOT NULL
        )
        """

    let insert =
        $"""
        INSERT INTO {DividendDates}
        (
            {TimeStamp},
            {Amount},
            {TickerId},
            {CurrencyId},
            {BrokerAccountId},
            {DividendCode}
        )
        VALUES
        (
            {SQLParameterName.TimeStamp},
            {SQLParameterName.Amount},
            {SQLParameterName.TickerId},
            {SQLParameterName.CurrencyId},
            {SQLParameterName.BrokerAccountId},
            {SQLParameterName.DividendCode}
        )
        """

    let update =
        $"""
        UPDATE {DividendDates}
        SET
            {TimeStamp} = {SQLParameterName.TimeStamp},
            {Amount} = {SQLParameterName.Amount},
            {TickerId} = {SQLParameterName.TickerId},
            {CurrencyId} = {SQLParameterName.CurrencyId},
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId},
            {DividendCode} = {SQLParameterName.DividendCode}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {DividendDates}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {DividendDates}
        """

    let getById =
        $"""
        SELECT * FROM {DividendDates}
        WHERE 
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """