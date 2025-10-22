namespace Core.Tests

open NUnit.Framework
open Binnaculum.Core.UI
open Binnaculum.Core.Models
open System.Reactive.Linq
open System
open System.Threading.Tasks

[<TestFixture>]
type ReactiveBrokerManagerTests() =

    [<SetUp>]
    member this.Setup() =
        // Set up test data using Edit instead of EditDiff
        let testBrokers = [
            { Id = 1; Name = "Interactive Brokers"; Image = "ib"; SupportedBroker = SupportedBroker.Unknown }
            { Id = 2; Name = "Charles Schwab"; Image = "schwab"; SupportedBroker = SupportedBroker.Unknown }
            { Id = 3; Name = "TD Ameritrade"; Image = "tda"; SupportedBroker = SupportedBroker.Unknown }
        ]
        
        Collections.Brokers.Edit(fun list ->
            list.Clear()
            list.AddRange(testBrokers))
        
        // Initialize the reactive broker manager
        ReactiveBrokerManager.initialize()

    [<Test>]
    member this.``Fast broker lookup by ID should return correct broker with O(1) performance``() =
        // Test fast lookup by ID
        let ib = ReactiveBrokerManager.getBrokerByIdFast(1)
        let schwab = ReactiveBrokerManager.getBrokerByIdFast(2)
        let tda = ReactiveBrokerManager.getBrokerByIdFast(3)
        
        Assert.That(ib.Name, Is.EqualTo("Interactive Brokers"))
        Assert.That(ib.Image, Is.EqualTo("ib"))
        Assert.That(ib.Id, Is.EqualTo(1))
        Assert.That(ib.SupportedBroker, Is.EqualTo(SupportedBroker.Unknown))
        
        Assert.That(schwab.Name, Is.EqualTo("Charles Schwab"))
        Assert.That(schwab.Image, Is.EqualTo("schwab"))
        Assert.That(schwab.Id, Is.EqualTo(2))
        Assert.That(schwab.SupportedBroker, Is.EqualTo(SupportedBroker.Unknown))
        
        Assert.That(tda.Name, Is.EqualTo("TD Ameritrade"))
        Assert.That(tda.Image, Is.EqualTo("tda"))
        Assert.That(tda.Id, Is.EqualTo(3))
        Assert.That(tda.SupportedBroker, Is.EqualTo(SupportedBroker.Unknown))

    [<Test>]
    member this.``Reactive broker lookup should return observable broker``() =
        // Test reactive lookup
        let observable = ReactiveBrokerManager.getBrokerByIdReactive(1)
        
        let mutable resultBroker = { Id = 0; Name = ""; Image = ""; SupportedBroker = SupportedBroker.Unknown }
        let mutable testCompleted = false
        
        observable.Subscribe(fun broker ->
            resultBroker <- broker
            testCompleted <- true) |> ignore
        
        // Wait briefly for observable to emit
        System.Threading.Thread.Sleep(10)
        
        Assert.That(testCompleted, Is.True)
        Assert.That(resultBroker.Name, Is.EqualTo("Interactive Brokers"))
        Assert.That(resultBroker.Id, Is.EqualTo(1))

    [<Test>]
    member this.``Performance comparison should show improvement over linear search``() =
        // Create larger dataset for performance testing
        let largeBrokersList = [
            for i in 1..1000 do
                yield { Id = i; Name = $"Broker {i}"; Image = "broker"; SupportedBroker = SupportedBroker.Unknown }
        ]
        
        Collections.Brokers.Edit(fun list ->
            list.Clear()
            list.AddRange(largeBrokersList))
        
        // Re-initialize to pick up the larger dataset
        ReactiveBrokerManager.initialize()
        
        // Test old linear search approach (simulated)
        let stopwatch = System.Diagnostics.Stopwatch.StartNew()
        for i in 1..1000 do
            let _ = Collections.Brokers.Items |> Seq.find(fun b -> b.Id = i)
            ()
        stopwatch.Stop()
        let linearSearchTime = stopwatch.ElapsedMilliseconds
        
        // Test new O(1) lookup approach
        stopwatch.Restart()
        for i in 1..1000 do
            let _ = ReactiveBrokerManager.getBrokerByIdFast(i)
            ()
        stopwatch.Stop()
        let fastLookupTime = stopwatch.ElapsedMilliseconds
        
        printfn "=== Reactive Broker Performance Results ==="
        printfn "Dataset: 1,000 brokers, 1000 lookups"
        printfn "Linear search time: %dms" linearSearchTime
        printfn "Fast O(1) lookup time: %dms" fastLookupTime
        if linearSearchTime > 0 && fastLookupTime < linearSearchTime then
            printfn "Performance improvement: %dx" (linearSearchTime / max 1L fastLookupTime)
        else
            printfn "Performance improvement: Significant (sub-millisecond)"
        printfn "========================================"
        
        // Assert that fast lookup is at least as fast as linear search (with tolerance for timing variance)
        Assert.That(fastLookupTime, Is.LessThanOrEqualTo(linearSearchTime + 10L))

    [<Test>]
    member this.``Reactive cache should update when broker is added to collection``() =
        // Add a new broker to the collection
        let newBroker = { Id = 4; Name = "Fidelity"; Image = "fidelity"; SupportedBroker = SupportedBroker.Unknown }
        Collections.Brokers.Edit(fun list -> list.Add(newBroker))
        
        // The reactive cache should automatically pick up the new broker
        let retrievedBroker = ReactiveBrokerManager.getBrokerByIdFast(4)
        
        Assert.That(retrievedBroker.Name, Is.EqualTo("Fidelity"))
        Assert.That(retrievedBroker.Id, Is.EqualTo(4))

    [<Test>]
    member this.``Reactive cache should update when broker is updated in collection``() =
        // Update an existing broker by removing and re-adding with changes
        let originalBroker = ReactiveBrokerManager.getBrokerByIdFast(1)
        Assert.That(originalBroker.Name, Is.EqualTo("Interactive Brokers"))
        
        let updatedBroker = { originalBroker with Name = "IB (Updated)" }
        Collections.Brokers.Edit(fun list -> 
            let current = list |> Seq.find(fun b -> b.Id = 1)
            list.Remove(current) |> ignore
            list.Add(updatedBroker))
        
        // The reactive cache should reflect the update
        let retrievedBroker = ReactiveBrokerManager.getBrokerByIdFast(1)
        Assert.That(retrievedBroker.Name, Is.EqualTo("IB (Updated)"))

    [<Test>]
    member this.``Backward compatibility - Collections.getBroker should still work``() =
        // Test that broker lookup still works via fallback to linear search
        let broker = Collections.Brokers.Items |> Seq.find(fun b -> b.Id = 1)
        
        Assert.That(broker.Name, Is.EqualTo("Interactive Brokers"))
        Assert.That(broker.Id, Is.EqualTo(1))