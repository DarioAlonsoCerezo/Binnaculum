namespace Binnaculum.Core.UI

open System.Reactive.Subjects
open Binnaculum.Core

module Overview =   
    
    let Data = new BehaviorSubject<Models.Home>(Storage.defaulHomeData)

    let Init () =
        let data = Storage.load<Models.Home> Keys.HomeData Storage.defaulHomeData
        Data.OnNext data