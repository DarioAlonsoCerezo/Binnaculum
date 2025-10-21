namespace Core.Tests.Integration

open NUnit.Framework
open System
open System.Threading.Tasks
open Binnaculum.Core.Models
open Binnaculum.Core.UI

/// <summary>
/// Reactive integration tests using signal-based approach.
/// These tests validate reactive stream emissions and collection updates
/// without relying on brittle timing mechanisms or platform-specific APIs.
/// </summary>
[<TestFixture>]
type ReactiveOverviewTests() =
    
    let mutable testContext : ReactiveTestContext option = None
    let mutable testActions : ReactiveTestActions option = None
    
    /// <summary>
    /// Setup before each test - start observing streams and prepare clean state
    /// </summary>
    [<SetUp>]
    member _.Setup() = async {
        printfn "\n=== Test Setup ==="
        
        // Wipe data before starting observation to ensure clean state
        Overview.WorkOnMemory()  // Ensure we're in memory mode
        try
            do! Overview.WipeAllDataForTesting() |> Async.AwaitTask
            printfn "✅ Data wiped successfully"
        with ex ->
            printfn "⚠️  Wipe failed: %s (may be expected if DB not initialized)" ex.Message
        
        ReactiveStreamObserver.startObserving()
        
        let ctx = ReactiveTestContext.create()
        testContext <- Some ctx
        testActions <- Some (ReactiveTestActions(ctx))
        
        printfn "=== Setup Complete ===\n"
    }
    
    /// <summary>
    /// Teardown after each test - stop observing streams
    /// </summary>
    [<TearDown>]
    member _.Teardown() = async {
        printfn "\n=== Test Teardown ==="
        ReactiveStreamObserver.stopObserving()
        testContext <- None
        testActions <- None
        printfn "=== Teardown Complete ===\n"
    }
    
    /// <summary>
    /// Test: Database initialization loads brokers and currencies with reactive updates
    /// </summary>
    [<Test>]
    [<Category("Integration")>]
    member _.``Database initialization loads brokers and currencies reactively`` () = async {
        printfn "\n=== TEST: Database initialization loads brokers and currencies reactively ==="
        
        let actions = testActions.Value
        
        // Expect signals for initial data loading
        ReactiveStreamObserver.expectSignals([
            Brokers_Updated
            Currencies_Updated
            Tickers_Updated
        ])
        
        // Initialize database (loads brokers, currencies, tickers)
        let! (ok, details, error) = actions.initDatabase()
        Assert.That(ok, Is.True, sprintf "Init should succeed: %s" (error |> Option.defaultValue ""))
        
        // Wait for signals (NOT Thread.Sleep!)
        let! signalsReceived = ReactiveStreamObserver.waitForAllSignalsAsync(TimeSpan.FromSeconds(10.0))
        Assert.That(signalsReceived, Is.True, "All expected signals should be received")
        
        // Verify collections were populated
        let! (verified, count, _) = actions.verifyBrokerCount(2)  // Tastytrade + IBKR
        Assert.That(verified, Is.True, count)
        
        let! (verified, count, _) = actions.verifyCurrencyCount(2)  // USD + EUR
        Assert.That(verified, Is.True, count)
    }
    
    /// <summary>
    /// Test: BrokerAccount creation updates accounts and snapshots reactively
    /// </summary>
    [<Test>]
    [<Category("Integration")>]
    member _.``BrokerAccount creation updates accounts and snapshots reactively`` () = async {
        printfn "\n=== TEST: BrokerAccount creation updates accounts and snapshots reactively ==="
        
        let actions = testActions.Value
        
        // Initialize first (with its own signals)
        ReactiveStreamObserver.expectSignals([
            Brokers_Updated
            Currencies_Updated
            Tickers_Updated
        ])
        
        let! (ok, _, _) = actions.initDatabase()
        Assert.That(ok, Is.True)
        
        let! _ = ReactiveStreamObserver.waitForAllSignalsAsync(TimeSpan.FromSeconds(10.0))
        
        // Now test account creation with fresh signal expectations
        ReactiveStreamObserver.expectSignals([
            Accounts_Updated
            Snapshots_Updated
        ])
        
        // Create broker account
        let! (ok, details, error) = actions.createBrokerAccount("TestAccount")
        Assert.That(ok, Is.True, sprintf "Account creation should succeed: %s" (error |> Option.defaultValue ""))
        
        // Wait for signals
        let! signalsReceived = ReactiveStreamObserver.waitForAllSignalsAsync(TimeSpan.FromSeconds(10.0))
        Assert.That(signalsReceived, Is.True, "Accounts and Snapshots should be updated")
        
        // Verify collections
        let! (verified, count, _) = actions.verifyAccountCount(1)
        Assert.That(verified, Is.True, count)
        
        let! (verified, count, _) = actions.verifySnapshotCount(1)
        Assert.That(verified, Is.True, count)
    }
    
    /// <summary>
    /// Test: Movement creation updates movements and snapshots
    /// NOTE: Temporarily disabled - needs proper BrokerMovement model
    /// </summary>
    [<Test>]
    [<Category("Integration")>]
    [<Ignore("Needs proper BrokerMovement construction")>]
    member _.``Movement creation updates movements and snapshots`` () = async {
        printfn "\n=== TEST: Movement creation updates movements and snapshots ==="
        return ()
    }
    
    /// <summary>
    /// Test: Multiple movements create multiple reactive updates
    /// NOTE: Temporarily disabled - needs proper BrokerMovement model
    /// </summary>
    [<Test>]
    [<Category("Integration")>]
    [<Ignore("Needs proper BrokerMovement construction")>]
    member _.``Multiple movements create multiple reactive updates`` () = async {
        printfn "\n=== TEST: Multiple movements create multiple reactive updates ==="
        return ()
    }
    
    /// <summary>
    /// Test: Signal timeout detection works correctly
    /// </summary>
    [<Test>]
    [<Category("Integration")>]
    member _.``Signal timeout detection works correctly`` () = async {
        printfn "\n=== TEST: Signal timeout detection works correctly ==="
        
        // Set up expectation for a signal that will never come
        ReactiveStreamObserver.expectSignals([Accounts_Updated])
        
        // Don't perform any operation that would trigger the signal
        // Just wait and verify timeout
        let! signalsReceived = ReactiveStreamObserver.waitForAllSignalsAsync(TimeSpan.FromSeconds(2.0))
        
        // Should timeout and return false
        Assert.That(signalsReceived, Is.False, "Should timeout when signals don't arrive")
        
        // Verify signal status shows missing signal
        let (expected, received, missing) = ReactiveStreamObserver.getSignalStatus()
        Assert.That(missing.Length, Is.GreaterThan(0), "Should report missing signals")
    }
    
    /// <summary>
    /// Test: Complete workflow - init, create account, verify state
    /// NOTE: Simplified to exclude movements until BrokerMovement construction is fixed
    /// </summary>
    [<Test>]
    [<Category("Integration")>]
    member _.``Complete workflow validates all reactive signals`` () = async {
        printfn "\n=== TEST: Complete workflow validates all reactive signals ==="
        
        let actions = testActions.Value
        
        // Phase 1: Initialize
        printfn "Phase 1: Database initialization"
        ReactiveStreamObserver.expectSignals([Brokers_Updated; Currencies_Updated; Tickers_Updated])
        let! (ok, _, error) = actions.initDatabase()
        Assert.That(ok, Is.True, sprintf "Init: %s" (error |> Option.defaultValue ""))
        let! received = ReactiveStreamObserver.waitForAllSignalsAsync(TimeSpan.FromSeconds(10.0))
        Assert.That(received, Is.True, "Init signals")
        
        // Phase 2: Create Account
        printfn "Phase 2: Create broker account"
        ReactiveStreamObserver.expectSignals([Accounts_Updated; Snapshots_Updated])
        let! (ok, _, error) = actions.createBrokerAccount("WorkflowTest")
        Assert.That(ok, Is.True, sprintf "Create account: %s" (error |> Option.defaultValue ""))
        let! received = ReactiveStreamObserver.waitForAllSignalsAsync(TimeSpan.FromSeconds(10.0))
        Assert.That(received, Is.True, "Account creation signals")
        
        // Phase 3: Verify Final State
        printfn "Phase 3: Verify final state"
        let! (verified, count, _) = actions.verifyAccountCount(1)
        Assert.That(verified, Is.True, sprintf "Final account count: %s" count)
        
        let! (verified, count, _) = actions.verifySnapshotCount(1)
        Assert.That(verified, Is.True, sprintf "Final snapshot count: %s" count)
        
        printfn "=== Workflow Complete ✅ ==="
    }
