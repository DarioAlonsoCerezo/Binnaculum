namespace Binnaculum.Core.SQL

module internal BankQuery =
    let createTable =
        @"
            CREATE TABLE IF NOT EXISTS Banks
            (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                Image TEXT NOT NULL
            )
        "

    let insert =
        @"
            INSERT INTO Banks
            (
                Name,
                Image
            )
            VALUES
            (
                @Name,
                @Image
            )
        "

    let update =
        @"
            UPDATE Banks
            SET
                Name = @Name,
                Image = @Image
            WHERE
                Id = @Id
        "

    let getAll =
        @"
            SELECT * FROM Banks
        "

    let getById =
        @"
            SELECT * FROM Banks
            WHERE
                Id = @Id
            LIMIT 1
        "

