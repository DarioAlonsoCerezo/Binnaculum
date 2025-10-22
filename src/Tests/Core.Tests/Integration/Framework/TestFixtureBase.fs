namespace Core.Tests.Integration

open NUnit.Framework
open System

/// <summary>
/// Base class for reactive integration tests using signal-based approach.
///
/// Handles all Setup/Teardown boilerplate:
/// - InMemory database initialization
/// - Reactive stream observation start/stop
/// - Test context and actions management
///
/// USAGE:
/// ------
/// 1. Inherit from this class
/// 2. Add [<Test>] methods using this.Actions
/// 3. Use StreamObserver.expectSignals() and waitForAllSignalsAsync()
/// 4. Use TestVerifications for assertions
///
/// EXAMPLE:
/// --------
///     [<TestFixture>]
///     type MyTests() =
///         inherit TestFixtureBase()
///
///         [<Test>]
///         member this.``My test``() = async {
///             let actions = this.Actions
///
///             // Setup
///             let! (ok, _, _) = actions.initDatabase()
///
///             // Expect signals BEFORE operation
///             StreamObserver.expectSignals([ Accounts_Updated ])
///
///             // Execute
///             let! (ok, _) = actions.createBrokerAccount("Test")
///
///             // Wait for signals (NOT Thread.Sleep!)
///             let! received = StreamObserver.waitForAllSignalsAsync(TimeSpan.FromSeconds(10.0))
///             Assert.That(received, Is.True)
///
///             // Verify
///             let! (verified, count, _) = actions.verifyAccountCount(1)
///             Assert.That(verified, Is.True)
///         }
///
/// PATTERN: Setup/Expect/Execute/Wait/Verify
/// See README.md for full documentation and more examples.
/// See PATTERN_GUIDE.fs for detailed implementation guide.
///
/// Provides common setup, teardown, and access to test context and actions.
///
/// Derived classes should:
/// 1. Override test methods with [<Test>] attribute
/// 2. Use Context property to access TestContext
/// 3. Use Actions property to access TestActions
/// 4. Call TestSetup utility functions for standardized operations
/// </summary>
[<AbstractClass>]
type TestFixtureBase() =

    let mutable testContext: Core.Tests.Integration.TestContext option = None
    let mutable testActions: Core.Tests.Integration.TestActions option = None

    /// <summary>
    /// Gets the test context.
    /// Available after Setup() completes.
    /// </summary>
    member _.Context =
        match testContext with
        | Some ctx -> ctx
        | None -> failwith "Test context not initialized. Ensure Setup() has completed."

    /// <summary>
    /// Gets the test actions helper.
    /// Available after Setup() completes.
    /// </summary>
    member _.Actions =
        match testActions with
        | Some actions -> actions
        | None -> failwith "Test actions not initialized. Ensure Setup() has completed."

    /// <summary>
    /// Setup before each test - prepare environment and start observing streams.
    /// This is called automatically by NUnit before each test method.
    /// </summary>
    [<SetUp>]
    member _.Setup() =
        async {
            let! (ctx, actions) = TestSetup.setupTestEnvironment ()
            testContext <- Some ctx
            testActions <- Some actions
        }

    /// <summary>
    /// Teardown after each test - stop observing streams and clean up.
    /// This is called automatically by NUnit after each test method.
    /// </summary>
    [<TearDown>]
    member _.Teardown() =
        async {
            do! TestSetup.teardownTestEnvironment ()
            testContext <- None
            testActions <- None
        }
