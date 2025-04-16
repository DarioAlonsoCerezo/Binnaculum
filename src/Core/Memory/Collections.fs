namespace Binnaculum.Core.UI

open DynamicData
open Binnaculum.Core.Models
open System.Reactive.Subjects

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
    let OverviewAccounts = new SourceList<Account>()

    /// <summary>
    /// Movements are loaded when Database is connected and rest of the data is loaded.
    /// This should start with a minimum data loaded from the database based on UI interactions
    /// and then be updated with the rest of the data when required
    /// </summary>
    let OverviewMovements = new SourceList<Movement>()

    /// <summary>
    /// This should be filled from UI interactions and populated when navigate to the account details
    /// </summary>
    let AccountDetails = new BehaviorSubject<Account>(Account.EmptyAccount "")

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
    let MovementDetails = new BehaviorSubject<Movement>(Movement.EmptyMovement "")
