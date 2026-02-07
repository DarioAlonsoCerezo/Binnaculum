namespace Binnaculum.Core.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.Managers
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Database
open System
open System.Diagnostics

/// <summary>
/// Unit tests for ProgressiveLoadingManager phased loading functionality.
/// Tests performance and correctness of progressive data loading.
/// </summary>
[<TestClass>]
type ProgressiveLoadingManagerTests() =
    inherit InMemoryDatabaseFixture()
    
    let mutable testBrokerAccountId = 0
    let mutable testCurrencyId = 0
    let mutable testBrokerId = 0
    
    [<TestInitialize>]
    member _.Setup() =
        task {
            // Create a test currency since in-memory DB starts empty
            let currency = {
                Id = 0
                Name = "US Dollar"
                Code = "USD"
                Symbol = "$"
            }
            do! CurrencyExtensions.Do.save(currency)
            let! currencies = CurrencyExtensions.Do.getAll()
            testCurrencyId <- currencies |> List.head |> fun c -> c.Id
            
            // Create test broker
            let newBroker = {
                Id = 0
                Name = "Test Broker"
                Image = "test.png"
                SupportedBroker = SupportedBroker.Unknown
            }
            do! BrokerExtensions.Do.save(newBroker)
            let! brokers = BrokerExtensions.Do.getAll()
            testBrokerId <- brokers |> List.head |> fun b -> b.Id
            
            // Create test broker account
            let newAccount = {
                Id = 0
                BrokerId = testBrokerId
                AccountNumber = "TEST123"
                Audit = AuditableEntity.FromDateTime(DateTime.UtcNow)
            }
            do! BrokerAccountExtensions.Do.save(newAccount)
            let! accounts = BrokerAccountExtensions.Do.getAll()
            testBrokerAccountId <- accounts |> List.head |> fun a -> a.Id
        } |> Async.AwaitTask |> Async.RunSynchronously
    
    /// Helper to create test movements
    member private _.createTestMovements(count: int) =
        task {
            for i in 1..count do
                let movement = {
                    Id = 0
                    TimeStamp = DateTimePattern.FromDateTime(DateTime.UtcNow.AddDays(-float i))
                    Amount = Money.FromAmount(decimal (i * 100))
                    CurrencyId = testCurrencyId
                    BrokerAccountId = testBrokerAccountId
                    Commissions = Money.FromAmount(1.0m)
                    Fees = Money.FromAmount(0.5m)
                    MovementType = BrokerMovementType.Deposit
                    Notes = Some $"Test movement {i}"
                    FromCurrencyId = None
                    AmountChanged = None
                    TickerId = None
                    Quantity = None
                    Audit = AuditableEntity.FromDateTime(DateTime.UtcNow)
                }
                let! _ = BrokerMovementExtensions.Do.save(movement)
                ()
        }
    
    [<TestMethod>]
    member this.``loadCriticalData should load account info``() =
        task {
            // Arrange
            do! this.createTestMovements(100)
            
            // Act
            let! result = ProgressiveLoadingManager.loadCriticalData testBrokerAccountId
            
            // Assert
            Assert.IsNotNull(result.AccountInfo, "Should load account info")
            match result.AccountInfo with
            | Some account ->
                Assert.AreEqual(testBrokerAccountId, account.Id)
                Assert.AreEqual("TEST123", account.AccountNumber)
            | None ->
                Assert.Fail("Account info should be present")
        } |> Async.AwaitTask |> Async.RunSynchronously
    
    [<TestMethod>]
    member this.``loadSecondaryData should load first page of movements``() =
        task {
            // Arrange
            do! this.createTestMovements(100)
            
            // Act
            let! result = ProgressiveLoadingManager.loadSecondaryData testBrokerAccountId 50
            
            // Assert
            Assert.AreEqual(50, result.FirstPageMovements.Length, "Should load 50 movements")
            Assert.AreEqual(100, result.TotalMovementCount, "Should have correct total count")
        } |> Async.AwaitTask |> Async.RunSynchronously
    
    [<TestMethod>]
    member this.``loadAllPhases should load both critical and secondary data``() =
        task {
            // Arrange
            do! this.createTestMovements(75)
            
            // Act
            let! (critical, secondary) = ProgressiveLoadingManager.loadAllPhases testBrokerAccountId 30
            
            // Assert - Critical data
            Assert.IsNotNull(critical.AccountInfo, "Should have account info")
            
            // Assert - Secondary data
            Assert.AreEqual(30, secondary.FirstPageMovements.Length, "Should load 30 movements")
            Assert.AreEqual(75, secondary.TotalMovementCount, "Should have correct total")
        } |> Async.AwaitTask |> Async.RunSynchronously
    
    [<TestMethod>]
    member this.``loadCriticalData should be fast``() =
        task {
            // Arrange
            do! this.createTestMovements(1000)
            
            // Act
            let sw = Stopwatch.StartNew()
            let! result = ProgressiveLoadingManager.loadCriticalData testBrokerAccountId
            sw.Stop()
            
            // Assert
            Assert.IsNotNull(result.AccountInfo)
            // Performance assertion - critical data should load quickly
            // Note: In-memory database is very fast, so this is a sanity check
            Assert.IsTrue(sw.ElapsedMilliseconds < 500, $"Critical data loading should be fast (was {sw.ElapsedMilliseconds}ms)")
        } |> Async.AwaitTask |> Async.RunSynchronously
    
    [<TestMethod>]
    member this.``loadSecondaryData should handle empty movements``() =
        task {
            // Arrange - No movements created
            
            // Act
            let! result = ProgressiveLoadingManager.loadSecondaryData testBrokerAccountId 50
            
            // Assert
            Assert.AreEqual(0, result.FirstPageMovements.Length, "Should return empty list")
            Assert.AreEqual(0, result.TotalMovementCount, "Should have zero count")
        } |> Async.AwaitTask |> Async.RunSynchronously
    
    [<TestMethod>]
    member this.``loadSecondaryData with custom page size should respect the size``() =
        task {
            // Arrange
            do! this.createTestMovements(100)
            
            // Act - Test with different page sizes
            let! result25 = ProgressiveLoadingManager.loadSecondaryData testBrokerAccountId 25
            let! result75 = ProgressiveLoadingManager.loadSecondaryData testBrokerAccountId 75
            
            // Assert
            Assert.AreEqual(25, result25.FirstPageMovements.Length)
            Assert.AreEqual(75, result75.FirstPageMovements.Length)
            Assert.AreEqual(100, result25.TotalMovementCount)
            Assert.AreEqual(100, result75.TotalMovementCount)
        } |> Async.AwaitTask |> Async.RunSynchronously
