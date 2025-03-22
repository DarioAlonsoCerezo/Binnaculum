namespace Binnaculum.Core.SQL

module internal BrokerQuery =
    let createTable =
        @"
            CREATE TABLE IF NOT EXISTS Brokers
            (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                SupportedBroker TEXT NOT NULL
            )
        "

    let insert =
        @"
            INSERT INTO Brokers
            (
                Name,
                SupportedBroker
            )
            VALUES
            (
                @Name,
                @SupportedBroker
            )
        "

    let update =
        @"
            UPDATE Brokers
            SET
                Name = @Name,
                SupportedBroker = @SupportedBroker
            WHERE
                Id = @Id
        "

    let delete =
        @"
            DELETE FROM Brokers
            WHERE
                Id = @Id
        "

    let getAll =
        @"
            SELECT * FROM Brokers
        "

    let getById =         @"
            SELECT * FROM Brokers
            WHERE
                Id = @Id
            LIMIT 1
        "