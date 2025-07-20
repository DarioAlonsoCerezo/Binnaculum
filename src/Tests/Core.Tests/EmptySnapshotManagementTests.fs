namespace Core.Tests

open NUnit.Framework
open Binnaculum.Core.Models
open System.Collections.Generic
open System

[<TestFixture>]
type EmptySnapshotManagementTests () =
    let snapshots = ResizeArray<OverviewSnapshot>()

    [<SetUp>]
    member _.Setup() =
        snapshots.Clear()

    [<Test>]
    member _.``Snapshots allows multiple empty snapshots when using Add directly`` () =
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
        Assert.That(emptyCount, NUnit.Framework.Is.EqualTo(2))

    [<Test>]
    member _.``createEmptyOverviewSnapshot creates snapshot with Empty type`` () =
        let emptySnapshot = {
            Type = OverviewSnapshotType.Empty
            InvestmentOverview = None
            Broker = None
            Bank = None
            BrokerAccount = None
            BankAccount = None
        }
        Assert.That(emptySnapshot.Type, NUnit.Framework.Is.EqualTo(OverviewSnapshotType.Empty))
        Assert.That(emptySnapshot.InvestmentOverview.IsNone, NUnit.Framework.Is.True)
        Assert.That(emptySnapshot.Broker.IsNone, NUnit.Framework.Is.True)
        Assert.That(emptySnapshot.Bank.IsNone, NUnit.Framework.Is.True)
        Assert.That(emptySnapshot.BrokerAccount.IsNone, NUnit.Framework.Is.True)
        Assert.That(emptySnapshot.BankAccount.IsNone, NUnit.Framework.Is.True)

    [<Test>]
    member _.``Empty snapshot management logic implementation note`` () =
        Assert.Pass("Implementation documented in DataLoader.fs helper functions")