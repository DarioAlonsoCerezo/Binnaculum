namespace Binnaculum.Core.UI

open System.Reactive.Subjects
open Binnaculum.Core.Models

module Overview = 

    let Data = new BehaviorSubject<OverviewUI>(ModelUI.defaultOverviewUI());

    let InitDatabase() = task {
        do! ModelUI.initialize()
        let! data = ModelUI.initializeOverview() 
        Data.OnNext data
    }