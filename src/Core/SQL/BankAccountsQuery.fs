namespace Binnaculum.Core.SQL

open Binnaculum.Core.Keys

module internal BankAccountsQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {TableName_BankAccounts}
        (
            Id INTEGER PRIMARY KEY,
            BankId INTEGER NOT NULL,
            Name TEXT NOT NULL,
            Description TEXT,
            CurrencyId INTEGER NOT NULL
        )
        """

    let insert =
        $"""
        INSERT INTO {TableName_BankAccounts}
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
        """

    let update =
        $"""
        UPDATE {TableName_BankAccounts}
        SET
            Name = @Name,
            BankId = @BankId,
            Description = @Description,
            CurrencyId = @CurrencyId
        WHERE
            Id = @Id
        """

    let delete =
        $"""
        DELETE FROM {TableName_BankAccounts}
        WHERE
            Id = @Id
        """

    let getAll = 
        $"""
        SELECT * FROM {TableName_BankAccounts}
        """

    let getById =
        $"""
        SELECT * FROM {TableName_BankAccounts}
        WHERE
            Id = @Id
        LIMIT 1
        """