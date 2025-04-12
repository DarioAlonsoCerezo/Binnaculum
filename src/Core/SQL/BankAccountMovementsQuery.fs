namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName

module internal BankAccountMovementsQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {BankAccountMovements}
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
        INSERT INTO {BankAccountMovements}
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
        UPDATE {BankAccountMovements}
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
        DELETE FROM {BankAccountMovements}
        WHERE
            Id = @Id
        """

    let getAll =
        $"""
        SELECT * FROM {BankAccountMovements}
        """

    let getById =
        $"""
        SELECT * FROM {BankAccountMovements}
        WHERE
            Id = @Id
        LIMIT 1
        """