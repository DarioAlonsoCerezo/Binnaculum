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
            {DividendTaxAmount} TEXT NOT NULL,
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
        CREATE INDEX IF NOT EXISTS idx_DividendTaxes_TimeStamp ON {DividendTaxes}({TimeStamp});

        -- Index to optimize queries filtering by TickerId
        CREATE INDEX IF NOT EXISTS idx_DividendTaxes_TickerId ON {DividendTaxes}({TickerId});

        -- Index to optimize queries filtering by CurrencyId
        CREATE INDEX IF NOT EXISTS idx_DividendTaxes_CurrencyId ON {DividendTaxes}({CurrencyId});

        -- Index to optimize queries filtering by BrokerAccountId
        CREATE INDEX IF NOT EXISTS idx_DividendTaxes_BrokerAccountId ON {DividendTaxes}({BrokerAccountId});

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_DividendTaxes_UpdatedAt
        AFTER UPDATE ON {DividendTaxes}
        FOR EACH ROW
        BEGIN
            UPDATE {DividendTaxes}
            SET {UpdatedAt} = strftime('%%Y-%%m-%%dT%%H:%%M:%%S', 'now')
            WHERE {Id} = OLD.{Id};
        END;
        """

    let insert =
        $"""
        INSERT INTO {DividendTaxes}
        (
            {TimeStamp},
            {DividendTaxAmount},
            {TickerId},
            {CurrencyId},
            {BrokerAccountId},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.TimeStamp},
            {SQLParameterName.DividendTaxAmount},
            {SQLParameterName.TickerId},
            {SQLParameterName.CurrencyId},
            {SQLParameterName.BrokerAccountId},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
        )
        """

    let update =
        $"""
        UPDATE {DividendTaxes}
        SET
            {TimeStamp} = {SQLParameterName.TimeStamp},
            {DividendTaxAmount} = {SQLParameterName.DividendTaxAmount},
            {TickerId} = {SQLParameterName.TickerId},
            {CurrencyId} = {SQLParameterName.CurrencyId},
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
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

    let getBetweenDates =
        $"""
        SELECT * FROM {DividendTaxes}
        WHERE {TimeStamp} BETWEEN {SQLParameterName.StartDate} AND {SQLParameterName.EndDate}
        """

    let getByTickerCurrencyAndDateRange =
        $"""
        SELECT * FROM {DividendTaxes}
        WHERE {TickerId} = {SQLParameterName.TickerId}
        AND {CurrencyId} = {SQLParameterName.CurrencyId}
        AND {TimeStamp} > {SQLParameterName.StartDate}
        AND {TimeStamp} <= {SQLParameterName.EndDate}
        """

    let getFilteredDividendTaxes =
        $"""
        SELECT * FROM {DividendTaxes}
        WHERE {TickerId} = {SQLParameterName.TickerId}
        AND {CurrencyId} = {SQLParameterName.CurrencyId}
        AND {TimeStamp} >= {SQLParameterName.StartDate}
        AND {TimeStamp} <= {SQLParameterName.EndDate}
        """