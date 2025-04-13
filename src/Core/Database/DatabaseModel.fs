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
        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt
    
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

    type BrokerMovement = {
        Id: int
        TimeStamp: DateTimePattern
        Amount: Money
        CurrencyId: int
        BrokerAccountId: int
        Commissions: Money
        Fees: Money
        MovementType: BrokerMovementType
        Audit: AuditableEntity
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = BrokerMovementQuery.insert
            member this.UpdateSQL = BrokerMovementQuery.update
            member this.DeleteSQL = BrokerMovementQuery.delete
        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt
    
    type Ticker = {
        Id: int
        Symbol: string
        Image: string option
        Name: string option
        Audit: AuditableEntity
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = TickersQuery.insert
            member this.UpdateSQL = TickersQuery.update
            member this.DeleteSQL = TickersQuery.delete
        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type TickerSplit = {
        Id: int
        SplitDate: DateTimePattern
        TickerId: int
        SplitFactor: decimal
        Audit: AuditableEntity
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = TickerSplitQuery.insert
            member this.UpdateSQL = TickerSplitQuery.update
            member this.DeleteSQL = TickerSplitQuery.delete
        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type TickerPrice = {
        Id: int
        PriceDate: DateTimePattern
        TickerId: int
        Price: Money
        CurrencyId: int
        Audit: AuditableEntity
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = TickerPriceQuery.insert
            member this.UpdateSQL = TickerPriceQuery.update
            member this.DeleteSQL = TickerPriceQuery.delete

        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt
            
    type Trade = {
        Id: int
        TimeStamp: DateTimePattern
        TickerId: int
        BrokerAccountId: int
        CurrencyId: int
        Quantity: decimal
        Price: Money
        Commissions: Money
        Fees: Money
        TradeCode: TradeCode
        TradeType: TradeType
        Notes: string option
        Audit: AuditableEntity
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = TradesQuery.insert
            member this.UpdateSQL = TradesQuery.update
            member this.DeleteSQL = TradesQuery.delete
        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type Dividend = {
        Id: int
        TimeStamp: DateTimePattern
        DividendAmount: Money
        TickerId: int
        CurrencyId: int
        BrokerAccountId: int
        Audit: AuditableEntity
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = DividendsQuery.insert
            member this.UpdateSQL = DividendsQuery.update
            member this.DeleteSQL = DividendsQuery.delete
        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type DividendTax = {
        Id: int
        TimeStamp: DateTimePattern
        DividendTaxAmount: Money
        TickerId: int
        CurrencyId: int
        BrokerAccountId: int
        Audit: AuditableEntity
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = DividendTaxesQuery.insert
            member this.UpdateSQL = DividendTaxesQuery.update
            member this.DeleteSQL = DividendTaxesQuery.delete
        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type DividendDate = {
        Id: int
        TimeStamp: DateTimePattern
        Amount: Money
        TickerId: int
        CurrencyId: int
        BrokerAccountId: int
        DividendCode: DividendCode
        Audit: AuditableEntity
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = DividendDateQuery.insert
            member this.UpdateSQL = DividendDateQuery.update
            member this.DeleteSQL = DividendDateQuery.delete
        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type OptionTrade = {
        Id: int
        TimeStamp: DateTimePattern
        ExpirationDate: DateTimePattern
        Premium: Money
        NetPremium: Money
        TickerId: int
        BrokerAccountId: int
        CurrencyId: int
        OptionType: OptionType
        Code: OptionCode
        Strike: Money
        Commissions: Money
        Fees: Money
        IsOpen: bool
        ClosedWith: int option
        Audit: AuditableEntity
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = OptionsQuery.insert
            member this.UpdateSQL = OptionsQuery.update
            member this.DeleteSQL = OptionsQuery.delete
        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type Bank = {
        Id: int
        Name: string
        Image: string option
        Audit: AuditableEntity
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = BankQuery.insert
            member this.UpdateSQL = BankQuery.update
            member this.DeleteSQL = BankQuery.delete
        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type BankAccount = {
        Id: int
        BankId: int
        Name: string
        Description: string option
        CurrencyId: int
        Audit: AuditableEntity
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = BankAccountsQuery.insert
            member this.UpdateSQL = BankAccountsQuery.update
            member this.DeleteSQL = BankAccountsQuery.delete
        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type BankAccountMovement = {
        Id: int
        TimeStamp: DateTimePattern
        Amount: Money
        BankAccountId: int
        CurrencyId: int
        MovementType: BankAccountMovementType
        Audit: AuditableEntity
    } with
        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = BankAccountMovementsQuery.insert
            member this.UpdateSQL = BankAccountMovementsQuery.update
            member this.DeleteSQL = BankAccountMovementsQuery.delete
        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt