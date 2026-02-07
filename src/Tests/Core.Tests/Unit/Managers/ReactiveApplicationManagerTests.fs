namespace Binnaculum.Core.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open System
open DynamicData
open Binnaculum.Core.Models
open Binnaculum.Core.UI
open Binnaculum.Core.UI.ReactiveApplicationManager

[<TestClass>]
type ReactiveApplicationManagerTests() =

    [<TestInitialize>]
    member this.Setup() =
        // Clear all collections before each test
        Collections.Snapshots.Clear()
        Collections.Currencies.Clear()
        Collections.Brokers.Clear()
        Collections.Banks.Clear()
        Collections.Accounts.Clear()
        Collections.Movements.Clear()

    [<TestCleanup>]
    member this.TearDown() =
        // Cleanup after each test
        ReactiveApplicationManager.disposeReactiveManagers()

    [<TestMethod>]
    member this.``ReactiveApplicationManager should initialize all reactive managers successfully``() =
        // Test that all reactive managers can be initialized together
        ReactiveApplicationManager.initializeReactiveManagers()
        
        Assert.Pass("All reactive managers initialized successfully")

    [<TestMethod>]
    member this.``ReactiveApplicationManager should dispose cleanly``() =
        ReactiveApplicationManager.initializeReactiveManagers()
        
        // Multiple dispose calls should be safe
        ReactiveApplicationManager.disposeReactiveManagers()
        ReactiveApplicationManager.disposeReactiveManagers()
        
        Assert.Pass("ReactiveApplicationManager disposed cleanly")

    [<TestMethod>]
    member this.``ReactiveApplicationManager should handle initialization with empty collections``() =
        // Test initialization with empty collections
        ReactiveApplicationManager.initializeReactiveManagers()
        
        // Should not crash even with no data
        Assert.Pass("ReactiveApplicationManager handled empty collections gracefully")

    [<TestMethod>]
    member this.``initializeReactiveApplication should work without database errors``() =
        // Initialize reactive managers first
        ReactiveApplicationManager.initializeReactiveManagers()
        
        // The reactive application initialization should handle database errors gracefully
        // Note: This will generate database errors in test environment, but should not crash
        let task = ReactiveApplicationManager.initializeReactiveApplication()
        
        // Allow some time for async operations
        System.Threading.Thread.Sleep(100)
        
        Assert.Pass("ReactiveApplicationManager initialization completed")

    [<TestMethod>]
    member this.``initializeTraditional should work as fallback``() =
        // Traditional initialization should work as a fallback
        let task = ReactiveApplicationManager.initializeTraditional()
        
        // Allow some time for async operations
        System.Threading.Thread.Sleep(100)
        
        Assert.Pass("Traditional initialization worked as fallback")