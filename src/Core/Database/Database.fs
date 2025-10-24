namespace Binnaculum.Core.Database

open Microsoft.Data.Sqlite
open System
open System.IO
open Microsoft.Maui.Storage
open System.Data
open Binnaculum.Core.TableName
open Binnaculum.Core.Logging.CoreLogger
open Binnaculum.Core.Providers

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

    /// Mutable connection mode - None means use default file system mode
    let mutable private connectionMode: DatabaseMode option = None

    /// Sets the database connection mode (for testing purposes)
    let setConnectionMode (mode: DatabaseMode) =
        connectionMode <- Some mode
        // Close existing connection to force reconnection with new mode
        if connection <> null then
            try
                connection.Close()
                connection.Dispose()
                connection <- null
            with _ ->
                ()

    let private getConnectionString () =
        match connectionMode with
        | Some mode -> ConnectionProvider.createConnectionString mode
        | None -> ConnectionProvider.createConnectionString (ConnectionProvider.defaultFileSystemMode ())

    let private tablesSQL: string list =
        [ Binnaculum.Core.SQL.BrokerQuery.createTable
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
          Binnaculum.Core.SQL.TickerSnapshotQuery.createTable
          Binnaculum.Core.SQL.TickerCurrencySnapshotQuery.createTable
          Binnaculum.Core.SQL.BrokerAccountSnapshotQuery.createTable
          Binnaculum.Core.SQL.BrokerSnapshotQuery.createTable
          Binnaculum.Core.SQL.BrokerFinancialSnapshotQuery.createTable
          Binnaculum.Core.SQL.BankAccountSnapshotQuery.createTable
          Binnaculum.Core.SQL.BankSnapshotQuery.createTable
          Binnaculum.Core.SQL.InvestmentOverviewSnapshotQuery.createTable ]

    let private connect () =
        task {
            if connection = null then
                connection <- new SqliteConnection(getConnectionString ())
                do! connection.OpenAsync() |> Async.AwaitTask |> Async.Ignore

                // Enable foreign key constraints
                let pragmaCommand = connection.CreateCommand()
                pragmaCommand.CommandText <- "PRAGMA foreign_keys = ON;"
                do! pragmaCommand.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
                pragmaCommand.Dispose()

                // Create tables if they do not exist
                let command = connection.CreateCommand()

                tablesSQL
                |> List.map (fun sqlQuery ->
                    async {
                        command.CommandText <- sqlQuery
                        return! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
                    })
                |> Async.Sequential
                |> Async.Ignore
                |> Async.RunSynchronously

                command.Dispose()
        }

    let createCommand () =
        task {
            do! connect () |> Async.AwaitTask |> Async.Ignore
            let command = connection.CreateCommand()
            command.CommandType = CommandType.Text |> ignore
            return command
        }

    let read<'a> (command: SqliteCommand, reader: SqliteDataReader -> 'a) =
        task {
            do! connect () |> Async.AwaitTask |> Async.Ignore
            let! result = command.ExecuteReaderAsync() |> Async.AwaitTask
            let mutable resultList = []

            while result.Read() do
                let item = reader result
                resultList <- item :: resultList

            command.Dispose()
            return resultList |> List.tryHead
        }

    let readAll<'a> (command: SqliteCommand, reader: SqliteDataReader -> 'a) =
        task {
            do! connect () |> Async.AwaitTask |> Async.Ignore
            let! result = command.ExecuteReaderAsync() |> Async.AwaitTask
            let mutable resultList = []

            while result.Read() do
                let item = reader result
                resultList <- item :: resultList

            command.Dispose()
            return resultList
        }

    let executeNonQuery (command: SqliteCommand) =
        task {
            try
                // Verbose step-by-step logging (commented for performance, uncomment if debugging needed)
                // logDatabaseDebug "Database.Do" "executeNonQuery - Step 1: Connecting to database..."
                do! connect () |> Async.AwaitTask |> Async.Ignore

                // logDatabaseDebug "Database.Do" "executeNonQuery - Step 2: Database connected, executing command..."
                // logDatabaseDebugf "Database.Do" "executeNonQuery - CommandText: %s" command.CommandText
                // logDatabaseDebugf "Database.Do" "executeNonQuery - Parameters count: %d" command.Parameters.Count

                // Single optimized debug log with all relevant info (disabled by default, zero-cost when disabled)
                logDatabaseDebugOptimized "Database.Do" (fun () ->
                    $"executeNonQuery - Command: {command.CommandText}, Params: {command.Parameters.Count}")

                do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore

                // logDatabaseDebug "Database.Do" "executeNonQuery - Step 3: Command executed successfully"
                // logDatabaseDebug "Database.Do" "executeNonQuery - Step 4: Disposing command..."
                command.Dispose()

            // logDatabaseDebug "Database.Do" "executeNonQuery - Step 5: Command disposed successfully"
            with ex ->
                // Database errors are ALWAYS logged regardless of database logging setting
                logDatabaseError "Database.Do" $"executeNonQuery failed - {ex.Message}"
                logDatabaseDebugf "Database.Do" "Stack trace: %s" ex.StackTrace

                let innerMsg =
                    if ex.InnerException <> null then
                        ex.InnerException.Message
                    else
                        "None"

                logDatabaseDebugf "Database.Do" "Inner exception: %s" innerMsg

                // Don't forget to dispose on error
                try
                    command.Dispose()
                with _ ->
                    ()

                raise ex
        }

    let saveEntity<'T when 'T :> IEntity> (entity: 'T) (fill: 'T -> SqliteCommand -> SqliteCommand) =
        task {
            try
                // Verbose step-by-step logging (commented for performance, uncomment if debugging needed)
                // logDatabaseDebug "Database.Do" "saveEntity - Step 1: Creating database command..."
                let! command = createCommand ()
                // logDatabaseDebug "Database.Do" "saveEntity - Step 2: Command created successfully"

                // logDatabaseDebugf "Database.Do" "saveEntity - Step 3: Setting CommandText based on entity ID = %d" entity.Id

                command.CommandText <-
                    match entity.Id with
                    | 0 ->
                        // logDatabaseDebug "Database.Do" "saveEntity - Step 4a: Using INSERT SQL (new entity)"
                        entity.InsertSQL
                    | _ ->
                        // logDatabaseDebug "Database.Do" "saveEntity - Step 4b: Using UPDATE SQL (existing entity)"
                        entity.UpdateSQL

                // logDatabaseDebugf "Database.Do" "saveEntity - Step 5: CommandText set to: %s" command.CommandText
                // logDatabaseDebug "Database.Do" "saveEntity - Step 6: Calling fill method to populate parameters..."

                let filledCommand = fill entity command

                // logDatabaseDebugf
                //     "Database.Do"
                //     "saveEntity - Step 7: Fill method completed, command has %d parameters"
                //     filledCommand.Parameters.Count

                // logDatabaseDebug "Database.Do" "saveEntity - Step 8: Calling executeNonQuery..."

                // Single optimized debug log with all relevant info (disabled by default, zero-cost when disabled)
                logDatabaseDebugOptimized "Database.Do" (fun () ->
                    let operation = if entity.Id = 0 then "INSERT" else "UPDATE"
                    $"saveEntity - {operation} entity (ID: {entity.Id}), Params: {filledCommand.Parameters.Count}")

                do! executeNonQuery (filledCommand) |> Async.AwaitTask |> Async.Ignore

            // logDatabaseDebug "Database.Do" "saveEntity - Step 9: executeNonQuery completed successfully"
            with ex ->
                // Database errors are ALWAYS logged regardless of database logging setting
                logDatabaseError "Database.Do" $"saveEntity failed - {ex.Message}"
                logDatabaseDebugf "Database.Do" "Stack trace: %s" ex.StackTrace

                let innerMsg =
                    if ex.InnerException <> null then
                        ex.InnerException.Message
                    else
                        "None"

                logDatabaseDebugf "Database.Do" "Inner exception: %s" innerMsg
                raise ex
        }

    let insertEntityAndGetId<'T when 'T :> IEntity> (entity: 'T) (fill: 'T -> SqliteCommand -> SqliteCommand) =
        task {
            if entity.Id <> 0 then
                invalidArg "entity" "insertEntityAndGetId can only be used for new entities with Id = 0."

            let! command = createCommand ()
            let connection = command.Connection
            use transaction = connection.BeginTransaction()

            command.Transaction <- transaction
            command.CommandText <- (entity :> IEntity).InsertSQL

            let filledCommand = fill entity command

            try
                do! filledCommand.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore

                use lastIdCommand = connection.CreateCommand()
                lastIdCommand.Transaction <- transaction
                lastIdCommand.CommandText <- "SELECT last_insert_rowid();"
                let! idObj = lastIdCommand.ExecuteScalarAsync() |> Async.AwaitTask

                let newId =
                    try
                        Convert.ToInt32 idObj
                    with
                    | :? InvalidCastException
                    | :? FormatException -> failwithf "Unexpected identifier type returned from SQLite: %A" idObj

                transaction.Commit()
                filledCommand.Dispose()
                return newId
            with ex ->
                try
                    transaction.Rollback()
                with _ ->
                    ()

                filledCommand.Dispose()
                return raise ex
        }

    let deleteEntity<'T when 'T :> IEntity> (entity: 'T) =
        task {
            let! command = createCommand ()
            command.CommandText <- entity.DeleteSQL
            command.Parameters.AddWithValue("@Id", entity.Id) |> ignore
            do! executeNonQuery (command) |> Async.AwaitTask |> Async.Ignore
        }

    let getAllEntities<'T when 'T :> IEntity> (map: SqliteDataReader -> 'T) (sql: string) =
        task {
            let! command = createCommand ()
            command.CommandText <- sql
            let! entities = readAll<'T> (command, map)
            return entities
        }

    let getById<'T when 'T :> IEntity> (map: SqliteDataReader -> 'T) (id: int) (sql: string) =
        task {
            let! command = createCommand ()
            command.CommandText <- sql
            command.Parameters.AddWithValue("@Id", id) |> ignore
            let! entities = readAll<'T> (command, map)
            return entities |> List.tryHead
        }

    let executeExcalar (command: SqliteCommand) =
        task {
            do! connect () |> Async.AwaitTask |> Async.Ignore
            let! result = command.ExecuteScalarAsync() |> Async.AwaitTask
            command.Dispose()
            return result
        }

    let cleanTable (table: string) =
        task {
            let! command = createCommand ()
            command.CommandText <- $"DELETE FROM {table}"
            do! executeNonQuery command |> Async.AwaitTask |> Async.Ignore
        }

    let init () =
        task { return connect () |> Async.AwaitTask |> Async.Ignore }

    /// <summary>
    /// 🚨 WARNING: TEST-ONLY METHOD - DO NOT USE IN PRODUCTION! 🚨
    /// Wipes all data from all database tables for testing purposes only.
    /// This method is intended strictly for integration tests to reset the database
    /// to a clean state as if the app was freshly installed.
    /// NOTE: Drops and recreates tables to ensure schema is up-to-date with latest changes.
    /// </summary>
    let wipeAllTablesForTesting () =
        task {
            // List of all table names in the order they should be dropped
            // (reverse order of dependencies to avoid foreign key constraints)
            let tableNames =
                [ InvestmentOverviewSnapshots
                  BrokerFinancialSnapshots
                  BankSnapshots
                  BankAccountSnapshots
                  BrokerSnapshots
                  BrokerAccountSnapshots
                  TickerCurrencySnapshots
                  TickerSnapshots
                  BankAccountMovements
                  BankAccounts
                  Banks
                  Options
                  DividendDates
                  DividendTaxes
                  Dividends
                  Trades
                  TickerSplits
                  TickerPrices
                  Tickers
                  BrokerMovements
                  Currencies
                  BrokerAccounts
                  Brokers ]

            // Drop all tables sequentially to avoid foreign key constraint issues
            let! command = createCommand ()
            for tableName in tableNames do
                command.CommandText <- $"DROP TABLE IF EXISTS {tableName}"
                do! executeNonQuery command |> Async.AwaitTask |> Async.Ignore
            command.Dispose()
            
            // Recreate all tables with current schema by executing all CREATE TABLE statements
            for createTableSql in tablesSQL do
                let! cmd = createCommand ()
                cmd.CommandText <- createTableSql
                do! executeNonQuery cmd |> Async.AwaitTask |> Async.Ignore
                cmd.Dispose()
        }
