namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // For SQLParameterName access

module internal BrokerSnapshotQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {BrokerSnapshots}
        (
            {Id} INTEGER PRIMARY KEY,
            {Date} TEXT NOT NULL,
            {BrokerId} INTEGER NOT NULL,
            {PortfoliosValue} TEXT NOT NULL,
            {RealizedGains} TEXT NOT NULL,
            {RealizedPercentage} TEXT NOT NULL,
            {AccountCount} INTEGER NOT NULL,
            {Invested} TEXT NOT NULL,
            {Commissions} TEXT NOT NULL,
            {Fees} TEXT NOT NULL,
            {CreatedAt} TEXT NOT NULL DEFAULT (DATETIME('now')),
            {UpdatedAt} TEXT,
            -- Foreign key to ensure BrokerId references a valid Broker in the Brokers table
            FOREIGN KEY ({BrokerId}) REFERENCES {Brokers}({Id}) ON DELETE CASCADE ON UPDATE CASCADE
        );

        -- Index to optimize queries filtering by BrokerId
        CREATE INDEX IF NOT EXISTS idx_BrokerSnapshots_BrokerId ON {BrokerSnapshots}({BrokerId});

        -- Index to optimize queries filtering by Date
        CREATE INDEX IF NOT EXISTS idx_BrokerSnapshots_Date ON {BrokerSnapshots}({Date});
        
        -- Index to optimize queries for broker and date combination 
        CREATE INDEX IF NOT EXISTS idx_BrokerSnapshots_BrokerId_Date ON {BrokerSnapshots}({BrokerId}, {Date});

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_BrokerSnapshots_UpdatedAt
        AFTER UPDATE ON {BrokerSnapshots}
        FOR EACH ROW
        BEGIN
            UPDATE {BrokerSnapshots}
            SET {UpdatedAt} = strftime('%%Y-%%m-%%dT%%H:%%M:%%S', 'now')
            WHERE {Id} = OLD.{Id};
        END;
        """

    let insert =
        $"""
        INSERT INTO {BrokerSnapshots}
        (
            {Date},
            {BrokerId},
            {PortfoliosValue},
            {RealizedGains},
            {RealizedPercentage},
            {AccountCount},
            {Invested},
            {Commissions},
            {Fees},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.Date},
            {SQLParameterName.BrokerId},
            {SQLParameterName.PortfoliosValue},
            {SQLParameterName.RealizedGains},
            {SQLParameterName.RealizedPercentage},
            {SQLParameterName.AccountCount},
            {SQLParameterName.Invested},
            {SQLParameterName.Commissions},
            {SQLParameterName.Fees},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
        )
        """

    let update =
        $"""
        UPDATE {BrokerSnapshots}
        SET
            {Date} = {SQLParameterName.Date},
            {BrokerId} = {SQLParameterName.BrokerId},
            {PortfoliosValue} = {SQLParameterName.PortfoliosValue},
            {RealizedGains} = {SQLParameterName.RealizedGains},
            {RealizedPercentage} = {SQLParameterName.RealizedPercentage},
            {AccountCount} = {SQLParameterName.AccountCount},
            {Invested} = {SQLParameterName.Invested},
            {Commissions} = {SQLParameterName.Commissions},
            {Fees} = {SQLParameterName.Fees},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {BrokerSnapshots}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {BrokerSnapshots}
        """

    let getById =
        $"""
        SELECT * FROM {BrokerSnapshots}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """
        
    let getByBrokerId =
        $"""
        SELECT * FROM {BrokerSnapshots}
        WHERE
            {BrokerId} = {SQLParameterName.BrokerId}
        ORDER BY {Date} DESC
        """
        
    let getLatestByBrokerId =
        $"""
        SELECT * FROM {BrokerSnapshots}
        WHERE
            {BrokerId} = {SQLParameterName.BrokerId}
        ORDER BY {Date} DESC
        LIMIT 1
        """
        
    let getByBrokerIdAndDate =
        $"""
        SELECT * FROM {BrokerSnapshots}
        WHERE
            {BrokerId} = {SQLParameterName.BrokerId} AND
            {Date} = {SQLParameterName.Date}
        LIMIT 1
        """
        
    let getByDateRange =
        $"""
        SELECT * FROM {BrokerSnapshots}
        WHERE
            {BrokerId} = {SQLParameterName.BrokerId} AND
            {Date} BETWEEN {SQLParameterName.Date} AND {SQLParameterName.DateEnd}
        ORDER BY {Date} ASC
        """

