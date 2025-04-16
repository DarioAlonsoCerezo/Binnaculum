namespace Binnacle.Core.Storage

open Binnaculum.Core
open Binnaculum.Core.Models

module internal ModelParser =
    let fromDatabaseSupportedBroker (databaseSupportedBroker: Database.DatabaseModel.SupportedBroker) =
        match databaseSupportedBroker with
        | Database.DatabaseModel.SupportedBroker.IBKR -> Keys.Broker_IBKR
        | Database.DatabaseModel.SupportedBroker.Tastytrade -> Keys.Broker_Tastytrade

    let fromDatabaseCurrency (databaseCurrency: Database.DatabaseModel.Currency) =
        { 
            Id = databaseCurrency.Id
            Name = databaseCurrency.Name
            Code = databaseCurrency.Code
            Symbol = databaseCurrency.Symbol
        }

    let fromDatabaseBroker (databaseBroker: Database.DatabaseModel.Broker) =
        { 
            Id = databaseBroker.Id
            Name = databaseBroker.Name
            Image = databaseBroker.Image
            SupportedBroker = fromDatabaseSupportedBroker databaseBroker.SupportedBroker
        }

