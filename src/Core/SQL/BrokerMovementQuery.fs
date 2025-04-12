namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName

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
            {MovementType} TEXT NOT NULL CHECK ({MovementType} IN ('DEPOSIT', 'WITHDRAWAL', 'FEE', 'INTERESTS_GAINED', 'LENDING', 'ACAT_MONEY_TRANSFER', 'ACAT_SECURITIES_TRANSFER', 'INTERESTS_PAID', 'CONVERSION')),
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
            @TimeStamp,
            @Amount,
            @CurrencyId,
            @BrokerAccountId,
            @Commissions,
            @Fees,
            @MovementType
        )
        """

    let update =
        $"""
        UPDATE {BrokerMovements}
        SET
            {TimeStamp} = @TimeStamp,
            {Amount} = @Amount,
            {CurrencyId} = @Currency,
            {BrokerAccountId} = @BrokerAccountId,
            {Commissions} = @Commissions,
            {Fees} = @Fees,
            {MovementType} = @MovementType
        WHERE
            {Id} = @Id
        """

    let delete = 
        $"""
        DELETE FROM {BrokerMovements}
        WHERE
            {Id} = @Id
        """

    let getAll =
        $"""
        SELECT * FROM {BrokerMovements}
        """

    let getById =
        $"""
        SELECT * FROM {BrokerMovements}
        WHERE
            {Id} = @Id
        LIMIT 1
        """