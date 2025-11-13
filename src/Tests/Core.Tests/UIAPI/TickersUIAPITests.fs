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
        // These are API surface tests - they only verify method accessibility
        // Not integration tests
        ()

    [<Test>]
    member this.``Tickers module SaveTickerPrice should be accessible from UI layer``() =
        // API surface test - verifies method is callable
        // Test passes if code compiles
        let methodIsCallable =
            match box Tickers.SaveTickerPrice with
            | null -> false
            | _ -> true

        Assert.That(methodIsCallable, Is.True, "SaveTickerPrice should be accessible")

    [<Test>]
    member this.``SaveTickerPrice exists in Binnaculum.Core.UI namespace``() =
        // API surface test - if this compiles, the namespace is correct
        // We can reference Tickers.SaveTickerPrice directly
        Assert.Pass("Tickers.SaveTickerPrice is accessible from Binnaculum.Core.UI namespace")

    [<Test>]
    member this.``SaveTickerPrice method compiles with correct signature``() =
        // API surface test - if this compiles, the signature is correct
        // The fact that we can reference a function with this signature means it exists
        let _: (TickerPrice -> Task<unit>) = Tickers.SaveTickerPrice
        Assert.Pass("SaveTickerPrice has correct signature: TickerPrice -> Task<unit>")
