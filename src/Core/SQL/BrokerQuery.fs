namespace Binnaculum.Core.SQL

open Binnaculum.Core.Keys

module internal BrokerQuery =
    /// Creates the Brokers table if it does not already exist.
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {TableName_Brokers}
        (
            Id INTEGER PRIMARY KEY,
            Name TEXT NOT NULL,
            Image TEXT NOT NULL,
            SupportedBroker TEXT NOT NULL,
            UNIQUE(Name) -- Ensures Name is unique and implicitly indexed
        )
        """

    /// Inserts a new broker into the Brokers table.
    let insert =
        $"""
        INSERT INTO {TableName_Brokers}
        (
            Name,
            Image,
            SupportedBroker
        )
        VALUES
        (
            @Name,
            @Image,
            @SupportedBroker
        )
        """

    /// Updates an existing broker in the Brokers table.
    let update =
        $"""
        UPDATE {TableName_Brokers}
        SET
            Name = @Name,
            Image = @Image,
            SupportedBroker = @SupportedBroker
        WHERE Id = @Id
        """

    /// Deletes a broker from the Brokers table by Id.
    let delete =
        $"""
        DELETE FROM {TableName_Brokers}
        WHERE Id = @Id
        """

    /// Retrieves all brokers from the Brokers table.
    let getAll =
        $"""
        SELECT * FROM {TableName_Brokers}
        """

    /// Retrieves a broker by Id from the Brokers table.
    let getById =
        $"""
        SELECT * FROM {TableName_Brokers}
        WHERE Id = @Id
        LIMIT 1
        """

    /// Checks if a broker with the given Name exists in the Brokers table.
    let getByName =
        $"""
        SELECT 1 FROM {TableName_Brokers}
        WHERE Name = @Name
        LIMIT 1
        """