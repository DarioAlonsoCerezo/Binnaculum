namespace Binnaculum.Core.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open System

/// <summary>
/// Example tests demonstrating the in-memory database testing infrastructure.
/// These tests show how to use InMemoryDatabaseFixture for isolated database testing.
/// </summary>
[<TestClass>]
type public InMemoryDatabaseExampleTests() =
    inherit InMemoryDatabaseFixture()
    
    [<TestMethod>]
    member public this.``In-memory database infrastructure is properly initialized``() =
        // This test verifies that the in-memory database is set up correctly
        // by checking that we can query from an empty database without errors
        let currencies = CurrencyExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        Assert.IsNotNull(currencies, "Currency list should be returned (even if empty)")
    
    [<TestMethod>]
    member public this.``In-memory database is isolated between tests``() =
        // Verify database is empty at start of test (no data from previous tests)
        let currencies = CurrencyExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        Assert.AreEqual(0, currencies.Length, "Database should be empty for fresh test")
    
    [<TestMethod>]
    member public this.``In-memory database connection mode is correctly set``() =
        // Verify we're using in-memory mode by checking the test database name is set
        Assert.IsFalse(String.IsNullOrEmpty(this.TestDatabaseName), "Test database name should be set")
        StringAssert.StartsWith(this.TestDatabaseName, "test_db_", "Test database should use test prefix")

