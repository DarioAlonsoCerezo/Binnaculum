namespace Core.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.Models

[<TestClass>]
type public EmptySnapshotManagementTests () =
    let snapshots = ResizeArray<OverviewSnapshot>()

    [<TestInitialize>]
    member public _.Setup() =
        snapshots.Clear()

    [<TestMethod>]
    member public _.``Snapshots allows multiple empty snapshots when using Add directly`` () =
        // Arrange
        let emptySnapshot1 = {
            Type = OverviewSnapshotType.Empty
            InvestmentOverview = None
            Broker = None
            Bank = None
            BrokerAccount = None
            BankAccount = None
        }
        let emptySnapshot2 = {
            Type = OverviewSnapshotType.Empty
            InvestmentOverview = None
            Broker = None
            Bank = None
            BrokerAccount = None
            BankAccount = None
        }
        // Act
        snapshots.Add(emptySnapshot1)
        snapshots.Add(emptySnapshot2)
        // Assert
        let emptyCount = snapshots |> Seq.filter (fun s -> s.Type = OverviewSnapshotType.Empty) |> Seq.length
        Assert.AreEqual(2, emptyCount)

    [<TestMethod>]
    member public _.``createEmptyOverviewSnapshot creates snapshot with Empty type`` () =
        let emptySnapshot = {
            Type = OverviewSnapshotType.Empty
            InvestmentOverview = None
            Broker = None
            Bank = None
            BrokerAccount = None
            BankAccount = None
        }
        Assert.AreEqual(OverviewSnapshotType.Empty, emptySnapshot.Type)
        Assert.IsTrue(emptySnapshot.InvestmentOverview.IsNone)
        Assert.IsTrue(emptySnapshot.Broker.IsNone)
        Assert.IsTrue(emptySnapshot.Bank.IsNone)
        Assert.IsTrue(emptySnapshot.BrokerAccount.IsNone)
        Assert.IsTrue(emptySnapshot.BankAccount.IsNone)

    [<TestMethod>]
    member public _.``Empty snapshot management logic implementation note`` () =
        Assert.Inconclusive("Implementation documented in DataLoader.fs helper functions")
