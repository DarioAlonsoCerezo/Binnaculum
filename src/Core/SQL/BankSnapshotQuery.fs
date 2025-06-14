namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // For SQLParameterName access

module internal BankSnapshotQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {BankSnapshots}
        (
            {Id} INTEGER PRIMARY KEY,
            {Date} TEXT NOT NULL,
            {BankId} INTEGER NOT NULL,
            {TotalBalance} TEXT NOT NULL,
            {InterestEarned} TEXT NOT NULL,
            {FeesPaid} TEXT NOT NULL,
            {AccountCount} INTEGER NOT NULL,
            {CreatedAt} TEXT NOT NULL DEFAULT (DATETIME('now')),
            {UpdatedAt} TEXT,
            -- Foreign key to ensure BankId references a valid Bank in the Banks table
            FOREIGN KEY ({BankId}) REFERENCES {Banks}({Id}) ON DELETE CASCADE ON UPDATE CASCADE
        );

        -- Index to optimize queries filtering by BankId
        CREATE INDEX IF NOT EXISTS idx_BankSnapshots_BankId ON {BankSnapshots}({BankId});

        -- Index to optimize queries filtering by Date
        CREATE INDEX IF NOT EXISTS idx_BankSnapshots_Date ON {BankSnapshots}({Date});
        
        -- Index to optimize queries for bank and date combination 
        CREATE INDEX IF NOT EXISTS idx_BankSnapshots_BankId_Date ON {BankSnapshots}({BankId}, {Date});

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_BankSnapshots_UpdatedAt
        AFTER UPDATE ON {BankSnapshots}
        FOR EACH ROW
        BEGIN
            UPDATE {BankSnapshots}
            SET {UpdatedAt} = strftime('%%Y-%%m-%%dT%%H:%%M:%%S', 'now')
            WHERE {Id} = OLD.{Id};
        END;
        """

    let insert =
        $"""
        INSERT INTO {BankSnapshots}
        (
            {Date},
            {BankId},
            {TotalBalance},
            {InterestEarned},
            {FeesPaid},
            {AccountCount},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.Date},
            {SQLParameterName.BankId},
            {SQLParameterName.TotalBalance},
            {SQLParameterName.InterestEarned},
            {SQLParameterName.FeesPaid},
            {SQLParameterName.AccountCount},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
        )
        """

    let update =
        $"""
        UPDATE {BankSnapshots}
        SET
            {Date} = {SQLParameterName.Date},
            {BankId} = {SQLParameterName.BankId},
            {TotalBalance} = {SQLParameterName.TotalBalance},
            {InterestEarned} = {SQLParameterName.InterestEarned},
            {FeesPaid} = {SQLParameterName.FeesPaid},
            {AccountCount} = {SQLParameterName.AccountCount},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {BankSnapshots}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {BankSnapshots}
        """

    let getById =
        $"""
        SELECT * FROM {BankSnapshots}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """
        
    let getByBankId =
        $"""
        SELECT * FROM {BankSnapshots}
        WHERE
            {BankId} = {SQLParameterName.BankId}
        ORDER BY {Date} DESC
        """
        
    let getLatestByBankId =
        $"""
        SELECT * FROM {BankSnapshots}
        WHERE
            {BankId} = {SQLParameterName.BankId}
        ORDER BY {Date} DESC
        LIMIT 1
        """
        
    let getByBankIdAndDate =
        $"""
        SELECT * FROM {BankSnapshots}
        WHERE
            {BankId} = {SQLParameterName.BankId} AND
            {Date} = {SQLParameterName.Date}
        LIMIT 1
        """
        
    let getByDateRange =
        $"""
        SELECT * FROM {BankSnapshots}
        WHERE
            {BankId} = {SQLParameterName.BankId} AND
            {Date} BETWEEN {SQLParameterName.Date} AND {SQLParameterName.DateEnd}
        ORDER BY {Date} ASC
        """