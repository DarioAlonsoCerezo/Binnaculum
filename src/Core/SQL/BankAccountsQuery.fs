namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal BankAccountsQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {BankAccounts}
        (
            {Id} INTEGER PRIMARY KEY,
            {BankId} INTEGER NOT NULL,
            {Name} TEXT NOT NULL,
            {Description} TEXT,
            {CurrencyId} INTEGER NOT NULL,
            {CreatedAt} TEXT NOT NULL DEFAULT (DATETIME('now')),
            {UpdatedAt} TEXT,
            -- Foreign key to ensure BankId references a valid Bank in the Banks table
            FOREIGN KEY ({BankId}) REFERENCES {Banks}({Id}) ON DELETE CASCADE ON UPDATE CASCADE,
            -- Foreign key to ensure CurrencyId references a valid Currency in the Currencies table
            FOREIGN KEY ({CurrencyId}) REFERENCES {Currencies}({Id}) ON DELETE CASCADE ON UPDATE CASCADE
        );

        -- Index to optimize queries filtering by BankId
        CREATE INDEX IF NOT EXISTS idx_BankAccounts_BankId ON {BankAccounts}({BankId});

        -- Index to optimize queries filtering by CurrencyId
        CREATE INDEX IF NOT EXISTS idx_BankAccounts_CurrencyId ON {BankAccounts}({CurrencyId});

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_BankAccounts_UpdatedAt
        AFTER UPDATE ON {BankAccounts}
        FOR EACH ROW
        BEGIN
            UPDATE {BankAccounts}
            SET {UpdatedAt} = strftime('%%Y-%%m-%%dT%%H:%%M:%%S', 'now')
            WHERE {Id} = OLD.{Id};
        END;
        """

    let insert =
        $"""
        INSERT INTO {BankAccounts}
        (
            {Name},
            {BankId},
            {Description},
            {CurrencyId},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.Name},
            {SQLParameterName.BankId},
            {SQLParameterName.Description},
            {SQLParameterName.CurrencyId},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
        )
        """

    let update =
        $"""
        UPDATE {BankAccounts}
        SET
            {Name} = {SQLParameterName.Name},
            {BankId} = {SQLParameterName.BankId},
            {Description} = {SQLParameterName.Description},
            {CurrencyId} = {SQLParameterName.CurrencyId},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {BankAccounts}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll = 
        $"""
        SELECT * FROM {BankAccounts}
        """

    let getById =
        $"""
        SELECT * FROM {BankAccounts}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """

    let getByBankId =
        $"""
        SELECT * FROM {BankAccounts}
        WHERE {BankId} = {SQLParameterName.BankId}
        """