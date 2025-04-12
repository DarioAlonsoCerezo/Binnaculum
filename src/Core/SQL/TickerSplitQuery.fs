namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName

module internal TickerSplitQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {TickerSplits}
        (
            {Id} INTEGER PRIMARY KEY,
            {SplitDate} TEXT NOT NULL,
            {TickerId} INTEGER NOT NULL,
            {SplitFactor} TEXT NOT NULL
        )
        """

    let insert =
        $"""
        INSERT INTO {TickerSplits}
        (
            {SplitDate},
            {TickerId},
            {SplitFactor}
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
        UPDATE {TickerSplits}
        SET
            {SplitDate} = @SplitDate,
            {TickerId} = @TickerId,
            {SplitFactor} = @SplitFactor
        WHERE
            {Id} = @Id
        """

    let delete = 
        $"""
        DELETE FROM {TickerSplits}
        WHERE
            {Id} = @Id
        """

    let getAll =
        $"""
        SELECT * FROM {TickerSplits}
        """

    let getById =
        $"""
        SELECT * FROM {TickerSplits}
        WHERE
            {Id} = @Id
        """
