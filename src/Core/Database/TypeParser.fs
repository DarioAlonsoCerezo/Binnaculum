namespace Binnaculum.Core.Database

open DatabaseModel
open Binnaculum.Core

module internal TypeParser =
    
    let fromDatabaseToSupportedBroker (value: string) =
        match value with
        | Keys.Broker_IBKR -> SupportedBroker.IBKR
        | Keys.Broker_Tastytrade -> SupportedBroker.Tastytrade
        | Keys.Broker_SigmaTrade -> SupportedBroker.SigmaTrade
        | _ -> SupportedBroker.Unknown

    let fromSupportedBrokerToDatabase (value: SupportedBroker) =
        match value with
        | SupportedBroker.IBKR -> Keys.Broker_IBKR
        | SupportedBroker.Tastytrade -> Keys.Broker_Tastytrade
        | SupportedBroker.SigmaTrade -> Keys.Broker_SigmaTrade
        | SupportedBroker.Unknown -> Keys.Broker_Unknown
    
    let fromTextToOptionCode(code: string) =
        match code with
        | "BUY_TO_OPEN" -> OptionCode.BuyToOpen
        | "SELL_TO_OPEN" -> OptionCode.SellToOpen
        | "BUY_TO_CLOSE" -> OptionCode.BuyToClose
        | "SELL_TO_CLOSE" -> OptionCode.SellToClose
        | "O" -> OptionCode.SellToOpen
        | "O;P" -> OptionCode.SellToOpen
        | "C;Ep" -> OptionCode.Expired
        | "A;C" -> OptionCode.Assigned
        | "C" -> OptionCode.BuyToClose
        | "C;P" -> OptionCode.BuyToClose
        | "CP;O" -> OptionCode.SellToOpen
        | "EXPIRED" -> OptionCode.Expired
        | "ASSIGNED" -> OptionCode.Assigned
        | _ -> failwith $"Invalid Option Code: {code}"

    let fromOptionCodeToDatabase(code: OptionCode) =
        match code with
        | OptionCode.BuyToOpen -> SQLConstants.BuyToOpen
        | OptionCode.SellToOpen -> SQLConstants.SellToOpen
        | OptionCode.BuyToClose -> SQLConstants.BuyToClose
        | OptionCode.SellToClose -> SQLConstants.SellToClose
        | OptionCode.Expired -> SQLConstants.Expired
        | OptionCode.Assigned -> SQLConstants.Assigned
        | OptionCode.CashSettledAssigned -> SQLConstants.CashSettledAssigned
        | OptionCode.CashSettledExercised -> SQLConstants.CashSettledExercised
        | OptionCode.Exercised -> SQLConstants.Exercised

    let fromDatabaseToOptionCode(code: string) =
        match code with
        | SQLConstants.BuyToOpen -> OptionCode.BuyToOpen
        | SQLConstants.SellToOpen -> OptionCode.SellToOpen
        | SQLConstants.BuyToClose -> OptionCode.BuyToClose
        | SQLConstants.SellToClose -> OptionCode.SellToClose
        | SQLConstants.Expired -> OptionCode.Expired
        | SQLConstants.Assigned -> OptionCode.Assigned
        | SQLConstants.CashSettledAssigned -> OptionCode.CashSettledAssigned
        | SQLConstants.CashSettledExercised -> OptionCode.CashSettledExercised
        | SQLConstants.Exercised -> OptionCode.Exercised
        | _ -> failwith $"Invalid Option Code: {code}"

    let fromOptionTypeToDatabase(optionType: OptionType) =
        match optionType with
        | OptionType.Call -> SQLConstants.Call
        | OptionType.Put -> SQLConstants.Put 

    let fromDatabaseToOptionType(optionType: string) =
        match optionType with
        | SQLConstants.Call -> OptionType.Call
        | SQLConstants.Put -> OptionType.Put
        | _ -> failwith $"Invalid Option Type: {optionType}"

    let fromTradeCodeToDatabase(tradeCode: TradeCode) =
        match tradeCode with
        | TradeCode.BuyToOpen -> SQLConstants.BuyToOpen
        | TradeCode.SellToOpen -> SQLConstants.SellToOpen
        | TradeCode.BuyToClose -> SQLConstants.BuyToClose
        | TradeCode.SellToClose -> SQLConstants.SellToClose

    let fromDatabaseToTradeCode(tradeCode: string) =
        match tradeCode with
        | SQLConstants.BuyToOpen -> TradeCode.BuyToOpen
        | SQLConstants.SellToOpen -> TradeCode.SellToOpen
        | SQLConstants.BuyToClose -> TradeCode.BuyToClose
        | SQLConstants.SellToClose -> TradeCode.SellToClose
        | _ -> failwith $"Invalid Trade Code: {tradeCode}"

    let fromTradeTypeToDatabase(tradeType: TradeType) =
        match tradeType with
        | TradeType.Long -> SQLConstants.Long
        | TradeType.Short -> SQLConstants.Short

    let fromDatabaseToTradeType(tradeType: string) =
        match tradeType with
        | SQLConstants.Long -> TradeType.Long
        | SQLConstants.Short -> TradeType.Short
        | _ -> failwith $"Invalid Trade Type: {tradeType}"

    let fromDataseToMovementType(value: string) =
        match value with
        | SQLConstants.Deposit -> BrokerMovementType.Deposit
        | SQLConstants.Withdrawal -> BrokerMovementType.Withdrawal
        | SQLConstants.Fee -> BrokerMovementType.Fee
        | SQLConstants.InterestsGained -> BrokerMovementType.InterestsGained
        | SQLConstants.Lending -> BrokerMovementType.Lending
        | SQLConstants.AcatMoneyTransferSent -> BrokerMovementType.ACATMoneyTransferSent
        | SQLConstants.AcatMoneyTransferReceived -> BrokerMovementType.ACATMoneyTransferReceived
        | SQLConstants.AcatSecuritiesTransferSent -> BrokerMovementType.ACATSecuritiesTransferSent
        | SQLConstants.AcatSecuritiesTransferReceived -> BrokerMovementType.ACATSecuritiesTransferReceived
        | SQLConstants.InterestsPaid -> BrokerMovementType.InterestsPaid
        | SQLConstants.Conversion -> BrokerMovementType.Conversion
        | _ -> failwith $"Invalid Movement Type: {value}"

    let fromMovementTypeToDatabase(value: BrokerMovementType) =
        match value with
        | BrokerMovementType.Deposit -> SQLConstants.Deposit
        | BrokerMovementType.Withdrawal -> SQLConstants.Withdrawal
        | BrokerMovementType.Fee -> SQLConstants.Fee
        | BrokerMovementType.InterestsGained -> SQLConstants.InterestsGained
        | BrokerMovementType.Lending -> SQLConstants.Lending
        | BrokerMovementType.ACATMoneyTransferSent -> SQLConstants.AcatMoneyTransferSent
        | BrokerMovementType.ACATMoneyTransferReceived -> SQLConstants.AcatMoneyTransferReceived
        | BrokerMovementType.ACATSecuritiesTransferSent -> SQLConstants.AcatSecuritiesTransferSent
        | BrokerMovementType.ACATSecuritiesTransferReceived -> SQLConstants.AcatSecuritiesTransferReceived
        | BrokerMovementType.InterestsPaid -> SQLConstants.InterestsPaid
        | BrokerMovementType.Conversion -> SQLConstants.Conversion

    let fromDividendDateCodeToDatabase(value: DividendCode) =
        match value with
        | DividendCode.ExDividendDate -> SQLConstants.ExDividendDate
        | DividendCode.PayDividendDate -> SQLConstants.PayDividendDate

    let fromDatabaseToDividendDateCode(value: string) =
        match value with
        | SQLConstants.ExDividendDate -> DividendCode.ExDividendDate
        | SQLConstants.PayDividendDate -> DividendCode.PayDividendDate
        | _ -> failwith $"Invalid Dividend Code: {value}"

    let fromDatabaseToBankMovementType(value: string) =
        match value with
        | SQLConstants.Balance -> BankAccountMovementType.Balance
        | SQLConstants.Interest -> BankAccountMovementType.Interest
        | SQLConstants.Fee -> BankAccountMovementType.Fee
        | _ -> failwith $"Invalid Bank Movement Type: {value}"

    let fromBankMovementTypeToDatabase(value: BankAccountMovementType) =
        match value with
        | BankAccountMovementType.Balance -> SQLConstants.Balance
        | BankAccountMovementType.Interest -> SQLConstants.Interest
        | BankAccountMovementType.Fee -> SQLConstants.Fee