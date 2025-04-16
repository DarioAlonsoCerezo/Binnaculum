namespace Binnaculum.Core

open System
open DynamicData

module Models =
    
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

    type TradeCode = 
        | BuyToOpen
        | SellToOpen
        | BuyToClose
        | SellToClose

    type TradeType =
        | Long
        | Short

    type DividendCode =
        | ExDividendDate
        | PayDividendDate

    type MovementType =
        | Deposit
        | Withdrawal
        | Fee
        | InterestsGained
        | Lending
        | ACATMoneyTransfer
        | ACATSecuritiesTransfer
        | InterestsPaid
        | Conversion
        
    type BankAccountMovementType =
        | Balance
        | Interest
        | Fee

    type Broker = {
        Id: int
        Name: string
        Image: string
        SupportedBroker: string
    }

    type BrokerAccountModel = {
        Id: int
        Broker: Broker
        AccountNumber: string
    }
    
    type Currency = {
        Id: int;
        Title: string;
        Code: string;
        Symbol: string;
    }

    type BrokerMovement = {
        Id: int
        TimeStamp: DateTime
        Amount: decimal
        Currency: Currency
        BrokerAccount: BrokerAccountModel
        Commissions: decimal
        Fees: decimal
        MovementType: MovementType
    }
    
    type Ticker = {
        Id: int
        Symbol: string
        Image: string option
        Name: string option
    }

    type TickerSplit = {
        Id: int
        SplitDate: DateTime
        Ticker: Ticker
        SplitFactor: decimal
    }

    type TickerPrice = {
        Id: int
        PriceDate: DateTime
        Ticker: Ticker
        Price: decimal
        Currency: Currency
    }
    
    type Trade = {
        Id: int
        TimeStamp: DateTime
        TotalInvestedAmount: decimal
        Ticker: Ticker
        BrokerAccount: BrokerAccountModel
        Currency: Currency
        Quantity: decimal
        Price: decimal
        Commissions: decimal
        Fees: decimal
        TradeCode: TradeCode
        TradeType: TradeType
        Notes: string option
    }

    type Dividend = {
        Id: int
        TimeStamp: DateTime
        Amount: decimal
        Ticker: Ticker
        Currency: Currency
        BrokerAccount: BrokerAccountModel
    }

    type DividendTax = {
        Id: int
        TimeStamp: DateTime
        Amount: decimal
        Ticker: Ticker
        Currency: Currency
        BrokerAccount: BrokerAccountModel
    }

    type DividendDate = {
        Id: int
        TimeStamp: DateTime
        Amount: decimal
        Ticker: Ticker
        Currency: Currency
        BrokerAccount: BrokerAccountModel
        DividendCode: DividendCode
    }

    type OptionTrade = {
        Id: int
        TimeStamp: DateTime
        ExpirationDate: DateTime
        Premium: decimal
        NetPremium: decimal
        Ticker: Ticker
        BrokerAccount: BrokerAccountModel
        Currency: Currency
        OptionType: OptionType
        Code: OptionCode
        Strike: decimal
        Commissions: decimal
        Fees: decimal
        IsOpen: bool
        ClosedWith: int option
    }

    type Bank = {
        Id: int
        Name: string
        Image: string option
    }

    type BankAccount = {
        Id: int
        Bank: Bank
        Name: string
        Description: string option
        Currency: Currency
    }

    type BankAccountMovement = {
        Id: int
        TimeStamp: DateTime
        Amount: decimal
        BankAccount: BankAccount
        Currency: Currency
        MovementType: BankAccountMovementType
    }

    type Movement =
        | Trade of Trade
        | Dividend of Dividend
        | DividendTax of DividendTax
        | DividendDate of DividendDate
        | OptionTrade of OptionTrade
        | BrokerMovement of BrokerMovement
        | BankAccountMovement of BankAccountMovement
        | TickerSplit of TickerSplit
        | EmptyMovement of string    

    type AccountType = 
        | BrokerAccount of BrokerAccountModel
        | BankAccount of BankAccount
        | EmptyAccount of string

    type OverviewUI = {
        IsDatabaseInitialized: bool 
        TransactionsLoaded: bool
    }