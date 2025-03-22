namespace Binnaculum.Core.SQL

module internal BankAccountFeesQuery =
    let createTable =
        @"
            CREATE TABLE IF NOT EXISTS BankAccountFees
            (
                Id INTEGER PRIMARY KEY,
                TimeStamp TEXT NOT NULL,
                Amount TEXT NOT NULL,
                BankAccountId INTEGER NOT NULL,
                CurrencyId INTEGER NOT NULL
            )
        "

    let insert =
        @"
            INSERT INTO BankAccountFees
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
            UPDATE BankAccountFees
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
            DELETE FROM BankAccountFees
            WHERE
                Id = @Id
        "

    let getAll =
        @"
            SELECT * FROM BankAccountFees
        "

    let getById =
        @"
            SELECT * FROM BankAccountFees
            WHERE
                Id = @Id
            LIMIT 1
        "