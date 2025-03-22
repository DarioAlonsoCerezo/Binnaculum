namespace Binnaculum.Core.Database

open Microsoft.Data.Sqlite
open System.IO
open Microsoft.Maui.Storage
open System.Data

module internal Do =
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
        Binnaculum.Core.SQL.MovementQuery.createTable
        Binnaculum.Core.SQL.TickersQuery.createTable
        Binnaculum.Core.SQL.TradesQuery.createTable
        Binnaculum.Core.SQL.DividendsQuery.createTable
        Binnaculum.Core.SQL.DividendTaxesQuery.createTable
        Binnaculum.Core.SQL.OptionsQuery.createTable
        Binnaculum.Core.SQL.BankAccountsQuery.createTable
        Binnaculum.Core.SQL.BankAccountBalancesQuery.createTable
        Binnaculum.Core.SQL.BankAccountInterestsQuery.createTable
        Binnaculum.Core.SQL.BankAccountFeesQuery.createTable
    ]

    let private connect() = task {
        if connection = null then
            connection <- new SqliteConnection(getConnectionString())
            do! connection.OpenAsync() |> Async.AwaitTask |> Async.Ignore
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

    let cleanTable(table: string) = task {
        let! command = createCommand()
        command.CommandText <- $"DELETE FROM {table}"
        do! executeNonQuery command |> Async.AwaitTask |> Async.Ignore
    }