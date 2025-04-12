namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal TickerPriceQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {TickerPrices}
        (
            {Id} INTEGER PRIMARY KEY,
            {PriceDate} TEXT NOT NULL,
            {TickerId} INTEGER NOT NULL,
            {Price} TEXT NOT NULL,
            {CurrencyId} INTEGER NOT NULL
        )
        """        

    let insert = 
        $"""
        INSERT INTO {TickerPrices}
        (
            {PriceDate},
            {TickerId},
            {Price},
            {CurrencyId}
        )
        VALUES
        (
            {SQLParameterName.PriceDate},
            {SQLParameterName.TickerId},
            {SQLParameterName.Price},
            {SQLParameterName.CurrencyId}
        )
        """

    let update = 
        $"""
        UPDATE {TickerPrices}
        SET
            {PriceDate} = {SQLParameterName.PriceDate},
            {TickerId} = {SQLParameterName.TickerId},
            {Price} = {SQLParameterName.Price},
            {CurrencyId} = {SQLParameterName.CurrencyId}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete = 
        $"""
        DELETE FROM {TickerPrices}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll = 
        $"""
        SELECT * FROM {TickerPrices}
        """

    let getById = 
        $"""
        SELECT * FROM {TickerPrices}
        WHERE
            {Id} = {SQLParameterName.Id}
        """