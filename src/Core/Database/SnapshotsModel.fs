namespace Binnaculum.Core.Database

open Binnaculum.Core.Patterns
open DatabaseModel
open Do
open Binnaculum.Core.SQL

module internal SnapshotsModel =

    type BaseSnapshot = {
        Id: int
        Date: DateTimePattern
        Audit: AuditableEntity
    } with
        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    /// <summary>
    /// Financial snapshot record. Exactly one of broker-level or account-level IDs must be set.
    /// Invariant: Only one of (BrokerId, BrokerSnapshotId) or (BrokerAccountId, BrokerAccountSnapshotId) should be non-default.
    /// </summary>
    type BrokerFinancialSnapshot = {
        Base: BaseSnapshot
        BrokerId: int // Broker ID - Default: -1 (indicates not for specific broker)
        BrokerAccountId: int // Broker Account ID - Default: -1 (indicates not for specific broker account)
        CurrencyId: int
        MovementCounter: int
        BrokerSnapshotId: int // Reference to BrokerSnapshot
        BrokerAccountSnapshotId: int // Reference to BrokerAccountSnapshot
        RealizedGains: Money // Cumulative realized gains - Default currency: USD
        RealizedPercentage: decimal // Percentage of realized gains
        UnrealizedGains: Money // Unrealized gains/losses - Default currency: USD
        UnrealizedGainsPercentage: decimal // Percentage of unrealized gains
        Invested: Money // Total invested amount - Default currency: USD
        Commissions: Money // Total commissions paid - Default currency: USD
        Fees: Money // Total fees paid - Default currency: USD
        Deposited: Money // Total amount deposited - Default currency: USD
        Withdrawn: Money // Total amount withdrawn - Default currency: USD
        DividendsReceived: Money // Total dividends received - Default currency: USD
        OptionsIncome: Money // Total options premiums received - Default currency: USD
        OtherIncome: Money // Total other income - Default currency: USD
        OpenTrades: bool // Whether there are open trades
    } with
        interface IEntity with
            member this.Id = this.Base.Id
            member this.InsertSQL = BrokerFinancialSnapshotQuery.insert
            member this.UpdateSQL = BrokerFinancialSnapshotQuery.update
            member this.DeleteSQL = BrokerFinancialSnapshotQuery.delete
        interface IAuditEntity with
            member this.CreatedAt = this.Base.Audit.CreatedAt
            member this.UpdatedAt = this.Base.Audit.UpdatedAt

    type TickerCurrencySnapshot = {
        Base: BaseSnapshot
        TickerId: int
        CurrencyId: int 
        TickerSnapshotId: int // Reference to TickerSnapshot
        TotalShares: decimal // Total shares held
        Weight: decimal  // Percentage weight in portfolio
        CostBasis: Money // Cost basis for the ticker
        RealCost: Money // Real cost basis after adjustments
        Dividends: Money // Total dividends received for the ticker
        Options: Money // Total options premiums received
        TotalIncomes: Money // Total income from trades, dividends, etc.
        Unrealized: Money // Unrealized gains/losses
        Realized: Money // Realized gains/losses
        Performance: decimal // Performance percentage
        LatestPrice: Money // Latest price of the ticker
        OpenTrades: bool // Whether there are open trades
    } with
        interface IEntity with
            member this.Id = this.Base.Id
            member this.InsertSQL = TickerCurrencySnapshotQuery.insert
            member this.UpdateSQL = TickerCurrencySnapshotQuery.update
            member this.DeleteSQL = TickerCurrencySnapshotQuery.delete
        interface IAuditEntity with
            member this.CreatedAt = this.Base.Audit.CreatedAt
            member this.UpdatedAt = this.Base.Audit.UpdatedAt

    type TickerSnapshot = {
        Base: BaseSnapshot
        TickerId: int
    } with
        interface IEntity with
            member this.Id = this.Base.Id
            member this.InsertSQL = TickerSnapshotQuery.insert
            member this.UpdateSQL = TickerSnapshotQuery.update
            member this.DeleteSQL = TickerSnapshotQuery.delete
        interface IAuditEntity with
            member this.CreatedAt = this.Base.Audit.CreatedAt
            member this.UpdatedAt = this.Base.Audit.UpdatedAt

    type BrokerAccountSnapshot = {
        Base: BaseSnapshot
        BrokerAccountId: int
        PortfolioValue: Money // End-of-day portfolio value
    } with
        interface IEntity with
            member this.Id = this.Base.Id
            member this.InsertSQL = BrokerAccountSnapshotQuery.insert
            member this.UpdateSQL = BrokerAccountSnapshotQuery.update
            member this.DeleteSQL = BrokerAccountSnapshotQuery.delete
        interface IAuditEntity with
            member this.CreatedAt = this.Base.Audit.CreatedAt
            member this.UpdatedAt = this.Base.Audit.UpdatedAt

    type BrokerSnapshot = {
        Base: BaseSnapshot
        BrokerId: int
        PortfoliosValue: Money // End-of-day portfolio value
        AccountCount: int // Number of accounts
    } with
        interface IEntity with
            member this.Id = this.Base.Id
            member this.InsertSQL = BrokerSnapshotQuery.insert
            member this.UpdateSQL = BrokerSnapshotQuery.update
            member this.DeleteSQL = BrokerSnapshotQuery.delete
        interface IAuditEntity with
            member this.CreatedAt = this.Base.Audit.CreatedAt
            member this.UpdatedAt = this.Base.Audit.UpdatedAt

    type BankAccountSnapshot = {
        Base: BaseSnapshot
        BankAccountId: int
        Balance: Money  // End-of-day balance
        CurrencyId: int
        InterestEarned: Money // Interest earned on this date
        FeesPaid: Money // Fees paid on this date
    } with
        interface IEntity with
            member this.Id = this.Base.Id
            member this.InsertSQL = BankAccountSnapshotQuery.insert
            member this.UpdateSQL = BankAccountSnapshotQuery.update
            member this.DeleteSQL = BankAccountSnapshotQuery.delete
        interface IAuditEntity with
            member this.CreatedAt = this.Base.Audit.CreatedAt
            member this.UpdatedAt = this.Base.Audit.UpdatedAt    

    type BankSnapshot = {
        Base: BaseSnapshot
        BankId: int
        TotalBalance: Money // Total balance across all accounts
        InterestEarned: Money // Interest earned across all accounts
        FeesPaid: Money // Fees paid across all accounts
        AccountCount: int // Number of accounts
    } with
        interface IEntity with
            member this.Id = this.Base.Id
            member this.InsertSQL = BankSnapshotQuery.insert
            member this.UpdateSQL = BankSnapshotQuery.update
            member this.DeleteSQL = BankSnapshotQuery.delete
        interface IAuditEntity with
            member this.CreatedAt = this.Base.Audit.CreatedAt
            member this.UpdatedAt = this.Base.Audit.UpdatedAt

    type InvestmentOverviewSnapshot = {
        Base: BaseSnapshot
        PortfoliosValue: Money // End-of-day portfolio value
        RealizedGains: Money // Cumulative realized gains across all accounts
        RealizedPercentage: decimal // Percentage of realized gains
        Invested: Money // Total invested amount
        Commissions: Money // Total commissions paid
        Fees: Money // Total fees paid
    } with
        interface IEntity with
            member this.Id = this.Base.Id
            member this.InsertSQL = InvestmentOverviewSnapshotQuery.insert
            member this.UpdateSQL = InvestmentOverviewSnapshotQuery.update
            member this.DeleteSQL = InvestmentOverviewSnapshotQuery.delete
        interface IAuditEntity with
            member this.CreatedAt = this.Base.Audit.CreatedAt
            member this.UpdatedAt = this.Base.Audit.UpdatedAt