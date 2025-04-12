namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName

module internal CurrencyQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {Currencies}
        (
            Id INTEGER PRIMARY KEY,
            Name TEXT NOT NULL,
            Code TEXT NOT NULL,
            Symbol TEXT NOT NULL
        )
        """

    let getCounted = $"""SELECT COUNT(*) FROM {Currencies}"""

    let insert =
        $"""
        INSERT INTO {Currencies}
        (
            Name, 
            Code, 
            Symbol
        )
        VALUES 
        (
            @Name, 
            @Code, 
            @Symbol
        )
        """

    let update =
        $"""
        UPDATE {Currencies}
        SET
            Name = @Name,
            Code = @Code,
            Symbol = @Symbol
        WHERE
            Id = @Id
        """

    let delete =
        $"""
        DELETE FROM {Currencies}
        WHERE
            Id = @Id
        """

    let getAll = $"""SELECT * FROM {Currencies}"""

    let getById = 
        $"""
        SELECT * FROM {Currencies}
        WHERE
            Id = @Id
        LIMIT 1
        """