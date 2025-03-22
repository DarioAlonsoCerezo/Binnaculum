namespace Binnaculum.Core.SQL

module internal BrokerAccountQuery =
    let createTable =
        @"
            CREATE TABLE IF NOT EXISTS BrokerAccounts
            (
                Id INTEGER PRIMARY KEY,
                BrokerId INTEGER NOT NULL,
                AccountNumber TEXT NOT NULL,
            )
        "

    let insert =
        @"
            INSERT INTO BrokerAccounts
            (
                BrokerId,
                AccountNumber
            )
            VALUES
            (
                @BrokerId,
                @AccountNumber
            )
        "

    let update = 
        @"
            UPDATE BrokerAccounts
            SET
                BrokerId = @BrokerId,
                AccountNumber = @AccountNumber
            WHERE
                Id = @Id
        "

    let delete =
        @"
            DELETE FROM BrokerAccounts
            WHERE
                Id = @Id
        "

    let getAll = 
        @"
            SELECT * FROM BrokerAccounts
        "

    let getById =
        @"
            SELECT * FROM BrokerAccounts
            WHERE
                Id = @Id
            LIMIT 1
        "