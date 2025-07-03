namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal BrokerMovementQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {BrokerMovements}
        (
            {Id} INTEGER PRIMARY KEY,
            {TimeStamp} TEXT NOT NULL,
            {Amount} TEXT NOT NULL DEFAULT '0',
            {CurrencyId} INTEGER NOT NULL,
            {BrokerAccountId} INTEGER NOT NULL,
            {Commissions} TEXT NOT NULL DEFAULT '0',
            {Fees} TEXT NOT NULL DEFAULT '0',
            {MovementType} TEXT NOT NULL CHECK ({MovementType} IN ('{SQLConstants.Deposit}', '{SQLConstants.Withdrawal}', '{SQLConstants.Fee}', '{SQLConstants.InterestsGained}', '{SQLConstants.Lending}', '{SQLConstants.AcatMoneyTransferSent}', '{SQLConstants.AcatMoneyTransferReceived}', '{SQLConstants.AcatSecuritiesTransferSent}', '{SQLConstants.AcatSecuritiesTransferReceived}', '{SQLConstants.InterestsPaid}', '{SQLConstants.Conversion}')),
            {Notes} TEXT,
            {FromCurrencyId} INTEGER,
            {CreatedAt} TEXT NOT NULL DEFAULT (datetime('now')),
            {UpdatedAt} TEXT,
            FOREIGN KEY ({CurrencyId}) REFERENCES {Currencies}({Id}),
            FOREIGN KEY ({BrokerAccountId}) REFERENCES {BrokerAccounts}({Id}),
            FOREIGN KEY ({FromCurrencyId}) REFERENCES {Currencies}({Id})
        );

        -- Index to optimize queries filtering by TimeStamp
        CREATE INDEX IF NOT EXISTS idx_BrokerMovements_TimeStamp ON {BrokerMovements}({TimeStamp});
        
        -- Index to optimize queries filtering by CurrencyId
        CREATE INDEX IF NOT EXISTS idx_BrokerMovements_CurrencyId ON {BrokerMovements}({CurrencyId});
        
        -- Index to optimize queries filtering by BrokerAccountId
        CREATE INDEX IF NOT EXISTS idx_BrokerMovements_BrokerAccountId ON {BrokerMovements}({BrokerAccountId});

        -- Trigger to automatically update the UpdatedAt column whenever a row is updated
        CREATE TRIGGER IF NOT EXISTS trg_BrokerMovements_UpdatedAt
        AFTER UPDATE ON {BrokerMovements}
        FOR EACH ROW
        BEGIN
            UPDATE {BrokerMovements}
            SET {UpdatedAt} = strftime('%%Y-%%m-%%dT%%H:%%M:%%S', 'now')
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
            {MovementType},
            {Notes},
            {FromCurrencyId},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.TimeStamp},
            {SQLParameterName.Amount},
            {SQLParameterName.CurrencyId},
            {SQLParameterName.BrokerAccountId},
            {SQLParameterName.Commissions},
            {SQLParameterName.Fees},
            {SQLParameterName.MovementType},
            {SQLParameterName.Notes},
            {SQLParameterName.FromCurrencyId},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
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
            {MovementType} = {SQLParameterName.MovementType},
            {Notes} = {SQLParameterName.Notes},
            {FromCurrencyId} = {SQLParameterName.FromCurrencyId},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
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