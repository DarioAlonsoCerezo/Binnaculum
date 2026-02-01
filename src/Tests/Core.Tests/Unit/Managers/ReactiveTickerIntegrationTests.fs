namespace Core.Tests

open NUnit.Framework
open Binnaculum.Core.UI
open Binnaculum.Core.Models
open System.Diagnostics

[<TestFixture>]
type ReactiveTickerIntegrationTests() =

    [<SetUp>]
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

    [<Test>]
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
        
        TestContext.Out.WriteLine($"=== Reactive Ticker Performance Results ===")
        TestContext.Out.WriteLine($"Dataset: 1,000 tickers, {iterations} lookups")
        TestContext.Out.WriteLine($"Linear search time: {linearSearchTime}ms")
        TestContext.Out.WriteLine($"Fast O(1) lookup time: {fastLookupTime}ms")
        
        if fastLookupTime > 0L && linearSearchTime > 0L then
            let improvement = float linearSearchTime / float fastLookupTime
            TestContext.Out.WriteLine($"Performance improvement: {improvement:F1}x faster")
        else
            TestContext.Out.WriteLine($"Performance improvement: Significant (sub-millisecond)")
        
        TestContext.Out.WriteLine($"========================================")
        
        // The new approach should be faster or at least as fast
        Assert.That(fastLookupTime, Is.LessThanOrEqualTo(linearSearchTime + 5L))

    [<Test>]
    member this.``Validate integration with fast lookups``() =
        // Test that the new fast lookups work correctly in realistic scenarios
        let ticker500 = (500).ToFastTickerById()
        let ticker250 = "STOCK250".ToFastTicker()
        
        Assert.That(ticker500.Id, Is.EqualTo(500))
        Assert.That(ticker500.Symbol, Is.EqualTo("STOCK500"))
        Assert.That(ticker500.Name.Value, Is.EqualTo("Stock 500 Corporation"))
        
        Assert.That(ticker250.Id, Is.EqualTo(250))
        Assert.That(ticker250.Symbol, Is.EqualTo("STOCK250"))
        Assert.That(ticker250.Name.Value, Is.EqualTo("Stock 250 Corporation"))

    [<Test>]
    member this.``Validate backward compatibility with existing Collections methods``() =
        // Ensure old methods still work for any code that hasn't been updated yet
        let ticker1 = Collections.Tickers.Items |> Seq.find(fun t -> t.Symbol = "STOCK1")
        let ticker2 = "STOCK1".ToFastTicker()
        
        Assert.That(ticker1.Id, Is.EqualTo(ticker2.Id))
        Assert.That(ticker1.Symbol, Is.EqualTo(ticker2.Symbol))
        Assert.That(ticker1.Name, Is.EqualTo(ticker2.Name))

    [<Test>]
    member this.``Validate cache updates correctly when collection changes``() =
        // Add a new ticker
        let newTicker = { Id = 1001; Symbol = "NEWSTOCK"; Image = None; Name = Some "New Stock Corp"; OptionsEnabled = true; OptionContractMultiplier = 100 }
        
        Collections.Tickers.Edit(fun list -> list.Add(newTicker))
        
        // Should be immediately available via fast lookup
        let retrievedTicker = "NEWSTOCK".ToFastTicker()
        Assert.That(retrievedTicker.Id, Is.EqualTo(1001))
        Assert.That(retrievedTicker.Symbol, Is.EqualTo("NEWSTOCK"))
        
        // Remove the ticker
        Collections.Tickers.Edit(fun list -> list.Remove(newTicker) |> ignore)
        
        // Should no longer be available
        Assert.Throws<System.Collections.Generic.KeyNotFoundException>(fun () -> 
            "NEWSTOCK".ToFastTicker() |> ignore) |> ignore