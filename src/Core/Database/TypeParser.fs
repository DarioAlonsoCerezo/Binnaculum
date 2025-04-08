namespace Binnaculum.Core.Database

open DatabaseModel
open Binnaculum.Core

module internal TypeParser =
    
    let fromDatabaseToSupportedBroker (value: string) =
        match value with
        | Keys.Broker_IBKR -> SupportedBroker.IBKR
        | Keys.Broker_Tastytrade -> SupportedBroker.Tastytrade
        | _ -> failwith "Unsupported broker"

    let fromSupportedBrokerToDatabase (value: SupportedBroker) =
        match value with
        | SupportedBroker.IBKR -> Keys.Broker_IBKR
        | SupportedBroker.Tastytrade -> Keys.Broker_Tastytrade
    
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
        | OptionCode.BuyToOpen -> "BUY_TO_OPEN"
        | OptionCode.SellToOpen -> "SELL_TO_OPEN"
        | OptionCode.BuyToClose -> "BUY_TO_CLOSE"
        | OptionCode.SellToClose -> "SELL_TO_CLOSE"
        | OptionCode.Expired -> "EXPIRED"
        | OptionCode.Assigned -> "ASSIGNED"

    let fromOptionTypeToDatabase(optionType: OptionType) =
        match optionType with
        | OptionType.Call -> "CALL"
        | OptionType.Put -> "PUT"

    let fromDatabaseToOptionType(optionType: string) =
        match optionType with
        | "CALL" -> OptionType.Call
        | "PUT" -> OptionType.Put
        | _ -> failwith $"Invalid Option Type: {optionType}"

    let fromTradeCodeToDatabase(tradeCode: TradeCode) =
        match tradeCode with
        | TradeCode.BuyToOpen -> "BUY_TO_OPEN"
        | TradeCode.SellToOpen -> "SELL_TO_OPEN"
        | TradeCode.BuyToClose -> "BUY_TO_CLOSE"
        | TradeCode.SellToClose -> "SELL_TO_CLOSE"

    let fromDatabaseToTradeCode(tradeCode: string) =
        match tradeCode with
        | "BUY_TO_OPEN" -> TradeCode.BuyToOpen
        | "SELL_TO_OPEN" -> TradeCode.SellToOpen
        | "BUY_TO_CLOSE" -> TradeCode.BuyToClose
        | "SELL_TO_CLOSE" -> TradeCode.SellToClose
        | _ -> failwith $"Invalid Trade Code: {tradeCode}"

    let fromTradeTypeToDatabase(tradeType: TradeType) =
        match tradeType with
        | TradeType.Long -> "LONG"
        | TradeType.Short -> "SHORT"

    let fromDatabaseToTradeType(tradeType: string) =
        match tradeType with
        | "LONG" -> TradeType.Long
        | "SHORT" -> TradeType.Short
        | _ -> failwith $"Invalid Trade Type: {tradeType}"

    let fromDataseToMovementType(value: string) =
        match value with
        | "DEPOSIT" -> BrokerMovementType.Deposit
        | "WITHDRAWAL" -> BrokerMovementType.Withdrawal
        | "FEE" -> BrokerMovementType.Fee
        | "INTERESTS_GAINED" -> BrokerMovementType.InterestsGained
        | "LENDING" -> BrokerMovementType.Lending
        | "ACAT_MONEY_TRANSFER" -> BrokerMovementType.ACATMoneyTransfer
        | "ACAT_SECURITIES_TRANSFER" -> BrokerMovementType.ACATSecuritiesTransfer
        | "INTERESTS_PAID" -> BrokerMovementType.InterestsPaid
        | "CONVERSION" -> BrokerMovementType.Conversion
        | _ -> failwith $"Invalid Movement Type: {value}"

    let fromMovementTypeToDatabase(value: BrokerMovementType) =
        match value with
        | BrokerMovementType.Deposit -> "DEPOSIT"
        | BrokerMovementType.Withdrawal -> "WITHDRAWAL"
        | BrokerMovementType.Fee -> "FEE"
        | BrokerMovementType.InterestsGained -> "INTERESTS_GAINED"
        | BrokerMovementType.Lending -> "LENDING"
        | BrokerMovementType.ACATMoneyTransfer -> "ACAT_MONEY_TRANSFER"
        | BrokerMovementType.ACATSecuritiesTransfer -> "ACAT_SECURITIES_TRANSFER"
        | BrokerMovementType.InterestsPaid -> "INTERESTS_PAID"
        | BrokerMovementType.Conversion -> "CONVERSION"