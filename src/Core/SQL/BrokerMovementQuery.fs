﻿namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal BrokerMovementQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {BrokerMovements}
        (
            {Id} INTEGER PRIMARY KEY,
            {TimeStamp} TEXT NOT NULL CHECK ({TimeStamp} GLOB '____-__-__T__:__:__'),
            {Amount} TEXT NOT NULL DEFAULT '0',
            {CurrencyId} INTEGER NOT NULL,
            {BrokerAccountId} INTEGER NOT NULL,
            {Commissions} TEXT NOT NULL DEFAULT '0',
            {Fees} TEXT NOT NULL DEFAULT '0',
            {MovementType} TEXT NOT NULL CHECK ({MovementType} IN ('{SQLConstants.Deposit}', '{SQLConstants.Withdrawal}', '{SQLConstants.Fee}', '{SQLConstants.InterestsGained}', '{SQLConstants.Lending}', '{SQLConstants.AcatMoneyTransfer}', '{SQLConstants.AcatSecuritiesTransfer}', '{SQLConstants.InterestsPaid}', '{SQLConstants.Conversion}')),
            {CreatedAt} TEXT NOT NULL DEFAULT (datetime('now')),
            {UpdatedAt} TEXT,
            FOREIGN KEY ({CurrencyId}) REFERENCES {Currencies}({Id}),
            FOREIGN KEY ({BrokerAccountId}) REFERENCES {BrokerAccounts}({Id})
        );

        CREATE INDEX IF NOT EXISTS idx_BrokerMovements_TimeStamp ON {BrokerMovements}({TimeStamp});
        CREATE INDEX IF NOT EXISTS idx_BrokerMovements_CurrencyId ON {BrokerMovements}({CurrencyId});
        CREATE INDEX IF NOT EXISTS idx_BrokerMovements_BrokerAccountId ON {BrokerMovements}({BrokerAccountId});

        CREATE TRIGGER IF NOT EXISTS trg_BrokerMovements_UpdatedAt
        AFTER UPDATE ON {BrokerMovements}
        FOR EACH ROW
        BEGIN
            UPDATE {BrokerMovements}
            SET {UpdatedAt} = datetime('now')
            WHERE {Id} = OLD.{Id};
        END;
        """

    let insert =
        $"""
        INSERT INTO {BrokerMovements}
        (
            {TimeStamp},
            {Amount},
            {CurrencyId},
            {BrokerAccountId},
            {Commissions},
            {Fees},
            {MovementType}
        )
        VALUES
        (
            {SQLParameterName.TimeStamp},
            {SQLParameterName.Amount},
            {SQLParameterName.CurrencyId},
            {SQLParameterName.BrokerAccountId},
            {SQLParameterName.Commissions},
            {SQLParameterName.Fees},
            {SQLParameterName.MovementType}
        )
        """

    let update =
        $"""
        UPDATE {BrokerMovements}
        SET
            {TimeStamp} = {SQLParameterName.TimeStamp},
            {Amount} = {SQLParameterName.Amount},
            {CurrencyId} = {SQLParameterName.CurrencyId},
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId},
            {Commissions} = {SQLParameterName.Commissions},
            {Fees} = {SQLParameterName.Fees},
            {MovementType} = {SQLParameterName.MovementType}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete = 
        $"""
        DELETE FROM {BrokerMovements}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {BrokerMovements}
        """

    let getById =
        $"""
        SELECT * FROM {BrokerMovements}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """