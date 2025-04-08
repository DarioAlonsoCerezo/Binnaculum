namespace Binnaculum.Core.Database

open System

module internal DatabaseModel =
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

    type Broker = {
        Id: int
        Name: string
        Image: string
        SupportedBroker: SupportedBroker
    }

    type BrokerAccount = {
        Id: int
        BrokerId: int
        AccountNumber: string
    }
    
    type Currency = {
        Id: int;
        Name: string;
        Code: string;
        Symbol: string;
    }

    type BrokerMovement = {
        Id: int
        TimeStamp: DateTime
        Amount: decimal
        CurrencyId: int
        BrokerAccountId: int
        Commissions: decimal
        Fees: decimal
        MovementType: BrokerMovementType
    }
    
    type Ticker = {
        Id: int
        Symbol: string
        Image: string option
    }
    
    type Trade = {
        Id: int
        TimeStamp: DateTime
        TotalInvestedAmount: decimal
        TickerId: int
        BrokerAccountId: int
        CurrencyId: int
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
        TickerId: int
        CurrencyId: int
        BrokerAccountId: int
    }

    type DividendTax = {
        Id: int
        TimeStamp: DateTime
        Amount: decimal
        TickerId: int
        CurrencyId: int
        BrokerAccountId: int
    }

    type DividendDate = {
        Id: int
        Amount: DateTime
        TimeStamp: DateTime
        TickerId: int
        DividendCode: DividendCode
    }

    type OptionTrade = {
        Id: int
        TimeStamp: DateTime
        ExpirationDate: DateTime
        Premium: decimal
        NetPremium: decimal
        TickerId: int
        BrokerAccountId: int
        CurrencyId: int
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
        BankId: int
        Name: string
        Description: int
        CurrencyId: int
    }

    type BankAccountBalance = {
        Id: int
        TimeStamp: DateTime
        Amount: decimal
        BankAccountId: int
        CurrencyId: int
    }

    type BankAccountInterest = {
        Id: int
        TimeStamp: DateTime
        Amount: decimal
        BankAccountId: int
        CurrencyId: int
    }

    type BankAccountFee = {
        Id: int
        TimeStamp: DateTime
        Amount: decimal
        BankAccountId: int
        CurrencyId: int
    }