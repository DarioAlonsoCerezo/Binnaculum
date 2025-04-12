namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal BrokerAccountQuery =

    // SQL to create the table
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {BrokerAccounts}
        (
            {Id} INTEGER PRIMARY KEY,
            {BrokerId} INTEGER NOT NULL,
            {AccountNumber} TEXT NOT NULL,
            {CreatedAt} TEXT NOT NULL DEFAULT (datetime('now')),
            {UpdatedAt} TEXT,
            FOREIGN KEY ({BrokerId}) REFERENCES {Brokers}({Id}) ON DELETE CASCADE ON UPDATE CASCADE
        );

        -- Index on BrokerId for faster lookups
        CREATE INDEX IF NOT EXISTS idx_BrokerAccounts_BrokerId ON {BrokerAccounts}({BrokerId});

        -- Index on AccountNumber for faster lookups
        CREATE INDEX IF NOT EXISTS idx_BrokerAccounts_AccountNumber ON {BrokerAccounts}({AccountNumber});

        -- Trigger to update UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_BrokerAccounts_UpdatedAt
        AFTER UPDATE ON {BrokerAccounts}
        FOR EACH ROW
        BEGIN
            UPDATE {BrokerAccounts}
            SET {UpdatedAt} = datetime('now')
            WHERE {Id} = OLD.{Id};
        END;
        """

    // SQL to insert a new record
    let insert =
        $"""
        INSERT INTO {BrokerAccounts}
        (
            {BrokerId},
            {AccountNumber},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.BrokerId},
            {SQLParameterName.AccountNumber},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
        )
        """

    // SQL to update an existing record
    let update = 
        $"""
        UPDATE {BrokerAccounts}
        SET
            {BrokerId} = {SQLParameterName.BrokerId},
            {AccountNumber} = {SQLParameterName.AccountNumber},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    // SQL to delete a record by Id
    let delete =
        $"""
        DELETE FROM {BrokerAccounts}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    // SQL to retrieve all records
    let getAll = 
        $"""
        SELECT * FROM {BrokerAccounts}
        """

    // SQL to retrieve a record by Id
    let getById =
        $"""
        SELECT * FROM {BrokerAccounts}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """