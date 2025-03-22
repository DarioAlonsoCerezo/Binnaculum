namespace Binnaculum.Core

open System

module Models =
    
    type SupportedBroker =
        | IBKR
        | Tastytrade

    type OptionCode =
        | BuyToOpen
        | SellToOpen
        | BuyToClose
        | SellToClose
        | Assigned
        | Expired

    type OptionType =
        | Call
        | Put

    type Broker = {
        Id: int
        Name: string
        SupportedBroker: SupportedBroker
    }

    type BrokerAccount = {
        Id: int
        Broker: Broker
        AccountNumber: string
    }

    type Currency = {
        Id: int;
        Name: string;
        Code: string;
        Symbol: string;
    }
    
    type Ticker = {
        Id: int
        Ticker: string
    }

    type Trade = {
        Id: int
        TimeStamp: DateTime
        TotalInvestedAmount: decimal
        Ticker: Ticker
        BrokerAccount: BrokerAccount
        Currency: Currency
        Quantity: decimal
        Price: decimal
        Commissions: decimal
        Fees: decimal
        IsBuy: bool
    }

    type Option = {
        Id: int
        TimeStamp: string
        ExpirationDate: string
        Premium: string
        NetAmount: string
        Ticker: Ticker
        BrokerAccount: BrokerAccount
        Currency: Currency
        OptionType: OptionType
        Code: OptionCode
        Strike: string
        Commissions: string
        Fees: string
        IsOpen: bool
        ClosedWith: int option
    }