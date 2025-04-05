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

    type Broker = {
        Id: int
        Name: string
        Image: string
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

    type Movement = {
        Id: int
        TimeStamp: DateTime
        Amount: decimal
        Currency: Currency
        BrokerAccount: BrokerAccount
        Commissions: decimal
        Fees: decimal
        MovementType: MovementType
    }
    
    type Ticker = {
        Id: int
        Symbol: string
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
        BrokerAccount: BrokerAccount
    }

    type DividendTax = {
        Id: int
        TimeStamp: DateTime
        Amount: decimal
        Ticker: Ticker
        Currency: Currency
        BrokerAccount: BrokerAccount
    }

    type DividendDate = {
        Id: int
        Amount: DateTime
        TimeStamp: DateTime
        Ticker: Ticker
        DividendCode: DividendCode
    }

    type OptionTrade = {
        Id: int
        TimeStamp: DateTime
        ExpirationDate: DateTime
        Premium: decimal
        NetPremium: decimal
        Ticker: Ticker
        BrokerAccount: BrokerAccount
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
        Image: string
    }

    type BankAccount = {
        Id: int
        Bank: Bank
        Name: string
        Description: int
        Currency: Currency
    }

    type BankAccountBalance = {
        Id: int
        TimeStamp: DateTime
        Amount: decimal
        BankAccount: BankAccount
        Currency: Currency
    }

    type BankAccountInterest = {
        Id: int
        TimeStamp: DateTime
        Amount: decimal
        BankAccount: BankAccount
        Currency: Currency
    }

    type BankAccountFee = {
        Id: int
        TimeStamp: DateTime
        Amount: decimal
        BankAccount: BankAccount
        Currency: Currency
    }

    type Transaction = {
        TimeStamp: DateTime
        Amount: decimal
        Image: string
        Description: string
        Currency: Currency
        BrokerAccount: BrokerAccount option
        BankAccount: BankAccount option
        Trade: Trade option
        Dividend: Dividend option
        DividendTax: DividendTax option
        DividendDate: DividendDate option
        OptionTrade: OptionTrade option
        Movement: Movement option
        BankAccountBalance: BankAccountBalance option
        BankAccountInterest: BankAccountInterest option
        BankAccountFee: BankAccountFee option
    }

    //This model should allow us to save and load data quickly until we have the database connected
    //To achive this goal, we should simplify the weight as much as we can
    //Once we have the database connected, this model could load more data
    //and we could use the database to load the data and load dinamically the data we need to show
    //By default, we don't want to load all transactions for a broker or bank at the same time
    //We should delay until the user request to check more in detail and load under demand
    //As this model is subscribed from the UI, we should be careful with the amount of data we load
    type Home = {
        BrokerAccounts: BrokerAccount list
        BankAccounts: BankAccount list
        Transactions: Transaction list
    }