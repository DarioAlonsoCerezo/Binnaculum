namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal TickerSplitQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {TickerSplits}
        (
            {Id} INTEGER PRIMARY KEY,
            {SplitDate} TEXT NOT NULL,
            {TickerId} INTEGER NOT NULL,
            {SplitFactor} TEXT NOT NULL,
            {CreatedAt} TEXT NOT NULL DEFAULT (datetime('now')),
            {UpdatedAt} TEXT,
            FOREIGN KEY ({TickerId}) REFERENCES {Tickers}({Id}) ON DELETE CASCADE ON UPDATE CASCADE
        );

        -- Index to optimize queries filtering by TickerId
        CREATE INDEX IF NOT EXISTS idx_TickerSplits_TickerId ON {TickerSplits}({TickerId});

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_TickerSplits_UpdatedAt
        AFTER UPDATE ON {TickerSplits}
        FOR EACH ROW
        BEGIN
            UPDATE {TickerSplits}
            SET {UpdatedAt} = datetime('now')
            WHERE {Id} = OLD.{Id};
        END;
        """

    let insert =
        $"""
        INSERT INTO {TickerSplits}
        (
            {SplitDate},
            {TickerId},
            {SplitFactor},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.SplitDate},
            {SQLParameterName.TickerId},
            {SQLParameterName.SplitFactor},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
        )
        """

    let update = 
        $"""
        UPDATE {TickerSplits}
        SET
            {SplitDate} = {SQLParameterName.SplitDate},
            {TickerId} = {SQLParameterName.TickerId},
            {SplitFactor} = {SQLParameterName.SplitFactor},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete = 
        $"""
        DELETE FROM {TickerSplits}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {TickerSplits}
        """

    let getById =
        $"""
        SELECT * FROM {TickerSplits}
        WHERE
            {Id} = {SQLParameterName.Id}
        """
