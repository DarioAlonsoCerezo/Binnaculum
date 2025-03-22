namespace Binnaculum.Core.SQL

module internal BankAccountInterestsQuery =
    let createTable =
        @"
            CREATE TABLE IF NOT EXISTS BankAccountInterests
            (
                Id INTEGER PRIMARY KEY,
                TimeStamp TEXT NOT NULL,
                Amount TEXT NOT NULL,
                BankAccountId INTEGER NOT NULL,
                CurrencyId INTEGER NOT
            )
        "

    let insert =
        @"
            INSERT INTO BankAccountInterests
            (
                TimeStamp,
                Amount,
                BankAccountId,
                CurrencyId
            )
            VALUES
            (
                @TimeStamp,
                @Amount,
                @BankAccountId,
                @CurrencyId
            )
        "

    let update =
        @"
            UPDATE BankAccountInterests
            SET
                TimeStamp = @TimeStamp,
                Amount = @Amount,
                BankAccountId = @BankAccountId,
                CurrencyId = @Currency
            WHERE
                Id = @Id
        "

    let delete = 
        @"
            DELETE FROM BankAccountInterests
            WHERE
                Id = @Id
        "

    let getAll =
        @"
            SELECT * FROM BankAccountInterests
        "

    let getById =
        @"
            SELECT * FROM BankAccountInterests
            WHERE
                Id = @Id
            LIMIT 1
        "