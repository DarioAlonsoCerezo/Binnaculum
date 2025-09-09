namespace Binnaculum.Core.UI

open System.Reactive.Subjects
open Binnaculum.Core.Models
open Binnaculum.Core.Storage

module Overview = 

    let Data = new BehaviorSubject<OverviewUI>({ IsDatabaseInitialized = false; TransactionsLoaded = false});

    let InitDatabase() = task {
        do! DataLoader.loadBasicData() |> Async.AwaitTask |> Async.Ignore
        Data.OnNext { Data.Value with IsDatabaseInitialized = true; }
    }

    let LoadData() = task {
        do! DataLoader.initialization() |> Async.AwaitTask |> Async.Ignore
        // Use reactive movement manager instead of manual loading
        ReactiveMovementManager.refresh()
        Data.OnNext { Data.Value with TransactionsLoaded = true; }
    }

    let LoadMovements() = 
        // Use reactive movement manager instead of manual DataLoader
        ReactiveMovementManager.refresh()
        System.Threading.Tasks.Task.CompletedTask
    
    let RefreshSnapshots() = 
        // Use reactive snapshot manager instead of manual DataLoader
        ReactiveSnapshotManager.refresh()
        System.Threading.Tasks.Task.CompletedTask
    