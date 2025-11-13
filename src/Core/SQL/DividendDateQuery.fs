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
            {DividendCode} TEXT NOT NULL,
            {CreatedAt} TEXT NOT NULL DEFAULT (datetime('now')),
            {UpdatedAt} TEXT,
            -- Foreign key to ensure TickerId references a valid Ticker in the Tickers table
            FOREIGN KEY ({TickerId}) REFERENCES {Tickers}({Id}) ON DELETE CASCADE ON UPDATE CASCADE,
            -- Foreign key to ensure CurrencyId references a valid Currency in the Currencies table
            FOREIGN KEY ({CurrencyId}) REFERENCES {Currencies}({Id}) ON DELETE CASCADE ON UPDATE CASCADE,
            -- Foreign key to ensure BrokerAccountId references a valid BrokerAccount in the BrokerAccounts table
            FOREIGN KEY ({BrokerAccountId}) REFERENCES {BrokerAccounts}({Id}) ON DELETE CASCADE ON UPDATE CASCADE
        );

        -- Index to optimize queries filtering by TimeStamp
        CREATE INDEX IF NOT EXISTS idx_DividendDates_TimeStamp ON {DividendDates}({TimeStamp});

        -- Index to optimize queries filtering by TickerId
        CREATE INDEX IF NOT EXISTS idx_DividendDates_TickerId ON {DividendDates}({TickerId});

        -- Index to optimize queries filtering by CurrencyId
        CREATE INDEX IF NOT EXISTS idx_DividendDates_CurrencyId ON {DividendDates}({CurrencyId});

        -- Index to optimize queries filtering by BrokerAccountId
        CREATE INDEX IF NOT EXISTS idx_DividendDates_BrokerAccountId ON {DividendDates}({BrokerAccountId});

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_DividendDates_UpdatedAt
        AFTER UPDATE ON {DividendDates}
        FOR EACH ROW
        BEGIN
            UPDATE {DividendDates}
            SET {UpdatedAt} = strftime('%%Y-%%m-%%dT%%H:%%M:%%S', 'now')
            WHERE {Id} = OLD.{Id};
        END;
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
            {DividendCode},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.TimeStamp},
            {SQLParameterName.Amount},
            {SQLParameterName.TickerId},
            {SQLParameterName.CurrencyId},
            {SQLParameterName.BrokerAccountId},
            {SQLParameterName.DividendCode},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
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
            {DividendCode} = {SQLParameterName.DividendCode},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
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

    let getByBrokerAccountIdPaged =
        $"""
        SELECT * FROM {DividendDates}
        WHERE
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId}
        ORDER BY {TimeStamp} DESC, {Id} DESC
        LIMIT @PageSize OFFSET @Offset
        """