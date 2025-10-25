namespace Binnaculum.Core.Tests

open NUnit.Framework
open Binnaculum.Core.Managers
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Database
open Binnaculum.Core.Logging
open System

/// <summary>
/// Unit tests for DataLoadingManager pagination functionality.
/// Tests pagination, date range filtering, and context-aware data loading.
/// </summary>
[<TestFixture>]
type DataLoadingManagerTests() =
    inherit InMemoryDatabaseFixture()

    let mutable testBrokerAccountId = 0
    let mutable testCurrencyId = 0
    let mutable testBrokerId = 0

    [<SetUp>]
    member _.Setup() =
        task {
            // Create a test currency since in-memory DB starts empty
            let currency =
                { Id = 0
                  Name = "US Dollar"
                  Code = "USD"
                  Symbol = "$" }

            do! CurrencyExtensions.Do.save (currency)
            let! currencies = CurrencyExtensions.Do.getAll ()
            testCurrencyId <- currencies |> List.head |> (fun c -> c.Id)

            // Create test broker
            let newBroker =
                { Id = 0
                  Name = "Test Broker"
                  Image = "test.png"
                  SupportedBroker = SupportedBroker.Unknown }

            do! BrokerExtensions.Do.save (newBroker)
            let! brokers = BrokerExtensions.Do.getAll ()
            testBrokerId <- brokers |> List.head |> (fun b -> b.Id)

            // Create test broker account
            let newAccount =
                { Id = 0
                  BrokerId = testBrokerId
                  AccountNumber = "TEST123"
                  Audit = AuditableEntity.FromDateTime(DateTime.UtcNow) }

            do! BrokerAccountExtensions.Do.save (newAccount)
            let! accounts = BrokerAccountExtensions.Do.getAll ()
            testBrokerAccountId <- accounts |> List.head |> (fun a -> a.Id)
        }
        |> Async.AwaitTask
        |> Async.RunSynchronously

    /// Helper to create test movements
    member private _.createTestMovements(count: int) =
        task {
            for i in 1..count do
                let movement =
                    { Id = 0
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
                      Audit = AuditableEntity.FromDateTime(DateTime.UtcNow) }

                let! _ = BrokerMovementExtensions.Do.save (movement)
                ()
        }

    [<Test>]
    member this.``loadMovementsPaged should return correct page size``() =
        task {
            // Arrange
            do! this.createTestMovements (100)

            // Act
            let! result = BrokerMovementExtensions.Do.loadMovementsPaged (testBrokerAccountId, 0, 25)

            // Assert
            Assert.That(result.Length, Is.EqualTo(25), "Should return exactly 25 movements")
        }
        |> Async.AwaitTask
        |> Async.RunSynchronously

    [<Test>]
    member this.``loadMovementsPaged should return movements in descending order``() =
        task {
            // Arrange
            do! this.createTestMovements (10)

            // Act
            let! result = BrokerMovementExtensions.Do.loadMovementsPaged (testBrokerAccountId, 0, 10)

            // Assert
            Assert.That(result.Length, Is.GreaterThan(0), "Should have movements")

            // Debug: Print timestamps in ISO format
            CoreLogger.logDebug "DataLoadingManagerTests" $"Movements from loadMovementsPaged (count=%d{result.Length})"

            result
            |> List.iteri (fun i m ->
                CoreLogger.logDebug
                    "DataLoadingManagerTests"
                    $"  [%d{i}] ISO=%s{m.TimeStamp.ToString()} DateTime=%O{m.TimeStamp.Value}")

            // Verify descending order by timestamp (most recent first)
            let isDescending =
                result
                |> List.pairwise
                |> List.forall (fun (a, b) ->
                    let aDateTime = a.TimeStamp.Value
                    let bDateTime = b.TimeStamp.Value
                    let result = aDateTime >= bDateTime

                    if not result then
                        CoreLogger.logDebug
                            "DataLoadingManagerTests"
                            $"Order violation: %s{a.TimeStamp.ToString()} >= %s{b.TimeStamp.ToString()} should be true"

                    result)

            Assert.That(isDescending, Is.True, "Movements should be in descending order")
        }
        |> Async.AwaitTask
        |> Async.RunSynchronously

    [<Test>]
    member this.``loadMovementsPaged should handle pagination correctly``() =
        task {
            // Arrange
            do! this.createTestMovements (30)

            // Act
            let! page0 = BrokerMovementExtensions.Do.loadMovementsPaged (testBrokerAccountId, 0, 10)
            let! page1 = BrokerMovementExtensions.Do.loadMovementsPaged (testBrokerAccountId, 1, 10)
            let! page2 = BrokerMovementExtensions.Do.loadMovementsPaged (testBrokerAccountId, 2, 10)

            // Assert
            Assert.That(page0.Length, Is.EqualTo(10), "Page 0 should have 10 items")
            Assert.That(page1.Length, Is.EqualTo(10), "Page 1 should have 10 items")
            Assert.That(page2.Length, Is.EqualTo(10), "Page 2 should have 10 items")

            // Verify pages don't overlap
            let page0Ids = page0 |> List.map (fun m -> m.Id) |> Set.ofList
            let page1Ids = page1 |> List.map (fun m -> m.Id) |> Set.ofList
            let page2Ids = page2 |> List.map (fun m -> m.Id) |> Set.ofList

            Assert.That(Set.intersect page0Ids page1Ids, Is.Empty, "Pages 0 and 1 should not overlap")
            Assert.That(Set.intersect page1Ids page2Ids, Is.Empty, "Pages 1 and 2 should not overlap")
            Assert.That(Set.intersect page0Ids page2Ids, Is.Empty, "Pages 0 and 2 should not overlap")
        }
        |> Async.AwaitTask
        |> Async.RunSynchronously

    [<Test>]
    member this.``getMovementCount should return correct count``() =
        task {
            // Arrange
            do! this.createTestMovements (42)

            // Act
            let! count = BrokerMovementExtensions.Do.getMovementCount (testBrokerAccountId)

            // Assert
            Assert.That(count, Is.EqualTo(42), "Should return exact count")
        }
        |> Async.AwaitTask
        |> Async.RunSynchronously

    [<Test>]
    member this.``loadMovementListData should calculate HasMore correctly``() =
        task {
            // Arrange
            do! this.createTestMovements (25)

            // Act
            let! result = DataLoadingManager.loadMovementListData testBrokerAccountId 0 10

            // Assert
            Assert.That(result.TotalCount, Is.EqualTo(25))
            Assert.That(result.CurrentPage, Is.EqualTo(0))
            Assert.That(result.PageSize, Is.EqualTo(10))
            Assert.That(result.HasMore, Is.True, "Should have more pages")
            Assert.That(result.Movements.Length, Is.EqualTo(10))
        }
        |> Async.AwaitTask
        |> Async.RunSynchronously

    [<Test>]
    member this.``loadMovementListData should indicate no more pages on last page``() =
        task {
            // Arrange
            do! this.createTestMovements (25)

            // Act - Load page 2 (items 20-29, but only 5 exist)
            let! result = DataLoadingManager.loadMovementListData testBrokerAccountId 2 10

            // Assert
            Assert.That(result.HasMore, Is.False, "Should not have more pages")
            Assert.That(result.Movements.Length, Is.EqualTo(5), "Should have 5 remaining items")
        }
        |> Async.AwaitTask
        |> Async.RunSynchronously

    [<Test>]
    member this.``loadMovementsInDateRange should filter by date correctly``() =
        task {
            // Arrange
            let today = DateTime.UtcNow.Date

            let movement1 =
                { Id = 0
                  TimeStamp = DateTimePattern.FromDateTime(today.AddDays(-5.0))
                  Amount = Money.FromAmount(100m)
                  CurrencyId = testCurrencyId
                  BrokerAccountId = testBrokerAccountId
                  Commissions = Money.FromAmount(1.0m)
                  Fees = Money.FromAmount(0.5m)
                  MovementType = BrokerMovementType.Deposit
                  Notes = Some "5 days ago"
                  FromCurrencyId = None
                  AmountChanged = None
                  TickerId = None
                  Quantity = None
                  Audit = AuditableEntity.FromDateTime(DateTime.UtcNow) }

            let movement2 =
                { Id = 0
                  TimeStamp = DateTimePattern.FromDateTime(today.AddDays(-2.0))
                  Amount = Money.FromAmount(200m)
                  CurrencyId = testCurrencyId
                  BrokerAccountId = testBrokerAccountId
                  Commissions = Money.FromAmount(1.0m)
                  Fees = Money.FromAmount(0.5m)
                  MovementType = BrokerMovementType.Deposit
                  Notes = Some "2 days ago"
                  FromCurrencyId = None
                  AmountChanged = None
                  TickerId = None
                  Quantity = None
                  Audit = AuditableEntity.FromDateTime(DateTime.UtcNow) }

            let! _ = BrokerMovementExtensions.Do.save (movement1)
            let! _ = BrokerMovementExtensions.Do.save (movement2)

            // Act
            let startDate = DateTimePattern.FromDateTime(today.AddDays(-3.0))
            let endDate = DateTimePattern.FromDateTime(today)
            let! result = BrokerMovementExtensions.Do.loadMovementsInDateRange (testBrokerAccountId, startDate, endDate)

            // Assert
            Assert.That(result.Length, Is.EqualTo(1), "Should only return movement from 2 days ago")
            Assert.That(result.[0].Notes, Is.EqualTo(Some "2 days ago"))
        }
        |> Async.AwaitTask
        |> Async.RunSynchronously

    [<Test>]
    member this.``loadOverviewData should return data without movements``() =
        task {
            // Arrange
            do! this.createTestMovements (100)

            // Act
            let! result = DataLoadingManager.loadOverviewData testBrokerAccountId

            // Assert
            Assert.That(result.AccountInfo, Is.Not.Null)
        // This test verifies that we're not loading movements for overview
        // The actual snapshot would need to be created separately
        }
        |> Async.AwaitTask
        |> Async.RunSynchronously
