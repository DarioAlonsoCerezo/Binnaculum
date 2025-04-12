namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal BankAccountsQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {BankAccounts}
        (
            {Id} INTEGER PRIMARY KEY,
            {BankId} INTEGER NOT NULL,
            {Name} TEXT NOT NULL,
            {Description} TEXT,
            {CurrencyId} INTEGER NOT NULL
        )
        """

    let insert =
        $"""
        INSERT INTO {BankAccounts}
        (
            {Name},
            {BankId},
            {Description},
            {CurrencyId}
        )
        VALUES
        (
            {SQLParameterName.Name},
            {SQLParameterName.BankId},
            {SQLParameterName.Description},
            {SQLParameterName.CurrencyId}
        )
        """

    let update =
        $"""
        UPDATE {BankAccounts}
        SET
            {Name} = {SQLParameterName.Name},
            {BankId} = {SQLParameterName.BankId},
            {Description} = {SQLParameterName.Description},
            {CurrencyId} = {SQLParameterName.CurrencyId}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {BankAccounts}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll = 
        $"""
        SELECT * FROM {BankAccounts}
        """

    let getById =
        $"""
        SELECT * FROM {BankAccounts}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """