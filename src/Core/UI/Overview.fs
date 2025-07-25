﻿namespace Binnaculum.Core.UI

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
        do! DataLoader.loadMovementsFor(None) |> Async.AwaitTask |> Async.Ignore
        Data.OnNext { Data.Value with TransactionsLoaded = true; }
    }

    let LoadMovements(account: Account) = DataLoader.loadMovementsFor(Some account)
    
    let RefreshSnapshots() = DataLoader.loadLatestSnapshots()
    