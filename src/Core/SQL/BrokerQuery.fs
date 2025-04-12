namespace Binnaculum.Core.SQL

module internal BrokerQuery =
    /// Creates the Brokers table if it does not already exist.
    let createTable =
        @"
            CREATE TABLE IF NOT EXISTS Brokers
            (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                Image TEXT NOT NULL,
                SupportedBroker TEXT NOT NULL,
                UNIQUE(Name) -- Ensures Name is unique and implicitly indexed
            )
        "

    /// Inserts a new broker into the Brokers table.
    let insert =
        @"
            INSERT INTO Brokers
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
        "

    /// Updates an existing broker in the Brokers table.
    let update =
        @"
            UPDATE Brokers
            SET
                Name = @Name,
                Image = @Image,
                SupportedBroker = @SupportedBroker
            WHERE Id = @Id
        "

    /// Deletes a broker from the Brokers table by Id.
    let delete =
        @"
            DELETE FROM Brokers
            WHERE Id = @Id
        "

    /// Retrieves all brokers from the Brokers table.
    let getAll =
        @"
            SELECT * FROM Brokers
        "

    /// Retrieves a broker by Id from the Brokers table.
    let getById =
        @"
            SELECT * FROM Brokers
            WHERE Id = @Id
            LIMIT 1
        "

    /// Checks if a broker with the given Name exists in the Brokers table.
    let getByName =
        @"
            SELECT 1 FROM Brokers
            WHERE Name = @Name
            LIMIT 1
        "