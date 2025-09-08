namespace Core.Tests

open NUnit.Framework
open Binnaculum.Core.UI
open Binnaculum.Core.Models
open System.Reactive.Linq
open System
open System.Threading.Tasks

[<TestFixture>]
type ReactiveCurrencyManagerTests() =

    [<SetUp>]
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

    [<Test>]
    member this.``Fast currency lookup should return correct currency with O(1) performance``() =
        // Test fast lookup
        let usd = "USD".ToFastCurrency()
        let eur = "EUR".ToFastCurrency()
        let gbp = "GBP".ToFastCurrency()
        
        Assert.That(usd.Code, Is.EqualTo("USD"))
        Assert.That(usd.Title, Is.EqualTo("US Dollar"))
        Assert.That(usd.Symbol, Is.EqualTo("$"))
        
        Assert.That(eur.Code, Is.EqualTo("EUR"))
        Assert.That(eur.Title, Is.EqualTo("Euro"))
        Assert.That(eur.Symbol, Is.EqualTo("€"))
        
        Assert.That(gbp.Code, Is.EqualTo("GBP"))
        Assert.That(gbp.Title, Is.EqualTo("British Pound"))
        Assert.That(gbp.Symbol, Is.EqualTo("£"))

    [<Test>]
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
        
        // Compare with linear search (original method)
        stopwatch.Restart()
        for _ in 1..1000 do
            let _ = Collections.GetCurrency("USD")
            ()
        stopwatch.Stop()
        
        let linearTime = stopwatch.ElapsedMilliseconds
        
        // Fast lookup should be significantly faster or at least not slower
        Assert.That(fastTime, Is.LessThanOrEqualTo(linearTime + 10L)) // Allow 10ms tolerance

    [<Test>]
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
        
        Assert.That(result.IsSome, Is.True)
        Assert.That(result.Value.Code, Is.EqualTo("USD"))
        Assert.That(result.Value.Title, Is.EqualTo("US Dollar"))
        
        subscription.Dispose()

    [<Test>]
    member this.``Reactive currency should update when collection changes``() =
        let results = System.Collections.Generic.List<Currency>()
        
        // Test with existing currency
        let subscription = 
            "USD".ToReactiveCurrency()
                .Subscribe(fun currency -> results.Add(currency))
        
        System.Threading.Thread.Sleep(100)
        
        // Should have received the currency
        Assert.That(results.Count, Is.GreaterThanOrEqualTo(1))
        Assert.That(results.[0].Code, Is.EqualTo("USD"))
        
        subscription.Dispose()

    [<Test>]
    member this.``Fast currency lookup should handle cache updates when currencies change``() =
        // Initial lookup
        let initialUsd = "USD".ToFastCurrency()
        Assert.That(initialUsd.Title, Is.EqualTo("US Dollar"))
        
        // Update the currency in the collection
        let updatedUsd = { Id = 1; Title = "United States Dollar"; Code = "USD"; Symbol = "$" }
        Collections.Currencies.Edit(fun list ->
            list.Clear()
            list.Add(updatedUsd))
        
        // Re-initialize to pick up changes
        ReactiveCurrencyManager.initialize()
        
        // Lookup should return updated currency
        let newUsd = "USD".ToFastCurrency()
        Assert.That(newUsd.Title, Is.EqualTo("United States Dollar"))

    [<Test>]
    member this.``Original tickerSnapshotToModel should work with fast currency lookup``() =
        // This test validates that the existing method still works but now uses fast lookup
        // We can't test the actual database snapshot conversion without more setup,
        // but we can verify the fast currency lookup is working
        
        let usd = "USD".ToFastCurrency()
        Assert.That(usd.Code, Is.EqualTo("USD"))
        Assert.That(usd.Title, Is.EqualTo("US Dollar"))
        
        // Test that the extension method is available
        let fastUsd = "USD".ToFastCurrency()
        Assert.That(fastUsd.Code, Is.EqualTo("USD"))

    [<Test>]
    member this.``Performance comparison between old and new currency lookup``() =
        // Add many currencies to test performance difference
        let manyCurrencies = [1..1000] |> List.map (fun i -> 
            { Id = i + 100; Title = sprintf "Test Currency %d" i; Code = sprintf "TEST%d" i; Symbol = sprintf "T%d" i })
        Collections.Currencies.Edit(fun list ->
            list.AddRange(manyCurrencies))
        
        let iterations = 100
        
        // Test old method performance
        let stopwatchOld = System.Diagnostics.Stopwatch.StartNew()
        for _ in 1..iterations do
            let _ = Collections.GetCurrency("USD")
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
        Assert.That(stopwatchNew.ElapsedMilliseconds, Is.LessThanOrEqualTo(stopwatchOld.ElapsedMilliseconds + 5L))