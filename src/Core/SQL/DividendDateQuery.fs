namespace Binnaculum.Core.SQL

open Binnaculum.Core.Keys

module internal DividendDateQuery =
    let createTable = 
        $"""
        CREATE TABLE IF NOT EXISTS {TableName_DividendDates}
        (
            Id INTEGER PRIMARY KEY,
            TimeStamp TEXT NOT NULL,
            Amount TEXT NOT NULL,
            TickerId INTEGER NOT NULL,
            CurrencyId INTEGER NOT NULL,
            BrokerAccountId INTEGER NOT NULL,
            DividendCode TEXT NOT NULL
        )
        """

    let insert =
        $"""
        INSERT INTO {TableName_DividendDates}
        (
            TimeStamp,
            Amount,
            TickerId,
            CurrencyId,
            BrokerAccountId,
            DividendCode
        )
        VALUES
        (
            @TimeStamp,
            @Amount,
            @TickerId,
            @CurrencyId,
            @BrokerAccountId,
            @DividendCode
        )
        """

    let update =
        $"""
        UPDATE {TableName_DividendDates}
        SET
            TimeStamp = @TimeStamp,
            Amount = @Amount,
            TickerId = @TickerId,
            CurrencyId = @CurrencyId,
            BrokerAccountId = @BrokerAccountId,
            DividendCode = @DividendCode
        WHERE
            Id = @Id
        """

    let delete =
        $"""
        DELETE FROM {TableName_DividendDates}
        WHERE
            Id = @Id
        """

    let getAll =
        $"""
        SELECT * FROM {TableName_DividendDates}
        """

    let getById =
        $"""
        SELECT * FROM {TableName_DividendDates}
        WHERE 
            Id = @Id
        LIMIT 1
        """