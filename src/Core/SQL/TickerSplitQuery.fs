namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

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
            {SQLParameterName.SplitDate},
            {SQLParameterName.TickerId},
            {SQLParameterName.SplitFactor}
        )
        """

    let update = 
        $"""
        UPDATE {TickerSplits}
        SET
            {SplitDate} = {SQLParameterName.SplitDate},
            {TickerId} = {SQLParameterName.TickerId},
            {SplitFactor} = {SQLParameterName.SplitFactor}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete = 
        $"""
        DELETE FROM {TickerSplits}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {TickerSplits}
        """

    let getById =
        $"""
        SELECT * FROM {TickerSplits}
        WHERE
            {Id} = {SQLParameterName.Id}
        """
