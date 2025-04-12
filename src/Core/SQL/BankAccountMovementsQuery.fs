namespace Binnaculum.Core.SQL

open Binnaculum.Core.Keys

module internal BankAccountMovementsQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {TableName_BankAccountMovements}
        (
            Id INTEGER PRIMARY KEY,
            TimeStamp TEXT NOT NULL,
            Amount TEXT NOT NULL,
            BankAccountId INTEGER NOT NULL,
            CurrencyId INTEGER NOT NULL,
            MovementType TEXT NOT NULL
        )
        """

    let insert =
        $"""
        INSERT INTO {TableName_BankAccountMovements}
        (
            TimeStamp,
            Amount,
            BankAccountId,
            CurrencyId,
            MovementType
        )
        VALUES
        (
            @TimeStamp,
            @Amount,
            @BankAccountId,
            @CurrencyId,
            @MovementType
        )
        """

    let update =
        $"""
        UPDATE {TableName_BankAccountMovements}
        SET
            TimeStamp = @TimeStamp,
            Amount = @Amount,
            BankAccountId = @BankAccountId,
            CurrencyId = @Currency,
            MovementType = @MovementType
        WHERE
            Id = @Id
        """

    let delete =
        $"""
        DELETE FROM {TableName_BankAccountMovements}
        WHERE
            Id = @Id
        """

    let getAll =
        $"""
        SELECT * FROM {TableName_BankAccountMovements}
        """

    let getById =
        $"""
        SELECT * FROM {TableName_BankAccountMovements}
        WHERE
            Id = @Id
        LIMIT 1
        """