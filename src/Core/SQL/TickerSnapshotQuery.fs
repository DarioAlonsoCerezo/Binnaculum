namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // For SQLParameterName access

module internal TickerSnapshotQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {TickerSnapshots}
        (
            {Id} INTEGER PRIMARY KEY,
            {Date} TEXT NOT NULL,
            {TickerId} INTEGER NOT NULL,
            {CurrencyId} INTEGER NOT NULL,
            {TotalShares} REAL NOT NULL,
            {Weight} REAL NOT NULL,
            {CostBasis} REAL NOT NULL,
            {RealCost} REAL NOT NULL, 
            {Dividends} REAL NOT NULL,
            {Options} REAL NOT NULL,
            {TotalIncomes} REAL NOT NULL,
            {CreatedAt} TEXT NOT NULL DEFAULT (DATETIME('now')),
            {UpdatedAt} TEXT,
            -- Foreign key to ensure TickerId references a valid Ticker in the Tickers table
            FOREIGN KEY ({TickerId}) REFERENCES {Tickers}({Id}) ON DELETE CASCADE ON UPDATE CASCADE,
            -- Foreign key to ensure CurrencyId references a valid Currency in the Currencies table
            FOREIGN KEY ({CurrencyId}) REFERENCES {Currencies}({Id}) ON DELETE CASCADE ON UPDATE CASCADE
        );

        -- Index to optimize queries filtering by TickerId
        CREATE INDEX IF NOT EXISTS idx_TickerSnapshots_TickerId ON {TickerSnapshots}({TickerId});

        -- Index to optimize queries filtering by CurrencyId
        CREATE INDEX IF NOT EXISTS idx_TickerSnapshots_CurrencyId ON {TickerSnapshots}({CurrencyId});

        -- Index to optimize queries filtering by Date
        CREATE INDEX IF NOT EXISTS idx_TickerSnapshots_Date ON {TickerSnapshots}({Date});
        
        -- Index to optimize queries for ticker and date combination 
        CREATE INDEX IF NOT EXISTS idx_TickerSnapshots_TickerId_Date ON {TickerSnapshots}({TickerId}, {Date});

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_TickerSnapshots_UpdatedAt
        AFTER UPDATE ON {TickerSnapshots}
        FOR EACH ROW
        BEGIN
            UPDATE {TickerSnapshots}
            SET {UpdatedAt} = strftime('%%Y-%%m-%%dT%%H:%%M:%%S', 'now')
            WHERE {Id} = OLD.{Id};
        END;
        """

    let insert =
        $"""
        INSERT INTO {TickerSnapshots}
        (
            {Date},
            {TickerId},
            {CurrencyId},
            {TotalShares},
            {Weight},
            {CostBasis},
            {RealCost},
            {Dividends},
            {Options},
            {TotalIncomes},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.Date},
            {SQLParameterName.TickerId},
            {SQLParameterName.CurrencyId},
            {SQLParameterName.TotalShares},
            {SQLParameterName.Weight},
            {SQLParameterName.CostBasis},
            {SQLParameterName.RealCost},
            {SQLParameterName.Dividends},
            {SQLParameterName.Options},
            {SQLParameterName.TotalIncomes},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
        )
        """

    let update =
        $"""
        UPDATE {TickerSnapshots}
        SET
            {Date} = {SQLParameterName.Date},
            {TickerId} = {SQLParameterName.TickerId},
            {CurrencyId} = {SQLParameterName.CurrencyId},
            {TotalShares} = {SQLParameterName.TotalShares},
            {Weight} = {SQLParameterName.Weight},
            {CostBasis} = {SQLParameterName.CostBasis},
            {RealCost} = {SQLParameterName.RealCost},
            {Dividends} = {SQLParameterName.Dividends},
            {Options} = {SQLParameterName.Options},
            {TotalIncomes} = {SQLParameterName.TotalIncomes},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {TickerSnapshots}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {TickerSnapshots}
        """

    let getById =
        $"""
        SELECT * FROM {TickerSnapshots}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """
        
    let getByTickerId =
        $"""
        SELECT * FROM {TickerSnapshots}
        WHERE
            {TickerId} = {SQLParameterName.TickerId}
        ORDER BY {Date} DESC
        """
        
    let getLatestByTickerId =
        $"""
        SELECT * FROM {TickerSnapshots}
        WHERE
            {TickerId} = {SQLParameterName.TickerId}
        ORDER BY {Date} DESC
        LIMIT 1
        """
        
    let getByTickerIdAndDate =
        $"""
        SELECT * FROM {TickerSnapshots}
        WHERE
            {TickerId} = {SQLParameterName.TickerId} AND
            {Date} = {SQLParameterName.Date}
        LIMIT 1
        """
        
    let getTickerSnapshotsByDateRange =
        $"""
        SELECT * FROM {TickerSnapshots}
        WHERE
            {TickerId} = {SQLParameterName.TickerId} AND
            {Date} BETWEEN {SQLParameterName.Date} AND {SQLParameterName.DateEnd}
        ORDER BY {Date} ASC
        """

