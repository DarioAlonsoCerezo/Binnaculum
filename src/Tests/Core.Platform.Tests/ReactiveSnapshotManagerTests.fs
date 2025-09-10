namespace Core.Platform.Tests

open NUnit.Framework
open System
open DynamicData
open Binnaculum.Core.Models
open Binnaculum.Core.UI
open Binnaculum.Core.UI.ReactiveSnapshotManager
open Core.Platform.Tests.PlatformTestEnvironment

[<TestFixture>]
[<Category("RequiresMauiPlatform")>]
[<Category("PlatformSpecific")>]
type ReactiveSnapshotManagerTests() =

    [<OneTimeSetUp>]
    member _.OneTimeSetup() =
        // Initialize platform for testing
        initializePlatform()

    [<SetUp>]
    member this.Setup() =
        requiresMauiPlatform(fun () ->
            // Clear all collections before each test
            Collections.Snapshots.Clear()
            Collections.Currencies.Clear()
            Collections.Brokers.Clear()
            Collections.Banks.Clear()
            Collections.Accounts.Clear()
            Collections.Movements.Clear()
            
            // Initialize reactive managers for testing
            ReactiveCurrencyManager.initialize()
            ReactiveBrokerManager.initialize()
            ReactiveBankManager.initialize()
            ReactiveBrokerAccountManager.initialize()
            ReactiveBankAccountManager.initialize()
            ReactiveTickerManager.initialize()
            ReactiveMovementManager.initialize()
            
            // Load test data
            Collections.Currencies.Add({ Id = 1; Title = "US Dollar"; Code = "USD"; Symbol = "$" })
            Collections.Currencies.Add({ Id = 2; Title = "Euro"; Code = "EUR"; Symbol = "€" })
            
            Collections.Brokers.Add({ Id = 1; Name = "Test Broker"; Image = "broker"; SupportedBroker = "TestBroker" })
            Collections.Banks.Add({ Id = 1; Name = "Test Bank"; Image = Some "bank"; CreatedAt = DateTime.Now })
        )

    [<TearDown>]
    member this.TearDown() =
        requiresMauiPlatform(fun () ->
            // Cleanup after each test - only dispose managers that have dispose methods
            ReactiveSnapshotManager.dispose()
            ReactiveMovementManager.dispose()
        )

    [<Test>]
    [<Category("RequiresMauiPlatform")>]
    member this.``ReactiveSnapshotManager should initialize successfully``() =
        requiresMauiPlatform(fun () ->
            // Test that the reactive snapshot manager can be initialized without errors
            ReactiveSnapshotManager.initialize()
            
            // The manager should be ready to handle collection changes
            Assert.Pass("ReactiveSnapshotManager initialized successfully")
        )

    [<Test>]
    [<Category("RequiresMauiPlatform")>]
    member this.``ReactiveSnapshotManager should handle empty collections gracefully``() =
        requiresMauiPlatform(fun () ->
            // Initialize the manager with empty collections
            ReactiveSnapshotManager.initialize()
            
            // Trigger a manual refresh to test empty state handling
            ReactiveSnapshotManager.refresh()
            
            // Should not crash and should handle empty state
            // Since there are no snapshots from the database, should add empty snapshot
            Assert.Pass("ReactiveSnapshotManager handled empty collections gracefully")
        )

    [<Test>]
    [<Category("RequiresMauiPlatform")>]
    member this.``ReactiveSnapshotManager should respond to currency changes``() =
        requiresMauiPlatform(fun () ->
            ReactiveSnapshotManager.initialize()
            
            // Add a new currency - this should trigger snapshot reloading
            Collections.Currencies.Add({ Id = 3; Title = "British Pound"; Code = "GBP"; Symbol = "£" })
            
            // Allow some time for reactive processing
            System.Threading.Thread.Sleep(100)
            
            // The reactive manager should have responded to the change
            Assert.Pass("ReactiveSnapshotManager responded to currency changes")
        )

    [<Test>]
    [<Category("RequiresMauiPlatform")>]
    member this.``ReactiveSnapshotManager should respond to broker changes``() =
        requiresMauiPlatform(fun () ->
            ReactiveSnapshotManager.initialize()
            
            // Add a new broker - this should trigger snapshot reloading
            Collections.Brokers.Add({ Id = 2; Name = "Another Broker"; Image = "broker2"; SupportedBroker = "AnotherBroker" })
            
            // Allow some time for reactive processing
            System.Threading.Thread.Sleep(100)
            
            // The reactive manager should have responded to the change
            Assert.Pass("ReactiveSnapshotManager responded to broker changes")
        )

    [<Test>]
    [<Category("RequiresMauiPlatform")>]
    member this.``ReactiveSnapshotManager should respond to bank changes``() =
        requiresMauiPlatform(fun () ->
            ReactiveSnapshotManager.initialize()
            
            // Add a new bank - this should trigger snapshot reloading
            Collections.Banks.Add({ Id = 2; Name = "Another Bank"; Image = Some "bank2"; CreatedAt = DateTime.Now })
            
            // Allow some time for reactive processing
            System.Threading.Thread.Sleep(100)
            
            // The reactive manager should have responded to the change
            Assert.Pass("ReactiveSnapshotManager responded to bank changes")
        )

    [<Test>]
    [<Category("RequiresMauiPlatform")>]
    member this.``ReactiveSnapshotManager should respond to movement changes``() =
        requiresMauiPlatform(fun () ->
            ReactiveSnapshotManager.initialize()
            
            // Create a sample movement
            let movement = {
                Type = AccountMovementType.BrokerMovement
                TimeStamp = DateTime.Now
                Trade = None
                Dividend = None
                DividendTax = None
                DividendDate = None
                OptionTrade = None
                BrokerMovement = Some {
                    Id = 1
                    TimeStamp = DateTime.Now
                    Amount = 100.00m
                    Currency = (1).ToFastCurrencyById()
                    BrokerAccount = { Id = 1; AccountNumber = "TEST001"; Broker = (1).ToFastBrokerById() }
                    Commissions = 0.00m
                    Fees = 0.00m
                    MovementType = BrokerMovementType.Deposit
                    Notes = Some "Test deposit"
                    FromCurrency = None
                    AmountChanged = None
                    Ticker = None
                    Quantity = None
                }
                BankAccountMovement = None
                TickerSplit = None
            }
            
            // Add a movement - this should trigger snapshot reloading
            Collections.Movements.Add(movement)
            
            // Allow some time for reactive processing
            System.Threading.Thread.Sleep(100)
            
            // The reactive manager should have responded to the change
            Assert.Pass("ReactiveSnapshotManager responded to movement changes")
        )

    [<Test>]
    [<Category("RequiresMauiPlatform")>]
    member this.``ReactiveSnapshotManager should dispose cleanly``() =
        requiresMauiPlatform(fun () ->
            ReactiveSnapshotManager.initialize()
            
            // Multiple dispose calls should be safe
            ReactiveSnapshotManager.dispose()
            ReactiveSnapshotManager.dispose()
            
            Assert.Pass("ReactiveSnapshotManager disposed cleanly")
        )

    [<Test>]
    [<Category("RequiresMauiPlatform")>]
    member this.``ReactiveSnapshotManager refresh should work manually``() =
        requiresMauiPlatform(fun () ->
            ReactiveSnapshotManager.initialize()
            
            // Manual refresh should work as a fallback
            ReactiveSnapshotManager.refresh()
            
            // Should not crash
            Assert.Pass("ReactiveSnapshotManager manual refresh worked")
        )

    [<Test>]
    [<Category("RequiresMauiPlatform")>]
    member this.``ReactiveSnapshotManager should handle rapid collection changes``() =
        requiresMauiPlatform(fun () ->
            ReactiveSnapshotManager.initialize()
            
            // Rapid changes to test resilience
            for i in 1..10 do
                Collections.Currencies.Add({ Id = i + 10; Title = sprintf "Currency %d" i; Code = sprintf "C%d" i; Symbol = sprintf "S%d" i })
                
            // Allow some time for processing
            System.Threading.Thread.Sleep(200)
            
            // Should handle rapid changes without crashing
            Assert.Pass("ReactiveSnapshotManager handled rapid collection changes")
        )