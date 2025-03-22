namespace Binnaculum.Core.SQL

module internal BankAccountsQuery =
    let createTable =
        @"
            CREATE TABLE IF NOT EXISTS BankAccounts
            (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                Description TEXT NOT NULL,
                CurrencyId INTEGER NOT NULL
            )
        "

    let insert =
        @"
            INSERT INTO BankAccounts
            (
                Name,
                Description,
                CurrencyId
            )
            VALUES
            (
                @Name,
                @Description,
                @CurrencyId
            )
        "

    let update =
        @"
            UPDATE BankAccounts
            SET
                Name = @Name,
                Description = @Description,
                CurrencyId = @CurrencyId
            WHERE
                Id = @Id
        "

    let getAll = 
        @"
            SELECT * FROM BankAccounts
        "

    let getById =
        @"
            SELECT * FROM BankAccounts
            WHERE
                Id = @Id
            LIMIT 1
        "