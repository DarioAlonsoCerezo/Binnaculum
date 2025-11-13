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
            {BrokerAccountId} INTEGER NOT NULL,
            {CreatedAt} TEXT NOT NULL DEFAULT (DATETIME('now')),
            {UpdatedAt} TEXT,
            -- Foreign key to ensure TickerId references a valid Ticker in the Tickers table
            FOREIGN KEY ({TickerId}) REFERENCES {Tickers}({Id}) ON DELETE CASCADE ON UPDATE CASCADE,
            -- Foreign key to ensure CurrencyId references a valid Currency in the Currencies table
            FOREIGN KEY ({CurrencyId}) REFERENCES {Currencies}({Id}) ON DELETE CASCADE ON UPDATE CASCADE,
            -- Foreign key to ensure BrokerAccountId references a valid BrokerAccount in the BrokerAccounts table
            FOREIGN KEY ({BrokerAccountId}) REFERENCES {BrokerAccounts}({Id}) ON DELETE CASCADE ON UPDATE CASCADE
        );

        -- Index to optimize queries filtering by TimeStamp
        CREATE INDEX IF NOT EXISTS idx_Dividends_TimeStamp ON {Dividends}({TimeStamp});

        -- Index to optimize queries filtering by TickerId
        CREATE INDEX IF NOT EXISTS idx_Dividends_TickerId ON {Dividends}({TickerId});

        -- Index to optimize queries filtering by CurrencyId
        CREATE INDEX IF NOT EXISTS idx_Dividends_CurrencyId ON {Dividends}({CurrencyId});

        -- Index to optimize queries filtering by BrokerAccountId
        CREATE INDEX IF NOT EXISTS idx_Dividends_BrokerAccountId ON {Dividends}({BrokerAccountId});

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_Dividends_UpdatedAt
        AFTER UPDATE ON {Dividends}
        FOR EACH ROW
        BEGIN
            UPDATE {Dividends}
            SET {UpdatedAt} = strftime('%%Y-%%m-%%dT%%H:%%M:%%S', 'now')
            WHERE {Id} = OLD.{Id};
        END;
        """

    let insert =
        $"""
        INSERT INTO {Dividends}
        (
            {TimeStamp},
            {DividendAmount},
            {TickerId},
            {CurrencyId},
            {BrokerAccountId},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.TimeStamp},
            {SQLParameterName.DividendAmount},
            {SQLParameterName.TickerId},
            {SQLParameterName.CurrencyId},
            {SQLParameterName.BrokerAccountId},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
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
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId},
            {CreatedAt} = {SQLParameterName.CreatedAt},
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

    let getBetweenDates =
        $"""
        SELECT * FROM {Dividends}
        WHERE {TimeStamp} BETWEEN {SQLParameterName.StartDate} AND {SQLParameterName.EndDate}
        """

    let getByTickerCurrencyAndDateRange =
        $"""
        SELECT * FROM {Dividends}
        WHERE {TickerId} = {SQLParameterName.TickerId}
        AND {CurrencyId} = {SQLParameterName.CurrencyId}
        AND {TimeStamp} > {SQLParameterName.StartDate}
        AND {TimeStamp} <= {SQLParameterName.EndDate}
        """

    let getFilteredDividends =
        $"""
        SELECT * FROM {Dividends}
        WHERE {TickerId} = {SQLParameterName.TickerId}
        AND {CurrencyId} = {SQLParameterName.CurrencyId}
        AND {TimeStamp} >= {SQLParameterName.StartDate}
        AND {TimeStamp} <= {SQLParameterName.EndDate}
        """

    let getCurrenciesByTickerAndDate =
        $"""
        SELECT DISTINCT {CurrencyId} FROM {Dividends}
        WHERE {TickerId} = {SQLParameterName.TickerId}
        AND DATE({TimeStamp}) = DATE({SQLParameterName.Date})
        """

    let getByBrokerAccountIdFromDate =
        $"""
        SELECT * FROM {Dividends}
        WHERE
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId}
            AND {TimeStamp} >= {SQLParameterName.TimeStamp}
        ORDER BY {TimeStamp}
        """

    let getByBrokerAccountIdForDate =
        $"""
        SELECT * FROM {Dividends}
        WHERE
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId}
            AND DATE({TimeStamp}) = DATE({SQLParameterName.TimeStamp})
        ORDER BY {TimeStamp}
        """

    let getByTickerIdFromDate =
        $"""
        SELECT * FROM {Dividends}
        WHERE
            {TickerId} = {SQLParameterName.TickerId}
            AND {TimeStamp} >= {SQLParameterName.TimeStamp}
        ORDER BY {TimeStamp}
        """

    let getByBrokerAccountIdPaged =
        $"""
        SELECT * FROM {Dividends}
        WHERE
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId}
        ORDER BY {TimeStamp} DESC, {Id} DESC
        LIMIT @PageSize OFFSET @Offset
        """
