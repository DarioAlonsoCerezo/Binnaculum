namespace Binnaculum.Core.SQL

open Binnaculum.Core.Keys

module internal TickerSplitQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {TableName_TickerSplits}
        (
            Id INTEGER PRIMARY KEY,
            SplitDate TEXT NOT NULL,
            TickerId INTEGER NOT NULL,
            SplitFactor TEXT NOT NULL
        )
        """

    let insert =
        $"""
        INSERT INTO {TableName_TickerSplits}
        (
            SplitDate,
            TickerId,
            SplitFactor
        )
        VALUES
        (
            @SplitDate,
            @TickerId,
            @SplitFactor
        )
        """

    let update = 
        $"""
        UPDATE {TableName_TickerSplits}
        SET
            SplitDate = @SplitDate,
            TickerId = @TickerId,
            SplitFactor = @SplitFactor
        WHERE
            Id = @Id
        """

    let delete = 
        $"""
        DELETE FROM {TableName_TickerSplits}
        WHERE
            Id = @Id
        """

    let getAll =
        $"""
        SELECT * FROM {TableName_TickerSplits}
        """

    let getById =
        $"""
        SELECT * FROM {TableName_TickerSplits}
        WHERE
            Id = @Id
        """
