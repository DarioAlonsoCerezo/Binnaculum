namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal BankAccountMovementsQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {BankAccountMovements}
        (
            {Id} INTEGER PRIMARY KEY,
            {TimeStamp} TEXT NOT NULL,
            {Amount} TEXT NOT NULL,
            {BankAccountId} INTEGER NOT NULL,
            {CurrencyId} INTEGER NOT NULL,
            {MovementType} TEXT NOT NULL,
            {CreatedAt} TEXT NOT NULL DEFAULT (DATETIME('now')),
            {UpdatedAt} TEXT,
            -- Foreign key to ensure BankAccountId references a valid BankAccount in the BankAccounts table
            FOREIGN KEY ({BankAccountId}) REFERENCES {BankAccounts}({Id}) ON DELETE CASCADE ON UPDATE CASCADE,
            -- Foreign key to ensure CurrencyId references a valid Currency in the Currencies table
            FOREIGN KEY ({CurrencyId}) REFERENCES {Currencies}({Id}) ON DELETE CASCADE ON UPDATE CASCADE
        );

        -- Index to optimize queries filtering by BankAccountId
        CREATE INDEX IF NOT EXISTS idx_BankAccountMovements_BankAccountId ON {BankAccountMovements}({BankAccountId});

        -- Index to optimize queries filtering by CurrencyId
        CREATE INDEX IF NOT EXISTS idx_BankAccountMovements_CurrencyId ON {BankAccountMovements}({CurrencyId});

        -- Index to optimize queries filtering by TimeStamp
        CREATE INDEX IF NOT EXISTS idx_BankAccountMovements_TimeStamp ON {BankAccountMovements}({TimeStamp});

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_BankAccountMovements_UpdatedAt
        AFTER UPDATE ON {BankAccountMovements}
        FOR EACH ROW
        BEGIN
            UPDATE {BankAccountMovements}
            SET {UpdatedAt} = DATETIME('now')
            WHERE {Id} = OLD.{Id};
        END;
        """

    let insert =
        $"""
        INSERT INTO {BankAccountMovements}
        (
            {TimeStamp},
            {Amount},
            {BankAccountId},
            {CurrencyId},
            {MovementType},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.TimeStamp},
            {SQLParameterName.Amount},
            {SQLParameterName.BankAccountId},
            {SQLParameterName.CurrencyId},
            {SQLParameterName.MovementType},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
        )
        """

    let update =
        $"""
        UPDATE {BankAccountMovements}
        SET
            {TimeStamp} = {SQLParameterName.TimeStamp},
            {Amount} = {SQLParameterName.Amount},
            {BankAccountId} = {SQLParameterName.BankAccountId},
            {CurrencyId} = {SQLParameterName.CurrencyId},
            {MovementType} = {SQLParameterName.MovementType},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {BankAccountMovements}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {BankAccountMovements}
        """

    let getById =
        $"""
        SELECT * FROM {BankAccountMovements}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """