namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // For SQLParameterName access

module internal BrokerAccountSnapshotQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {BrokerAccountSnapshots}
        (
            {Id} INTEGER PRIMARY KEY,
            {Date} TEXT NOT NULL,
            {BrokerAccountId} INTEGER NOT NULL,
            {PortfolioValue} TEXT NOT NULL,
            {CreatedAt} TEXT NOT NULL DEFAULT (DATETIME('now')),
            {UpdatedAt} TEXT,
            -- Foreign key to ensure BrokerAccountId references a valid BrokerAccount in the BrokerAccounts table
            FOREIGN KEY ({BrokerAccountId}) REFERENCES {BrokerAccounts}({Id}) ON DELETE CASCADE ON UPDATE CASCADE
        );

        -- Index to optimize queries filtering by BrokerAccountId
        CREATE INDEX IF NOT EXISTS idx_BrokerAccountSnapshots_BrokerAccountId ON {BrokerAccountSnapshots}({BrokerAccountId});

        -- Index to optimize queries filtering by Date
        CREATE INDEX IF NOT EXISTS idx_BrokerAccountSnapshots_Date ON {BrokerAccountSnapshots}({Date});
        
        -- Index to optimize queries for broker account and date combination 
        CREATE INDEX IF NOT EXISTS idx_BrokerAccountSnapshots_BrokerAccountId_Date ON {BrokerAccountSnapshots}({BrokerAccountId}, {Date});

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_BrokerAccountSnapshots_UpdatedAt
        AFTER UPDATE ON {BrokerAccountSnapshots}
        FOR EACH ROW
        BEGIN
            UPDATE {BrokerAccountSnapshots}
            SET {UpdatedAt} = strftime('%%Y-%%m-%%dT%%H:%%M:%%S', 'now')
            WHERE {Id} = OLD.{Id};
        END;
        """

    let insert =
        $"""
        INSERT INTO {BrokerAccountSnapshots}
        (
            {Date},
            {BrokerAccountId},
            {PortfolioValue},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.Date},
            {SQLParameterName.BrokerAccountId},
            {SQLParameterName.PortfolioValue},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
        )
        """

    let update =
        $"""
        UPDATE {BrokerAccountSnapshots}
        SET
            {Date} = {SQLParameterName.Date},
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId},
            {PortfolioValue} = {SQLParameterName.PortfolioValue},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {BrokerAccountSnapshots}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {BrokerAccountSnapshots}
        """

    let getById =
        $"""
        SELECT * FROM {BrokerAccountSnapshots}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """
        
    let getByBrokerAccountId =
        $"""
        SELECT * FROM {BrokerAccountSnapshots}
        WHERE
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId}
        ORDER BY {Date} DESC
        """
        
    let getLatestByBrokerAccountId =
        $"""
        SELECT * FROM {BrokerAccountSnapshots}
        WHERE
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId}
        ORDER BY {Date} DESC
        LIMIT 1
        """
        
    let getByBrokerAccountIdAndDate =
        $"""
        SELECT * FROM {BrokerAccountSnapshots}
        WHERE
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId} AND
            {Date} = {SQLParameterName.Date}
        LIMIT 1
        """
        
    let getByDateRange =
        $"""
        SELECT * FROM {BrokerAccountSnapshots}
        WHERE
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId} AND
            {Date} BETWEEN {SQLParameterName.Date} AND {SQLParameterName.DateEnd}
        ORDER BY {Date} ASC
        """

