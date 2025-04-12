namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName

module internal TickersQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {Tickers}
        (
            Id INTEGER PRIMARY KEY,
            Symbol TEXT NOT NULL,
            Image TEXT,
            Name TEXT
        )
        """

    let insert = 
        $"""
        INSERT INTO {Tickers}
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
        UPDATE {Tickers}
        SET
            Symbol = @Symbol,
            Image = @Image,
            Name = @Name
        WHERE
            Id = @Id
        """

    let delete =
        $"""
        DELETE FROM {Tickers}
        WHERE
            Id = @Id
        """

    let getAll =
        $"""
        SELECT * FROM {Tickers}
        """

    let getByTicker =
        $"""
        SELECT * FROM {Tickers}
        WHERE
            Symbol = @Symbol
        LIMIT 1
        """

    let getById =
        $"""
        SELECT * FROM {Tickers}
        WHERE
            Id = @Id
        LIMIT 1
        """