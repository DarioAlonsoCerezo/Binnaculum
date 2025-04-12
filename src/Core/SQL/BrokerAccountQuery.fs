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
            {AccountNumber} TEXT NOT NULL
        )
        """

    // SQL to insert a new record
    let insert =
        $"""
        INSERT INTO {BrokerAccounts}
        (
            {BrokerId},
            {AccountNumber}
        )
        VALUES
        (
            {SQLParameterName.BrokerId},
            {SQLParameterName.AccountNumber}
        )
        """

    // SQL to update an existing record
    let update = 
        $"""
        UPDATE {BrokerAccounts}
        SET
            {BrokerId} = {SQLParameterName.BrokerId},
            {AccountNumber} = {SQLParameterName.AccountNumber}
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