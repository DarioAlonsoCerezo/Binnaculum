namespace Binnaculum.Core.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.Database
open Binnaculum.Core.Providers
open System
open System.Threading.Tasks

/// <summary>
/// Abstract base class for tests that require an isolated in-memory database.
/// Each test gets a fresh database instance, ensuring complete isolation.
/// </summary>
[<AbstractClass>]
type public InMemoryDatabaseFixture() =

    let mutable testDatabaseName = ""

    // Initialize SQLite native library once for all tests
    static do SQLitePCL.Batteries_V2.Init()

    /// <summary>
    /// Sets up a fresh in-memory database before each test.
    /// Called automatically by MSTest before each test method.
    /// </summary>
    [<TestInitialize>]
    member public this.SetUpDatabase() : Task =
        // Generate a unique database name for this test to ensure isolation
        let uniqueId = Guid.NewGuid().ToString("N")
        testDatabaseName <- $"test_db_{uniqueId}"

        // Configure database to use in-memory mode with the unique name
        let inMemoryMode = DatabaseMode.InMemory testDatabaseName
        Do.setConnectionMode inMemoryMode

        // Initialize the database (creates tables)
        let initAsync = Do.init () |> Async.AwaitTask |> Async.RunSynchronously
        initAsync |> Async.RunSynchronously
        Task.CompletedTask

    /// <summary>
    /// Cleans up the in-memory database after each test.
    /// Called automatically by MSTest after each test method.
    /// In-memory databases are automatically disposed when connections close.
    /// </summary>
    [<TestCleanup>]
    member public this.TearDownDatabase() : Task =
        // For in-memory databases, we don't need to wipe tables
        // The database will be automatically disposed when we close the connection
        // and a new unique database name will be generated for the next test
        Task.CompletedTask

    /// <summary>
    /// Gets the current test database name (useful for debugging)
    /// </summary>
    member public this.TestDatabaseName = testDatabaseName
