namespace Binnaculum.Core.SQL

module internal TickerPriceQuery =
    let createTable =
        @"
            CREATE TABLE IF NOT EXISTS TickerPrices
            (
                Id INTEGER PRIMARY KEY,
                PriceDate TEXT NOT NULL,
                TickerId INTEGER NOT NULL,
                Price TEXT NOT NULL,
                CurrencyId INTEGER NOT NULL
            )
        "        

    let insert = 
        @"
            INSERT INTO TickerPrices
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
        "

    let update = 
        @"
            UPDATE TickerPrices
            SET
                PriceDate = @PriceDate,
                TickerId = @TickerId,
                Price = @Price,
                CurrencyId = @CurrencyId
            WHERE
                Id = @Id
        "

    let delete = 
        @"
            DELETE FROM TickerPrices
            WHERE
                Id = @Id
        "

    let getAll = 
        @"
            SELECT * FROM TickerPrices
        "

    let getById = 
        @"
            SELECT * FROM TickerPrices
            WHERE
                Id = @Id
        "