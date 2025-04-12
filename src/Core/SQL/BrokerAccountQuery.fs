namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName

module internal BrokerAccountQuery =

    // SQL to create the table
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {BrokerAccounts}
        (
            Id INTEGER PRIMARY KEY,
            BrokerId INTEGER NOT NULL,
            AccountNumber TEXT NOT NULL
        )
        """

    // SQL to insert a new record
    let insert =
        $"""
        INSERT INTO {BrokerAccounts}
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
        UPDATE {BrokerAccounts}
        SET
            BrokerId = @BrokerId,
            AccountNumber = @AccountNumber
        WHERE
            Id = @Id
        """

    // SQL to delete a record by Id
    let delete =
        $"""
        DELETE FROM {BrokerAccounts}
        WHERE
            Id = @Id
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
            Id = @Id
        LIMIT 1
        """