namespace Binnaculum.Core.SQL

open Binnaculum.Core.Keys

module internal DividendTaxesQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {TableName_DividendTaxes}
        (
            Id INTEGER PRIMARY KEY,
            TimeStamp TEXT NOT NULL,
            Amount TEXT NOT NULL,
            TickerId INTEGER NOT NULL,
            CurrencyId INTEGER NOT NULL,
            BrokerAccountId INTEGER NOT NULL
        )
        """

    let insert =
        $"""
        INSERT INTO {TableName_DividendTaxes}
        (
            TimeStamp,
            Amount,
            TickerId,
            CurrencyId,
            BrokerAccountId
        )
        VALUES
        (
            @TimeStamp,
            @Amount,
            @TickerId,
            @CurrencyId,
            @BrokerAccountId
        )
        """

    let update =
        $"""
        UPDATE {TableName_DividendTaxes}
        SET
            TimeStamp = @TimeStamp,
            Amount = @Amount,
            TickerId = @TickerId,
            CurrencyId = @CurrencyId,
            BrokerAccountId = @BrokerAccountId
        WHERE
            Id = @Id
        """

    let delete =
        $"""
        DELETE FROM {TableName_DividendTaxes}
        WHERE
            Id = @Id
        """

    let getAll =
        $"""
        SELECT * FROM {TableName_DividendTaxes}
        """

    let getById =
        $"""
        SELECT * FROM {TableName_DividendTaxes}
        WHERE
            Id = @Id
        LIMIT 1
        """