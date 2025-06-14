namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // For SQLParameterName access

module internal BankAccountSnapshotQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {BankAccountSnapshots}
        (
            {Id} INTEGER PRIMARY KEY,
            {Date} TEXT NOT NULL,
            {BankAccountId} INTEGER NOT NULL,
            {Balance} TEXT NOT NULL,
            {CurrencyId} INTEGER NOT NULL,
            {InterestEarned} TEXT NOT NULL,
            {FeesPaid} TEXT NOT NULL,
            {CreatedAt} TEXT NOT NULL DEFAULT (DATETIME('now')),
            {UpdatedAt} TEXT,
            -- Foreign key to ensure BankAccountId references a valid BankAccount in the BankAccounts table
            FOREIGN KEY ({BankAccountId}) REFERENCES {BankAccounts}({Id}) ON DELETE CASCADE ON UPDATE CASCADE,
            -- Foreign key to ensure CurrencyId references a valid Currency in the Currencies table
            FOREIGN KEY ({CurrencyId}) REFERENCES {Currencies}({Id}) ON DELETE CASCADE ON UPDATE CASCADE
        );

        -- Index to optimize queries filtering by BankAccountId
        CREATE INDEX IF NOT EXISTS idx_BankAccountSnapshots_BankAccountId ON {BankAccountSnapshots}({BankAccountId});

        -- Index to optimize queries filtering by CurrencyId
        CREATE INDEX IF NOT EXISTS idx_BankAccountSnapshots_CurrencyId ON {BankAccountSnapshots}({CurrencyId});

        -- Index to optimize queries filtering by Date
        CREATE INDEX IF NOT EXISTS idx_BankAccountSnapshots_Date ON {BankAccountSnapshots}({Date});
        
        -- Index to optimize queries for bank account and date combination 
        CREATE INDEX IF NOT EXISTS idx_BankAccountSnapshots_BankAccountId_Date ON {BankAccountSnapshots}({BankAccountId}, {Date});

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_BankAccountSnapshots_UpdatedAt
        AFTER UPDATE ON {BankAccountSnapshots}
        FOR EACH ROW
        BEGIN
            UPDATE {BankAccountSnapshots}
            SET {UpdatedAt} = strftime('%%Y-%%m-%%dT%%H:%%M:%%S', 'now')
            WHERE {Id} = OLD.{Id};
        END;
        """

    let insert =
        $"""
        INSERT INTO {BankAccountSnapshots}
        (
            {Date},
            {BankAccountId},
            {Balance},
            {CurrencyId},
            {InterestEarned},
            {FeesPaid},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.Date},
            {SQLParameterName.BankAccountId},
            {SQLParameterName.Balance},
            {SQLParameterName.CurrencyId},
            {SQLParameterName.InterestEarned},
            {SQLParameterName.FeesPaid},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
        )
        """

    let update =
        $"""
        UPDATE {BankAccountSnapshots}
        SET
            {Date} = {SQLParameterName.Date},
            {BankAccountId} = {SQLParameterName.BankAccountId},
            {Balance} = {SQLParameterName.Balance},
            {CurrencyId} = {SQLParameterName.CurrencyId},
            {InterestEarned} = {SQLParameterName.InterestEarned},
            {FeesPaid} = {SQLParameterName.FeesPaid},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {BankAccountSnapshots}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {BankAccountSnapshots}
        """

    let getById =
        $"""
        SELECT * FROM {BankAccountSnapshots}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """
        
    let getByBankAccountId =
        $"""
        SELECT * FROM {BankAccountSnapshots}
        WHERE
            {BankAccountId} = {SQLParameterName.BankAccountId}
        ORDER BY {Date} DESC
        """
        
    let getLatestByBankAccountId =
        $"""
        SELECT * FROM {BankAccountSnapshots}
        WHERE
            {BankAccountId} = {SQLParameterName.BankAccountId}
        ORDER BY {Date} DESC
        LIMIT 1
        """
        
    let getByCurrencyId =
        $"""
        SELECT * FROM {BankAccountSnapshots}
        WHERE
            {CurrencyId} = {SQLParameterName.CurrencyId}
        ORDER BY {Date} DESC
        """
        
    let getByBankAccountIdAndDate =
        $"""
        SELECT * FROM {BankAccountSnapshots}
        WHERE
            {BankAccountId} = {SQLParameterName.BankAccountId} AND
            {Date} = {SQLParameterName.Date}
        LIMIT 1
        """
        
    let getByDateRange =
        $"""
        SELECT * FROM {BankAccountSnapshots}
        WHERE
            {BankAccountId} = {SQLParameterName.BankAccountId} AND
            {Date} BETWEEN {SQLParameterName.Date} AND {SQLParameterName.DateEnd}
        ORDER BY {Date} ASC
        """