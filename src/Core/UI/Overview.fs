namespace Binnaculum.Core.UI

open System.Reactive.Subjects
open Binnaculum.Core.Models

module Overview = 

    let Data = new BehaviorSubject<OverviewUI>({ IsDatabaseInitialized = false; TransactionsLoaded = false});

    let InitDatabase() = task {
        let! overview = ModelUI.initialize(Data.Value)
        Data.OnNext overview
    }

    let LoadData() = task {
        let! overview = ModelUI.loadData(Data.Value)
        Data.OnNext overview
    }