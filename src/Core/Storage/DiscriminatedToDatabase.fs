namespace Binnaculum.Core.Storage

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel

module internal DiscriminatedToDatabase =
    
    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member bankMovementTypeToDatabase(movementType: Binnaculum.Core.Models.BankAccountMovementType) =
            match movementType with
            | Binnaculum.Core.Models.BankAccountMovementType.Balance -> BankAccountMovementType.Balance
            | Binnaculum.Core.Models.BankAccountMovementType.Interest -> BankAccountMovementType.Interest
            | Binnaculum.Core.Models.BankAccountMovementType.Fee -> BankAccountMovementType.Fee

        [<Extension>]
        static member moveventTypeToBrokerMovementTypeDatabase(movementType: Binnaculum.Core.Models.MovementType) =
            match movementType with
            | Binnaculum.Core.Models.MovementType.Deposit -> BrokerMovementType.Deposit
            | Binnaculum.Core.Models.MovementType.Withdrawal -> BrokerMovementType.Withdrawal
            | Binnaculum.Core.Models.MovementType.Fee -> BrokerMovementType.Fee
            | Binnaculum.Core.Models.MovementType.InterestsGained -> BrokerMovementType.InterestsGained
            | Binnaculum.Core.Models.MovementType.Lending -> BrokerMovementType.Lending
            | Binnaculum.Core.Models.MovementType.ACATMoneyTransferSent -> BrokerMovementType.ACATMoneyTransferSent
            | Binnaculum.Core.Models.MovementType.ACATMoneyTransferReceived -> BrokerMovementType.ACATMoneyTransferReceived
            | Binnaculum.Core.Models.MovementType.ACATSecuritiesTransferSent -> BrokerMovementType.ACATSecuritiesTransferSent
            | Binnaculum.Core.Models.MovementType.ACATSecuritiesTransferReceived -> BrokerMovementType.ACATSecuritiesTransferReceived
            | Binnaculum.Core.Models.MovementType.InterestsPaid -> BrokerMovementType.InterestsPaid
            | Binnaculum.Core.Models.MovementType.Conversion -> BrokerMovementType.Conversion
            | _ -> failwithf "MovementType %A is not a BrokerMovementType" movementType

        [<Extension>]
        static member brokerMovementTypeToDatabase(movementType: Binnaculum.Core.Models.BrokerMovementType) =
            match movementType with
            | Binnaculum.Core.Models.BrokerMovementType.Deposit -> BrokerMovementType.Deposit
            | Binnaculum.Core.Models.BrokerMovementType.Withdrawal -> BrokerMovementType.Withdrawal
            | Binnaculum.Core.Models.BrokerMovementType.Fee -> BrokerMovementType.Fee
            | Binnaculum.Core.Models.BrokerMovementType.InterestsGained -> BrokerMovementType.InterestsGained
            | Binnaculum.Core.Models.BrokerMovementType.Lending -> BrokerMovementType.Lending
            | Binnaculum.Core.Models.BrokerMovementType.ACATMoneyTransferSent -> BrokerMovementType.ACATMoneyTransferSent
            | Binnaculum.Core.Models.BrokerMovementType.ACATMoneyTransferReceived -> BrokerMovementType.ACATMoneyTransferReceived
            | Binnaculum.Core.Models.BrokerMovementType.ACATSecuritiesTransferSent -> BrokerMovementType.ACATSecuritiesTransferSent
            | Binnaculum.Core.Models.BrokerMovementType.ACATSecuritiesTransferReceived -> BrokerMovementType.ACATSecuritiesTransferReceived
            | Binnaculum.Core.Models.BrokerMovementType.InterestsPaid -> BrokerMovementType.InterestsPaid
            | Binnaculum.Core.Models.BrokerMovementType.Conversion -> BrokerMovementType.Conversion

        [<Extension>]
        static member tradeCodeToDatabase(tradeCode: Binnaculum.Core.Models.TradeCode) =
            match tradeCode with
            | Binnaculum.Core.Models.TradeCode.BuyToOpen -> TradeCode.BuyToOpen
            | Binnaculum.Core.Models.TradeCode.SellToOpen -> TradeCode.SellToOpen
            | Binnaculum.Core.Models.TradeCode.BuyToClose -> TradeCode.BuyToClose
            | Binnaculum.Core.Models.TradeCode.SellToClose -> TradeCode.SellToClose

        [<Extension>]
        static member tradeTypeToDatabase(tradeType: Binnaculum.Core.Models.TradeType) =
            match tradeType with
            | Binnaculum.Core.Models.TradeType.Long -> TradeType.Long
            | Binnaculum.Core.Models.TradeType.Short -> TradeType.Short

        [<Extension>]
        static member dividendCodeToDatabase(dividendCode: Binnaculum.Core.Models.DividendCode) =
            match dividendCode with
            | Binnaculum.Core.Models.DividendCode.ExDividendDate -> DividendCode.ExDividendDate
            | Binnaculum.Core.Models.DividendCode.PayDividendDate -> DividendCode.PayDividendDate
        
        [<Extension>]
        static member optionTypeToDatabase(optionType: Binnaculum.Core.Models.OptionType) =
            match optionType with
            | Binnaculum.Core.Models.OptionType.Call -> OptionType.Call
            | Binnaculum.Core.Models.OptionType.Put -> OptionType.Put

        [<Extension>]
        static member optionCodeToDatabase(optionCode: Binnaculum.Core.Models.OptionCode) =
            match optionCode with
            | Binnaculum.Core.Models.OptionCode.BuyToOpen -> OptionCode.BuyToOpen
            | Binnaculum.Core.Models.OptionCode.SellToOpen -> OptionCode.SellToOpen
            | Binnaculum.Core.Models.OptionCode.BuyToClose -> OptionCode.BuyToClose
            | Binnaculum.Core.Models.OptionCode.SellToClose -> OptionCode.SellToClose
            | Binnaculum.Core.Models.OptionCode.Assigned -> OptionCode.Assigned
            | Binnaculum.Core.Models.OptionCode.Expired -> OptionCode.Expired
            | Binnaculum.Core.Models.OptionCode.CashSettledAssigned -> OptionCode.CashSettledAssigned