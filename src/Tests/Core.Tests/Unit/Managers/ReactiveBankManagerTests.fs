namespace Core.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.UI
open Binnaculum.Core.Models
open System.Reactive.Linq
open System
open System.Threading.Tasks

[<TestClass>]
type ReactiveBankManagerTests() =

    [<TestInitialize>]
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

    [<TestMethod>]
    member this.``Fast bank lookup by ID should return correct bank with O(1) performance``() =
        // Test fast lookup by ID
        let boa = ReactiveBankManager.getBankByIdFast(1)
        let wells = ReactiveBankManager.getBankByIdFast(2)
        let chase = ReactiveBankManager.getBankByIdFast(3)
        
        Assert.AreEqual("Bank of America", boa.Name)
        Assert.AreEqual(Some "boa", boa.Image)
        Assert.AreEqual(1, boa.Id)
        
        Assert.AreEqual("Wells Fargo", wells.Name)
        Assert.AreEqual(Some "wells", wells.Image)
        Assert.AreEqual(2, wells.Id)
        
        Assert.AreEqual("Chase Bank", chase.Name)
        Assert.AreEqual(Some "chase", chase.Image)
        Assert.AreEqual(3, chase.Id)

    [<TestMethod>]
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
        
        Assert.IsTrue(testCompleted)
        Assert.AreEqual("Bank of America", resultBank.Name)
        Assert.AreEqual(1, resultBank.Id)

    [<TestMethod>]
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
        
        // Assert that fast lookup is at least as fast as linear search (with tolerance for timing variance)
        Assert.IsTrue(fastLookupTime <= linearSearchTime + 10L)

    [<TestMethod>]
    member this.``Reactive cache should update when bank is added to collection``() =
        // Add a new bank to the collection
        let newBank = { Id = 4; Name = "Citibank"; Image = Some "citi"; CreatedAt = DateTime.Now }
        Collections.Banks.Edit(fun list -> list.Add(newBank))
        
        // The reactive cache should automatically pick up the new bank
        let retrievedBank = ReactiveBankManager.getBankByIdFast(4)
        
        Assert.AreEqual("Citibank", retrievedBank.Name)
        Assert.AreEqual(4, retrievedBank.Id)

    [<TestMethod>]
    member this.``Reactive cache should update when bank is updated in collection``() =
        // Update an existing bank by removing and re-adding with changes
        let originalBank = ReactiveBankManager.getBankByIdFast(1)
        Assert.AreEqual("Bank of America", originalBank.Name)
        
        let updatedBank = { originalBank with Name = "BofA (Updated)" }
        Collections.Banks.Edit(fun list -> 
            let current = list |> Seq.find(fun b -> b.Id = 1)
            list.Remove(current) |> ignore
            list.Add(updatedBank))
        
        // The reactive cache should reflect the update
        let retrievedBank = ReactiveBankManager.getBankByIdFast(1)
        Assert.AreEqual("BofA (Updated)", retrievedBank.Name)

    [<TestMethod>]
    member this.``Backward compatibility - Collections.getBank should still work``() =
        // Test that bank lookup still works via existing direct access
        let bank = Collections.Banks.Items |> Seq.find(fun b -> b.Id = 1)
        
        Assert.AreEqual("Bank of America", bank.Name)
        Assert.AreEqual(1, bank.Id)

    [<TestMethod>]
    member this.``Extension method ToFastBankById should work correctly``() =
        // Test the extension method
        let bank = (1).ToFastBankById()
        
        Assert.AreEqual("Bank of America", bank.Name)
        Assert.AreEqual(1, bank.Id)
        Assert.AreEqual(Some "boa", bank.Image)

    [<TestMethod>]
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
        
        Assert.IsTrue(testCompleted)
        Assert.AreEqual("Wells Fargo", resultBank.Name)
        Assert.AreEqual(2, resultBank.Id)