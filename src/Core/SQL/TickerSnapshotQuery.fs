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
            {CreatedAt} TEXT NOT NULL DEFAULT (DATETIME('now')),
            {UpdatedAt} TEXT,
            FOREIGN KEY ({TickerId}) REFERENCES {Tickers}({Id}) ON DELETE CASCADE ON UPDATE CASCADE
        );

        CREATE INDEX IF NOT EXISTS idx_TickerSnapshots_TickerId ON {TickerSnapshots}({TickerId});
        CREATE INDEX IF NOT EXISTS idx_TickerSnapshots_Date ON {TickerSnapshots}({Date});
        CREATE INDEX IF NOT EXISTS idx_TickerSnapshots_TickerId_Date ON {TickerSnapshots}({TickerId}, {Date});

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
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.Date},
            {SQLParameterName.TickerId},
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

    let getLatestBeforeDate =
        $"""
        SELECT * FROM {TickerSnapshots}
        WHERE
            {TickerId} = {SQLParameterName.TickerId} AND
            {Date} < {SQLParameterName.Date}
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

    let getTickerSnapshotsAfterDate =
        $"""
        SELECT * FROM {TickerSnapshots}
        WHERE
            {TickerId} = {SQLParameterName.TickerId} AND
            {Date} > {SQLParameterName.Date}
        ORDER BY {Date} ASC
        """
