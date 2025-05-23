namespace Binnacle.Core.Storage

open Binnaculum.Core
open Binnaculum.Core.Models
open Binnaculum.Core.UI

module internal ModelParser =
    
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

    let fromBankMovementTypeToDatabase(movementType: BankAccountMovementType) =
        match movementType with
        | BankAccountMovementType.Balance -> Database.DatabaseModel.BankAccountMovementType.Balance
        | BankAccountMovementType.Interest -> Database.DatabaseModel.BankAccountMovementType.Interest
        | BankAccountMovementType.Fee -> Database.DatabaseModel.BankAccountMovementType.Fee
