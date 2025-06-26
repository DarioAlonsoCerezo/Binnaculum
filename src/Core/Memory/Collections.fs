namespace Binnaculum.Core.UI

open DynamicData
open Binnaculum.Core.Models
open System.Reactive.Subjects

/// <summary>
/// This module serves as a MemoryCache to connect from the UI to show data to the user
/// </summary>
module Collections =
    /// <summary>
    /// The Brokers list is loaded every time to ensure it reflects the most accurate and up-to-date information.
    /// This is necessary as the list must be accessible for creating new broker accounts and 
    /// should always be available for selection from the UI.
    /// </summary>
    let Brokers = new SourceList<Broker>()

    /// <summary>
    /// The Currencies list is loaded every time to ensure it reflects the most accurate and up-to-date information.
    /// This is necessary as the list must be accessible for creating new accounts and 
    /// should always be available for selection from the UI.
    /// </summary>
    let Currencies = new SourceList<Currency>()

    /// <summary>
    /// The Banks list is loaded every time to ensure it reflects the most accurate and up-to-date information.
    /// This is necessary as the list must be accessible for creating new bank accounts and 
    /// should always be available for selection from the UI.
    /// </summary>
    let Banks = new SourceList<Bank>()

    /// <summary>
    /// Accounts are loaded when Database is connected and rest of the data is loaded.
    /// This should reflect all the accounts in the Database and allow interaction from the UI
    /// </summary>
    let Accounts = new SourceList<Account>()

    /// <summary>
    /// Movements are loaded when Database is connected and rest of the data is loaded.
    /// This should start with a minimum data loaded from the database based on UI interactions
    /// and then be updated with the rest of the data when user scrolls
    /// </summary>
    let Movements = new SourceList<Movement>()

    /// <summary>
    /// This should be filled from UI interactions and populated when navigate to the account details
    /// </summary>
    let AccountDetails = new BehaviorSubject<Account>({ Type = AccountType.EmptyAccount; Broker = None; Bank = None; HasMovements = false; })

    /// <summary>
    /// This should be filled from UI interactions and populated when AccountDetails is updated
    /// We should load some movements from the database to populate the UI and add more data
    /// when UI requires it scrolling
    /// </summary>
    let AccountMovements = new SourceList<Movement>()

    /// <summary>
    /// This should be filled from UI interactions when navigate to the movement details
    /// This should be cleared when navigate back from details
    /// </summary>
    let MovementDetails = new BehaviorSubject<Movement>(emptyMovement())

    /// <summary>
    /// This collection stores filenames of images available in FileSystem.AppDataDirectory.
    /// It's populated at app startup and allows the application to offer a selection of images 
    /// to users when they're creating or editing banks, brokers, accounts, or movements.
    /// The stored values are image filenames without extensions, ready to be used directly
    /// with UI image controls.
    /// </summary>
    let AvailableImages = new SourceList<string>()

    /// <summary>
    /// This collection stores tickers that are available in the application.
    /// It is used to provide a list of tickers for selection in various UI components.
    /// The tickers are loaded from the database and can be updated as needed.
    /// </summary>
    let Tickers = new SourceList<Ticker>()

    /// <summary>
    /// This collection stores the latest snapshots for all entity types (brokers, banks, broker accounts, bank accounts).
    /// Each snapshot represents the most recent state of an entity and uses the Type field to discriminate between different snapshot types.
    /// Use the Type field and corresponding optional properties to access specific snapshot data.
    /// </summary>
    let Snapshots = new SourceList<OverviewSnapshot>()

    /// <summary>
    /// This function is used to get a broker by its ID.
    /// It searches through the Brokers collection and returns the first broker that matches the provided ID.
    /// </summary>
    let internal getBroker(id: int) =
        Brokers.Items |> Seq.find(fun b -> b.Id = id)

    let internal getBrokerAccount(id: int) =
        Accounts.Items 
        |> Seq.find(fun b -> b.Broker.IsSome && b.Broker.Value.Id = id)
        |> fun b -> b.Broker.Value

    /// <summary>
    /// This function is used to get a bank by its ID.
    /// It searches through the Banks collection and returns the first bank that matches the provided ID.
    /// </summary>
    let internal getBank(id: int) =
        Banks.Items |> Seq.find(fun b -> b.Id = id)

    /// <summary>
    /// This function is used to get a bank by its ID.
    /// It searches through the Banks collection and returns the first bank that matches the provided ID.
    /// </summary>
    let internal getBankAccount(id: int) =
        Accounts.Items 
        |> Seq.find(fun b -> b.Bank.IsSome && b.Bank.Value.Id = id)
        |> fun b -> b.Bank.Value

    /// <summary>
    /// This function is used to update the list of accounts in the UI.
    /// It finds the current account in the list by its ID and replaces it with the updated account information.
    /// </summary>
    let internal updateBankAccount(updated: Account) =
        let current = Accounts.Items |> Seq.find(fun b -> b.Bank.IsSome && b.Bank.Value.Id = updated.Bank.Value.Id)
        Accounts.Replace(current, updated)

    /// <summary>
    /// This function is used to update the list of broker accounts in the UI.
    /// It finds the current broker account in the list by its ID and replaces it with the updated broker account information.
    /// </summary>
    let internal updateBrokerAccount(updated: Account) =
        let current = Accounts.Items |> Seq.find(fun b -> b.Broker.IsSome && b.Broker.Value.Id = updated.Broker.Value.Id)
        Accounts.Replace(current, updated)

    /// <summary>
    /// This function is used to get a currency by its ID. 
    /// </summary>
    /// <param name="id"></param>
    let internal getCurrency(id: int) =
        Currencies.Items |> Seq.find(fun c -> c.Id = id)

    /// <summary>
    /// This function is used to get a currency by its code.
    /// It searches through the Currencies collection and returns the first currency that matches the provided code.
    /// </summary>
    let GetCurrency code = Currencies.Items |> Seq.find(fun c -> c.Code = code)

    /// <summary>
    /// This function is used to update the list of brokers in the UI.
    /// It finds the current broker in the list by its ID and replaces it with the updated broker information.
    /// </summary>
    let internal updateBroker(broker: Broker) =
        let current = Brokers.Items |> Seq.find(fun b -> b.Id = broker.Id)
        Brokers.Replace(current, broker)

    /// <summary>
    /// This function is used to update the list of banks in the UI.
    /// It finds the current bank in the list by its ID and replaces it with the updated bank information.
    /// </summary>
    let internal updateBank(updated: Bank) =
        let current = Banks.Items |> Seq.find(fun b -> b.Id = updated.Id)
        Banks.Replace(current, updated)

    /// <summary>
    /// This function is used to get a ticker by its symbol.
    /// It searches through the Tickers collection and returns the first ticker that matches the provided symbol.
    /// </summary>
    let GetTicker (ticker: string) =
        Tickers.Items |> Seq.find(fun t -> t.Symbol = ticker)

    /// <summary>
    /// This function is used to get a ticker by its ID.
    /// It searches through the Tickers collection and returns the first ticker that matches the provided ID.
    /// </summary>
    let internal getTickerById(id: int) =
        Tickers.Items |> Seq.find(fun t -> t.Id = id)
