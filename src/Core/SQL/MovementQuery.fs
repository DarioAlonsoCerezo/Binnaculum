namespace Binnaculum.Core.SQL

module internal MovementQuery =
    let createTable =
        @"
            CREATE TABLE IF NOT EXISTS Movements
            (
                Id INTEGER PRIMARY KEY,
                TimeStamp TEXT NOT NULL,
                Amount TEXT NOT NULL,
                CurrencyId INTEGER NOT NULL,
                BankAccountId INTEGER NOT NULL,
                Commisions TEXT NOT NULL,
                Fees TEXT NOT NULL,
                MovementType TEXT NOT NULL
            )
        "

    let insert =
        @"
            INSERT INTO Movements
            (
                TimeStamp,
                Amount,
                CurrencyId,
                BankAccountId,
                Commisions,
                Fees,
                MovementType
            )
            VALUES
            (
                @TimeStamp,
                @Amount,
                @CurrencyId,
                @BankAccountId,
                @Commisions,
                @Fees,
                @MovementType
            )
        "

    let update =
        @"
            UPDATE Movements
            SET
                TimeStamp = @TimeStamp,
                Amount = @Amount,
                CurrencyId = @Currency
                BankAccountId = @BankAccountId,
                Commisions = @Commisions,
                Fees = @Fees,
                MovementType = @MovementType
            WHERE
                Id = @Id
        "

    let delete = 
        @"
            DELETE FROM Movements
            WHERE
                Id = @Id
        "

    let getAll =
        @"
            SELECT * FROM Movements
        "

    let getById =
        @"
            SELECT * FROM Movements
            WHERE
                Id = @Id
            LIMIT 1
        "