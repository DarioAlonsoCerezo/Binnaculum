namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal BankQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {Banks}
        (
            {Id} INTEGER PRIMARY KEY,
            {Name} TEXT NOT NULL,
            {Image} TEXT
        )
        """

    let insert =
        $"""
        INSERT INTO {Banks}
        (
            {Name},
            {Image}
        )
        VALUES
        (
            {SQLParameterName.Name},
            {SQLParameterName.Image}
        )
        """

    let update =
        $"""
        UPDATE {Banks}
        SET
            {Name} = {SQLParameterName.Name},
            {Image} = {SQLParameterName.Image}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {Banks}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {Banks}
        """

    let getById =
        $"""
        SELECT * FROM {Banks}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """
