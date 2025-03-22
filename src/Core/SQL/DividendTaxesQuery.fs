namespace Binnaculum.Core.SQL

module internal DividendTaxesQuery =
    let createTable =
        @"
            CREATE TABLE IF NOT EXISTS DividendTaxes
            (
                Id INTEGER PRIMARY KEY,
                TimeStamp TEXT NOT NULL,
                Amount TEXT NOT NULL,
                TickerId INTEGER NOT NULL,
                CurrencyId INTEGER NOT NULL,
                BrokerAccountId INTEGER NOT NULL
            )
        "

    let insert =
        @"
            INSERT INTO DividendTaxes
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
            UPDATE DividendTaxes
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
            DELETE FROM DividendTaxes
            WHERE
                Id = @Id
        "

    let getAll =
        @"
            SELECT * FROM DividendTaxes
        "

    let getById =
        @"
            SELECT * FROM DividendTaxes
            WHERE
                Id = @Id
            LIMIT 1
        "