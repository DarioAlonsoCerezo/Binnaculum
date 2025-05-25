namespace Binnaculum.Core.Storage

open System.Runtime.CompilerServices
open Binnaculum.Core.Models
open Binnaculum.Core

module internal DiscriminatedToModel =
    
    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member bankMovementTypeToModel(movementType: Binnaculum.Core.Database.DatabaseModel.BankAccountMovementType) =
            match movementType with
            | Binnaculum.Core.Database.DatabaseModel.BankAccountMovementType.Balance -> BankAccountMovementType.Balance
            | Binnaculum.Core.Database.DatabaseModel.BankAccountMovementType.Interest -> BankAccountMovementType.Interest
            | Binnaculum.Core.Database.DatabaseModel.BankAccountMovementType.Fee -> BankAccountMovementType.Fee

        [<Extension>]
        static member supportedBrokerToModel(databaseSupportedBroker: Binnaculum.Core.Database.DatabaseModel.SupportedBroker) =
            match databaseSupportedBroker with
            | Binnaculum.Core.Database.DatabaseModel.SupportedBroker.IBKR -> Keys.Broker_IBKR
            | Binnaculum.Core.Database.DatabaseModel.SupportedBroker.Tastytrade -> Keys.Broker_Tastytrade
            | Binnaculum.Core.Database.DatabaseModel.SupportedBroker.SigmaTrade -> Keys.Broker_SigmaTrade
            | Binnaculum.Core.Database.DatabaseModel.SupportedBroker.Unknown -> Keys.Broker_Unknown


        [<Extension>]
        static member brokerMovementTypeToModel(movementType: Binnaculum.Core.Database.DatabaseModel.BrokerMovementType) =
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
            
        [<Extension>]
        static member databaseToTradeCode(tradeCode: Binnaculum.Core.Database.DatabaseModel.TradeCode) =
            match tradeCode with
            | Database.DatabaseModel.TradeCode.BuyToOpen -> TradeCode.BuyToOpen
            | Database.DatabaseModel.TradeCode.SellToOpen -> TradeCode.SellToOpen
            | Database.DatabaseModel.TradeCode.BuyToClose -> TradeCode.BuyToClose
            | Database.DatabaseModel.TradeCode.SellToClose -> TradeCode.SellToClose

        [<Extension>]
        static member databaseToTradeType(tradeType: Binnaculum.Core.Database.DatabaseModel.TradeType) =
            match tradeType with
            | Database.DatabaseModel.TradeType.Long -> TradeType.Long
            | Database.DatabaseModel.TradeType.Short -> TradeType.Short

        [<Extension>]
        static member databaseToDividendCode(dividendCode: Binnaculum.Core.Database.DatabaseModel.DividendCode) =
            match dividendCode with
            | Database.DatabaseModel.DividendCode.ExDividendDate -> DividendCode.ExDividendDate
            | Database.DatabaseModel.DividendCode.PayDividendDate -> DividendCode.PayDividendDate