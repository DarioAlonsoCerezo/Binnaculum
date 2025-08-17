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
        do! DataLoader.loadMovementsFor() |> Async.AwaitTask |> Async.Ignore
        Data.OnNext { Data.Value with TransactionsLoaded = true; }
    }

    let LoadMovements() = DataLoader.loadMovementsFor()
    
    let RefreshSnapshots() = DataLoader.loadOverviewSnapshots()
    