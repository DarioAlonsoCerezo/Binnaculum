namespace Binnaculum.Core.Database

open Microsoft.Data.Sqlite
open System.IO
open Microsoft.Maui.Storage

/// Represents the database connection mode
type DatabaseMode =
    | FileSystem of string  // File path for file-based database
    | InMemory of string    // Connection name for in-memory database

module ConnectionProvider =
    
    /// Creates a SQLite connection string based on the database mode
    let createConnectionString (mode: DatabaseMode) : string =
        match mode with
        | FileSystem filePath ->
            let init = SqliteConnectionStringBuilder($"Data Source = {filePath}")
            init.Mode <- SqliteOpenMode.ReadWriteCreate
            init.ToString()
        | InMemory connectionName ->
            // In-memory databases use Mode=Memory and Cache=Shared for isolation
            let init = SqliteConnectionStringBuilder($"Data Source = {connectionName}")
            init.Mode <- SqliteOpenMode.Memory
            init.Cache <- SqliteCacheMode.Shared
            init.ToString()
    
    /// Creates a new SQLite connection based on the database mode
    let createConnection (mode: DatabaseMode) : SqliteConnection =
        let connectionString = createConnectionString mode
        new SqliteConnection(connectionString)
    
    /// Gets the default file system database path (production use)
    let getDefaultDatabasePath () : string =
        Path.Combine(FileSystem.AppDataDirectory, "binnaculumDatabase.db")
    
    /// Creates the default file system database mode
    let defaultFileSystemMode () : DatabaseMode =
        FileSystem (getDefaultDatabasePath ())
