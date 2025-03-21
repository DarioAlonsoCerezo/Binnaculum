﻿namespace Binnaculum.Core.SQL

module internal OptionsQuery =
    let createTable =
        @"
            CREATE TABLE IF NOT EXISTS Options
           (
                Id INTEGER PRIMARY KEY,
                TimeStamp TEXT NOT NULL,
                ExpirationDate TEXT NOT NULL,
                Premium TEXT NOT NULL,
                NetPremium TEXT NOT NULL,
                TickerId INTEGER NOT NULL,
                BrokerAccountId INTEGER NOT NULL,
                CurrencyId INTEGER NOT NULL,
                OptionType TEXT NOT NULL,
                Code TEXT NOT NULL,
                Strike TEXT NOT NULL,
                Commissions TEXT NOT NULL,
                Fees TEXT NOT NULL,
                IsOpen INTEGER NOT NULL,
                ClosedWith INTEGER
            )
        "

    let insert =
        @"
            INSERT INTO Options
            (
                TimeStamp,
                ExpirationDate,
                Premium,
                NetPremium,
                TickerId,
                BrokerAccountId,
                CurrencyId,
                OptionType,
                Code,
                Strike,
                Commissions,
                Fees,
                IsOpen,
                ClosedWith
            )
            VALUES
            (
                @TimeStamp,
                @ExpirationDate,
                @Premium,
                @NetPremium,
                @TickerId,
                @BrokerAccountId,
                @CurrencyId,
                @OptionType,
                @Code,
                @Strike,
                @Commissions,
                @Fees,
                @IsOpen,
                @ClosedWith
            )
        "

    let update =
        @"
            UPDATE Options
            SET
                TimeStamp = @TimeStamp,
                ExpirationDate = @ExpirationDate,
                Premium = @Premium,
                NetPremium = @NetPremium,
                TickerId = @TickerId,
                BrokerAccountId = @BrokerAccountId,
                CurrencyId = @Currency
                OptionType = @OptionType,
                Code = @Code,
                Strike = @Strike,
                Commissions = @Commissions,
                Fees = @Fees,
                IsOpen = @IsOpen,
                ClosedWith = @ClosedWith
            WHERE
                Id = @Id
        "

    let delete = 
        @"
            DELETE FROM Options
            WHERE
                Id = @Id
        "

    let getAll =
        @"
            SELECT * FROM Options
        "

    let getById =
        @"
            SELECT * FROM Options
            WHERE
                Id = @Id
            LIMIT 1
        "