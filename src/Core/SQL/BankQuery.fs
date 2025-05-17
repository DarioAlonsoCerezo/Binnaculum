namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal BankQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {Banks}
        (
            {Id} INTEGER PRIMARY KEY,
            {Name} TEXT NOT NULL,
            {Image} TEXT,
            {CreatedAt} TEXT NOT NULL DEFAULT (DATETIME('now')),
            {UpdatedAt} TEXT
        );

        -- Index to optimize queries filtering by Name
        CREATE INDEX IF NOT EXISTS idx_Banks_Name ON {Banks}({Name});

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_Banks_UpdatedAt
        AFTER UPDATE ON {Banks}
        FOR EACH ROW
        BEGIN
            UPDATE {Banks}
            SET {UpdatedAt} = strftime('%%Y-%%m-%%dT%%H:%%M:%%S', 'now')
            WHERE {Id} = OLD.{Id};
        END;
        """

    let insert =
        $"""
        INSERT INTO {Banks}
        (
            {Name},
            {Image},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.Name},
            {SQLParameterName.Image},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
        )
        """

    let update =
        $"""
        UPDATE {Banks}
        SET
            {Name} = {SQLParameterName.Name},
            {Image} = {SQLParameterName.Image},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {Banks}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {Banks}
        """

    let getById =
        $"""
        SELECT * FROM {Banks}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """
