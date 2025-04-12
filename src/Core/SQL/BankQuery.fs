namespace Binnaculum.Core.SQL

open Binnaculum.Core.Keys

module internal BankQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {TableName_Banks}
        (
            Id INTEGER PRIMARY KEY,
            Name TEXT NOT NULL,
            Image TEXT
        )
        """

    let insert =
        $"""
        INSERT INTO {TableName_Banks}
        (
            Name,
            Image
        )
        VALUES
        (
            @Name,
            @Image
        )
        """

    let update =
        $"""
        UPDATE {TableName_Banks}
        SET
            Name = @Name,
            Image = @Image
        WHERE
            Id = @Id
        """

    let delete =
        $"""
        DELETE FROM {TableName_Banks}
        WHERE
            Id = @Id
        """

    let getAll =
        $"""
        SELECT * FROM {TableName_Banks}
        """

    let getById =
        $"""
        SELECT * FROM {TableName_Banks}
        WHERE
            Id = @Id
        LIMIT 1
        """
