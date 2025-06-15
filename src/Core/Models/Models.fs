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

    type BrokerMovementType =
        | Deposit
        | Withdrawal
        | Fee
        | InterestsGained
        | Lending
        | ACATMoneyTransfer
        | ACATSecuritiesTransfer
        | InterestsPaid
        | Conversion

    type MovementType =
        | Deposit
        | Withdrawal
        | Trade
        | OptionTrade
        | DividendReceived
        | DividendTaxWithheld
        | DividendExDate
        | DividendPayDate
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

    type BrokerAccount = {
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
        BrokerAccount: BrokerAccount
        Commissions: decimal
        Fees: decimal
        MovementType: BrokerMovementType
        Notes: string option
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
        BrokerAccount: BrokerAccount
        Currency: Currency
        Quantity: decimal
        Price: decimal
        Commissions: decimal
        Fees: decimal
        TradeCode: TradeCode
        TradeType: TradeType
        Leveraged: decimal
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
        TaxAmount: decimal
        Ticker: Ticker
        Currency: Currency
        BrokerAccount: BrokerAccount
    }

    type DividendDate = {
        Id: int
        TimeStamp: DateTime
        Amount: decimal
        Ticker: Ticker
        Currency: Currency
        BrokerAccount: BrokerAccount
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
        ClosedWith: int
        Multiplier: decimal
        Quantity: int
        Notes: string option
    }

    type Bank = {
        Id: int
        Name: string
        Image: string option
        CreatedAt: DateTime
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

    // Ticker snapshot model - represents the state of a ticker at a specific point in time
    type TickerSnapshot = {
        Date: DateOnly
        Ticker: Ticker
        Currency: Currency
        TotalShares: decimal
        Weight: decimal
        CostBasis: decimal
        RealCost: decimal
        Dividends: decimal
        Options: decimal
        TotalIncomes: decimal
        Unrealized: decimal
        Realized: decimal
        Performance: decimal
        LatestPrice: decimal
        OpenTrades: bool
    }
    
    // Broker account snapshot - represents the state of a broker account at a specific point in time
    type BrokerAccountSnapshot = {
        Date: DateOnly
        BrokerAccount: BrokerAccount
        PortfolioValue: decimal
        RealizedGains: decimal
        RealizedPercentage: decimal
        UnrealizedGains: decimal
        UnrealizedGainsPercentage: decimal
        Invested: decimal
        Commissions: decimal
        Fees: decimal
        OpenTrades: bool
    }
    
    // Broker snapshot - represents the state of a broker at a specific point in time
    type BrokerSnapshot = {
        Date: DateOnly
        Broker: Broker
        PortfoliosValue: decimal
        RealizedGains: decimal
        RealizedPercentage: decimal
        AccountCount: int
        Invested: decimal
        Commissions: decimal
        Fees: decimal
    }
    
    // Bank account snapshot - represents the state of a bank account at a specific point in time
    type BankAccountSnapshot = {
        Date: DateOnly
        BankAccount: BankAccount
        Balance: decimal
        InterestEarned: decimal
        FeesPaid: decimal
    }
    
    // Bank snapshot - represents the state of a bank at a specific point in time
    type BankSnapshot = {
        Date: DateOnly
        Bank: Bank
        TotalBalance: decimal
        InterestEarned: decimal
        FeesPaid: decimal
        AccountCount: int
    }
    
    // Investment overview snapshot - represents the state of the entire investment portfolio at a specific point in time
    type InvestmentOverviewSnapshot = {
        Date: DateOnly
        PortfoliosValue: decimal
        RealizedGains: decimal
        RealizedPercentage: decimal
        Invested: decimal
        Commissions: decimal
        Fees: decimal
    }

    type OverviewSnapshotType =
        | InvestmentOverview
        | Broker
        | Bank
        | BrokerAccount
        | BankAccount

    type OverviewSnapshot = {
        Type: OverviewSnapshotType
        InvestmentOverview: InvestmentOverviewSnapshot option
        Broker: BrokerSnapshot option
        Bank: BankSnapshot option
        BrokerAccount: BrokerAccountSnapshot option
        BankAccount: BankAccountSnapshot option
    }

    type AccountMovementType =
        | Trade 
        | Dividend 
        | DividendTax 
        | DividendDate 
        | OptionTrade 
        | BrokerMovement 
        | BankAccountMovement 
        | TickerSplit 
        | EmptyMovement    

    type AccountType = 
        | BrokerAccount
        | BankAccount
        | EmptyAccount

    type Account = {
        Type: AccountType
        Broker: BrokerAccount option
        Bank: BankAccount option
        HasMovements: bool
    }

    type Movement = {
        Type: AccountMovementType
        TimeStamp: DateTime
        Trade: Trade option
        Dividend: Dividend option
        DividendTax: DividendTax option
        DividendDate: DividendDate option
        OptionTrade: OptionTrade option
        BrokerMovement: BrokerMovement option
        BankAccountMovement: BankAccountMovement option
        TickerSplit: TickerSplit option
    }

    type OverviewUI = {
        IsDatabaseInitialized: bool 
        TransactionsLoaded: bool
    }

    let emptyMovement() = {
        Type = AccountMovementType.EmptyMovement
        TimeStamp = DateTime.MinValue
        Trade = None
        Dividend = None
        DividendTax = None
        DividendDate = None
        OptionTrade = None
        BrokerMovement = None
        BankAccountMovement = None
        TickerSplit = None
    }