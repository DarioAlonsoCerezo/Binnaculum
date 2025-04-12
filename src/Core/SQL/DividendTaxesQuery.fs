namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal DividendTaxesQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {DividendTaxes}
        (
            {Id} INTEGER PRIMARY KEY,
            {TimeStamp} TEXT NOT NULL,
            {Amount} TEXT NOT NULL,
            {TickerId} INTEGER NOT NULL,
            {CurrencyId} INTEGER NOT NULL,
            {BrokerAccountId} INTEGER NOT NULL
        )
        """

    let insert =
        $"""
        INSERT INTO {DividendTaxes}
        (
            {TimeStamp},
            {Amount},
            {TickerId},
            {CurrencyId},
            {BrokerAccountId}
        )
        VALUES
        (
            {SQLParameterName.TimeStamp},
            {SQLParameterName.Amount},
            {SQLParameterName.TickerId},
            {SQLParameterName.CurrencyId},
            {SQLParameterName.BrokerAccountId}
        )
        """

    let update =
        $"""
        UPDATE {DividendTaxes}
        SET
            {TimeStamp} = {SQLParameterName.TimeStamp},
            {Amount} = {SQLParameterName.Amount},
            {TickerId} = {SQLParameterName.TickerId},
            {CurrencyId} = {SQLParameterName.CurrencyId},
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {DividendTaxes}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {DividendTaxes}
        """

    let getById =
        $"""
        SELECT * FROM {DividendTaxes}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """