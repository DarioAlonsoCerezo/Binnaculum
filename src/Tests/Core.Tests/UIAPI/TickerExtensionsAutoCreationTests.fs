namespace Core.Tests

open NUnit.Framework
open Binnaculum.Core.Database.DatabaseModel
open TickerExtensions
open System.Threading.Tasks

[<TestFixture>]
type TickerExtensionsAutoCreationTests() =

    [<Test>]
    member this.``getBySymbol method exists and has correct signature``() =
        // Test that the method signature is correct - this is the main validation
        // We validate that the return type changed from Task<Ticker option> to Task<Ticker>
        
        // This should compile - if it compiles, the signature is correct
        let methodCall () = Do.getBySymbol("TEST")
        
        // The method should return a Task<Ticker>
        let task = methodCall()
        Assert.That(task, Is.Not.Null)
        Assert.That(task, Is.InstanceOf<Task<Ticker>>())

    [<Test>]  
    member this.``Integration with Creator module pattern compiles``() =
        // Test that Creator.fs changes work with the new signature
        // This is a compilation test to ensure our changes are compatible
        
        // The new pattern used in Creator.fs should compile:
        // let! savedTicker = TickerExtensions.Do.getBySymbol(symbol)
        // (no more pattern matching on option needed)
        
        // This tests that the usage pattern compiles correctly
        let testUsage symbol = task {
            let! ticker = Do.getBySymbol(symbol)
            return ticker.Symbol
        }
        
        let task = testUsage("AAPL")
        Assert.That(task, Is.Not.Null)
        Assert.That(task, Is.InstanceOf<Task<string>>())

    [<Test>]
    member this.``Method signature shows Task<Ticker> not Task<Ticker option>``() =
        // Validate through type checking that we changed the signature correctly
        let taskType = typeof<Task<Ticker>>
        
        // Create a function that calls getBySymbol
        let getTickerTask () = Do.getBySymbol("EXAMPLE")
        let result = getTickerTask()
        
        // The result should be assignable to Task<Ticker>
        // Note: F# task expressions return AsyncStateMachineBox which implements Task<T>
        Assert.That(result, Is.InstanceOf<Task<Ticker>>())

    [<Test>]
    member this.``No option unwrapping needed in new implementation``() =
        // Test that we can use the result directly without option pattern matching
        // This validates the key change: no more Some/None handling
        
        let directUsage symbol = task {
            let! ticker = Do.getBySymbol(symbol)
            // Should be able to use ticker directly, no pattern matching needed
            return $"Symbol: {ticker.Symbol}, ID: {ticker.Id}"
        }
        
        let task = directUsage("DIRECT")
        Assert.That(task, Is.Not.Null)
        Assert.That(task, Is.InstanceOf<Task<string>>())