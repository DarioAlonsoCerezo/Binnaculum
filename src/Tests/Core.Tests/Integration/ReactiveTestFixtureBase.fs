namespace Core.Tests.Integration

open NUnit.Framework
open System

/// <summary>
/// Base class for reactive integration tests.
/// Provides common setup, teardown, and access to test context and actions.
///
/// Derived classes should:
/// 1. Override test methods with [<Test>] attribute
/// 2. Use Context property to access ReactiveTestContext
/// 3. Use Actions property to access ReactiveTestActions
/// 4. Call ReactiveTestSetup utility functions for standardized operations
///
/// Example:
///     type MyReactiveTests() =
///         inherit ReactiveTestFixtureBase()
///
///         [<Test>]
///         member this.``My test``() = async {
///             let actions = this.Actions
///             let! result = actions.initDatabase()
///             // assertions
///         }
/// </summary>
[<AbstractClass>]
type ReactiveTestFixtureBase() =

    let mutable testContext: ReactiveTestContext option = None
    let mutable testActions: ReactiveTestActions option = None

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
            let! (ctx, actions) = ReactiveTestSetup.setupTestEnvironment ()
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
            do! ReactiveTestSetup.teardownTestEnvironment ()
            testContext <- None
            testActions <- None
        }
