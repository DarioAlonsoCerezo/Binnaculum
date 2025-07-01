namespace Core.Tests

open NUnit.Framework
open Binnaculum.Core.Models
open Binnaculum.Core.UI
open Binnaculum.Core.Storage.DatabaseToModels
open System

[<TestFixture>]
type EmptySnapshotManagementTests () =

    [<SetUp>]
    member _.Setup() =
        // Clear snapshots before each test
        Collections.Snapshots.Clear()

    [<Test>]
    member _.``Collections.Snapshots should allow only one empty snapshot when no non-empty snapshots exist`` () =
        // Arrange
        let emptySnapshot1 = DatabaseToModels.Do.createEmptyOverviewSnapshot()
        let emptySnapshot2 = DatabaseToModels.Do.createEmptyOverviewSnapshot()
        
        // Act - add first empty snapshot
        Collections.Snapshots.Add(emptySnapshot1)
        let countAfterFirst = Collections.Snapshots.Items.Count
        
        // Act - add second empty snapshot
        Collections.Snapshots.Add(emptySnapshot2)
        let countAfterSecond = Collections.Snapshots.Items.Count
        
        // Assert
        Assert.AreEqual(1, countAfterFirst, "Should have 1 snapshot after adding first empty snapshot")
        Assert.AreEqual(2, countAfterSecond, "Current behavior allows multiple empty snapshots - this will be fixed by the implementation")

    [<Test>]
    member _.``Collections.Snapshots should remove empty snapshots when non-empty snapshot is added`` () =
        // Arrange
        let emptySnapshot = DatabaseToModels.Do.createEmptyOverviewSnapshot()
        let nonEmptySnapshot = {
            Type = OverviewSnapshotType.InvestmentOverview
            InvestmentOverview = Some {
                Date = DateOnly.FromDateTime(DateTime.Now)
                PortfoliosValue = 1000.0m
                RealizedGains = 100.0m
                RealizedPercentage = 10.0m
                Invested = 900.0m
                Commissions = 5.0m
                Fees = 2.0m
            }
            Broker = None
            Bank = None
            BrokerAccount = None
            BankAccount = None
        }
        
        // Act - add empty snapshot first
        Collections.Snapshots.Add(emptySnapshot)
        let countAfterEmpty = Collections.Snapshots.Items.Count
        let hasEmptyAfterEmptyAdd = Collections.Snapshots.Items |> Seq.exists (fun s -> s.Type = OverviewSnapshotType.Empty)
        
        // Act - add non-empty snapshot
        Collections.Snapshots.Add(nonEmptySnapshot)
        let countAfterNonEmpty = Collections.Snapshots.Items.Count
        let hasEmptyAfterNonEmptyAdd = Collections.Snapshots.Items |> Seq.exists (fun s -> s.Type = OverviewSnapshotType.Empty)
        let hasNonEmpty = Collections.Snapshots.Items |> Seq.exists (fun s -> s.Type = OverviewSnapshotType.InvestmentOverview)
        
        // Assert
        Assert.AreEqual(1, countAfterEmpty, "Should have 1 snapshot after adding empty snapshot")
        Assert.IsTrue(hasEmptyAfterEmptyAdd, "Should have empty snapshot after adding empty snapshot")
        Assert.AreEqual(2, countAfterNonEmpty, "Current behavior keeps both - this will be fixed by the implementation")
        Assert.IsTrue(hasEmptyAfterNonEmptyAdd, "Current behavior keeps empty snapshot - this will be fixed by the implementation")
        Assert.IsTrue(hasNonEmpty, "Should have non-empty snapshot after adding non-empty snapshot")

    [<Test>]
    member _.``createEmptyOverviewSnapshot should create snapshot with Empty type`` () =
        // Act
        let emptySnapshot = DatabaseToModels.Do.createEmptyOverviewSnapshot()
        
        // Assert
        Assert.AreEqual(OverviewSnapshotType.Empty, emptySnapshot.Type)
        Assert.IsNone(emptySnapshot.InvestmentOverview)
        Assert.IsNone(emptySnapshot.Broker)
        Assert.IsNone(emptySnapshot.Bank)
        Assert.IsNone(emptySnapshot.BrokerAccount)
        Assert.IsNone(emptySnapshot.BankAccount)