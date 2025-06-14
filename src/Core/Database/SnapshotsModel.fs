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

    type TickerSnapshot = {
        Base: BaseSnapshot
        TickerId: int
        CurrencyId: int
        TotalShares: decimal // Total shares held
        Weight: decimal  // Percentage weight in portfolio
        CostBasis: Money // Cost basis for the ticker
        RealCost: Money // Real cost basis after adjustments
        Dividends: Money // Total dividends received for the ticker
        Options: Money // Total options premiums received
        TotalIncomes: Money // Total income from trades, dividends, etc.
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
        RealizedGains: Money // Cumulative realized gains
        RealizedPercentage: decimal // Percentage of realized gains
        Invested: Money // Total invested amount
        Commissions: Money // Total commissions paid
        Fees: Money // Total fees paid
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
        RealizedGains: Money // Cumulative realized gains across all accounts
        RealizedPercentage: decimal // Percentage of realized gains
        AccountCount: int // Number of accounts
        Invested: Money // Total invested amount
        Commissions: Money // Total commissions paid
        Fees: Money // Total fees paid
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
            member this.InsertSQL = "BankAccountSnapshotQuery.insert"
            member this.UpdateSQL = "BankAccountSnapshotQuery.update"
            member this.DeleteSQL = "BankAccountSnapshotQuery.delete"
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
            member this.InsertSQL = "BankSnapshotQuery.insert"
            member this.UpdateSQL = "BankSnapshotQuery.update"
            member this.DeleteSQL = "BankSnapshotQuery.delete"
        interface IAuditEntity with
            member this.CreatedAt = this.Base.Audit.CreatedAt
            member this.UpdatedAt = this.Base.Audit.UpdatedAt

