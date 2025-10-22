namespace Core.Tests

open NUnit.Framework
open Binnaculum.Core.UI
open Binnaculum.Core.Models
open System
open System.Threading.Tasks

[<TestFixture>]
type TickersUIAPITests() =

    [<SetUp>]
    member this.Setup() =
        // Initialize test environment
        Collections.clearAllCollectionsForTesting()
        
        // Add test currencies using Edit pattern
        let testCurrencies = [
            { Id = 1; Title = "US Dollar"; Code = "USD"; Symbol = "$" }
        ]
        Collections.Currencies.Edit(fun list ->
            list.Clear()
            list.AddRange(testCurrencies))
        
        // Add test tickers using Edit pattern
        let testTickers = [
            { Id = 1; Symbol = "AAPL"; Name = Some "Apple Inc."; Image = None }
        ]
        Collections.Tickers.Edit(fun list ->
            list.Clear()
            list.AddRange(testTickers))

    [<Test>]
    member this.``Tickers module SaveTickerPrice should be accessible from UI layer``() =
        // Test that the Tickers module and SaveTickerPrice method are accessible
        let tickerPrice = {
            Id = 0  // New record
            PriceDate = DateTime.Now
            Ticker = Collections.Tickers.Items |> Seq.head  // Use first available ticker
            Price = 150.25m
            Currency = Collections.Currencies.Items |> Seq.head  // Use first available currency
        }
        
        // This should not throw - the method should be callable and return a Task
        let saveTask = Tickers.SaveTickerPrice(tickerPrice)
        Assert.That(saveTask, Is.Not.Null)
        Assert.That(saveTask, Is.InstanceOf<Task>())

    [<Test>]
    member this.``SaveTickerPrice method signature accepts Models.TickerPrice``() =
        // Test that the method accepts the correct Models.TickerPrice type
        let tickerPrice = {
            Id = 0
            PriceDate = DateTime(2024, 1, 15, 10, 30, 0)
            Ticker = Collections.Tickers.Items |> Seq.head
            Price = 175.50m
            Currency = Collections.Currencies.Items |> Seq.head
        }
        
        // Test the method signature is correct - should compile and be callable
        let task = Tickers.SaveTickerPrice(tickerPrice)
        Assert.That(task, Is.Not.Null)
        
        // F# task methods return Task<unit> which has a different type name
        Assert.That(task.GetType().Name.StartsWith("Task"), Is.True)

    [<Test>]
    member this.``Tickers module follows established UI API patterns``() =
        // Test that the Tickers module follows the same patterns as Creator module
        // Just test that it compiles and is accessible - minimal test
        let tickerPrice = {
            Id = 0
            PriceDate = DateTime.Now
            Ticker = Collections.Tickers.Items |> Seq.head
            Price = 100.0m
            Currency = Collections.Currencies.Items |> Seq.head
        }
        
        // Should be callable and return a Task-like object (Task<unit> in F#)
        let task = Tickers.SaveTickerPrice(tickerPrice)
        Assert.That(task, Is.InstanceOf<Task>())

    [<Test>]  
    member this.``SaveTickerPrice method exists and is accessible``() =
        // Simple test - just verify the method can be called successfully
        let tickerPrice = {
            Id = 0
            PriceDate = DateTime.Now
            Ticker = Collections.Tickers.Items |> Seq.head
            Price = 99.99m
            Currency = Collections.Currencies.Items |> Seq.head
        }
        
        // Method should exist and be callable
        let task = Tickers.SaveTickerPrice(tickerPrice)
        Assert.That(task, Is.Not.Null)
        Assert.That(task, Is.InstanceOf<Task>())

    [<Test>]
    member this.``Tickers module exists in correct namespace``() =
        // Test that the Tickers module is in the Binnaculum.Core.UI namespace
        // This is verified by the fact that we can import it with "open Binnaculum.Core.UI"
        // and access Tickers.SaveTickerPrice directly
        
        let tickerPrice = {
            Id = 0
            PriceDate = DateTime.Now
            Ticker = Collections.Tickers.Items |> Seq.head
            Price = 50.0m
            Currency = Collections.Currencies.Items |> Seq.head
        }
        
        // If the namespace is wrong, this would not compile
        let task = Tickers.SaveTickerPrice(tickerPrice)
        Assert.That(task, Is.Not.Null, "Tickers.SaveTickerPrice should be accessible from Binnaculum.Core.UI namespace")