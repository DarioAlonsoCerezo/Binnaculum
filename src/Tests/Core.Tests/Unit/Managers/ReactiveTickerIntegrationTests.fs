namespace Core.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.UI
open Binnaculum.Core.Models
open System.Diagnostics

[<TestClass>]
type ReactiveTickerIntegrationTests() =

    [<TestInitialize>]
    member this.Setup() =
        // Set up larger test data for better performance measurement
        let testTickers = [
            for i in 1..1000 do
                yield { Id = i; Symbol = sprintf "STOCK%d" i; Image = None; Name = Some (sprintf "Stock %d Corporation" i); OptionsEnabled = true; OptionContractMultiplier = 100 }
        ]
        
        Collections.Tickers.Edit(fun list ->
            list.Clear()
            list.AddRange(testTickers))
        
        // Initialize both managers for comparison
        ReactiveCurrencyManager.initialize()
        ReactiveTickerManager.initialize()

    [<TestMethod>]
    member this.``Performance comparison with realistic dataset``() =
        let iterations = 1000
        let stopwatch = Stopwatch.StartNew()
        
        // Test old linear search pattern
        stopwatch.Restart()
        for i in 1..iterations do
            let tickerId = (i % 1000) + 1
            let _ = Collections.Tickers.Items |> Seq.find(fun t -> t.Id = tickerId)
            ()
        stopwatch.Stop()
        let linearSearchTime = stopwatch.ElapsedMilliseconds
        
        // Test new O(1) lookup pattern
        stopwatch.Restart()
        for i in 1..iterations do
            let tickerId = (i % 1000) + 1
            let _ = tickerId.ToFastTickerById()
            ()
        stopwatch.Stop()
        let fastLookupTime = stopwatch.ElapsedMilliseconds
        
        Console.WriteLine($"=== Reactive Ticker Performance Results ===")
        Console.WriteLine($"Dataset: 1,000 tickers, {iterations} lookups")
        Console.WriteLine($"Linear search time: {linearSearchTime}ms")
        Console.WriteLine($"Fast O(1) lookup time: {fastLookupTime}ms")
        
        if fastLookupTime > 0L && linearSearchTime > 0L then
            let improvement = float linearSearchTime / float fastLookupTime
            Console.WriteLine($"Performance improvement: {improvement:F1}x faster")
        else
            Console.WriteLine($"Performance improvement: Significant (sub-millisecond)")
        
        Console.WriteLine($"========================================")
        
        // The new approach should be faster or at least as fast
        Assert.IsTrue(fastLookupTime <= linearSearchTime + 5L)

    [<TestMethod>]
    member this.``Validate integration with fast lookups``() =
        // Test that the new fast lookups work correctly in realistic scenarios
        let ticker500 = (500).ToFastTickerById()
        let ticker250 = "STOCK250".ToFastTicker()
        
        Assert.AreEqual(500, ticker500.Id)
        Assert.AreEqual("STOCK500", ticker500.Symbol)
        Assert.AreEqual("Stock 500 Corporation", ticker500.Name.Value)
        
        Assert.AreEqual(250, ticker250.Id)
        Assert.AreEqual("STOCK250", ticker250.Symbol)
        Assert.AreEqual("Stock 250 Corporation", ticker250.Name.Value)

    [<TestMethod>]
    member this.``Validate backward compatibility with existing Collections methods``() =
        // Ensure old methods still work for any code that hasn't been updated yet
        let ticker1 = Collections.Tickers.Items |> Seq.find(fun t -> t.Symbol = "STOCK1")
        let ticker2 = "STOCK1".ToFastTicker()
        
        Assert.AreEqual(ticker2.Id, ticker1.Id)
        Assert.AreEqual(ticker2.Symbol, ticker1.Symbol)
        Assert.AreEqual(ticker2.Name, ticker1.Name)

    [<TestMethod>]
    member this.``Validate cache updates correctly when collection changes``() =
        // Add a new ticker
        let newTicker = { Id = 1001; Symbol = "NEWSTOCK"; Image = None; Name = Some "New Stock Corp"; OptionsEnabled = true; OptionContractMultiplier = 100 }
        
        Collections.Tickers.Edit(fun list -> list.Add(newTicker))
        
        // Should be immediately available via fast lookup
        let retrievedTicker = "NEWSTOCK".ToFastTicker()
        Assert.AreEqual(1001, retrievedTicker.Id)
        Assert.AreEqual("NEWSTOCK", retrievedTicker.Symbol)
        
        // Remove the ticker
        Collections.Tickers.Edit(fun list -> list.Remove(newTicker) |> ignore)
        
        // Should no longer be available
        Assert.Throws<System.Collections.Generic.KeyNotFoundException>(fun () -> 
            "NEWSTOCK".ToFastTicker() |> ignore) |> ignore