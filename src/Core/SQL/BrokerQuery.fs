namespace Binnaculum.Core.SQL

module internal BrokerQuery =
    let createTable =
        @"
            CREATE TABLE IF NOT EXISTS Brokers
            (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                Image TEXT NOT NULL,
                SupportedBroker TEXT NOT NULL
            )
        "

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

    let update =
        @"
            UPDATE Brokers
            SET
                Name = @Name,
                Image = @Image,
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

    let getById =         
        @"
            SELECT * FROM Brokers
            WHERE
                Id = @Id
            LIMIT 1
        "

    let getByName =
        @"
            SELECT 1 FROM Broker
            WHERE
                Name = @Name
            LIMIT 1
        "