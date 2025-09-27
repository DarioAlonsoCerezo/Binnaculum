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
            {Leveraged} TEXT NOT NULL DEFAULT '1.0',
            {Notes} TEXT,
            {CreatedAt} TEXT NOT NULL DEFAULT (datetime('now')),
            {UpdatedAt} TEXT,
            -- Foreign key to ensure TickerId references a valid Ticker in the Tickers table
            FOREIGN KEY ({TickerId}) REFERENCES {Tickers}({Id}) ON DELETE CASCADE ON UPDATE CASCADE,
            -- Foreign key to ensure BrokerAccountId references a valid BrokerAccount in the BrokerAccounts table
            FOREIGN KEY ({BrokerAccountId}) REFERENCES {BrokerAccounts}({Id}) ON DELETE CASCADE ON UPDATE CASCADE,
            -- Foreign key to ensure CurrencyId references a valid Currency in the Currencies table
            FOREIGN KEY ({CurrencyId}) REFERENCES {Currencies}({Id}) ON DELETE CASCADE ON UPDATE CASCADE
        );

        -- Index to optimize queries filtering by TimeStamp
        CREATE INDEX IF NOT EXISTS idx_Trades_TimeStamp ON {Trades}({TimeStamp});

        -- Index to optimize queries filtering by TickerId
        CREATE INDEX IF NOT EXISTS idx_Trades_TickerId ON {Trades}({TickerId});

        -- Index to optimize queries filtering by BrokerAccountId
        CREATE INDEX IF NOT EXISTS idx_Trades_BrokerAccountId ON {Trades}({BrokerAccountId});

        -- Index to optimize queries filtering by CurrencyId
        CREATE INDEX IF NOT EXISTS idx_Trades_CurrencyId ON {Trades}({CurrencyId});

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_Trades_UpdatedAt
        AFTER UPDATE ON {Trades}
        FOR EACH ROW
        BEGIN
            UPDATE {Trades}
            SET {UpdatedAt} = strftime('%%Y-%%m-%%dT%%H:%%M:%%S', 'now')
            WHERE {Id} = OLD.{Id};
        END;
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
            {Leveraged},
            {Notes},
            {CreatedAt},
            {UpdatedAt}
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
            {SQLParameterName.Leveraged},
            {SQLParameterName.Notes},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
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
            {Leveraged} = {SQLParameterName.Leveraged},
            {Notes} = {SQLParameterName.Notes},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
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

    let getBetweenDates =
        $"""
        SELECT * FROM {Trades}
        WHERE {TimeStamp} BETWEEN {SQLParameterName.StartDate} AND {SQLParameterName.EndDate}
        """

    let getByTickerCurrencyAndDateRange =
        $"""
        SELECT * FROM {Trades}
        WHERE {TickerId} = {SQLParameterName.TickerId}
        AND {CurrencyId} = {SQLParameterName.CurrencyId}
        AND {TimeStamp} > {SQLParameterName.StartDate}
        AND {TimeStamp} <= {SQLParameterName.EndDate}
        """

    let getFilteredTrades =
        $"""
        SELECT * FROM {Trades}
        WHERE {TickerId} = {SQLParameterName.TickerId}
        AND {CurrencyId} = {SQLParameterName.CurrencyId}
        AND {TimeStamp} >= {SQLParameterName.StartDate}
        AND {TimeStamp} <= {SQLParameterName.EndDate}
        """

    let getCurrenciesByTickerAndDateRange =
        $"""
        SELECT DISTINCT {CurrencyId} FROM {Trades}
        WHERE {TickerId} = {SQLParameterName.TickerId}
        AND {CurrencyId} = {SQLParameterName.CurrencyId}
        AND {TimeStamp} > {SQLParameterName.StartDate}
        AND {TimeStamp} <= {SQLParameterName.EndDate}
        """

    let getCurrenciesByTickerAndExactDate =
        $"""
        SELECT DISTINCT {CurrencyId} FROM {Trades}
        WHERE {TickerId} = {SQLParameterName.TickerId}
        AND {TimeStamp} = {SQLParameterName.Date}
        """

    let getDistinctCurrenciesByTickerAndDate =
        $"""
        SELECT DISTINCT {CurrencyId} FROM {Trades}
        WHERE {TickerId} = {SQLParameterName.TickerId}
        AND DATE({TimeStamp}) = DATE({SQLParameterName.Date})
        """

    let getCurrenciesByTickerAndDate =
        $"""
        SELECT DISTINCT {CurrencyId} FROM {Trades}
        WHERE {TickerId} = {SQLParameterName.TickerId}
        AND DATE({TimeStamp}) = DATE({SQLParameterName.Date})
        """

    // Query to get trades by BrokerAccountId and CurrencyId
    let getByBrokerAccountAndCurrency =
        $"""
        SELECT * FROM {Trades}
        WHERE {BrokerAccountId} = {SQLParameterName.BrokerAccountId}
        AND {CurrencyId} = {SQLParameterName.CurrencyId}
        """

    // Query to get trades by BrokerAccountId, CurrencyId, and date range
    let getByBrokerAccountAndCurrencyWithDates =
        $"""
        SELECT * FROM {Trades}
        WHERE {BrokerAccountId} = {SQLParameterName.BrokerAccountId}
        AND {CurrencyId} = {SQLParameterName.CurrencyId}
        AND {TimeStamp} >= {SQLParameterName.StartDate}
        AND {TimeStamp} <= {SQLParameterName.EndDate}
        """

    let getByBrokerAccountIdFromDate =
        $"""
        SELECT * FROM {Trades}
        WHERE
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId}
            AND {TimeStamp} >= {SQLParameterName.TimeStamp}
        ORDER BY {TimeStamp}
        """
    
    let getByBrokerAccountIdForDate =
        $"""
        SELECT * FROM {Trades}
        WHERE
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId}
            AND DATE({TimeStamp}) = DATE({SQLParameterName.TimeStamp})
        ORDER BY {TimeStamp}
        """