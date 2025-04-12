namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal BankAccountMovementsQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {BankAccountMovements}
        (
            {Id} INTEGER PRIMARY KEY,
            {TimeStamp} TEXT NOT NULL,
            {Amount} TEXT NOT NULL,
            {BankAccountId} INTEGER NOT NULL,
            {CurrencyId} INTEGER NOT NULL,
            {MovementType} TEXT NOT NULL
        )
        """

    let insert =
        $"""
        INSERT INTO {BankAccountMovements}
        (
            {TimeStamp},
            {Amount},
            {BankAccountId},
            {CurrencyId},
            {MovementType}
        )
        VALUES
        (
            {SQLParameterName.TimeStamp},
            {SQLParameterName.Amount},
            {SQLParameterName.BankAccountId},
            {SQLParameterName.CurrencyId},
            {SQLParameterName.MovementType}
        )
        """

    let update =
        $"""
        UPDATE {BankAccountMovements}
        SET
            {TimeStamp} = {SQLParameterName.TimeStamp},
            {Amount} = {SQLParameterName.Amount},
            {BankAccountId} = {SQLParameterName.BankAccountId},
            {CurrencyId} = {SQLParameterName.CurrencyId},
            {MovementType} = {SQLParameterName.MovementType}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {BankAccountMovements}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {BankAccountMovements}
        """

    let getById =
        $"""
        SELECT * FROM {BankAccountMovements}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """