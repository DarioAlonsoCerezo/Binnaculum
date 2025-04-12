namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal BrokerQuery =
    /// Creates the Brokers table if it does not already exist.
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {Brokers}
        (
            {Id} INTEGER PRIMARY KEY,
            {Name} TEXT NOT NULL,
            {Image} TEXT NOT NULL,
            {SupportedBroker} TEXT NOT NULL,
            UNIQUE({Name}) -- Ensures Name is unique and implicitly indexed
        )
        """

    /// Inserts a new broker into the Brokers table.
    let insert =
        $"""
        INSERT INTO {Brokers}
        (
            {Name},
            {Image},
            {SupportedBroker}
        )
        VALUES
        (
            {SQLParameterName.Name},
            {SQLParameterName.Image},
            {SQLParameterName.SupportedBroker}
        )
        """

    /// Updates an existing broker in the Brokers table.
    let update =
        $"""
        UPDATE {Brokers}
        SET
            {Name} = {SQLParameterName.Name},
            {Image} = {SQLParameterName.Image},
            {SupportedBroker} = {SQLParameterName.SupportedBroker}
        WHERE {Id} = {SQLParameterName.Id}
        """

    /// Deletes a broker from the Brokers table by Id.
    let delete =
        $"""
        DELETE FROM {Brokers}
        WHERE {Id} = {SQLParameterName.Id}
        """

    /// Retrieves all brokers from the Brokers table.
    let getAll =
        $"""
        SELECT * FROM {Brokers}
        """

    /// Retrieves a broker by Id from the Brokers table.
    let getById =
        $"""
        SELECT * FROM {Brokers}
        WHERE {Id} = {SQLParameterName.Id}
        LIMIT 1
        """

    /// Checks if a broker with the given Name exists in the Brokers table.
    let getByName =
        $"""
        SELECT 1 FROM {Brokers}
        WHERE {Name} = {SQLParameterName.Name}
        LIMIT 1
        """