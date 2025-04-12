namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal TickersQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {Tickers}
        (
            {Id} INTEGER PRIMARY KEY,
            {Symbol} TEXT NOT NULL,
            {Image} TEXT,
            {Name} TEXT
        )
        """

    let insert = 
        $"""
        INSERT INTO {Tickers}
        (
            {Symbol},
            {Image},
            {Name}
        )
        VALUES
        (
            {SQLParameterName.Symbol},
            {SQLParameterName.Image},
            {SQLParameterName.Name}
        )
        """

    let update =
        $"""
        UPDATE {Tickers}
        SET
            {Symbol} = {SQLParameterName.Symbol},
            {Image} = {SQLParameterName.Image},
            {Name} = {SQLParameterName.Name}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {Tickers}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {Tickers}
        """

    let getByTicker =
        $"""
        SELECT * FROM {Tickers}
        WHERE
            {Symbol} = {SQLParameterName.Symbol}
        LIMIT 1
        """

    let getById =
        $"""
        SELECT * FROM {Tickers}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """