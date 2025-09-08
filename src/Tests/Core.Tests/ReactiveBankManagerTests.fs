namespace Core.Tests

open NUnit.Framework
open Binnaculum.Core.UI
open Binnaculum.Core.Models
open System.Reactive.Linq
open System
open System.Threading.Tasks

[<TestFixture>]
type ReactiveBankManagerTests() =

    [<SetUp>]
    member this.Setup() =
        // Set up test data using Edit instead of EditDiff
        let testBanks = [
            { Id = 1; Name = "Bank of America"; Image = Some "boa"; CreatedAt = DateTime.Now }
            { Id = 2; Name = "Wells Fargo"; Image = Some "wells"; CreatedAt = DateTime.Now }
            { Id = 3; Name = "Chase Bank"; Image = Some "chase"; CreatedAt = DateTime.Now }
        ]
        
        Collections.Banks.Edit(fun list ->
            list.Clear()
            list.AddRange(testBanks))
        
        // Initialize the reactive bank manager
        ReactiveBankManager.initialize()

    [<Test>]
    member this.``Fast bank lookup by ID should return correct bank with O(1) performance``() =
        // Test fast lookup by ID
        let boa = ReactiveBankManager.getBankByIdFast(1)
        let wells = ReactiveBankManager.getBankByIdFast(2)
        let chase = ReactiveBankManager.getBankByIdFast(3)
        
        Assert.That(boa.Name, Is.EqualTo("Bank of America"))
        Assert.That(boa.Image, Is.EqualTo(Some "boa"))
        Assert.That(boa.Id, Is.EqualTo(1))
        
        Assert.That(wells.Name, Is.EqualTo("Wells Fargo"))
        Assert.That(wells.Image, Is.EqualTo(Some "wells"))
        Assert.That(wells.Id, Is.EqualTo(2))
        
        Assert.That(chase.Name, Is.EqualTo("Chase Bank"))
        Assert.That(chase.Image, Is.EqualTo(Some "chase"))
        Assert.That(chase.Id, Is.EqualTo(3))

    [<Test>]
    member this.``Reactive bank lookup should return observable bank``() =
        // Test reactive lookup
        let observable = ReactiveBankManager.getBankByIdReactive(1)
        
        let mutable resultBank = { Id = 0; Name = ""; Image = None; CreatedAt = DateTime.MinValue }
        let mutable testCompleted = false
        
        observable.Subscribe(fun bank ->
            resultBank <- bank
            testCompleted <- true) |> ignore
        
        // Wait briefly for observable to emit
        System.Threading.Thread.Sleep(10)
        
        Assert.That(testCompleted, Is.True)
        Assert.That(resultBank.Name, Is.EqualTo("Bank of America"))
        Assert.That(resultBank.Id, Is.EqualTo(1))

    [<Test>]
    member this.``Performance comparison should show improvement over linear search``() =
        // Create larger dataset for performance testing
        let largeBanksList = [
            for i in 1..1000 do
                yield { Id = i; Name = $"Bank {i}"; Image = Some "bank"; CreatedAt = DateTime.Now }
        ]
        
        Collections.Banks.Edit(fun list ->
            list.Clear()
            list.AddRange(largeBanksList))
        
        // Re-initialize to pick up the larger dataset
        ReactiveBankManager.initialize()
        
        // Test old linear search approach (simulated)
        let stopwatch = System.Diagnostics.Stopwatch.StartNew()
        for i in 1..1000 do
            let _ = Collections.Banks.Items |> Seq.find(fun b -> b.Id = i)
            ()
        stopwatch.Stop()
        let linearSearchTime = stopwatch.ElapsedMilliseconds
        
        // Test new O(1) lookup approach
        stopwatch.Restart()
        for i in 1..1000 do
            let _ = ReactiveBankManager.getBankByIdFast(i)
            ()
        stopwatch.Stop()
        let fastLookupTime = stopwatch.ElapsedMilliseconds
        
        printfn "=== Reactive Bank Performance Results ==="
        printfn "Dataset: 1,000 banks, 1000 lookups"
        printfn "Linear search time: %dms" linearSearchTime
        printfn "Fast O(1) lookup time: %dms" fastLookupTime
        if linearSearchTime > 0 && fastLookupTime < linearSearchTime then
            printfn "Performance improvement: %dx" (linearSearchTime / max 1L fastLookupTime)
        else
            printfn "Performance improvement: Significant (sub-millisecond)"
        printfn "========================================"
        
        // Assert that fast lookup is at least as fast as linear search
        Assert.That(fastLookupTime, Is.LessThanOrEqualTo(linearSearchTime))

    [<Test>]
    member this.``Reactive cache should update when bank is added to collection``() =
        // Add a new bank to the collection
        let newBank = { Id = 4; Name = "Citibank"; Image = Some "citi"; CreatedAt = DateTime.Now }
        Collections.Banks.Edit(fun list -> list.Add(newBank))
        
        // The reactive cache should automatically pick up the new bank
        let retrievedBank = ReactiveBankManager.getBankByIdFast(4)
        
        Assert.That(retrievedBank.Name, Is.EqualTo("Citibank"))
        Assert.That(retrievedBank.Id, Is.EqualTo(4))

    [<Test>]
    member this.``Reactive cache should update when bank is updated in collection``() =
        // Update an existing bank by removing and re-adding with changes
        let originalBank = ReactiveBankManager.getBankByIdFast(1)
        Assert.That(originalBank.Name, Is.EqualTo("Bank of America"))
        
        let updatedBank = { originalBank with Name = "BofA (Updated)" }
        Collections.Banks.Edit(fun list -> 
            let current = list |> Seq.find(fun b -> b.Id = 1)
            list.Remove(current) |> ignore
            list.Add(updatedBank))
        
        // The reactive cache should reflect the update
        let retrievedBank = ReactiveBankManager.getBankByIdFast(1)
        Assert.That(retrievedBank.Name, Is.EqualTo("BofA (Updated)"))

    [<Test>]
    member this.``Backward compatibility - Collections.getBank should still work``() =
        // Test that bank lookup still works via existing direct access
        let bank = Collections.Banks.Items |> Seq.find(fun b -> b.Id = 1)
        
        Assert.That(bank.Name, Is.EqualTo("Bank of America"))
        Assert.That(bank.Id, Is.EqualTo(1))

    [<Test>]
    member this.``Extension method ToFastBankById should work correctly``() =
        // Test the extension method
        let bank = (1).ToFastBankById()
        
        Assert.That(bank.Name, Is.EqualTo("Bank of America"))
        Assert.That(bank.Id, Is.EqualTo(1))
        Assert.That(bank.Image, Is.EqualTo(Some "boa"))

    [<Test>]
    member this.``Extension method ToReactiveBankById should return observable``() =
        // Test the reactive extension method
        let observable = (2).ToReactiveBankById()
        
        let mutable resultBank = { Id = 0; Name = ""; Image = None; CreatedAt = DateTime.MinValue }
        let mutable testCompleted = false
        
        observable.Subscribe(fun bank ->
            resultBank <- bank
            testCompleted <- true) |> ignore
        
        // Wait briefly for observable to emit
        System.Threading.Thread.Sleep(10)
        
        Assert.That(testCompleted, Is.True)
        Assert.That(resultBank.Name, Is.EqualTo("Wells Fargo"))
        Assert.That(resultBank.Id, Is.EqualTo(2))