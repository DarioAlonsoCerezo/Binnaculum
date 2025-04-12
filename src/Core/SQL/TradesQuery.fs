namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName

module internal TradesQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {Trades}
        (
            {Id} INTEGER PRIMARY KEY,
            {TimeStamp} TEXT NOT NULL,
            {TickerId} INTEGER NOT NULL,
            {BrokerAccountId} INTEGER NOT NULL,
            {CurrencyId} INTEGER NOT NULL,
            {Quantity} TEXT NOT NULL,
            {Price} TEXT NOT NULL,
            {Commissions} TEXT NOT NULL,
            {Fees} TEXT NOT NULL,
            {TradeCode} TEXT NOT NULL,
            {TradeType} TEXT NOT NULL,
            {Notes} TEXT
        )
        """

    let insert =
        $"""
        INSERT INTO {Trades}
        (
            {TimeStamp},
            {TickerId},
            {BrokerAccountId},
            {CurrencyId},
            {Quantity},
            {Price},
            {Commissions},
            {Fees},
            {TradeCode},
            {TradeType},
            {Notes}
        )
        VALUES
        (
            @TimeStamp,
            @TickerId,
            @BrokerAccountId,
            @CurrencyId,
            @Quantity,
            @Price,
            @Commissions,
            @Fees,
            @TradeCode,
            @TradeType,
            @Notes
        )
        """

    let update =
        $"""
        UPDATE {Trades}
        SET
            {TimeStamp} = @TimeStamp,
            {TickerId} = @TickerId,
            {BrokerAccountId} = @BrokerAccountId,
            {CurrencyId} = @Currency,
            {Quantity} = @Quantity,
            {Price} = @Price,
            {Commissions} = @Commissions,
            {Fees} = @Fees,   
            {TradeCode} = @TradeCode,
            {TradeType} = @TradeType,
            {Notes} = @Notes
        WHERE
            {Id} = @Id
        """

    let delete =
        $"""
        DELETE FROM {Trades}
        WHERE
            {Id} = @Id
        """

    let getAll =
        $"""
        SELECT * FROM {Trades}
        """

    let getById =
        $"""
        SELECT * FROM {Trades}
        WHERE
            {Id} = @Id
        LIMIT 1
        """