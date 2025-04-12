namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal TickersQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {Tickers}
        (
            {Id} INTEGER PRIMARY KEY,
            {Symbol} TEXT NOT NULL,
            {Image} TEXT,
            {Name} TEXT,
            {CreatedAt} TEXT NOT NULL DEFAULT (datetime('now')),
            {UpdatedAt} TEXT
        );

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_Tickers_UpdatedAt
        AFTER UPDATE ON {Tickers}
        FOR EACH ROW
        BEGIN
            UPDATE {Tickers}
            SET {UpdatedAt} = datetime('now')
            WHERE {Id} = OLD.{Id};
        END;
        """

    let insert = 
        $"""
        INSERT INTO {Tickers}
        (
            {Symbol},
            {Image},
            {Name},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.Symbol},
            {SQLParameterName.Image},
            {SQLParameterName.Name},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
        )
        """

    let update =
        $"""
        UPDATE {Tickers}
        SET
            {Symbol} = {SQLParameterName.Symbol},
            {Image} = {SQLParameterName.Image},
            {Name} = {SQLParameterName.Name},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {Tickers}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {Tickers}
        """

    let getByTicker =
        $"""
        SELECT * FROM {Tickers}
        WHERE
            {Symbol} = {SQLParameterName.Symbol}
        LIMIT 1
        """

    let getById =
        $"""
        SELECT * FROM {Tickers}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """