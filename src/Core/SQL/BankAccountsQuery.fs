﻿namespace Binnaculum.Core.SQL

module internal BankAccountsQuery =
    let createTable =
        @"
            CREATE TABLE IF NOT EXISTS BankAccounts
            (
                Id INTEGER PRIMARY KEY,
                BankId INTEGER NOT NULL,
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
                BankId,
                Description,
                CurrencyId
            )
            VALUES
            (
                @Name,
                @BankId,
                @Description,
                @CurrencyId
            )
        "

    let update =
        @"
            UPDATE BankAccounts
            SET
                Name = @Name,
                BankId = @BankId,
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