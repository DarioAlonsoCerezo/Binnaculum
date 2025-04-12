namespace Binnaculum.Core.SQL

open Binnaculum.Core.Keys

module internal TickersQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {TableName_Tickers}
        (
            Id INTEGER PRIMARY KEY,
            Symbol TEXT NOT NULL,
            Image TEXT,
            Name TEXT
        )
        """

    let insert = 
        $"""
        INSERT INTO {TableName_Tickers}
        (
            Symbol,
            Image,
            Name
        )
        VALUES
        (
            @Symbol,
            @Image,
            @Name
        )
        """

    let update =
        $"""
        UPDATE {TableName_Tickers}
        SET
            Symbol = @Symbol,
            Image = @Image,
            Name = @Name
        WHERE
            Id = @Id
        """

    let delete =
        $"""
        DELETE FROM {TableName_Tickers}
        WHERE
            Id = @Id
        """

    let getAll =
        $"""
        SELECT * FROM {TableName_Tickers}
        """

    let getByTicker =
        $"""
        SELECT * FROM {TableName_Tickers}
        WHERE
            Symbol = @Symbol
        LIMIT 1
        """

    let getById =
        $"""
        SELECT * FROM {TableName_Tickers}
        WHERE
            Id = @Id
        LIMIT 1
        """