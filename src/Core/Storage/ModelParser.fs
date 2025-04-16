namespace Binnaculum.Core

open Models

module internal ModelParser =
    let fromDatabaseSupportedBroker (databaseSupportedBroker: Database.DatabaseModel.SupportedBroker) =
        match databaseSupportedBroker with
        | Database.DatabaseModel.SupportedBroker.IBKR -> SupportedBroker.IBKR
        | Database.DatabaseModel.SupportedBroker.Tastytrade -> SupportedBroker.Tastytrade

    let toDatabaseSupportedBroker (supportedBroker: Models.SupportedBroker) =
        match supportedBroker with
        | SupportedBroker.IBKR -> Database.DatabaseModel.SupportedBroker.IBKR
        | SupportedBroker.Tastytrade -> Database.DatabaseModel.SupportedBroker.Tastytrade

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

