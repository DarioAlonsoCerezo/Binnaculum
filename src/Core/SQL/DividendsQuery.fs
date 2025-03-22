namespace Binnaculum.Core.SQL

module internal DividendsQuery =
    let createTable =
        @"
            CREATE TABLE IF NOT EXISTS Dividends
            (
                Id INTEGER PRIMARY KEY,
                TimeStamp TEXT NOT NULL,
                Amount TEXT NOT NULL,
                TickerId INTEGER NOT NULL,
                CurrencyId INTEGER NOT,
                BrokerAccountId INTEGER NOT NULL
            )
        "

    let insert =
        @"
            INSERT INTO Dividends
            (
                TimeStamp,
                Amount,
                TickerId,
                CurrencyId,
                BrokerAccountId
            )
            VALUES
            (
                @TimeStamp,
                @Amount,
                @TickerId,
                @CurrencyId,
                @BrokerAccountId
            )
        "

    let update =
        @"
            UPDATE Dividends
            SET
                TimeStamp = @TimeStamp,
                Amount = @Amount,
                TickerId = @TickerId,
                CurrencyId = @CurrencyId,
                BrokerAccountId = @BrokerAccountId
            WHERE
                Id = @Id
        "

    let delete =
        @"
            DELETE FROM Dividends
            WHERE
                Id = @Id
        "

    let getAll =
        @"
            SELECT * FROM Dividends
        "

    let getById =
        @"
            SELECT * FROM Dividends
            WHERE
                Id = @Id
            LIMIT 1
        "