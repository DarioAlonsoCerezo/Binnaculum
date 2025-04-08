namespace Binnaculum.Core.SQL

module internal BrokerMovementQuery =
    let createTable =
        @"
            CREATE TABLE IF NOT EXISTS BrokerMovement
            (
                Id INTEGER PRIMARY KEY,
                TimeStamp TEXT NOT NULL,
                Amount TEXT NOT NULL,
                CurrencyId INTEGER NOT NULL,
                BrokerAccountId INTEGER NOT NULL,
                Commissions TEXT NOT NULL,
                Fees TEXT NOT NULL,
                MovementType TEXT NOT NULL
            )
        "

    let insert =
        @"
            INSERT INTO BrokerMovement
            (
                TimeStamp,
                Amount,
                CurrencyId,
                BrokerAccountId,
                Commissions,
                Fees,
                MovementType
            )
            VALUES
            (
                @TimeStamp,
                @Amount,
                @CurrencyId,
                @BrokerAccountId,
                @Commissions,
                @Fees,
                @MovementType
            )
        "

    let update =
        @"
            UPDATE BrokerMovement
            SET
                TimeStamp = @TimeStamp,
                Amount = @Amount,
                CurrencyId = @Currency
                BrokerAccountId = @BrokerAccountId,
                Commissions = @Commissions,
                Fees = @Fees,
                MovementType = @MovementType
            WHERE
                Id = @Id
        "

    let delete = 
        @"
            DELETE FROM BrokerMovement
            WHERE
                Id = @Id
        "

    let getAll =
        @"
            SELECT * FROM BrokerMovement
        "

    let getById =
        @"
            SELECT * FROM BrokerMovement
            WHERE
                Id = @Id
            LIMIT 1
        "