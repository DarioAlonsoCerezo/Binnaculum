namespace Binnaculum.Core.SQL

module internal TickersQuery =
    let createTable =
        @"
            CREATE TABLE IF NOT EXISTS Tickers
            (
                Id INTEGER PRIMARY KEY,
                Symbol TEXT NOT NULL,
            )
        "

    let insert = 
        @"
            INSERT INTO Tickers
            (
                Symbol
            )
            VALUES
            (
                @Symbol
            )
        "

    let update =
        @"
            UPDATE Tickers
            SET
                Symbol = @Symbol
            WHERE
                Id = @Id
        "

    let delete =
        @"
            DELETE FROM Tickers
            WHERE
                Id = @Id
        "

    let getAll =
        @"
            SELECT * FROM Tickers
        "

    let getByTicker =
        @"
            SELECT * FROM Tickers
            WHERE
                Symbol = @Symbol
            LIMIT 1
        "

    let getById =
        @"
            SELECT * FROM Tickers
            WHERE
                Id = @Id
            LIMIT 1
        "