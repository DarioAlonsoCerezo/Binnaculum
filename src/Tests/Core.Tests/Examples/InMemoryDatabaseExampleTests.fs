namespace Binnaculum.Core.Tests

open NUnit.Framework
open Binnaculum.Core.Database
open Binnaculum.Core.Database.DatabaseModel

/// <summary>
/// Example tests demonstrating the in-memory database testing infrastructure.
/// These tests show how to use InMemoryDatabaseFixture for isolated database testing.
/// </summary>
[<TestFixture>]
type InMemoryDatabaseExampleTests() =
    inherit InMemoryDatabaseFixture()
    
    [<Test>]
    member this.``In-memory database infrastructure is properly initialized``() =
        // This test verifies that the in-memory database is set up correctly
        // by checking that we can query from an empty database without errors
        let currencies = CurrencyExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        Assert.That(currencies, Is.Not.Null, "Currency list should be returned (even if empty)")
    
    [<Test>]
    member this.``In-memory database is isolated between tests``() =
        // Verify database is empty at start of test (no data from previous tests)
        let currencies = CurrencyExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        Assert.That(currencies.Length, Is.EqualTo(0), "Database should be empty for fresh test")
    
    [<Test>]
    member this.``In-memory database connection mode is correctly set``() =
        // Verify we're using in-memory mode by checking the test database name is set
        Assert.That(this.TestDatabaseName, Is.Not.Empty, "Test database name should be set")
        Assert.That(this.TestDatabaseName, Does.StartWith("test_db_"), "Test database should use test prefix")

