namespace Binnaculum.Core.SQL

open Binnaculum.Core.Keys

module internal TickerPriceQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {TableName_TickerPrices}
        (
            Id INTEGER PRIMARY KEY,
            PriceDate TEXT NOT NULL,
            TickerId INTEGER NOT NULL,
            Price TEXT NOT NULL,
            CurrencyId INTEGER NOT NULL
        )
        """        

    let insert = 
        $"""
        INSERT INTO {TableName_TickerPrices}
        (
            PriceDate,
            TickerId,
            Price,
            CurrencyId
        )
        VALUES
        (
            @PriceDate,
            @TickerId,
            @Price,
            @CurrencyId
        )
        """

    let update = 
        $"""
        UPDATE {TableName_TickerPrices}
        SET
            PriceDate = @PriceDate,
            TickerId = @TickerId,
            Price = @Price,
            CurrencyId = @CurrencyId
        WHERE
            Id = @Id
        """

    let delete = 
        $"""
        DELETE FROM {TableName_TickerPrices}
        WHERE
            Id = @Id
        """

    let getAll = 
        $"""
        SELECT * FROM {TableName_TickerPrices}
        """

    let getById = 
        $"""
        SELECT * FROM {TableName_TickerPrices}
        WHERE
            Id = @Id
        """