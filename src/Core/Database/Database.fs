namespace Binnaculum.Core.Database

open Microsoft.Data.Sqlite
open System.IO
open Microsoft.Maui.Storage
open System.Data

module internal Do =
    type IEntity =
        abstract member Id: int
        abstract member InsertSQL: string
        abstract member UpdateSQL: string
        abstract member DeleteSQL: string

    type IAuditEntity =
        abstract member CreatedAt: Binnaculum.Core.Patterns.DateTimePattern option
        abstract member UpdatedAt: Binnaculum.Core.Patterns.DateTimePattern option

    let mutable private connection: SqliteConnection = null

    let private getConnectionString() =
        let databasePath = Path.Combine(FileSystem.AppDataDirectory, "binnaculumDatabase.db")
        let init = SqliteConnectionStringBuilder($"Data Source = {databasePath}")
        init.Mode <- SqliteOpenMode.ReadWriteCreate
        init.ToString()

    let private tablesSQL: string list =  [
        Binnaculum.Core.SQL.BrokerQuery.createTable
        Binnaculum.Core.SQL.BrokerAccountQuery.createTable
        Binnaculum.Core.SQL.CurrencyQuery.createTable
        Binnaculum.Core.SQL.BrokerMovementQuery.createTable
        Binnaculum.Core.SQL.TickersQuery.createTable
        Binnaculum.Core.SQL.TickerSplitQuery.createTable
        Binnaculum.Core.SQL.TickerPriceQuery.createTable
        Binnaculum.Core.SQL.TradesQuery.createTable
        Binnaculum.Core.SQL.DividendsQuery.createTable
        Binnaculum.Core.SQL.DividendTaxesQuery.createTable
        Binnaculum.Core.SQL.DividendDateQuery.createTable
        Binnaculum.Core.SQL.OptionsQuery.createTable
        Binnaculum.Core.SQL.BankQuery.createTable
        Binnaculum.Core.SQL.BankAccountsQuery.createTable
        Binnaculum.Core.SQL.BankAccountMovementsQuery.createTable
    ]

    let private connect() = task {
        if connection = null then
            connection <- new SqliteConnection(getConnectionString())
            do! connection.OpenAsync() |> Async.AwaitTask |> Async.Ignore

            // Enable foreign key constraints
            let pragmaCommand = connection.CreateCommand()
            pragmaCommand.CommandText <- "PRAGMA foreign_keys = ON;"
            do! pragmaCommand.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
            pragmaCommand.Dispose()

            // Create tables if they do not exist
            let command = connection.CreateCommand()
            tablesSQL
            |> List.map(fun sqlQuery -> async {
                        command.CommandText <- sqlQuery
                        return! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
                    })
            |> Async.Sequential
            |> Async.Ignore
            |> Async.RunSynchronously
            command.Dispose()
    }

    let createCommand() = task {
        do! connect() |> Async.AwaitTask |> Async.Ignore
        let command = connection.CreateCommand()
        command.CommandType = CommandType.Text |> ignore
        return command
    }

    let read<'a>(command: SqliteCommand, reader: SqliteDataReader -> 'a) = task {
        do! connect() |> Async.AwaitTask |> Async.Ignore
        let! result = command.ExecuteReaderAsync() |> Async.AwaitTask
        let mutable resultList = []
        while result.Read() do
            let item = reader result
            resultList <- item :: resultList
        command.Dispose()
        return resultList |> List.tryHead
    }

    let readAll<'a>(command: SqliteCommand, reader: SqliteDataReader -> 'a) = task {
        do! connect() |> Async.AwaitTask |> Async.Ignore
        let! result = command.ExecuteReaderAsync() |> Async.AwaitTask
        let mutable resultList = []
        while result.Read() do
            let item = reader result
            resultList <- item :: resultList
        command.Dispose()
        return resultList
    }

    let executeNonQuery(command: SqliteCommand) = task {
        do! connect() |> Async.AwaitTask |> Async.Ignore
        do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
        command.Dispose()
    }

    let saveEntity<'T when 'T :> IEntity> (entity: 'T) (fill: 'T -> SqliteCommand -> SqliteCommand) = task {
        let! command = createCommand()
        command.CommandText <- 
            match entity.Id with
            | 0 -> entity.InsertSQL
            | _ -> entity.UpdateSQL
        do! executeNonQuery(fill entity command) |> Async.AwaitTask |> Async.Ignore
    }

    let deleteEntity<'T when 'T :> IEntity> (entity: 'T) = task {
        let! command = createCommand()
        command.CommandText <- entity.DeleteSQL
        command.Parameters.AddWithValue("@Id", entity.Id) |> ignore
        do! executeNonQuery(command) |> Async.AwaitTask |> Async.Ignore
    }

    let getAllEntities<'T when 'T :> IEntity> (map: SqliteDataReader -> 'T) (sql: string) = task {
        let! command = createCommand()
        command.CommandText <- sql
        let! entities = readAll<'T>(command, map)
        return entities
    }

    let getById<'T when 'T :> IEntity>(map: SqliteDataReader -> 'T) (id: int) (sql: string) = task {
        let! command = createCommand()
        command.CommandText <- sql
        command.Parameters.AddWithValue("@Id", id) |> ignore
        let! entities = readAll<'T>(command, map)
        return entities |> List.tryHead
    }

    let executeExcalar(command: SqliteCommand) = task {
        do! connect() |> Async.AwaitTask |> Async.Ignore
        let! result = command.ExecuteScalarAsync() |> Async.AwaitTask
        command.Dispose()
        return result
    }

    let cleanTable(table: string) = task {
        let! command = createCommand()
        command.CommandText <- $"DELETE FROM {table}"
        do! executeNonQuery command |> Async.AwaitTask |> Async.Ignore
    }

    let init() = task {
        return connect() |> Async.AwaitTask |> Async.Ignore
    }