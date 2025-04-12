namespace Binnaculum.Core.SQL

open Binnaculum.Core.Keys

module internal CurrencyQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {TableName_Currencies}
        (
            Id INTEGER PRIMARY KEY,
            Name TEXT NOT NULL,
            Code TEXT NOT NULL,
            Symbol TEXT NOT NULL
        )
        """

    let getCounted = $"""SELECT COUNT(*) FROM {TableName_Currencies}"""

    let insert =
        $"""
        INSERT INTO {TableName_Currencies}
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
        UPDATE {TableName_Currencies}
        SET
            Name = @Name,
            Code = @Code,
            Symbol = @Symbol
        WHERE
            Id = @Id
        """

    let delete =
        $"""
        DELETE FROM {TableName_Currencies}
        WHERE
            Id = @Id
        """

    let getAll = $"""SELECT * FROM {TableName_Currencies}"""

    let getById = 
        $"""
        SELECT * FROM {TableName_Currencies}
        WHERE
            Id = @Id
        LIMIT 1
        """