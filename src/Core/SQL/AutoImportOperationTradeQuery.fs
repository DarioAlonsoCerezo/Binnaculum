namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core

module internal AutoImportOperationTradeQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {AutoImportOperationTrades}
        (
            {Id} INTEGER PRIMARY KEY,
            {AutoOperationId} INTEGER NOT NULL,
            {TradeType} TEXT NOT NULL,
            {ReferenceId} INTEGER NOT NULL,
            {CreatedAt} TEXT NOT NULL DEFAULT (datetime('now')),
            {UpdatedAt} TEXT,
            -- Foreign key to ensure AutoOperationId references a valid AutoImportOperation
            FOREIGN KEY ({AutoOperationId}) REFERENCES {AutoImportOperations}({Id}) ON DELETE CASCADE ON UPDATE CASCADE
        );

        -- Index to optimize queries filtering by AutoOperationId
        CREATE INDEX IF NOT EXISTS idx_AutoImportOperationTrades_AutoOperationId 
            ON {AutoImportOperationTrades}({AutoOperationId});

        -- Index to optimize queries filtering by TradeType
        CREATE INDEX IF NOT EXISTS idx_AutoImportOperationTrades_TradeType 
            ON {AutoImportOperationTrades}({TradeType});

        -- Composite index for finding trades by type and reference
        CREATE INDEX IF NOT EXISTS idx_AutoImportOperationTrades_TypeRef 
            ON {AutoImportOperationTrades}({TradeType}, {ReferenceId});

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_AutoImportOperationTrades_UpdatedAt
        AFTER UPDATE ON {AutoImportOperationTrades}
        FOR EACH ROW
        BEGIN
            UPDATE {AutoImportOperationTrades}
            SET {UpdatedAt} = strftime('%%Y-%%m-%%dT%%H:%%M:%%S', 'now')
            WHERE {Id} = OLD.{Id};
        END;
        """

    let insert =
        $"""
        INSERT INTO {AutoImportOperationTrades}
        (
            {AutoOperationId},
            {TradeType},
            {ReferenceId},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.AutoOperationId},
            {SQLParameterName.TradeType},
            {SQLParameterName.ReferenceId},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
        )
        """

    let update =
        $"""
        UPDATE {AutoImportOperationTrades}
        SET
            {AutoOperationId} = {SQLParameterName.AutoOperationId},
            {TradeType} = {SQLParameterName.TradeType},
            {ReferenceId} = {SQLParameterName.ReferenceId},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {AutoImportOperationTrades}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {AutoImportOperationTrades}
        """

    let getById =
        $"""
        SELECT * FROM {AutoImportOperationTrades}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """

    let selectByOperation =
        $"""
        SELECT * FROM {AutoImportOperationTrades}
        WHERE
            {AutoOperationId} = {SQLParameterName.AutoOperationId}
        ORDER BY {CreatedAt} ASC
        """
