﻿namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // For SQLParameterName access

module internal TickerCurrencySnapshotQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {TickerCurrencySnapshots}
        (
            {Id} INTEGER PRIMARY KEY,
            {Date} TEXT NOT NULL,
            {TickerId} INTEGER NOT NULL,
            {CurrencyId} INTEGER NOT NULL,
            {TickerSnapshotId} INTEGER NOT NULL,
            {TotalShares} TEXT NOT NULL,
            {Weight} TEXT NOT NULL,
            {CostBasis} TEXT NOT NULL,
            {RealCost} TEXT NOT NULL,
            {Dividends} TEXT NOT NULL,
            {Options} TEXT NOT NULL,
            {TotalIncomes} TEXT NOT NULL,
            {Unrealized} TEXT NOT NULL,
            {Realized} TEXT NOT NULL,
            {Performance} TEXT NOT NULL,
            {LatestPrice} TEXT NOT NULL,
            {OpenTrades} INTEGER NOT NULL,
            {CreatedAt} TEXT NOT NULL DEFAULT (DATETIME('now')),
            {UpdatedAt} TEXT,
            FOREIGN KEY ({TickerId}) REFERENCES {Tickers}({Id}) ON DELETE CASCADE ON UPDATE CASCADE,
            FOREIGN KEY ({CurrencyId}) REFERENCES {Currencies}({Id}) ON DELETE CASCADE ON UPDATE CASCADE,
            FOREIGN KEY ({TickerSnapshotId}) REFERENCES {TickerSnapshots}({Id}) ON DELETE CASCADE ON UPDATE CASCADE
        );

        CREATE INDEX IF NOT EXISTS idx_TickerCurrencySnapshots_TickerId ON {TickerCurrencySnapshots}({TickerId});
        CREATE INDEX IF NOT EXISTS idx_TickerCurrencySnapshots_CurrencyId ON {TickerCurrencySnapshots}({CurrencyId});
        CREATE INDEX IF NOT EXISTS idx_TickerCurrencySnapshots_TickerSnapshotId ON {TickerCurrencySnapshots}({TickerSnapshotId});
        CREATE INDEX IF NOT EXISTS idx_TickerCurrencySnapshots_Date ON {TickerCurrencySnapshots}({Date});
        CREATE INDEX IF NOT EXISTS idx_TickerCurrencySnapshots_TickerId_Date ON {TickerCurrencySnapshots}({TickerId}, {Date});

        CREATE TRIGGER IF NOT EXISTS trg_TickerCurrencySnapshots_UpdatedAt
        AFTER UPDATE ON {TickerCurrencySnapshots}
        FOR EACH ROW
        BEGIN
            UPDATE {TickerCurrencySnapshots}
            SET {UpdatedAt} = strftime('%%Y-%%m-%%dT%%H:%%M:%%S', 'now')
            WHERE {Id} = OLD.{Id};
        END;
        """
    let insert =
        $"""
        INSERT INTO {TickerCurrencySnapshots}
        (
            {Date},
            {TickerId},
            {CurrencyId},
            {TickerSnapshotId},
            {TotalShares},
            {Weight},
            {CostBasis},
            {RealCost},
            {Dividends},
            {Options},
            {TotalIncomes},
            {Unrealized},
            {Realized},
            {Performance},
            {LatestPrice},
            {OpenTrades},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.Date},
            {SQLParameterName.TickerId},
            {SQLParameterName.CurrencyId},
            {SQLParameterName.TickerSnapshotId},
            {SQLParameterName.TotalShares},
            {SQLParameterName.Weight},
            {SQLParameterName.CostBasis},
            {SQLParameterName.RealCost},
            {SQLParameterName.Dividends},
            {SQLParameterName.Options},
            {SQLParameterName.TotalIncomes},
            {SQLParameterName.Unrealized},
            {SQLParameterName.Realized},
            {SQLParameterName.Performance},
            {SQLParameterName.LatestPrice},
            {SQLParameterName.OpenTrades},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
        )
        """
    let update =
        $"""
        UPDATE {TickerCurrencySnapshots}
        SET
            {Date} = {SQLParameterName.Date},
            {TickerId} = {SQLParameterName.TickerId},
            {CurrencyId} = {SQLParameterName.CurrencyId},
            {TickerSnapshotId} = {SQLParameterName.TickerSnapshotId},
            {TotalShares} = {SQLParameterName.TotalShares},
            {Weight} = {SQLParameterName.Weight},
            {CostBasis} = {SQLParameterName.CostBasis},
            {RealCost} = {SQLParameterName.RealCost},
            {Dividends} = {SQLParameterName.Dividends},
            {Options} = {SQLParameterName.Options},
            {TotalIncomes} = {SQLParameterName.TotalIncomes},
            {Unrealized} = {SQLParameterName.Unrealized},
            {Realized} = {SQLParameterName.Realized},
            {Performance} = {SQLParameterName.Performance},
            {LatestPrice} = {SQLParameterName.LatestPrice},
            {OpenTrades} = {SQLParameterName.OpenTrades},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
        WHERE
            {Id} = {SQLParameterName.Id}
        """
    let delete =
        $"""
        DELETE FROM {TickerCurrencySnapshots}
        WHERE
            {Id} = {SQLParameterName.Id}
        """
    let getAll =
        $"""
        SELECT * FROM {TickerCurrencySnapshots}
        """
    let getById =
        $"""
        SELECT * FROM {TickerCurrencySnapshots}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """
    let getByTickerId =
        $"""
        SELECT * FROM {TickerCurrencySnapshots}
        WHERE
            {TickerId} = {SQLParameterName.TickerId}
        ORDER BY {Date} DESC
        """
    let getLatestByTickerId =
        $"""
        SELECT * FROM {TickerCurrencySnapshots}
        WHERE
            {TickerId} = {SQLParameterName.TickerId}
        ORDER BY {Date} DESC
        LIMIT 1
        """
    let getByTickerIdAndDate =
        $"""
        SELECT * FROM {TickerCurrencySnapshots}
        WHERE
            {TickerId} = {SQLParameterName.TickerId} AND
            {Date} = {SQLParameterName.Date}
        LIMIT 1
        """
    let getTickerCurrencySnapshotsByDateRange =
        $"""
        SELECT * FROM {TickerCurrencySnapshots}
        WHERE
            {TickerId} = {SQLParameterName.TickerId} AND
            {Date} BETWEEN {SQLParameterName.Date} AND {SQLParameterName.DateEnd}
        ORDER BY {Date} ASC
        """

