﻿namespace Binnaculum.Core.SQL

module internal CurrencyQuery =
    let createTable =
        @"CREATE TABLE IF NOT EXISTS Currency
         (
             Id INTEGER PRIMARY KEY,
             Name TEXT NOT NULL,
             Code TEXT NOT NULL,
             Symbol TEXT NOT NULL
         )"

    let getCounted = @"SELECT COUNT(*) FROM Currency"

    let insert =
        @"
            INSERT INTO Currency 
            (
                Name, 
                Code, 
                Symbol
            )
            VALUES 
            (
                @Name, 
                @Code, 
                @Symbol
            )
        "

    let getAll = "SELECT * FROM Currency"

    let getById = 
        @"
            SELECT * FROM Currency
            WHERE
                Id = @Id
            LIMIT 1
        "