namespace Binnaculum.Core.Database

open System
open Binnaculum.Core.Database
open Do
open Binnaculum.Core.SQL
open Binnaculum.Core.Patterns

module internal DatabaseModel =
    type AuditableEntity = {
        CreatedAt: DateTimePattern option
        UpdatedAt: DateTimePattern option
    }

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

    type BankAccountMovementType =
        | Balance
        | Interest
        | Fee

    type Broker = {
        Id: int
        Name: string
        Image: string
        SupportedBroker: SupportedBroker
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = BrokerQuery.insert
            member this.UpdateSQL = BrokerQuery.update
            member this.DeleteSQL = BrokerQuery.delete
            member this.GetAllSQL = BrokerQuery.getAll
            member this.GetByIdSQL = BrokerQuery.getById

    type BrokerAccount = {
        Id: int
        BrokerId: int
        AccountNumber: string
        Audit: AuditableEntity
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = BrokerAccountQuery.insert
            member this.UpdateSQL = BrokerAccountQuery.update
            member this.DeleteSQL = BrokerAccountQuery.delete
            member this.GetAllSQL = BrokerAccountQuery.getAll
            member this.GetByIdSQL = BrokerAccountQuery.getById
    
    type Currency = {
        Id: int;
        Name: string;
        Code: string;
        Symbol: string;
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = CurrencyQuery.insert
            member this.UpdateSQL = CurrencyQuery.update
            member this.DeleteSQL = CurrencyQuery.delete
            member this.GetAllSQL = CurrencyQuery.getAll
            member this.GetByIdSQL = CurrencyQuery.getById

    type BrokerMovement = {
        Id: int
        TimeStamp: DateTimePattern
        Amount: Money
        CurrencyId: int
        BrokerAccountId: int
        Commissions: Money
        Fees: Money
        MovementType: BrokerMovementType
        CreatedAt: DateTimePattern option
        UpdatedAt: DateTimePattern option
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = BrokerMovementQuery.insert
            member this.UpdateSQL = BrokerMovementQuery.update
            member this.DeleteSQL = BrokerMovementQuery.delete
            member this.GetAllSQL = BrokerMovementQuery.getAll
            member this.GetByIdSQL = BrokerMovementQuery.getById
    
    type Ticker = {
        Id: int
        Symbol: string
        Image: string option
        Name: string option
        CreatedAt: DateTimePattern option
        UpdatedAt: DateTimePattern option
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = TickersQuery.insert
            member this.UpdateSQL = TickersQuery.update
            member this.DeleteSQL = TickersQuery.delete
            member this.GetAllSQL = TickersQuery.getAll
            member this.GetByIdSQL = TickersQuery.getById

    type TickerSplit = {
        Id: int
        SplitDate: DateTimePattern
        TickerId: int
        SplitFactor: decimal
        CreatedAt: DateTimePattern option
        UpdatedAt: DateTimePattern option
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = TickerSplitQuery.insert
            member this.UpdateSQL = TickerSplitQuery.update
            member this.DeleteSQL = TickerSplitQuery.delete
            member this.GetAllSQL = TickerSplitQuery.getAll
            member this.GetByIdSQL = TickerSplitQuery.getById

    type TickerPrice = {
        Id: int
        PriceDate: DateTimePattern
        TickerId: int
        Price: Money
        CurrencyId: int
        CreatedAt: DateTimePattern option
        UpdatedAt: DateTimePattern option
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = TickerPriceQuery.insert
            member this.UpdateSQL = TickerPriceQuery.update
            member this.DeleteSQL = TickerPriceQuery.delete
            member this.GetAllSQL = TickerPriceQuery.getAll
            member this.GetByIdSQL = TickerPriceQuery.getById
            
    type Trade = {
        Id: int
        TimeStamp: DateTimePattern
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
        CreatedAt: DateTimePattern option
        UpdatedAt: DateTimePattern option
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = TradesQuery.insert
            member this.UpdateSQL = TradesQuery.update
            member this.DeleteSQL = TradesQuery.delete
            member this.GetAllSQL = TradesQuery.getAll
            member this.GetByIdSQL = TradesQuery.getById

    type Dividend = {
        Id: int
        TimeStamp: DateTime
        DividendAmount: decimal
        TickerId: int
        CurrencyId: int
        BrokerAccountId: int
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = DividendsQuery.insert
            member this.UpdateSQL = DividendsQuery.update
            member this.DeleteSQL = DividendsQuery.delete
            member this.GetAllSQL = DividendsQuery.getAll
            member this.GetByIdSQL = DividendsQuery.getById

    type DividendTax = {
        Id: int
        TimeStamp: DateTime
        Amount: decimal
        TickerId: int
        CurrencyId: int
        BrokerAccountId: int
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = DividendTaxesQuery.insert
            member this.UpdateSQL = DividendTaxesQuery.update
            member this.DeleteSQL = DividendTaxesQuery.delete
            member this.GetAllSQL = DividendTaxesQuery.getAll
            member this.GetByIdSQL = DividendTaxesQuery.getById

    type DividendDate = {
        Id: int
        TimeStamp: DateTime
        Amount: decimal
        TickerId: int
        CurrencyId: int
        BrokerAccountId: int
        DividendCode: DividendCode
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = DividendDateQuery.insert
            member this.UpdateSQL = DividendDateQuery.update
            member this.DeleteSQL = DividendDateQuery.delete
            member this.GetAllSQL = DividendDateQuery.getAll
            member this.GetByIdSQL = DividendDateQuery.getById

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
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = OptionsQuery.insert
            member this.UpdateSQL = OptionsQuery.update
            member this.DeleteSQL = OptionsQuery.delete
            member this.GetAllSQL = OptionsQuery.getAll
            member this.GetByIdSQL = OptionsQuery.getById

    type Bank = {
        Id: int
        Name: string
        Image: string option
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = BankQuery.insert
            member this.UpdateSQL = BankQuery.update
            member this.DeleteSQL = BankQuery.delete
            member this.GetAllSQL = BankQuery.getAll
            member this.GetByIdSQL = BankQuery.getById

    type BankAccount = {
        Id: int
        BankId: int
        Name: string
        Description: string option
        CurrencyId: int
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = BankAccountsQuery.insert
            member this.UpdateSQL = BankAccountsQuery.update
            member this.DeleteSQL = BankAccountsQuery.delete
            member this.GetAllSQL = BankAccountsQuery.getAll
            member this.GetByIdSQL = BankAccountsQuery.getById

    type BankAccountMovement = {
        Id: int
        TimeStamp: DateTime
        Amount: decimal
        BankAccountId: int
        CurrencyId: int
        MovementType: BankAccountMovementType
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = BankAccountMovementsQuery.insert
            member this.UpdateSQL = BankAccountMovementsQuery.update
            member this.DeleteSQL = BankAccountMovementsQuery.delete
            member this.GetAllSQL = BankAccountMovementsQuery.getAll
            member this.GetByIdSQL = BankAccountMovementsQuery.getById