namespace Binnaculum.Core.SQL

open Binnaculum.Core.Keys

module internal DividendsQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {TableName_Dividends}
        (
            Id INTEGER PRIMARY KEY,
            TimeStamp TEXT NOT NULL,
            DividendAmount TEXT NOT NULL,
            TickerId INTEGER NOT NULL,
            CurrencyId INTEGER NOT NULL,
            BrokerAccountId INTEGER NOT NULL
        )
        """

    let insert =
        $"""
        INSERT INTO {TableName_Dividends}
        (
            TimeStamp,
            DividendAmount,
            TickerId,
            CurrencyId,
            BrokerAccountId
        )
        VALUES
        (
            @TimeStamp,
            @DividendAmount,
            @TickerId,
            @CurrencyId,
            @BrokerAccountId
        )
        """

    let update =
        $"""
        UPDATE {TableName_Dividends}
        SET
            TimeStamp = @TimeStamp,
            DividendAmount = @DividendAmount,
            TickerId = @TickerId,
            CurrencyId = @CurrencyId,
            BrokerAccountId = @BrokerAccountId
        WHERE
            Id = @Id
        """

    let delete =
        $"""
        DELETE FROM {TableName_Dividends}
        WHERE
            Id = @Id
        """

    let getAll =
        $"""
        SELECT * FROM {TableName_Dividends}
        """

    let getById =
        $"""
        SELECT * FROM {TableName_Dividends}
        WHERE
            Id = @Id
        LIMIT 1
        """