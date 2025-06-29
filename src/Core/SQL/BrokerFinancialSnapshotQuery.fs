namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // For SQLParameterName access

module internal BrokerFinancialSnapshotQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {BrokerFinancialSnapshots}
        (
            {Id} INTEGER PRIMARY KEY,
            {Date} TEXT NOT NULL,
            {BrokerId} INTEGER NOT NULL DEFAULT -1,
            {BrokerAccountId} INTEGER NOT NULL DEFAULT -1,
            {CurrencyId} INTEGER NOT NULL,
            {MovementCounter} INTEGER NOT NULL,
            {RealizedGains} TEXT NOT NULL,
            {RealizedPercentage} TEXT NOT NULL,
            {UnrealizedGains} TEXT NOT NULL,
            {UnrealizedGainsPercentage} TEXT NOT NULL,
            {Invested} TEXT NOT NULL,
            {Commissions} TEXT NOT NULL,
            {Fees} TEXT NOT NULL,
            {Deposited} TEXT NOT NULL,
            {Withdrawn} TEXT NOT NULL,
            {DividendsReceived} TEXT NOT NULL,
            {OptionsIncome} TEXT NOT NULL,
            {OtherIncome} TEXT NOT NULL,
            {OpenTrades} INTEGER NOT NULL,
            {CreatedAt} TEXT NOT NULL DEFAULT (DATETIME('now')),
            {UpdatedAt} TEXT,
            -- Foreign key to ensure CurrencyId references a valid Currency in the Currencies table
            FOREIGN KEY ({CurrencyId}) REFERENCES {Currencies}({Id}) ON DELETE CASCADE ON UPDATE CASCADE,
            -- Foreign key to ensure BrokerId references a valid Broker (when not -1)
            FOREIGN KEY ({BrokerId}) REFERENCES {Brokers}({Id}) ON DELETE CASCADE ON UPDATE CASCADE,
            -- Foreign key to ensure BrokerAccountId references a valid BrokerAccount (when not -1)
            FOREIGN KEY ({BrokerAccountId}) REFERENCES {BrokerAccounts}({Id}) ON DELETE CASCADE ON UPDATE CASCADE
        );

        -- Index to optimize queries filtering by CurrencyId
        CREATE INDEX IF NOT EXISTS idx_BrokerFinancialSnapshots_CurrencyId ON {BrokerFinancialSnapshots}({CurrencyId});

        -- Index to optimize queries filtering by Date
        CREATE INDEX IF NOT EXISTS idx_BrokerFinancialSnapshots_Date ON {BrokerFinancialSnapshots}({Date});
        
        -- Index to optimize queries filtering by MovementCounter
        CREATE INDEX IF NOT EXISTS idx_BrokerFinancialSnapshots_MovementCounter ON {BrokerFinancialSnapshots}({MovementCounter});

        -- Index to optimize queries filtering by BrokerId
        CREATE INDEX IF NOT EXISTS idx_BrokerFinancialSnapshots_BrokerId ON {BrokerFinancialSnapshots}({BrokerId});

        -- Index to optimize queries filtering by BrokerAccountId
        CREATE INDEX IF NOT EXISTS idx_BrokerFinancialSnapshots_BrokerAccountId ON {BrokerFinancialSnapshots}({BrokerAccountId});

        -- Composite index to optimize queries filtering by BrokerId and Date
        CREATE INDEX IF NOT EXISTS idx_BrokerFinancialSnapshots_BrokerId_Date ON {BrokerFinancialSnapshots}({BrokerId}, {Date});

        -- Composite index to optimize queries filtering by BrokerAccountId and Date
        CREATE INDEX IF NOT EXISTS idx_BrokerFinancialSnapshots_BrokerAccountId_Date ON {BrokerFinancialSnapshots}({BrokerAccountId}, {Date});

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_BrokerFinancialSnapshots_UpdatedAt
        AFTER UPDATE ON {BrokerFinancialSnapshots}
        FOR EACH ROW
        BEGIN
            UPDATE {BrokerFinancialSnapshots}
            SET {UpdatedAt} = strftime('%%Y-%%m-%%dT%%H:%%M:%%S', 'now')
            WHERE {Id} = OLD.{Id};
        END;
        """

    let insert =
        $"""
        INSERT INTO {BrokerFinancialSnapshots}
        (
            {Date},
            {BrokerId},
            {BrokerAccountId},
            {CurrencyId},
            {MovementCounter},
            {RealizedGains},
            {RealizedPercentage},
            {UnrealizedGains},
            {UnrealizedGainsPercentage},
            {Invested},
            {Commissions},
            {Fees},
            {Deposited},
            {Withdrawn},
            {DividendsReceived},
            {OptionsIncome},
            {OtherIncome},
            {OpenTrades},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.Date},
            {SQLParameterName.BrokerId},
            {SQLParameterName.BrokerAccountId},
            {SQLParameterName.CurrencyId},
            {SQLParameterName.MovementCounter},
            {SQLParameterName.RealizedGains},
            {SQLParameterName.RealizedPercentage},
            {SQLParameterName.UnrealizedGains},
            {SQLParameterName.UnrealizedGainsPercentage},
            {SQLParameterName.Invested},
            {SQLParameterName.Commissions},
            {SQLParameterName.Fees},
            {SQLParameterName.Deposited},
            {SQLParameterName.Withdrawn},
            {SQLParameterName.DividendsReceived},
            {SQLParameterName.OptionsIncome},
            {SQLParameterName.OtherIncome},
            {SQLParameterName.OpenTrades},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
        )
        """

    let update =
        $"""
        UPDATE {BrokerFinancialSnapshots}
        SET
            {Date} = {SQLParameterName.Date},
            {BrokerId} = {SQLParameterName.BrokerId},
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId},
            {CurrencyId} = {SQLParameterName.CurrencyId},
            {MovementCounter} = {SQLParameterName.MovementCounter},
            {RealizedGains} = {SQLParameterName.RealizedGains},
            {RealizedPercentage} = {SQLParameterName.RealizedPercentage},
            {UnrealizedGains} = {SQLParameterName.UnrealizedGains},
            {UnrealizedGainsPercentage} = {SQLParameterName.UnrealizedGainsPercentage},
            {Invested} = {SQLParameterName.Invested},
            {Commissions} = {SQLParameterName.Commissions},
            {Fees} = {SQLParameterName.Fees},
            {Deposited} = {SQLParameterName.Deposited},
            {Withdrawn} = {SQLParameterName.Withdrawn},
            {DividendsReceived} = {SQLParameterName.DividendsReceived},
            {OptionsIncome} = {SQLParameterName.OptionsIncome},
            {OtherIncome} = {SQLParameterName.OtherIncome},
            {OpenTrades} = {SQLParameterName.OpenTrades},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {BrokerFinancialSnapshots}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {BrokerFinancialSnapshots}
        """

    let getById =
        $"""
        SELECT * FROM {BrokerFinancialSnapshots}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """
        
    let getByCurrencyId =
        $"""
        SELECT * FROM {BrokerFinancialSnapshots}
        WHERE
            {CurrencyId} = {SQLParameterName.CurrencyId}
        ORDER BY {Date} DESC
        """
        
    let getLatestByCurrencyId =
        $"""
        SELECT * FROM {BrokerFinancialSnapshots}
        WHERE
            {CurrencyId} = {SQLParameterName.CurrencyId}
        ORDER BY {Date} DESC
        LIMIT 1
        """
        
    let getByCurrencyIdAndDate =
        $"""
        SELECT * FROM {BrokerFinancialSnapshots}
        WHERE
            {CurrencyId} = {SQLParameterName.CurrencyId} AND
            {Date} = {SQLParameterName.Date}
        LIMIT 1
        """
        
    let getByDateRange =
        $"""
        SELECT * FROM {BrokerFinancialSnapshots}
        WHERE
            {CurrencyId} = {SQLParameterName.CurrencyId} AND
            {Date} >= {SQLParameterName.Date} AND
            {Date} <= {SQLParameterName.DateEnd}
        ORDER BY {Date} ASC
        """
        
    let getByMovementCounter =
        $"""
        SELECT * FROM {BrokerFinancialSnapshots}
        WHERE
            {MovementCounter} = {SQLParameterName.MovementCounter}
        ORDER BY {Date} DESC
        """
        
    let getByBrokerId =
        $"""
        SELECT * FROM {BrokerFinancialSnapshots}
        WHERE
            {BrokerId} = {SQLParameterName.BrokerId}
        ORDER BY {Date} DESC
        """
        
    let getLatestByBrokerId =
        $"""
        SELECT * FROM {BrokerFinancialSnapshots}
        WHERE
            {BrokerId} = {SQLParameterName.BrokerId}
        ORDER BY {Date} DESC
        LIMIT 1
        """
        
    let getByBrokerAccountId =
        $"""
        SELECT * FROM {BrokerFinancialSnapshots}
        WHERE
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId}
        ORDER BY {Date} DESC
        """
        
    let getLatestByBrokerAccountId =
        $"""
        SELECT * FROM {BrokerFinancialSnapshots}
        WHERE
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId}
        ORDER BY {Date} DESC
        LIMIT 1
        """
        
    let getLatestByBrokerIdGroupedByDate =
        $"""
        SELECT * FROM {BrokerFinancialSnapshots}
        WHERE
            {BrokerId} = {SQLParameterName.BrokerId} AND
            {Date} = (
                SELECT MAX({Date}) 
                FROM {BrokerFinancialSnapshots} 
                WHERE {BrokerId} = {SQLParameterName.BrokerId}
            )
        ORDER BY {MovementCounter} DESC
        """
        
    let getLatestByBrokerAccountIdGroupedByDate =
        $"""
        SELECT * FROM {BrokerFinancialSnapshots}
        WHERE
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId} AND
            {Date} = (
                SELECT MAX({Date}) 
                FROM {BrokerFinancialSnapshots} 
                WHERE {BrokerAccountId} = {SQLParameterName.BrokerAccountId}
            )
        ORDER BY {MovementCounter} DESC
        """