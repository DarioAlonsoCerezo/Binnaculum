namespace Binnaculum.Core.SQL

open Binnaculum.Core.Keys

module internal BrokerMovementQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {TableName_BrokerMovements}
        (
            Id INTEGER PRIMARY KEY,
            TimeStamp TEXT NOT NULL CHECK (TimeStamp GLOB '____-__-__T__:__:__'),
            Amount TEXT NOT NULL DEFAULT '0',
            CurrencyId INTEGER NOT NULL,
            BrokerAccountId INTEGER NOT NULL,
            Commissions TEXT NOT NULL DEFAULT '0',
            Fees TEXT NOT NULL DEFAULT '0',
            MovementType TEXT NOT NULL CHECK (MovementType IN ('DEPOSIT', 'WITHDRAWAL', 'FEE', 'INTERESTS_GAINED', 'LENDING', 'ACAT_MONEY_TRANSFER', 'ACAT_SECURITIES_TRANSFER', 'INTERESTS_PAID', 'CONVERSION')),
            CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
            UpdatedAt TEXT,
            FOREIGN KEY (CurrencyId) REFERENCES {TableName_Currencies}(Id),
            FOREIGN KEY (BrokerAccountId) REFERENCES {TableName_BrokerAccount}(Id)
        );

        CREATE INDEX IF NOT EXISTS idx_BrokerMovements_TimeStamp ON {TableName_BrokerMovements}(TimeStamp);
        CREATE INDEX IF NOT EXISTS idx_BrokerMovements_CurrencyId ON {TableName_BrokerMovements}(CurrencyId);
        CREATE INDEX IF NOT EXISTS idx_BrokerMovements_BrokerAccountId ON {TableName_BrokerMovements}(BrokerAccountId);

        CREATE TRIGGER IF NOT EXISTS trg_BrokerMovements_UpdatedAt
        AFTER UPDATE ON {TableName_BrokerMovements}
        FOR EACH ROW
        BEGIN
            UPDATE {TableName_BrokerMovements}
            SET UpdatedAt = datetime('now')
            WHERE Id = OLD.Id;
        END;
        """

    let insert =
        $"""
        INSERT INTO {TableName_BrokerMovements}
        (
            TimeStamp,
            Amount,
            CurrencyId,
            BrokerAccountId,
            Commissions,
            Fees,
            MovementType
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
        UPDATE {TableName_BrokerMovements}
        SET
            TimeStamp = @TimeStamp,
            Amount = @Amount,
            CurrencyId = @Currency,
            BrokerAccountId = @BrokerAccountId,
            Commissions = @Commissions,
            Fees = @Fees,
            MovementType = @MovementType
        WHERE
            Id = @Id
        """

    let delete = 
        $"""
        DELETE FROM {TableName_BrokerMovements}
        WHERE
            Id = @Id
        """

    let getAll =
        $"""
        SELECT * FROM {TableName_BrokerMovements}
        """

    let getById =
        $"""
        SELECT * FROM {TableName_BrokerMovements}
        WHERE
            Id = @Id
        LIMIT 1
        """