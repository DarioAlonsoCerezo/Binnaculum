namespace Binnaculum.Core.UI

open System.Reactive.Subjects
open Binnaculum.Core.Models
open Binnacle.Core.Storage

module Overview = 

    let Data = new BehaviorSubject<OverviewUI>({ IsDatabaseInitialized = false; TransactionsLoaded = false});

    let InitDatabase() = task {
        do! DataLoader.loadBasicData() |> Async.AwaitTask |> Async.Ignore
        Data.OnNext { Data.Value with IsDatabaseInitialized = true; }
    }

    let LoadData() = task {
        let! overview = ModelUI.loadData(Data.Value)
        Data.OnNext overview
    }