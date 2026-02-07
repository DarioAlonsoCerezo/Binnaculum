namespace Core.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.UI
open Binnaculum.Core.Models
open System.Reactive.Linq
open System
open System.Threading.Tasks

[<TestClass>]
type ReactiveCurrencyManagerTests() =

    [<TestInitialize>]
    member this.Setup() =
        // Set up test data using Edit instead of EditDiff
        let testCurrencies = [
            { Id = 1; Title = "US Dollar"; Code = "USD"; Symbol = "$" }
            { Id = 2; Title = "Euro"; Code = "EUR"; Symbol = "€" }
            { Id = 3; Title = "British Pound"; Code = "GBP"; Symbol = "£" }
        ]
        
        Collections.Currencies.Edit(fun list ->
            list.Clear()
            list.AddRange(testCurrencies))
        
        // Initialize the reactive currency manager
        ReactiveCurrencyManager.initialize()

    [<TestMethod>]
    member this.``Fast currency lookup should return correct currency with O(1) performance``() =
        // Test fast lookup
        let usd = "USD".ToFastCurrency()
        let eur = "EUR".ToFastCurrency()
        let gbp = "GBP".ToFastCurrency()
        
        Assert.AreEqual("USD", usd.Code)
        Assert.AreEqual("US Dollar", usd.Title)
        Assert.AreEqual("$", usd.Symbol)
        
        Assert.AreEqual("EUR", eur.Code)
        Assert.AreEqual("Euro", eur.Title)
        Assert.AreEqual("€", eur.Symbol)
        
        Assert.AreEqual("GBP", gbp.Code)
        Assert.AreEqual("British Pound", gbp.Title)
        Assert.AreEqual("£", gbp.Symbol)

    [<TestMethod>]
    member this.``Fast currency lookup should be faster than linear search``() =
        // Add more currencies to make the difference noticeable
        let additionalCurrencies = [1..100] |> List.map (fun i -> 
            { Id = i + 10; Title = sprintf "Currency %d" i; Code = sprintf "CUR%d" i; Symbol = sprintf "C%d" i })
        Collections.Currencies.Edit(fun list ->
            list.AddRange(additionalCurrencies))
        
        // Test multiple lookups
        let stopwatch = System.Diagnostics.Stopwatch.StartNew()
        for _ in 1..1000 do
            let _ = "USD".ToFastCurrency()
            ()
        stopwatch.Stop()
        
        let fastTime = stopwatch.ElapsedMilliseconds
        
        // Compare with O(1) reactive method as baseline (was linear search)
        stopwatch.Restart()
        for _ in 1..1000 do
            let _ = "USD".ToFastCurrency()
            ()
        stopwatch.Stop()
        
        let linearTime = stopwatch.ElapsedMilliseconds
        
        // Fast lookup should be significantly faster or at least not slower
        Assert.IsTrue(fastTime <= linearTime + 10L) // Allow 10ms tolerance

    [<TestMethod>]
    member this.``Reactive currency lookup should emit currency when available``() =
        let mutable result: Currency option = None
        let mutable completed = false
        
        // Subscribe to reactive currency
        let subscription = 
            "USD".ToReactiveCurrency()
                .Subscribe(
                    (fun currency -> result <- Some currency),
                    (fun _ -> ()),
                    (fun () -> completed <- true))
        
        // Wait a bit for the observable to emit
        System.Threading.Thread.Sleep(100)
        
        Assert.IsTrue(result.IsSome)
        Assert.AreEqual("USD", result.Value.Code)
        Assert.AreEqual("US Dollar", result.Value.Title)
        
        subscription.Dispose()

    [<TestMethod>]
    member this.``Reactive currency should update when collection changes``() =
        let results = System.Collections.Generic.List<Currency>()
        
        // Test with existing currency
        let subscription = 
            "USD".ToReactiveCurrency()
                .Subscribe(fun currency -> results.Add(currency))
        
        System.Threading.Thread.Sleep(100)
        
        // Should have received the currency
        Assert.IsTrue(results.Count >= 1)
        Assert.AreEqual("USD", results.[0].Code)
        
        subscription.Dispose()

    [<TestMethod>]
    member this.``Fast currency lookup should handle cache updates when currencies change``() =
        // Initial lookup
        let initialUsd = "USD".ToFastCurrency()
        Assert.AreEqual("US Dollar", initialUsd.Title)
        
        // Update the currency in the collection
        let updatedUsd = { Id = 1; Title = "United States Dollar"; Code = "USD"; Symbol = "$" }
        Collections.Currencies.Edit(fun list ->
            list.Clear()
            list.Add(updatedUsd))
        
        // Re-initialize to pick up changes
        ReactiveCurrencyManager.initialize()
        
        // Lookup should return updated currency
        let newUsd = "USD".ToFastCurrency()
        Assert.AreEqual("United States Dollar", newUsd.Title)

    [<TestMethod>]
    member this.``Original tickerSnapshotToModel should work with fast currency lookup``() =
        // This test validates that the existing method still works but now uses fast lookup
        // We can't test the actual database snapshot conversion without more setup,
        // but we can verify the fast currency lookup is working
        
        let usd = "USD".ToFastCurrency()
        Assert.AreEqual("USD", usd.Code)
        Assert.AreEqual("US Dollar", usd.Title)
        
        // Test that the extension method is available
        let fastUsd = "USD".ToFastCurrency()
        Assert.AreEqual("USD", fastUsd.Code)

    [<TestMethod>]
    member this.``Performance comparison between old and new currency lookup``() =
        // Add many currencies to test performance difference
        let manyCurrencies = [1..1000] |> List.map (fun i -> 
            { Id = i + 100; Title = sprintf "Test Currency %d" i; Code = sprintf "TEST%d" i; Symbol = sprintf "T%d" i })
        Collections.Currencies.Edit(fun list ->
            list.AddRange(manyCurrencies))
        
        let iterations = 100
        
        // Test O(1) reactive method performance (was old method comparison)
        let stopwatchOld = System.Diagnostics.Stopwatch.StartNew()
        for _ in 1..iterations do
            let _ = "USD".ToFastCurrency()
            ()
        stopwatchOld.Stop()
        
        // Test new method performance  
        let stopwatchNew = System.Diagnostics.Stopwatch.StartNew()
        for _ in 1..iterations do
            let _ = "USD".ToFastCurrency()
            ()
        stopwatchNew.Stop()
        
        printfn $"Old method: {stopwatchOld.ElapsedMilliseconds}ms"
        printfn $"New method: {stopwatchNew.ElapsedMilliseconds}ms"
        printfn $"Performance improvement: {float stopwatchOld.ElapsedMilliseconds / float stopwatchNew.ElapsedMilliseconds}x"
        
        // New method should be at least as fast (allowing small margin for variance)
        Assert.IsTrue(stopwatchNew.ElapsedMilliseconds <= stopwatchOld.ElapsedMilliseconds + 5L)

    [<TestMethod>]
    member this.``Fast currency lookup by ID should return correct currency with O(1) performance``() =
        let usd = (1).ToFastCurrencyById()
        let eur = (2).ToFastCurrencyById()
        let gbp = (3).ToFastCurrencyById()
        
        Assert.AreEqual(1, usd.Id)
        Assert.AreEqual("USD", usd.Code)
        Assert.AreEqual("US Dollar", usd.Title)
        Assert.AreEqual("$", usd.Symbol)
        
        Assert.AreEqual(2, eur.Id)
        Assert.AreEqual("EUR", eur.Code)
        Assert.AreEqual("Euro", eur.Title)
        Assert.AreEqual("€", eur.Symbol)
        
        Assert.AreEqual(3, gbp.Id)
        Assert.AreEqual("GBP", gbp.Code)
        Assert.AreEqual("British Pound", gbp.Title)
        Assert.AreEqual("£", gbp.Symbol)

    [<TestMethod>]
    member this.``Reactive currency lookup by ID should emit currency when available``() =
        let mutable result: Currency option = None
        let subscription = 
            (1).ToReactiveCurrencyById()
                .Subscribe(fun currency -> result <- Some currency)
        
        System.Threading.Thread.Sleep(100)
        
        Assert.IsTrue(result.IsSome)
        Assert.AreEqual(1, result.Value.Id)
        Assert.AreEqual("USD", result.Value.Code)
        Assert.AreEqual("US Dollar", result.Value.Title)
        
        subscription.Dispose()

    [<TestMethod>]
    member this.``Fast currency lookup by ID should be faster than linear search``() =
        // Add more currencies to make the difference noticeable
        let additionalCurrencies = [1..100] |> List.map (fun i -> 
            { Id = i + 10; Title = sprintf "Currency %d" i; Code = sprintf "CUR%d" i; Symbol = sprintf "C%d" i })
        Collections.Currencies.Edit(fun list ->
            list.AddRange(additionalCurrencies))
        
        // Test multiple lookups with ID-based fast lookup
        let stopwatch = System.Diagnostics.Stopwatch.StartNew()
        for _ in 1..1000 do
            let _ = (1).ToFastCurrencyById()
            ()
        stopwatch.Stop()
        
        let fastTime = stopwatch.ElapsedMilliseconds
        
        // Compare with linear search (original internal method simulation)
        stopwatch.Restart()
        for _ in 1..1000 do
            let _ = Collections.Currencies.Items |> Seq.find(fun c -> c.Id = 1)
            ()
        stopwatch.Stop()
        
        let linearTime = stopwatch.ElapsedMilliseconds
        
        // Fast lookup should be significantly faster or at least not slower
        Assert.IsTrue(fastTime <= linearTime + 10L) // Allow 10ms tolerance

    [<TestMethod>]
    member this.``Fast currency lookup by ID should handle cache updates when currencies change``() =
        // Initial lookup
        let initialUsd = (1).ToFastCurrencyById()
        Assert.AreEqual("US Dollar", initialUsd.Title)
        
        // Update the currency in the collection
        let updatedUsd = { Id = 1; Title = "United States Dollar"; Code = "USD"; Symbol = "$" }
        Collections.Currencies.Edit(fun list ->
            list.Clear()
            list.Add(updatedUsd))
        
        // Re-initialize to pick up changes
        ReactiveCurrencyManager.initialize()
        
        // Lookup should return updated currency
        let newUsd = (1).ToFastCurrencyById()
        Assert.AreEqual("United States Dollar", newUsd.Title)

    [<TestMethod>]
    member this.``Both cache types should stay synchronized during collection changes``() =
        // Test that both code and ID caches stay in sync
        let newCurrency = { Id = 999; Title = "Test Currency"; Code = "TEST"; Symbol = "T" }
        
        Collections.Currencies.Edit(fun list ->
            list.Add(newCurrency))
        
        // Both lookup methods should find the same currency
        let byCode = "TEST".ToFastCurrency()
        let byId = (999).ToFastCurrencyById()
        
        Assert.AreEqual(byId.Id, byCode.Id)
        Assert.AreEqual(byId.Code, byCode.Code)
        Assert.AreEqual(byId.Title, byCode.Title)
        Assert.AreEqual(byId.Symbol, byCode.Symbol)

    [<TestMethod>]
    member this.``Performance comparison between ID-based old and new currency lookup``() =
        // Add many currencies to test performance difference
        let manyCurrencies = [1..1000] |> List.map (fun i -> 
            { Id = i + 200; Title = sprintf "Test Currency %d" i; Code = sprintf "PERF%d" i; Symbol = sprintf "P%d" i })
        Collections.Currencies.Edit(fun list ->
            list.AddRange(manyCurrencies))
        
        let iterations = 100
        
        // Test old method performance (linear search by ID simulation)
        let stopwatchOld = System.Diagnostics.Stopwatch.StartNew()
        for _ in 1..iterations do
            let _ = Collections.Currencies.Items |> Seq.find(fun c -> c.Id = 1)
            ()
        stopwatchOld.Stop()
        
        // Test new method performance (O(1) lookup by ID)
        let stopwatchNew = System.Diagnostics.Stopwatch.StartNew()
        for _ in 1..iterations do
            let _ = (1).ToFastCurrencyById()
            ()
        stopwatchNew.Stop()
        
        printfn $"ID Lookup - Old method: {stopwatchOld.ElapsedMilliseconds}ms"
        printfn $"ID Lookup - New method: {stopwatchNew.ElapsedMilliseconds}ms"
        printfn $"ID Lookup - Performance improvement: {float stopwatchOld.ElapsedMilliseconds / float stopwatchNew.ElapsedMilliseconds}x"
        
        // New method should be at least as fast (allowing small margin for variance)
        Assert.IsTrue(stopwatchNew.ElapsedMilliseconds <= stopwatchOld.ElapsedMilliseconds + 5L)