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

    let InitDatabase() = task {
        do! Database.Do.init() |> Async.AwaitTask |> Async.Ignore
        do! BrokerExtensions.Do.insertIfNotExists() |> Async.AwaitTask |> Async.Ignore
        do! CurrencyExtensions.Do.insertDefaultValues() |> Async.AwaitTask |> Async.Ignore
        
        let! currencies = CurrencyExtensions.Do.getAll() |> Async.AwaitTask
        let databaseCurrencies = currencies

        let! brokers = BrokerExtensions.Do.getAll() |> Async.AwaitTask
        let databaseBrokers = brokers
        
        return()
    }