namespace Binnaculum.Core.UI

open System.Reactive.Subjects
open Binnaculum.Core
open DynamicData

module Overview =   
    
    //THis data should be used only while the app start, to do it quickly
    //Once the app is started and the user require more data, 
    //we should use the database to load more information
    let Data = new BehaviorSubject<Models.Home>(Storage.defaulHomeData)

    //We will activate these collections only if the user requires it
    let Accounts = new SourceList<Models.Account>()
    let Transactions = new SourceList<Models.Transaction>()

    let Init () =
        let data = Storage.load<Models.Home> Keys.HomeData Storage.defaulHomeData
        Data.OnNext data