namespace Binnaculum.Core.Tests

open NUnit.Framework
open Binnaculum.Core.Database
open System

/// <summary>
/// Abstract base class for tests that require an isolated in-memory database.
/// Each test gets a fresh database instance, ensuring complete isolation.
/// </summary>
[<AbstractClass>]
type InMemoryDatabaseFixture() =
    
    let mutable testDatabaseName = ""
    
    // Initialize SQLite native library once for all tests
    static do
        SQLitePCL.Batteries_V2.Init()
    
    /// <summary>
    /// Sets up a fresh in-memory database before each test.
    /// Called automatically by NUnit before each test method.
    /// </summary>
    [<SetUp>]
    member this.SetUpDatabase() =
        // Generate a unique database name for this test to ensure isolation
        let uniqueId = Guid.NewGuid().ToString("N")
        testDatabaseName <- $"test_db_{uniqueId}"
        
        // Configure database to use in-memory mode with the unique name
        let inMemoryMode = DatabaseMode.InMemory testDatabaseName
        Do.setConnectionMode inMemoryMode
        
        // Initialize the database (creates tables)
        Do.init() |> Async.AwaitTask |> Async.RunSynchronously
    
    /// <summary>
    /// Cleans up the in-memory database after each test.
    /// Called automatically by NUnit after each test method.
    /// In-memory databases are automatically disposed when connections close.
    /// </summary>
    [<TearDown>]
    member this.TearDownDatabase() =
        // For in-memory databases, we don't need to wipe tables
        // The database will be automatically disposed when we close the connection
        // and a new unique database name will be generated for the next test
        ()
    
    /// <summary>
    /// Gets the current test database name (useful for debugging)
    /// </summary>
    member this.TestDatabaseName = testDatabaseName
