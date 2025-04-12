namespace Binnaculum.Core.SQL

open Binnaculum.Core.Keys

module internal BrokerAccountQuery =

    // SQL to create the table
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {TableName_BrokerAccount}
        (
            Id INTEGER PRIMARY KEY,
            BrokerId INTEGER NOT NULL,
            AccountNumber TEXT NOT NULL
        )
        """

    // SQL to insert a new record
    let insert =
        $"""
        INSERT INTO {TableName_BrokerAccount}
        (
            BrokerId,
            AccountNumber
        )
        VALUES
        (
            @BrokerId,
            @AccountNumber
        )
        """

    // SQL to update an existing record
    let update = 
        $"""
        UPDATE {TableName_BrokerAccount}
        SET
            BrokerId = @BrokerId,
            AccountNumber = @AccountNumber
        WHERE
            Id = @Id
        """

    // SQL to delete a record by Id
    let delete =
        $"""
        DELETE FROM {TableName_BrokerAccount}
        WHERE
            Id = @Id
        """

    // SQL to retrieve all records
    let getAll = 
        $"""
        SELECT * FROM {TableName_BrokerAccount}
        """

    // SQL to retrieve a record by Id
    let getById =
        $"""
        SELECT * FROM {TableName_BrokerAccount}
        WHERE
            Id = @Id
        LIMIT 1
        """