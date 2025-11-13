namespace Binnaculum.Core.Database

open System
open Binnaculum.Core.SQL
open Binnaculum.Core.Patterns
open Do

module internal DatabaseModel =
    type AuditableEntity =
        { CreatedAt: DateTimePattern option
          UpdatedAt: DateTimePattern option }

        static member Default = { CreatedAt = None; UpdatedAt = None }

        static member FromDateTime(dateTime: DateTime) =
            { CreatedAt = Some(DateTimePattern.FromDateTime(dateTime))
              UpdatedAt = None }

    type SupportedBroker =
        | IBKR
        | Tastytrade
        | Unknown

    type OptionCode =
        | BuyToOpen
        | SellToOpen
        | BuyToClose
        | SellToClose
        | Assigned
        | CashSettledAssigned
        | CashSettledExercised
        | Expired
        | Exercised

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
        | DividendReceived
        | DividendTaxWithheld
        | Lending
        | ACATMoneyTransferSent
        | ACATMoneyTransferReceived
        | ACATSecuritiesTransferSent
        | ACATSecuritiesTransferReceived
        | InterestsPaid
        | Conversion

    type BankAccountMovementType =
        | Balance
        | Interest
        | Fee

    type OperationTradeType =
        | StockTrade
        | OptionTrade
        | Dividend
        | DividendTax

    type Broker =
        { Id: int
          Name: string
          Image: string
          SupportedBroker: SupportedBroker }

        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = BrokerQuery.insert
            member this.UpdateSQL = BrokerQuery.update
            member this.DeleteSQL = BrokerQuery.delete

    type BrokerAccount =
        { Id: int
          BrokerId: int
          AccountNumber: string
          Audit: AuditableEntity }

        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = BrokerAccountQuery.insert
            member this.UpdateSQL = BrokerAccountQuery.update
            member this.DeleteSQL = BrokerAccountQuery.delete

        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type Currency =
        { Id: int
          Name: string
          Code: string
          Symbol: string }

        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = CurrencyQuery.insert
            member this.UpdateSQL = CurrencyQuery.update
            member this.DeleteSQL = CurrencyQuery.delete

    type BrokerMovement =
        { Id: int
          TimeStamp: DateTimePattern
          Amount: Money
          CurrencyId: int
          BrokerAccountId: int
          Commissions: Money
          Fees: Money
          MovementType: BrokerMovementType
          Notes: string option
          FromCurrencyId: int option
          AmountChanged: Money option
          TickerId: int option
          Quantity: decimal option
          Audit: AuditableEntity }

        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = BrokerMovementQuery.insert
            member this.UpdateSQL = BrokerMovementQuery.update
            member this.DeleteSQL = BrokerMovementQuery.delete

        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type Ticker =
        { Id: int
          Symbol: string
          Image: string option
          Name: string option
          Audit: AuditableEntity }

        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = TickersQuery.insert
            member this.UpdateSQL = TickersQuery.update
            member this.DeleteSQL = TickersQuery.delete

        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type TickerSplit =
        { Id: int
          SplitDate: DateTimePattern
          TickerId: int
          SplitFactor: decimal
          Audit: AuditableEntity }

        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = TickerSplitQuery.insert
            member this.UpdateSQL = TickerSplitQuery.update
            member this.DeleteSQL = TickerSplitQuery.delete

        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type TickerPrice =
        { Id: int
          PriceDate: DateTimePattern
          TickerId: int
          Price: Money
          CurrencyId: int
          Audit: AuditableEntity }

        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = TickerPriceQuery.insert
            member this.UpdateSQL = TickerPriceQuery.update
            member this.DeleteSQL = TickerPriceQuery.delete

        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type Trade =
        { Id: int
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
          Leveraged: decimal
          Notes: string option
          Audit: AuditableEntity }

        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = TradesQuery.insert
            member this.UpdateSQL = TradesQuery.update
            member this.DeleteSQL = TradesQuery.delete

        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type Dividend =
        { Id: int
          TimeStamp: DateTimePattern
          DividendAmount: Money
          TickerId: int
          CurrencyId: int
          BrokerAccountId: int
          Audit: AuditableEntity }

        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = DividendsQuery.insert
            member this.UpdateSQL = DividendsQuery.update
            member this.DeleteSQL = DividendsQuery.delete

        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type DividendTax =
        { Id: int
          TimeStamp: DateTimePattern
          DividendTaxAmount: Money
          TickerId: int
          CurrencyId: int
          BrokerAccountId: int
          Audit: AuditableEntity }

        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = DividendTaxesQuery.insert
            member this.UpdateSQL = DividendTaxesQuery.update
            member this.DeleteSQL = DividendTaxesQuery.delete

        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type DividendDate =
        { Id: int
          TimeStamp: DateTimePattern
          Amount: Money
          TickerId: int
          CurrencyId: int
          BrokerAccountId: int
          DividendCode: DividendCode
          Audit: AuditableEntity }

        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = DividendDateQuery.insert
            member this.UpdateSQL = DividendDateQuery.update
            member this.DeleteSQL = DividendDateQuery.delete

        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type OptionTrade =
        { Id: int
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
          Multiplier: decimal
          Notes: string option
          Audit: AuditableEntity }

        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = OptionsQuery.insert
            member this.UpdateSQL = OptionsQuery.update
            member this.DeleteSQL = OptionsQuery.delete

        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type Bank =
        { Id: int
          Name: string
          Image: string option
          Audit: AuditableEntity }

        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = BankQuery.insert
            member this.UpdateSQL = BankQuery.update
            member this.DeleteSQL = BankQuery.delete

        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type BankAccount =
        { Id: int
          BankId: int
          Name: string
          Description: string option
          CurrencyId: int
          Audit: AuditableEntity }

        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = BankAccountsQuery.insert
            member this.UpdateSQL = BankAccountsQuery.update
            member this.DeleteSQL = BankAccountsQuery.delete

        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type BankAccountMovement =
        { Id: int
          TimeStamp: DateTimePattern
          Amount: Money
          BankAccountId: int
          CurrencyId: int
          MovementType: BankAccountMovementType
          Audit: AuditableEntity }

        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = BankAccountMovementsQuery.insert
            member this.UpdateSQL = BankAccountMovementsQuery.update
            member this.DeleteSQL = BankAccountMovementsQuery.delete

        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type AutoImportOperation =
        { Id: int
          BrokerAccountId: int
          TickerId: int
          CurrencyId: int
          IsOpen: bool

          // Financial metrics (cumulative, stored for fast aggregation)
          Realized: Money // CUMULATIVE - Total P&L from closed trades
          RealizedToday: Money // DELTA - Daily realized gains for BrokerFinancialSnapshot
          Commissions: Money // Total commissions paid
          Fees: Money // Total fees paid
          Premium: Money // Total option premiums (positive = collected, negative = paid)
          Dividends: Money // Total dividends received
          DividendTaxes: Money // Total dividend taxes withheld
          CapitalDeployed: Money // Total capital tied up in operation
          CapitalDeployedToday: Money // DELTA - Daily capital deployment for position sizing
          Performance: decimal // ROI % = (Realized / CapitalDeployed) * 100

          Audit: AuditableEntity }

        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = AutoImportOperationQuery.insert
            member this.UpdateSQL = AutoImportOperationQuery.update
            member this.DeleteSQL = AutoImportOperationQuery.delete

        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt // Operation open date
            member this.UpdatedAt = this.Audit.UpdatedAt // Operation close date (only set when IsOpen = false)

    type AutoImportOperationTrade =
        { Id: int
          AutoOperationId: int
          TradeType: OperationTradeType
          ReferenceId: int
          Audit: AuditableEntity }

        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = AutoImportOperationTradeQuery.insert
            member this.UpdateSQL = AutoImportOperationTradeQuery.update
            member this.DeleteSQL = AutoImportOperationTradeQuery.delete

        interface IAuditEntity with
            member this.CreatedAt = this.Audit.CreatedAt
            member this.UpdatedAt = this.Audit.UpdatedAt

    type ImportSession =
        { Id: int
          BrokerAccountId: int
          BrokerAccountName: string
          FileName: string
          FilePath: string
          FileHash: string
          State: string
          Phase: string
          TotalChunks: int
          ChunksCompleted: int
          MovementsPersisted: int
          BrokerSnapshotsCalculated: int
          TickerSnapshotsCalculated: int
          MinDate: string
          MaxDate: string
          TotalEstimatedMovements: int
          StartedAt: string
          Phase1CompletedAt: string option
          Phase2StartedAt: string option
          CompletedAt: string option
          LastProgressUpdateAt: string option
          LastError: string option
          Audit: AuditableEntity }

        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = ImportSessionQuery.insert
            member this.UpdateSQL = "" // Custom updates via specific methods
            member this.DeleteSQL = "" // No direct delete - cascade via FK

    type ImportSessionChunk =
        { Id: int
          ImportSessionId: int
          ChunkNumber: int
          StartDate: string
          EndDate: string
          EstimatedMovements: int
          State: string
          ActualMovements: int
          StartedAt: string option
          CompletedAt: string option
          DurationMs: int64 option
          Error: string option
          Audit: AuditableEntity }

        interface IEntity with
            member this.Id = this.Id
            member this.InsertSQL = ImportSessionChunkQuery.insert
            member this.UpdateSQL = "" // Custom updates via specific methods
            member this.DeleteSQL = "" // No direct delete - cascade via FK
