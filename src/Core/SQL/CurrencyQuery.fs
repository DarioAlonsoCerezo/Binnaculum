namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal CurrencyQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {Currencies}
        (
            {Id} INTEGER PRIMARY KEY,
            {Name} TEXT NOT NULL,
            {Code} TEXT NOT NULL,
            {Symbol} TEXT NOT NULL
        )
        """

    let getCounted = $"""SELECT COUNT(*) FROM {Currencies}"""

    let insert =
        $"""
        INSERT INTO {Currencies}
        (
            {Name}, 
            {Code}, 
            {Symbol}
        )
        VALUES 
        (
            {SQLParameterName.Name}, 
            {SQLParameterName.Code}, 
            {SQLParameterName.Symbol}
        )
        """

    let update =
        $"""
        UPDATE {Currencies}
        SET
            {Name} = {SQLParameterName.Name},
            {Code} = {SQLParameterName.Code},
            {Symbol} = {SQLParameterName.Symbol}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {Currencies}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll = $"""SELECT * FROM {Currencies}"""

    let getById = 
        $"""
        SELECT * FROM {Currencies}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """

    let getByCode =
        $"""
        SELECT * FROM {Currencies}
        WHERE
            {Code} = {SQLParameterName.Code}
        LIMIT 1
        """