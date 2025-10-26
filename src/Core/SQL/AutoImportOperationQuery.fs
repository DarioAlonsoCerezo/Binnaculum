namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core

module internal AutoImportOperationQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {AutoImportOperations}
        (
            {Id} INTEGER PRIMARY KEY,
            {BrokerAccountId} INTEGER NOT NULL,
            {TickerId} INTEGER NOT NULL,
            {CurrencyId} INTEGER NOT NULL,
            {IsOpen} INTEGER NOT NULL DEFAULT 1,
            {Realized} TEXT NOT NULL DEFAULT '0.0',
            {RealizedToday} TEXT NOT NULL DEFAULT '0.0',
            {Commissions} TEXT NOT NULL DEFAULT '0.0',
            {Fees} TEXT NOT NULL DEFAULT '0.0',
            {Premium} TEXT NOT NULL DEFAULT '0.0',
            {Dividends} TEXT NOT NULL DEFAULT '0.0',
            {DividendTaxes} TEXT NOT NULL DEFAULT '0.0',
            {CapitalDeployed} TEXT NOT NULL DEFAULT '0.0',
            {CapitalDeployedToday} TEXT NOT NULL DEFAULT '0.0',
            {Performance} TEXT NOT NULL DEFAULT '0.0',
            {Invested} TEXT NOT NULL DEFAULT '0.0',
            {CreatedAt} TEXT NOT NULL DEFAULT (datetime('now')),
            {UpdatedAt} TEXT,
            -- Foreign key to ensure BrokerAccountId references a valid BrokerAccount
            FOREIGN KEY ({BrokerAccountId}) REFERENCES {BrokerAccounts}({Id}) ON DELETE CASCADE ON UPDATE CASCADE,
            -- Foreign key to ensure TickerId references a valid Ticker
            FOREIGN KEY ({TickerId}) REFERENCES {Tickers}({Id}) ON DELETE CASCADE ON UPDATE CASCADE,
            -- Foreign key to ensure CurrencyId references a valid Currency
            FOREIGN KEY ({CurrencyId}) REFERENCES {Currencies}({Id}) ON DELETE CASCADE ON UPDATE CASCADE
        );

        -- Index to optimize queries filtering by BrokerAccountId
        CREATE INDEX IF NOT EXISTS idx_AutoImportOperations_BrokerAccountId ON {AutoImportOperations}({BrokerAccountId});

        -- Index to optimize queries filtering by TickerId
        CREATE INDEX IF NOT EXISTS idx_AutoImportOperations_TickerId ON {AutoImportOperations}({TickerId});

        -- Index to optimize queries filtering by IsOpen
        CREATE INDEX IF NOT EXISTS idx_AutoImportOperations_IsOpen ON {AutoImportOperations}({IsOpen});

        -- Composite index for finding open operations for a ticker + broker account
        CREATE INDEX IF NOT EXISTS idx_AutoImportOperations_TickerBrokerOpen 
            ON {AutoImportOperations}({TickerId}, {BrokerAccountId}, {IsOpen});

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_AutoImportOperations_UpdatedAt
        AFTER UPDATE ON {AutoImportOperations}
        FOR EACH ROW
        BEGIN
            UPDATE {AutoImportOperations}
            SET {UpdatedAt} = strftime('%%Y-%%m-%%dT%%H:%%M:%%S', 'now')
            WHERE {Id} = OLD.{Id};
        END;
        """

    let insert =
        $"""
        INSERT INTO {AutoImportOperations}
        (
            {BrokerAccountId},
            {TickerId},
            {CurrencyId},
            {IsOpen},
            {Realized},
            {RealizedToday},
            {Commissions},
            {Fees},
            {Premium},
            {Dividends},
            {DividendTaxes},
            {CapitalDeployed},
            {CapitalDeployedToday},
            {Performance},
            {Invested},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.BrokerAccountId},
            {SQLParameterName.TickerId},
            {SQLParameterName.CurrencyId},
            {SQLParameterName.IsOpen},
            {SQLParameterName.Realized},
            {SQLParameterName.RealizedToday},
            {SQLParameterName.Commissions},
            {SQLParameterName.Fees},
            {SQLParameterName.Premium},
            {SQLParameterName.Dividends},
            {SQLParameterName.DividendTaxes},
            {SQLParameterName.CapitalDeployed},
            {SQLParameterName.CapitalDeployedToday},
            {SQLParameterName.Performance},
            {SQLParameterName.Invested},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
        )
        """

    let update =
        $"""
        UPDATE {AutoImportOperations}
        SET
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId},
            {TickerId} = {SQLParameterName.TickerId},
            {CurrencyId} = {SQLParameterName.CurrencyId},
            {IsOpen} = {SQLParameterName.IsOpen},
            {Realized} = {SQLParameterName.Realized},
            {RealizedToday} = {SQLParameterName.RealizedToday},
            {Commissions} = {SQLParameterName.Commissions},
            {Fees} = {SQLParameterName.Fees},
            {Premium} = {SQLParameterName.Premium},
            {Dividends} = {SQLParameterName.Dividends},
            {DividendTaxes} = {SQLParameterName.DividendTaxes},
            {CapitalDeployed} = {SQLParameterName.CapitalDeployed},
            {CapitalDeployedToday} = {SQLParameterName.CapitalDeployedToday},
            {Performance} = {SQLParameterName.Performance},
            {Invested} = {SQLParameterName.Invested},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {AutoImportOperations}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {AutoImportOperations}
        """

    let getById =
        $"""
        SELECT * FROM {AutoImportOperations}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """

    let selectByTicker =
        $"""
        SELECT * FROM {AutoImportOperations}
        WHERE
            {TickerId} = {SQLParameterName.TickerId}
        ORDER BY {CreatedAt} ASC
        """

    let selectByBrokerAccount =
        $"""
        SELECT * FROM {AutoImportOperations}
        WHERE
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId}
        ORDER BY {CreatedAt} ASC
        """

    let selectOpenOperation =
        $"""
        SELECT * FROM {AutoImportOperations}
        WHERE
            {TickerId} = {SQLParameterName.TickerId}
            AND {BrokerAccountId} = {SQLParameterName.BrokerAccountId}
            AND {IsOpen} = 1
        LIMIT 1
        """

    let selectOpenOperationsByCurrency =
        $"""
        SELECT * FROM {AutoImportOperations}
        WHERE
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId}
            AND {CurrencyId} = {SQLParameterName.CurrencyId}
            AND {IsOpen} = 1
        ORDER BY {CreatedAt} ASC
        """
