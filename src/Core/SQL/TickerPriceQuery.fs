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
            {Price} TEXT NOT NULL DEFAULT '0',
            {CurrencyId} INTEGER NOT NULL,
            {CreatedAt} TEXT NOT NULL DEFAULT (datetime('now')),
            {UpdatedAt} TEXT,
            -- Foreign key to ensure TickerId references a valid Ticker in the Tickers table
            FOREIGN KEY ({TickerId}) REFERENCES {Tickers}({Id}) ON DELETE CASCADE ON UPDATE CASCADE,
            -- Foreign key to ensure CurrencyId references a valid Currency in the Currencies table
            FOREIGN KEY ({CurrencyId}) REFERENCES {Currencies}({Id}) ON DELETE CASCADE ON UPDATE CASCADE
        );

        -- Index to optimize queries filtering by PriceDate
        CREATE INDEX IF NOT EXISTS idx_TickerPrices_PriceDate ON {TickerPrices}({PriceDate});
        
        -- Index to optimize queries filtering by TickerId
        CREATE INDEX IF NOT EXISTS idx_TickerPrices_TickerId ON {TickerPrices}({TickerId});
        
        -- Index to optimize queries filtering by CurrencyId
        CREATE INDEX IF NOT EXISTS idx_TickerPrices_CurrencyId ON {TickerPrices}({CurrencyId});

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_TickerPrices_UpdatedAt
        AFTER UPDATE ON {TickerPrices}
        FOR EACH ROW
        BEGIN
            UPDATE {TickerPrices}
            SET {UpdatedAt} = strftime('%%Y-%%m-%%dT%%H:%%M:%%S', 'now')
            WHERE {Id} = OLD.{Id};
        END;
        """        

    let insert = 
        $"""
        INSERT INTO {TickerPrices}
        (
            {PriceDate},
            {TickerId},
            {Price},
            {CurrencyId},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.PriceDate},
            {SQLParameterName.TickerId},
            {SQLParameterName.Price},
            {SQLParameterName.CurrencyId},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
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
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
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

    let getPriceByDateOrPrevious =
        $"""
        SELECT {Price} FROM {TickerPrices}
        WHERE {TickerId} = {SQLParameterName.TickerId} AND {PriceDate} <= {SQLParameterName.PriceDate}
        ORDER BY {PriceDate} DESC
        LIMIT 1
        """