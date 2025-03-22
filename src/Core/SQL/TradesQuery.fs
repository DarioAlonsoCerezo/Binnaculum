namespace Binnaculum.Core.SQL

module internal TradesQuery =
    let createTable =
        @"
            CREATE TABLE IF NOT EXISTS Trades
            (
                Id INTEGER PRIMARY KEY,
                TimeStamp TEXT NOT NULL,
                TickerId INTEGER NOT NULL,
                BrokerAccountId INTEGER NOT NULL,
                CurrencyId INTEGER NOT NULL,
                Quantity TEXT NOT NULL,
                Price TEXT NOT NULL,
                Commission TEXT NOT NULL,
                Fees TEXT NOT NULL,
                TradeCode TEXT NOT NULL,
                TradeType TEXT NOT NULL,
                Notes Text
            )
        "

    let insert =
        @"
            INSERT INTO Trades
            (
                TimeStamp,
                TickerId,
                BrokerAccountId,
                CurrencyId,
                Quantity,
                Price,
                Commission,
                Fees,
                TradeCode,
                TradeType,
                Notes
            )
            VALUES
            (
                @TimeStamp,
                @TickerId,
                @BrokerAccountId,
                @CurrencyId,
                @Quantity,
                @Price,
                @Commission,
                @Fees,
                @TradeCode,
                @TradeType,
                @Notes
            )
        "

    let update =
        @"
            UPDATE Trades
            SET
                TimeStamp = @TimeStamp,
                TickerId = @TickerId,
                BrokerAccountId = @BrokerAccountId,
                CurrencyId = @Currency
                Quantity = @Quantity,
                Price = @Price,
                Commission = @Commission,
                Fees = @Fees,   
                TradeCode = @TradeCode,
                TradeType = @TradeType,
                Notes = @Notes
            WHERE
                Id = @Id
        "

    let delete =
        @"
            DELETE FROM Trades
            WHERE
                Id = @Id
        "

    let getAll =
        @"
            SELECT * FROM Trades
        "

    let getById =
        @"
            SELECT * FROM Trades
            WHERE
                Id = @Id
            LIMIT 1
        "