namespace Binnaculum.Core.Database

open Microsoft.Data.Sqlite
open System.IO
open Microsoft.Maui.Storage
open System.Data
open Binnaculum.Core.TableName

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
        Binnaculum.Core.SQL.TickerSnapshotQuery.createTable
        Binnaculum.Core.SQL.TickerCurrencySnapshotQuery.createTable
        Binnaculum.Core.SQL.BrokerAccountSnapshotQuery.createTable
        Binnaculum.Core.SQL.BrokerSnapshotQuery.createTable
        Binnaculum.Core.SQL.BrokerFinancialSnapshotQuery.createTable
        Binnaculum.Core.SQL.BankAccountSnapshotQuery.createTable
        Binnaculum.Core.SQL.BankSnapshotQuery.createTable
        Binnaculum.Core.SQL.InvestmentOverviewSnapshotQuery.createTable
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
        try
            System.Diagnostics.Debug.WriteLine($"[Database.Do.executeNonQuery] Step 1: Connecting to database...")
            do! connect() |> Async.AwaitTask |> Async.Ignore
            System.Diagnostics.Debug.WriteLine($"[Database.Do.executeNonQuery] Step 2: Database connected, executing command...")
            System.Diagnostics.Debug.WriteLine($"[Database.Do.executeNonQuery] CommandText: {command.CommandText}")
            System.Diagnostics.Debug.WriteLine($"[Database.Do.executeNonQuery] Parameters count: {command.Parameters.Count}")
            
            do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
            System.Diagnostics.Debug.WriteLine($"[Database.Do.executeNonQuery] Step 3: Command executed successfully")
            
            System.Diagnostics.Debug.WriteLine($"[Database.Do.executeNonQuery] Step 4: Disposing command...")
            command.Dispose()
            System.Diagnostics.Debug.WriteLine($"[Database.Do.executeNonQuery] Step 5: Command disposed successfully")
        with
        | ex ->
            System.Diagnostics.Debug.WriteLine($"[Database.Do.executeNonQuery] *** EXCEPTION *** - {ex.Message}")
            System.Diagnostics.Debug.WriteLine($"[Database.Do.executeNonQuery] *** STACK TRACE *** - {ex.StackTrace}")
            let innerMsg = if ex.InnerException <> null then ex.InnerException.Message else "None"
            System.Diagnostics.Debug.WriteLine($"[Database.Do.executeNonQuery] *** INNER EXCEPTION *** - {innerMsg}")
            // Don't forget to dispose on error
            try command.Dispose() with | _ -> ()
            raise ex
    }

    let saveEntity<'T when 'T :> IEntity> (entity: 'T) (fill: 'T -> SqliteCommand -> SqliteCommand) = task {
        try
            System.Diagnostics.Debug.WriteLine($"[Database.Do.saveEntity] Step 1: Creating database command...")
            let! command = createCommand()
            System.Diagnostics.Debug.WriteLine($"[Database.Do.saveEntity] Step 2: Command created successfully")
            
            System.Diagnostics.Debug.WriteLine($"[Database.Do.saveEntity] Step 3: Setting CommandText based on entity ID = {entity.Id}")
            command.CommandText <- 
                match entity.Id with
                | 0 -> 
                    System.Diagnostics.Debug.WriteLine($"[Database.Do.saveEntity] Step 4a: Using INSERT SQL (new entity)")
                    entity.InsertSQL
                | _ -> 
                    System.Diagnostics.Debug.WriteLine($"[Database.Do.saveEntity] Step 4b: Using UPDATE SQL (existing entity)")
                    entity.UpdateSQL
            System.Diagnostics.Debug.WriteLine($"[Database.Do.saveEntity] Step 5: CommandText set to: {command.CommandText}")
            
            System.Diagnostics.Debug.WriteLine($"[Database.Do.saveEntity] Step 6: Calling fill method to populate parameters...")
            let filledCommand = fill entity command
            System.Diagnostics.Debug.WriteLine($"[Database.Do.saveEntity] Step 7: Fill method completed, command has {filledCommand.Parameters.Count} parameters")
            
            System.Diagnostics.Debug.WriteLine($"[Database.Do.saveEntity] Step 8: Calling executeNonQuery...")
            do! executeNonQuery(filledCommand) |> Async.AwaitTask |> Async.Ignore
            System.Diagnostics.Debug.WriteLine($"[Database.Do.saveEntity] Step 9: executeNonQuery completed successfully")
        with
        | ex ->
            System.Diagnostics.Debug.WriteLine($"[Database.Do.saveEntity] *** EXCEPTION *** - {ex.Message}")
            System.Diagnostics.Debug.WriteLine($"[Database.Do.saveEntity] *** STACK TRACE *** - {ex.StackTrace}")
            let innerMsg = if ex.InnerException <> null then ex.InnerException.Message else "None"
            System.Diagnostics.Debug.WriteLine($"[Database.Do.saveEntity] *** INNER EXCEPTION *** - {innerMsg}")
            raise ex
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

    /// <summary>
    /// 🚨 WARNING: TEST-ONLY METHOD - DO NOT USE IN PRODUCTION! 🚨
    /// Wipes all data from all database tables for testing purposes only.
    /// This method is intended strictly for integration tests to reset the database
    /// to a clean state as if the app was freshly installed.
    /// </summary>
    let wipeAllTablesForTesting() = task {
        // List of all table names in the order they should be wiped
        // (reverse order of dependencies to avoid foreign key constraints)
        let tableNames = [
            InvestmentOverviewSnapshots
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
            Brokers
        ]

        // Clean all tables sequentially to avoid foreign key constraint issues
        for tableName in tableNames do
            do! cleanTable(tableName) |> Async.AwaitTask |> Async.Ignore
    }