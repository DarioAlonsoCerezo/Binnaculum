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
                
                // Add delay to allow reactive collections to populate
                do! System.Threading.Tasks.Task.Delay(300)
                
                // Verify that currencies collection is populated - same as MAUI app expects
                Assert.That(Collections.Currencies.Items.Count, Is.GreaterThan(0), "Currencies collection should not be empty after LoadData")
                
                // Verify specific expected currencies exist (like USD which should always be there)
                let usdExists = Collections.Currencies.Items |> Seq.exists (fun c -> c.Code = "USD")
                Assert.That(usdExists, Is.True, "Should contain USD currency")
            }
            testTask |> Async.AwaitTask |> Async.RunSynchronously
        )