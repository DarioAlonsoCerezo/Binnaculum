namespace Binnacle.Core.Storage

open Binnaculum.Core
open Binnaculum.Core.Models
open Binnaculum.Core.UI

module internal ModelParser =
    let fromDatabaseSupportedBroker (databaseSupportedBroker: Database.DatabaseModel.SupportedBroker) =
        match databaseSupportedBroker with
        | Database.DatabaseModel.SupportedBroker.IBKR -> Keys.Broker_IBKR
        | Database.DatabaseModel.SupportedBroker.Tastytrade -> Keys.Broker_Tastytrade
        | Database.DatabaseModel.SupportedBroker.SigmaTrade -> Keys.Broker_SigmaTrade
        | Database.DatabaseModel.SupportedBroker.Unknown -> Keys.Broker_Unknown

    let fromDatabaseCurrency (databaseCurrency: Database.DatabaseModel.Currency) =
        { 
            Id = databaseCurrency.Id
            Title = databaseCurrency.Name
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

    let fromDatabaseBank (databaseBank: Database.DatabaseModel.Bank) =
        { 
            Id = databaseBank.Id
            Name = databaseBank.Name
            Image = databaseBank.Image
        }

    let fromDatabaseBankAccount (databaseBankAccount: Database.DatabaseModel.BankAccount) =
        let bank = Collections.Banks.Items |> Seq.find (fun b -> b.Id = databaseBankAccount.BankId)
        let currency = Collections.Currencies.Items |> Seq.find (fun c -> c.Id = databaseBankAccount.CurrencyId)
        { 
            Id = databaseBankAccount.Id
            Bank = bank
            Name = databaseBankAccount.Name
            Description = databaseBankAccount.Description
            Currency = currency
        }

    let fromDatabaseBrokerAccount (databaseBrokerAccount: Database.DatabaseModel.BrokerAccount) =
        let broker = Collections.Brokers.Items |> Seq.find (fun b -> b.Id = databaseBrokerAccount.BrokerId)
        { 
            Id = databaseBrokerAccount.Id
            Broker = broker
            AccountNumber = databaseBrokerAccount.AccountNumber
        }

    let fromMovementTypeToBrokerMoveventType(movementType: MovementType) =
        match movementType with
        | MovementType.Deposit -> Database.DatabaseModel.BrokerMovementType.Deposit
        | MovementType.Withdrawal -> Database.DatabaseModel.BrokerMovementType.Withdrawal
        | MovementType.Fee -> Database.DatabaseModel.BrokerMovementType.Fee
        | MovementType.InterestsGained -> Database.DatabaseModel.BrokerMovementType.InterestsGained
        | MovementType.Lending -> Database.DatabaseModel.BrokerMovementType.Lending
        | MovementType.ACATMoneyTransfer -> Database.DatabaseModel.BrokerMovementType.ACATMoneyTransfer
        | MovementType.ACATSecuritiesTransfer -> Database.DatabaseModel.BrokerMovementType.ACATSecuritiesTransfer
        | MovementType.InterestsPaid -> Database.DatabaseModel.BrokerMovementType.InterestsPaid
        | MovementType.Conversion -> Database.DatabaseModel.BrokerMovementType.Conversion
        | _ -> failwithf "MovementType %A is not a BrokerMovementType" movementType

    let fromDatabaseMovementTypeToBrokerMovementType(movementType: Database.DatabaseModel.BrokerMovementType) =
        match movementType with
        | Database.DatabaseModel.BrokerMovementType.Deposit -> BrokerMovementType.Deposit
        | Database.DatabaseModel.BrokerMovementType.Withdrawal -> BrokerMovementType.Withdrawal
        | Database.DatabaseModel.BrokerMovementType.Fee -> BrokerMovementType.Fee
        | Database.DatabaseModel.BrokerMovementType.InterestsGained -> BrokerMovementType.InterestsGained
        | Database.DatabaseModel.BrokerMovementType.Lending -> BrokerMovementType.Lending
        | Database.DatabaseModel.BrokerMovementType.ACATMoneyTransfer -> BrokerMovementType.ACATMoneyTransfer
        | Database.DatabaseModel.BrokerMovementType.ACATSecuritiesTransfer -> BrokerMovementType.ACATSecuritiesTransfer
        | Database.DatabaseModel.BrokerMovementType.InterestsPaid -> BrokerMovementType.InterestsPaid
        | Database.DatabaseModel.BrokerMovementType.Conversion -> BrokerMovementType.Conversion
        //| _ -> failwithf "MovementType %A is not a BrokerMovementType" movementType

    let fromBrokerMovementToMovement(movement: Database.DatabaseModel.BrokerMovement) =
        let currency = Collections.Currencies.Items |> Seq.find(fun c -> c.Id = movement.CurrencyId)
        let account = 
            Collections.Accounts.Items 
            |> Seq.filter(fun a -> a.Broker.IsSome ) 
            |> Seq.find(fun c -> c.Broker.Value.Id = movement.BrokerAccountId)
        let brokerAccount = account.Broker.Value
        
        let brokerMovement =
            {
                Id = movement.Id
                TimeStamp = movement.TimeStamp.Value
                Amount = movement.Amount.Value
                Currency = currency
                BrokerAccount = brokerAccount
                Commissions = movement.Commissions.Value
                Fees = movement.Fees.Value
                MovementType = fromDatabaseMovementTypeToBrokerMovementType movement.MovementType
            }
        {
            Type = AccountMovementType.BrokerMovement
            Trade = None
            Dividend = None
            DividendTax = None
            DividendDate = None
            OptionTrade = None
            BrokerMovement = Some brokerMovement
            BankAccountMovement = None
            TickerSplit = None
        }