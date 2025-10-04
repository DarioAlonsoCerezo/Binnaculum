namespace Core.Platform.Tests

open NUnit.Framework
open Binnaculum.Core.UI
open Core.Platform.Tests.PlatformTestEnvironment

/// <summary>
/// Integration tests that simulate real UI workflows using only the public Core API.
/// These tests verify the complete flow from public UI methods to public observable collections,
/// exactly as the MAUI app uses the Core library.
/// </summary>
[<TestFixture>]
[<Category("RequiresMauiPlatform")>]
type PublicApiIntegrationTests() =

    [<OneTimeSetUp>]
    member _.OneTimeSetup() =
        initializePlatform()

    [<Test>]
    [<Category("RequiresMauiPlatform")>]
    [<Timeout(30000)>] // 30 second timeout for entire test
    member _.``Overview InitDatabase and LoadData work without errors`` () = 
        requiresMauiPlatform(fun () ->
            let testTask = task {
                // Test the exact same calls that MAUI app makes on startup
                do! Overview.InitDatabase()
                do! Overview.LoadData()
                
                // Verify public state changes that MAUI app observes
                let isInitialized = Overview.Data.Value.IsDatabaseInitialized
                let isLoaded = Overview.Data.Value.TransactionsLoaded
                
                Assert.That(isInitialized, Is.True, "Database should be initialized")
                Assert.That(isLoaded, Is.True, "Data should be loaded")
                
                // Wait for reactive collections to populate with exponential backoff
                // Based on Android emulator logs: InitDatabase ~2.9s + LoadData ~0.4s + reactive population ~0.3s = ~3.6s total
                let mutable retries = 0
                let maxRetries = 20 // Up to 20 retries with exponential backoff
                let mutable currenciesPopulated = false
                
                while not currenciesPopulated && retries < maxRetries do
                    let delay = min (100 * (retries + 1)) 1000 // 100ms -> 200ms -> ... -> 1000ms (max)
                    do! System.Threading.Tasks.Task.Delay(delay)
                    currenciesPopulated <- Collections.Currencies.Items.Count > 0
                    retries <- retries + 1
                
                // Verify that currencies collection is populated - same as MAUI app expects
                Assert.That(Collections.Currencies.Items.Count, Is.GreaterThan(0), 
                    $"Currencies collection should not be empty after LoadData (retries: {retries}, total wait: ~{retries * 100}ms)")
                
                // Verify specific expected currencies exist (like USD which should always be there)
                let usdExists = Collections.Currencies.Items |> Seq.exists (fun c -> c.Code = "USD")
                Assert.That(usdExists, Is.True, "Should contain USD currency")
            }
            testTask |> Async.AwaitTask |> Async.RunSynchronously
        )