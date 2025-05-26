namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // Added to access SQLParameterName

module internal OptionsQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {Options}
        (
            {Id} INTEGER PRIMARY KEY,
            {TimeStamp} TEXT NOT NULL,
            {ExpirationDate} TEXT NOT NULL,
            {Premium} TEXT NOT NULL,
            {NetPremium} TEXT NOT NULL,
            {TickerId} INTEGER NOT NULL,
            {BrokerAccountId} INTEGER NOT NULL,
            {CurrencyId} INTEGER NOT NULL,
            {OptionType} TEXT NOT NULL,
            {Code} TEXT NOT NULL,
            {Strike} TEXT NOT NULL,
            {Commissions} TEXT NOT NULL,
            {Fees} TEXT NOT NULL,
            {IsOpen} INTEGER NOT NULL,
            {ClosedWith} INTEGER,
            {Multiplier} TEXT NOT NULL DEFAULT 100.0,
            {Notes} TEXT,
            {CreatedAt} TEXT NOT NULL DEFAULT (DATETIME('now')),
            {UpdatedAt} TEXT,
            -- Foreign key to ensure TickerId references a valid Ticker in the Tickers table
            FOREIGN KEY ({TickerId}) REFERENCES {Tickers}({Id}) ON DELETE CASCADE ON UPDATE CASCADE,
            -- Foreign key to ensure BrokerAccountId references a valid BrokerAccount in the BrokerAccounts table
            FOREIGN KEY ({BrokerAccountId}) REFERENCES {BrokerAccounts}({Id}) ON DELETE CASCADE ON UPDATE CASCADE,
            -- Foreign key to ensure CurrencyId references a valid Currency in the Currencies table
            FOREIGN KEY ({CurrencyId}) REFERENCES {Currencies}({Id}) ON DELETE CASCADE ON UPDATE CASCADE
        );

        -- Index to optimize queries filtering by TimeStamp
        CREATE INDEX IF NOT EXISTS idx_Options_TimeStamp ON {Options}({TimeStamp});

        -- Index to optimize queries filtering by TickerId
        CREATE INDEX IF NOT EXISTS idx_Options_TickerId ON {Options}({TickerId});

        -- Index to optimize queries filtering by BrokerAccountId
        CREATE INDEX IF NOT EXISTS idx_Options_BrokerAccountId ON {Options}({BrokerAccountId});

        -- Index to optimize queries filtering by CurrencyId
        CREATE INDEX IF NOT EXISTS idx_Options_CurrencyId ON {Options}({CurrencyId});

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_Options_UpdatedAt
        AFTER UPDATE ON {Options}
        FOR EACH ROW
        BEGIN
            UPDATE {Options}
            SET {UpdatedAt} = strftime('%%Y-%%m-%%dT%%H:%%M:%%S', 'now')
            WHERE {Id} = OLD.{Id};
        END;
        """

    let insert =
        $"""
        INSERT INTO {Options}
        (
            {TimeStamp},
            {ExpirationDate},
            {Premium},
            {NetPremium},
            {TickerId},
            {BrokerAccountId},
            {CurrencyId},
            {OptionType},
            {Code},
            {Strike},
            {Commissions},
            {Fees},
            {IsOpen},
            {ClosedWith},
            {Multiplier},
            {Notes},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.TimeStamp},
            {SQLParameterName.ExpirationDate},
            {SQLParameterName.Premium},
            {SQLParameterName.NetPremium},
            {SQLParameterName.TickerId},
            {SQLParameterName.BrokerAccountId},
            {SQLParameterName.CurrencyId},
            {SQLParameterName.OptionType},
            {SQLParameterName.Code},
            {SQLParameterName.Strike},
            {SQLParameterName.Commissions},
            {SQLParameterName.Fees},
            {SQLParameterName.IsOpen},
            {SQLParameterName.ClosedWith},
            {SQLParameterName.Multiplier},
            {SQLParameterName.Notes},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
        )
        """

    let update =
        $"""
        UPDATE {Options}
        SET
            {TimeStamp} = {SQLParameterName.TimeStamp},
            {ExpirationDate} = {SQLParameterName.ExpirationDate},
            {Premium} = {SQLParameterName.Premium},
            {NetPremium} = {SQLParameterName.NetPremium},
            {TickerId} = {SQLParameterName.TickerId},
            {BrokerAccountId} = {SQLParameterName.BrokerAccountId},
            {CurrencyId} = {SQLParameterName.CurrencyId},
            {OptionType} = {SQLParameterName.OptionType},
            {Code} = {SQLParameterName.Code},
            {Strike} = {SQLParameterName.Strike},
            {Commissions} = {SQLParameterName.Commissions},
            {Fees} = {SQLParameterName.Fees},
            {IsOpen} = {SQLParameterName.IsOpen},
            {ClosedWith} = {SQLParameterName.ClosedWith},
            {Multiplier} = {SQLParameterName.Multiplier},
            {Notes} = {SQLParameterName.Notes},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete = 
        $"""
        DELETE FROM {Options}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {Options}
        """

    let getById =
        $"""
        SELECT * FROM {Options}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """