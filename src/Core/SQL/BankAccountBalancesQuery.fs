namespace Binnaculum.Core.SQL

module internal BankAccountBalancesQuery =
    let createTable =
        @"
            CREATE TABLE IF NOT EXISTS BankAccountBalances
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
            INSERT INTO BankAccountBalances
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
            UPDATE BankAccountBalances
            SET
                TimeStamp = @TimeStamp,
                Amount = @Amount,
                BankAccountId = @BankAccountId,
                CurrencyId = @Currency
            WHERE
                Id = @Id
        "

    let getAll =
        @"
            SELECT * FROM BankAccountBalances
        "

    let getById =
        @"
            SELECT * FROM BankAccountBalances
            WHERE
                Id = @Id
            LIMIT 1
        "