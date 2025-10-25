namespace Binnaculum.Core

open System
open System.Runtime.CompilerServices
open System.Reactive.Linq
open Binnaculum.Core.Models
open Binnaculum.Core.UI
open DiscriminatedToModel
open Microsoft.Maui.Storage
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Providers
open Binnaculum.Core.Keys
open Binnaculum.Core.Logging

module internal DatabaseToModels =

    [<Extension>]
    type Do() =

        [<Extension>]
        static member bankToModel(bank: Binnaculum.Core.Database.DatabaseModel.Bank) =
            { Id = bank.Id
              Name = bank.Name
              Image = bank.Image
              CreatedAt =
                match bank.Audit.CreatedAt with
                | Some createdAt -> createdAt.Value
                | None -> System.DateTime.Now }

        [<Extension>]
        static member banksToModel(banks: Binnaculum.Core.Database.DatabaseModel.Bank list) =
            banks |> List.map (fun b -> b.bankToModel ())

        [<Extension>]
        static member currencyToModel(currency: Binnaculum.Core.Database.DatabaseModel.Currency) =
            { Id = currency.Id
              Title = currency.Name
              Code = currency.Code
              Symbol = currency.Symbol }

        [<Extension>]
        static member currenciesToModel(currencies: Binnaculum.Core.Database.DatabaseModel.Currency list) =
            currencies |> List.map (fun c -> c.currencyToModel ())

        [<Extension>]
        static member bankAccountToModel(bankAccount: Binnaculum.Core.Database.DatabaseModel.BankAccount) =
            { Id = bankAccount.Id
              Bank = bankAccount.BankId.ToFastBankById()
              Name = bankAccount.Name
              Description = bankAccount.Description
              Currency = bankAccount.CurrencyId.ToFastCurrencyById() }

        [<Extension>]
        static member bankAccountsToModel(bankAccounts: Binnaculum.Core.Database.DatabaseModel.BankAccount list) =
            bankAccounts |> List.map (fun b -> b.bankAccountToModel ())

        [<Extension>]
        static member bankAccountToMovement(movement: Binnaculum.Core.Database.DatabaseModel.BankAccountMovement) =
            let bankMovement =
                { Id = movement.Id
                  TimeStamp = movement.TimeStamp.Value
                  Amount = movement.Amount.Value
                  Currency = movement.CurrencyId.ToFastCurrencyById()
                  BankAccount = movement.BankAccountId.ToFastBankAccountById()
                  MovementType = movement.MovementType.bankMovementTypeToModel () }

            { Type = AccountMovementType.BankAccountMovement
              TimeStamp = bankMovement.TimeStamp
              Trade = None
              Dividend = None
              DividendTax = None
              DividendDate = None
              OptionTrade = None
              BrokerMovement = None
              BankAccountMovement = Some bankMovement
              TickerSplit = None }

        [<Extension>]
        static member bankAccountMovementsToMovements
            (movements: Binnaculum.Core.Database.DatabaseModel.BankAccountMovement list)
            =
            movements |> List.map (fun m -> m.bankAccountToMovement ())

        [<Extension>]
        static member brokerToModel(broker: Binnaculum.Core.Database.DatabaseModel.Broker) =
            { Id = broker.Id
              Name = broker.Name
              Image = broker.Image
              SupportedBroker = broker.SupportedBroker.supportedBrokerToModel () }

        [<Extension>]
        static member brokersToModel(brokers: Binnaculum.Core.Database.DatabaseModel.Broker list) =
            brokers |> List.map (fun b -> b.brokerToModel ())

        [<Extension>]
        static member brokerAccountToModel(brokerAccount: Binnaculum.Core.Database.DatabaseModel.BrokerAccount) =
            { Id = brokerAccount.Id
              Broker = brokerAccount.BrokerId.ToFastBrokerById()
              AccountNumber = brokerAccount.AccountNumber }

        [<Extension>]
        static member brokerAccountsToModel(brokerAccounts: Binnaculum.Core.Database.DatabaseModel.BrokerAccount list) =
            brokerAccounts |> List.map (fun b -> b.brokerAccountToModel ())

        [<Extension>]
        static member brokerMovementToModel(movement: Binnaculum.Core.Database.DatabaseModel.BrokerMovement) =
            let brokerAccount = movement.BrokerAccountId.ToFastBrokerAccountById()
            let currency = movement.CurrencyId.ToFastCurrencyById()

            let fromCurrency =
                movement.FromCurrencyId |> Option.map (fun id -> id.ToFastCurrencyById())

            let ticker = movement.TickerId |> Option.map (fun id -> id.ToFastTickerById())

            let brokerMovement =
                { Id = movement.Id
                  TimeStamp = movement.TimeStamp.Value
                  Amount = movement.Amount.Value
                  Currency = currency
                  BrokerAccount = brokerAccount
                  Commissions = movement.Commissions.Value
                  Fees = movement.Fees.Value
                  MovementType = movement.MovementType.brokerMovementTypeToModel ()
                  Notes = movement.Notes
                  FromCurrency = fromCurrency
                  AmountChanged = movement.AmountChanged |> Option.map (fun m -> m.Value)
                  Ticker = ticker
                  Quantity = movement.Quantity }

            { Type = AccountMovementType.BrokerMovement
              TimeStamp = brokerMovement.TimeStamp
              Trade = None
              Dividend = None
              DividendTax = None
              DividendDate = None
              OptionTrade = None
              BrokerMovement = Some brokerMovement
              BankAccountMovement = None
              TickerSplit = None }

        [<Extension>]
        static member brokerMovementsToModel(movements: Binnaculum.Core.Database.DatabaseModel.BrokerMovement list) =
            movements |> List.map (fun m -> m.brokerMovementToModel ())

        [<Extension>]
        static member tickerToModel(ticker: Binnaculum.Core.Database.DatabaseModel.Ticker) =
            { Id = ticker.Id
              Symbol = ticker.Symbol
              Image = ticker.Image
              Name = ticker.Name }

        [<Extension>]
        static member tickersToModel(tikers: Binnaculum.Core.Database.DatabaseModel.Ticker list) =
            tikers |> List.map (fun t -> t.tickerToModel ())

        [<Extension>]
        static member tickerSnapshotToModel(dbSnapshot: TickerSnapshot) =
            { Id = dbSnapshot.Base.Id
              Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
              Ticker = dbSnapshot.TickerId.ToFastTickerById()
              MainCurrency =
                { Id = 0
                  Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                  Ticker = dbSnapshot.TickerId.ToFastTickerById()
                  Currency = "USD".ToFastCurrency() // Updated to use fast O(1) lookup
                  TotalShares = 0m // Default value, not used in this context
                  Weight = 0.0m // Default value, not used in this context
                  CostBasis = 0.0m // Default value, not used in this context
                  RealCost = 0.0m // Default value, not used in this context
                  Dividends = 0.0m // Default value, not used in this context
                  DividendTaxes = 0.0m // Default value, not used in this context
                  Options = 0.0m // Default value, not used in this context
                  TotalIncomes = 0.0m // Default value, not used in this context
                  Unrealized = 0.0m // Default value, not used in this context
                  Realized = 0.0m // Default value, not used in this context
                  Performance = 0.0m // Default value, not used in this context
                  LatestPrice = 0.0m // Default value, not used in this context
                  OpenTrades = false // Default value, not used in this context
                  Commissions = 0.0m // Default value, not used in this context
                  Fees = 0.0m // Default value, not used in this context
                }
              OtherCurrencies = [] }

        [<Extension>]
        static member tickerSnapshotsToModel(dbSnapshots: TickerSnapshot list) =
            dbSnapshots |> List.map (fun s -> s.tickerSnapshotToModel ())

        [<Extension>]
        static member autoImportOperationToModel
            (dbOperation: Binnaculum.Core.Database.DatabaseModel.AutoImportOperation)
            =
            { Id = dbOperation.Id
              BrokerAccount = dbOperation.BrokerAccountId.ToFastBrokerAccountById()
              Ticker = dbOperation.TickerId.ToFastTickerById()
              Currency = dbOperation.CurrencyId.ToFastCurrencyById()
              IsOpen = dbOperation.IsOpen
              OpenDate =
                match dbOperation.Audit.CreatedAt with
                | Some dt -> dt.Value
                | None -> DateTime.Now
              CloseDate =
                match dbOperation.Audit.UpdatedAt with
                | Some dt when not dbOperation.IsOpen -> Some dt.Value
                | _ -> None
              Realized = dbOperation.Realized.Value
              RealizedToday = dbOperation.RealizedToday.Value
              Commissions = dbOperation.Commissions.Value
              Fees = dbOperation.Fees.Value
              Premium = dbOperation.Premium.Value
              Dividends = dbOperation.Dividends.Value
              DividendTaxes = dbOperation.DividendTaxes.Value
              CapitalDeployed = dbOperation.CapitalDeployed.Value
              CapitalDeployedToday = dbOperation.CapitalDeployedToday.Value
              Performance = dbOperation.Performance }

        [<Extension>]
        static member autoImportOperationsToModel
            (dbOperations: Binnaculum.Core.Database.DatabaseModel.AutoImportOperation list)
            =
            dbOperations |> List.map (fun op -> op.autoImportOperationToModel ())

        [<Extension>]
        static member tradeToModel(trade: Binnaculum.Core.Database.DatabaseModel.Trade) =
            let amount = trade.Price.Value * trade.Quantity
            let commissions = trade.Fees.Value + trade.Commissions.Value
            let totalInvestedAmount = amount + commissions

            { Id = trade.Id
              TimeStamp = trade.TimeStamp.Value
              TotalInvestedAmount = totalInvestedAmount
              Ticker = trade.TickerId.ToFastTickerById()
              BrokerAccount = trade.BrokerAccountId.ToFastBrokerAccountById()
              Currency = trade.CurrencyId.ToFastCurrencyById()
              Quantity = trade.Quantity
              Price = trade.Price.Value
              Commissions = trade.Commissions.Value
              Fees = trade.Fees.Value
              TradeCode = trade.TradeCode.databaseToTradeCode ()
              TradeType = trade.TradeType.databaseToTradeType ()
              Leveraged = trade.Leveraged
              Notes = trade.Notes }

        [<Extension>]
        static member tradesToModel(trades: Binnaculum.Core.Database.DatabaseModel.Trade list) =
            trades |> List.map (fun t -> t.tradeToModel ())

        [<Extension>]
        static member tradeToMovement(trade: Binnaculum.Core.Database.DatabaseModel.Trade) =
            let tradeMovement = trade.tradeToModel ()

            { Type = AccountMovementType.Trade
              TimeStamp = tradeMovement.TimeStamp
              Trade = Some tradeMovement
              Dividend = None
              DividendTax = None
              DividendDate = None
              OptionTrade = None
              BrokerMovement = None
              BankAccountMovement = None
              TickerSplit = None }

        [<Extension>]
        static member tradesToMovements(trades: Binnaculum.Core.Database.DatabaseModel.Trade list) =
            trades |> List.map (fun t -> t.tradeToMovement ())

        [<Extension>]
        static member dividendReceivedToModel(dividend: Binnaculum.Core.Database.DatabaseModel.Dividend) =
            { Id = dividend.Id
              TimeStamp = dividend.TimeStamp.Value
              Amount = dividend.DividendAmount.Value
              Ticker = dividend.TickerId.ToFastTickerById()
              Currency = dividend.CurrencyId.ToFastCurrencyById()
              BrokerAccount = dividend.BrokerAccountId.ToFastBrokerAccountById() }

        [<Extension>]
        static member dividendsReceivedToModel(dividends: Binnaculum.Core.Database.DatabaseModel.Dividend list) =
            dividends |> List.map (fun d -> d.dividendReceivedToModel ())

        [<Extension>]
        static member dividendReceivedToMovement(dividend: Binnaculum.Core.Database.DatabaseModel.Dividend) =
            let model = dividend.dividendReceivedToModel ()

            { Type = AccountMovementType.Dividend
              TimeStamp = model.TimeStamp
              Trade = None
              Dividend = Some model
              DividendTax = None
              DividendDate = None
              OptionTrade = None
              BrokerMovement = None
              BankAccountMovement = None
              TickerSplit = None }

        [<Extension>]
        static member dividendsReceivedToMovements(dividends: Binnaculum.Core.Database.DatabaseModel.Dividend list) =
            dividends |> List.map (fun d -> d.dividendReceivedToMovement ())

        [<Extension>]
        static member dividendTaxToModel(dividend: Binnaculum.Core.Database.DatabaseModel.DividendTax) =
            { Id = dividend.Id
              TimeStamp = dividend.TimeStamp.Value
              TaxAmount = dividend.DividendTaxAmount.Value
              Ticker = dividend.TickerId.ToFastTickerById()
              Currency = dividend.CurrencyId.ToFastCurrencyById()
              BrokerAccount = dividend.BrokerAccountId.ToFastBrokerAccountById() }

        [<Extension>]
        static member dividendTaxesToModel(dividends: Binnaculum.Core.Database.DatabaseModel.DividendTax list) =
            dividends |> List.map (fun d -> d.dividendTaxToModel ())

        [<Extension>]
        static member dividendTaxToMovement(dividend: Binnaculum.Core.Database.DatabaseModel.DividendTax) =
            let model = dividend.dividendTaxToModel ()

            { Type = AccountMovementType.DividendTax
              TimeStamp = model.TimeStamp
              Trade = None
              Dividend = None
              DividendTax = Some model
              DividendDate = None
              OptionTrade = None
              BrokerMovement = None
              BankAccountMovement = None
              TickerSplit = None }

        [<Extension>]
        static member dividendTaxesToMovements(dividends: Binnaculum.Core.Database.DatabaseModel.DividendTax list) =
            dividends |> List.map (fun d -> d.dividendTaxToMovement ())

        [<Extension>]
        static member dividendDateToModel(dividend: Binnaculum.Core.Database.DatabaseModel.DividendDate) =
            { Id = dividend.Id
              TimeStamp = dividend.TimeStamp.Value
              Amount = dividend.Amount.Value
              Ticker = dividend.TickerId.ToFastTickerById()
              Currency = dividend.CurrencyId.ToFastCurrencyById()
              BrokerAccount = dividend.BrokerAccountId.ToFastBrokerAccountById()
              DividendCode = dividend.DividendCode.databaseToDividendCode () }

        [<Extension>]
        static member dividendDatesToModel(dividends: Binnaculum.Core.Database.DatabaseModel.DividendDate list) =
            dividends |> List.map (fun d -> d.dividendDateToModel ())

        [<Extension>]
        static member dividendDateToMovement(dividend: Binnaculum.Core.Database.DatabaseModel.DividendDate) =
            let model = dividend.dividendDateToModel ()

            { Type = AccountMovementType.DividendDate
              TimeStamp = model.TimeStamp
              Trade = None
              Dividend = None
              DividendTax = None
              DividendDate = Some model
              OptionTrade = None
              BrokerMovement = None
              BankAccountMovement = None
              TickerSplit = None }

        [<Extension>]
        static member dividendDatesToMovements(dividends: Binnaculum.Core.Database.DatabaseModel.DividendDate list) =
            dividends |> List.map (fun d -> d.dividendDateToMovement ())

        [<Extension>]
        static member optionTradeToModel(optionTrade: Binnaculum.Core.Database.DatabaseModel.OptionTrade) =
            { Id = optionTrade.Id
              TimeStamp = optionTrade.TimeStamp.Value
              ExpirationDate = optionTrade.ExpirationDate.Value
              Premium = optionTrade.Premium.Value
              NetPremium = optionTrade.NetPremium.Value
              Ticker = optionTrade.TickerId.ToFastTickerById()
              BrokerAccount = optionTrade.BrokerAccountId.ToFastBrokerAccountById()
              Currency = optionTrade.CurrencyId.ToFastCurrencyById()
              OptionType = optionTrade.OptionType.databaseToOptionType ()
              Code = optionTrade.Code.databaseToOptionCode ()
              Strike = optionTrade.Strike.Value
              Commissions = optionTrade.Commissions.Value
              Fees = optionTrade.Fees.Value
              IsOpen = optionTrade.IsOpen
              ClosedWith =
                match optionTrade.ClosedWith with
                | Some c -> c
                | None -> 0
              Multiplier = optionTrade.Multiplier
              Quantity = 1
              Notes = optionTrade.Notes
              FeesPerOperation = false }

        [<Extension>]
        static member optionTradesToModel(optionTrades: Binnaculum.Core.Database.DatabaseModel.OptionTrade list) =
            optionTrades |> List.map (fun o -> o.optionTradeToModel ())

        [<Extension>]
        static member optionTradesToMovements(optionTrades: Binnaculum.Core.Database.DatabaseModel.OptionTrade list) =
            // First convert all trades to models
            let optionTradeModels = optionTrades |> List.map (fun o -> o.optionTradeToModel ())

            let groupOptions = PreferencesProvider.getBoolean GroupOptionsKey true

            if groupOptions then
                // Group trades by key characteristics (ticker ID, option type, strike price, expiration date, option code, trade date)
                // This groups multiple contracts from the same trade (same day, same action) while keeping:
                // - Opening and closing trades separate
                // - Trades from different days separate
                // - Different trade types separate (BuyToOpen vs SellToOpen, etc.)
                let groupedTrades =
                    optionTradeModels
                    |> List.groupBy (fun trade ->
                        (trade.Ticker.Id,
                         trade.OptionType,
                         decimal trade.Strike,
                         trade.ExpirationDate.Date,
                         trade.Code,
                         trade.TimeStamp.Date))
                    |> List.map (fun ((tickerId, optionType, strike, expiration, optionCode, tradeDate), trades) ->
                        // Get first trade from group to use as template
                        let representative = List.head trades

                        // Calculate total quantity and net premium across all trades in the group
                        let totalQuantity = trades |> List.sumBy (fun t -> t.Quantity)
                        let totalNetPremium = trades |> List.sumBy (fun t -> t.NetPremium)

                        // Create an updated model with the combined values
                        let combinedTrade =
                            { representative with
                                NetPremium = totalNetPremium
                                Quantity = totalQuantity }

                        // Create movement for the combined trade
                        { Type = AccountMovementType.OptionTrade
                          TimeStamp = combinedTrade.TimeStamp
                          Trade = None
                          Dividend = None
                          DividendTax = None
                          DividendDate = None
                          OptionTrade = Some combinedTrade
                          BrokerMovement = None
                          BankAccountMovement = None
                          TickerSplit = None })

                groupedTrades

            else
                optionTradeModels
                |> List.map (fun optionTrade ->
                    { Type = AccountMovementType.OptionTrade
                      TimeStamp = optionTrade.TimeStamp
                      Trade = None
                      Dividend = None
                      DividendTax = None
                      DividendDate = None
                      OptionTrade = Some optionTrade
                      BrokerMovement = None
                      BankAccountMovement = None
                      TickerSplit = None })

        // Snapshot conversion functions (backward compatible)
        [<Extension>]
        static member brokerSnapshotToOverviewSnapshot(dbSnapshot: BrokerSnapshot, broker: Broker) =
            dbSnapshot.brokerSnapshotToOverviewSnapshot (broker, [])

        [<Extension>]
        static member brokerSnapshotToOverviewSnapshot
            (dbSnapshot: BrokerSnapshot, broker: Broker, financialSnapshots: BrokerFinancialSnapshot list)
            =
            let (mainFinancial, otherFinancials) =
                if financialSnapshots.IsEmpty then
                    // Create empty financial snapshot if no data available
                    let emptySnapshot =
                        { Id = 0
                          Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                          Broker = None // Default value indicating not for specific broker
                          BrokerAccount = None // Default value indicating not for specific broker account
                          Currency = (1).ToFastCurrencyById() // USD as default currency
                          MovementCounter = 0
                          RealizedGains = 0.0m
                          RealizedPercentage = 0.0m
                          UnrealizedGains = 0.0m
                          UnrealizedGainsPercentage = 0.0m
                          Invested = 0.0m
                          Commissions = 0.0m
                          Fees = 0.0m
                          Deposited = 0.0m
                          Withdrawn = 0.0m
                          DividendsReceived = 0.0m
                          OptionsIncome = 0.0m
                          OtherIncome = 0.0m
                          OpenTrades = false
                          NetCashFlow = 0.0m // Empty snapshot has no cash flow
                        }

                    (emptySnapshot, [])
                else
                    // Convert database snapshots to model snapshots using helper
                    let modelSnapshots =
                        financialSnapshots
                        |> List.map (fun dbFinancial -> dbFinancial.brokerFinancialSnapshotToModel ())

                    // Find the snapshot with the highest MovementCounter
                    let sortedSnapshots =
                        modelSnapshots |> List.sortByDescending (fun s -> s.MovementCounter)

                    match sortedSnapshots with
                    | head :: tail -> (head, tail)
                    | [] ->
                        // This shouldn't happen since we checked for empty list above, but handle it gracefully
                        let emptySnapshot =
                            { Id = 0
                              Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                              Broker = None // Default value indicating not for specific broker
                              BrokerAccount = None // Default value indicating not for specific broker account
                              Currency = (1).ToFastCurrencyById() // USD as default currency
                              MovementCounter = 0
                              RealizedGains = 0.0m
                              RealizedPercentage = 0.0m
                              UnrealizedGains = 0.0m
                              UnrealizedGainsPercentage = 0.0m
                              Invested = 0.0m
                              Commissions = 0.0m
                              Fees = 0.0m
                              Deposited = 0.0m
                              Withdrawn = 0.0m
                              DividendsReceived = 0.0m
                              OptionsIncome = 0.0m
                              OtherIncome = 0.0m
                              OpenTrades = false
                              NetCashFlow = 0.0m // Empty snapshot has no cash flow
                            }

                        (emptySnapshot, [])

            { Type = OverviewSnapshotType.Broker
              InvestmentOverview = None
              Broker =
                Some
                    { Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                      Broker = broker
                      PortfoliosValue = 0m // TODO: Calculate from currency snapshots at runtime
                      AccountCount = dbSnapshot.AccountCount
                      Financial = mainFinancial
                      FinancialOtherCurrencies = otherFinancials }
              Bank = None
              BrokerAccount = None
              BankAccount = None }

        [<Extension>]
        static member bankSnapshotToOverviewSnapshot(dbSnapshot: BankSnapshot, bank: Bank) =
            { Type = OverviewSnapshotType.Bank
              InvestmentOverview = None
              Broker = None
              Bank =
                Some
                    { Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                      Bank = bank
                      TotalBalance = dbSnapshot.TotalBalance.Value
                      InterestEarned = dbSnapshot.InterestEarned.Value
                      FeesPaid = dbSnapshot.FeesPaid.Value
                      AccountCount = dbSnapshot.AccountCount }
              BrokerAccount = None
              BankAccount = None }

        /// <summary>
        /// Converts a database BrokerFinancialSnapshot to a domain model BrokerFinancialSnapshot.
        /// Handles conversion of all financial metrics and calculates derived fields like NetCashFlow.
        /// </summary>
        /// <param name="dbFinancial">The database BrokerFinancialSnapshot to convert</param>
        /// <param name="broker">Optional broker model (use when snapshot is for a specific broker)</param>
        /// <param name="brokerAccount">Optional broker account model (use when snapshot is for a specific broker account)</param>
        /// <returns>A domain model BrokerFinancialSnapshot with all fields populated</returns>
        [<Extension>]
        static member brokerFinancialSnapshotToModel
            (
                dbFinancial: Binnaculum.Core.Database.SnapshotsModel.BrokerFinancialSnapshot,
                ?broker: Broker,
                ?brokerAccount: BrokerAccount
            ) : Binnaculum.Core.Models.BrokerFinancialSnapshot =
            { Id = dbFinancial.Base.Id
              Date = DateOnly.FromDateTime(dbFinancial.Base.Date.Value)
              Broker =
                match broker with
                | Some b -> Some b
                | None ->
                    // Check for invalid broker IDs (0, -1, or any non-positive value)
                    if dbFinancial.BrokerId <= 0 then
                        None
                    else
                        Some(dbFinancial.BrokerId.ToFastBrokerById())
              BrokerAccount =
                match brokerAccount with
                | Some ba -> Some ba
                | None ->
                    // Check for invalid broker account IDs (0, -1, or any non-positive value)
                    if dbFinancial.BrokerAccountId <= 0 then
                        None
                    else
                        Some(dbFinancial.BrokerAccountId.ToFastBrokerAccountById())
              Currency = dbFinancial.CurrencyId.ToFastCurrencyById()
              MovementCounter = dbFinancial.MovementCounter
              RealizedGains = dbFinancial.RealizedGains.Value
              RealizedPercentage = dbFinancial.RealizedPercentage
              UnrealizedGains = dbFinancial.UnrealizedGains.Value
              UnrealizedGainsPercentage = dbFinancial.UnrealizedGainsPercentage
              Invested = dbFinancial.Invested.Value
              Commissions = dbFinancial.Commissions.Value
              Fees = dbFinancial.Fees.Value
              Deposited = dbFinancial.Deposited.Value
              Withdrawn = dbFinancial.Withdrawn.Value
              DividendsReceived = dbFinancial.DividendsReceived.Value
              OptionsIncome = dbFinancial.OptionsIncome.Value
              OtherIncome = dbFinancial.OtherIncome.Value
              OpenTrades = dbFinancial.OpenTrades
              NetCashFlow = dbFinancial.NetCashFlow.Value }

        [<Extension>]
        static member brokerAccountSnapshotToOverviewSnapshot
            (
                dbSnapshot: BrokerAccountSnapshot,
                financialSnapshots: BrokerFinancialSnapshot list,
                brokerAccount: BrokerAccount
            ) =
            // CoreLogger.logDebug "DatabaseToModels" $"Converting broker account snapshot - BrokerAccountId: {brokerAccount.Id}, FinancialSnapshots count: {financialSnapshots.Length}"

            let (mainFinancial, otherFinancials) =
                if financialSnapshots.IsEmpty then
                    // Create empty financial snapshot if no data available
                    // CoreLogger.logDebug "DatabaseToModels" "No financial snapshots found for BrokerAccount - creating empty financial data"

                    let emptySnapshot =
                        { Id = 0
                          Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                          Broker = None // Default value indicating not for specific broker
                          BrokerAccount = Some brokerAccount // This is for a specific broker account
                          Currency = (1).ToFastCurrencyById() // USD as default currency
                          MovementCounter = 0
                          RealizedGains = 0.0m
                          RealizedPercentage = 0.0m
                          UnrealizedGains = 0.0m
                          UnrealizedGainsPercentage = 0.0m
                          Invested = 0.0m
                          Commissions = 0.0m
                          Fees = 0.0m
                          Deposited = 0.0m
                          Withdrawn = 0.0m
                          DividendsReceived = 0.0m
                          OptionsIncome = 0.0m
                          OtherIncome = 0.0m
                          OpenTrades = false
                          NetCashFlow = 0.0m // Empty snapshot has no cash flow
                        }

                    (emptySnapshot, [])
                else
                    // Convert database snapshots to model snapshots using helper
                    // CoreLogger.logDebug "DatabaseToModels" $"Converting {financialSnapshots.Length} financial snapshots for BrokerAccount"

                    let modelSnapshots =
                        financialSnapshots
                        |> List.map (fun dbFinancial ->
                            // CoreLogger.logDebug "DatabaseToModels" $"Financial snapshot - Deposited: {dbFinancial.Deposited.Value}, MovementCounter: {dbFinancial.MovementCounter}"
                            // Use the new helper function for conversion
                            dbFinancial.brokerFinancialSnapshotToModel (brokerAccount = brokerAccount))

                    // Find the snapshot with the highest MovementCounter
                    let sortedSnapshots =
                        modelSnapshots |> List.sortByDescending (fun s -> s.MovementCounter)

                    match sortedSnapshots with
                    | head :: tail -> (head, tail)
                    | [] ->
                        // This shouldn't happen since we checked for empty list above, but handle it gracefully
                        let emptySnapshot =
                            { Id = 0
                              Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                              Broker = None // Default value indicating not for specific broker
                              BrokerAccount = Some brokerAccount // This is for a specific broker account
                              Currency = (1).ToFastCurrencyById() // USD as default currency
                              MovementCounter = 0
                              RealizedGains = 0.0m
                              RealizedPercentage = 0.0m
                              UnrealizedGains = 0.0m
                              UnrealizedGainsPercentage = 0.0m
                              Invested = 0.0m
                              Commissions = 0.0m
                              Fees = 0.0m
                              Deposited = 0.0m
                              Withdrawn = 0.0m
                              DividendsReceived = 0.0m
                              OptionsIncome = 0.0m
                              OtherIncome = 0.0m
                              OpenTrades = false
                              NetCashFlow = 0.0m // Empty snapshot has no cash flow
                            }

                        (emptySnapshot, [])

            // Calculate portfolio value from financial snapshots (sum of invested + unrealized gains for all currencies)
            let portfolioValue =
                (mainFinancial.Invested + mainFinancial.UnrealizedGains)
                + (otherFinancials |> List.sumBy (fun f -> f.Invested + f.UnrealizedGains))

            { Type = OverviewSnapshotType.BrokerAccount
              InvestmentOverview = None
              Broker = None
              Bank = None
              BrokerAccount =
                Some
                    { Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                      BrokerAccount = brokerAccount
                      PortfolioValue = portfolioValue
                      Financial = mainFinancial
                      FinancialOtherCurrencies = otherFinancials }
              BankAccount = None }

        [<Extension>]
        static member bankAccountSnapshotToOverviewSnapshot(dbSnapshot: BankAccountSnapshot, bankAccount: BankAccount) =
            { Type = OverviewSnapshotType.BankAccount
              InvestmentOverview = None
              Broker = None
              Bank = None
              BrokerAccount = None
              BankAccount =
                Some
                    { Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                      BankAccount = bankAccount
                      Balance = dbSnapshot.Balance.Value
                      InterestEarned = dbSnapshot.InterestEarned.Value
                      FeesPaid = dbSnapshot.FeesPaid.Value } }

        [<Extension>]
        static member createEmptyOverviewSnapshot() =
            { Type = OverviewSnapshotType.Empty
              InvestmentOverview = None
              Broker = None
              Bank = None
              BrokerAccount = None
              BankAccount = None }

        // Reactive extension methods for ticker snapshot conversion

        /// <summary>
        /// Reactive version of tickerSnapshotToModel that automatically updates when currency data changes.
        /// Returns an observable that emits the updated ticker snapshot when the currency becomes available or changes.
        /// </summary>
        [<Extension>]
        static member tickerSnapshotToModelReactive
            (dbSnapshot: Binnaculum.Core.Database.SnapshotsModel.TickerSnapshot)
            : IObservable<Binnaculum.Core.Models.TickerSnapshot> =
            "USD"
                .ToReactiveCurrency()
                .Select(fun currency ->
                    { Id = dbSnapshot.Base.Id
                      Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                      Ticker = dbSnapshot.TickerId.ToFastTickerById()
                      MainCurrency =
                        { Id = 0
                          Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                          Ticker = dbSnapshot.TickerId.ToFastTickerById()
                          Currency = currency // Reactive currency lookup
                          TotalShares = 0m
                          Weight = 0.0m
                          CostBasis = 0.0m
                          RealCost = 0.0m
                          Dividends = 0.0m
                          DividendTaxes = 0.0m
                          Options = 0.0m
                          TotalIncomes = 0.0m
                          Unrealized = 0.0m
                          Realized = 0.0m
                          Performance = 0.0m
                          LatestPrice = 0.0m
                          OpenTrades = false
                          Commissions = 0.0m
                          Fees = 0.0m }
                      OtherCurrencies = [] })

        /// <summary>
        /// Reactive version of tickerSnapshotToModel with custom currency code.
        /// Returns an observable that emits the updated ticker snapshot when the specified currency becomes available or changes.
        /// </summary>
        [<Extension>]
        static member tickerSnapshotToModelReactive
            (dbSnapshot: Binnaculum.Core.Database.SnapshotsModel.TickerSnapshot, currencyCode: string)
            : IObservable<Binnaculum.Core.Models.TickerSnapshot> =
            currencyCode
                .ToReactiveCurrency()
                .Select(fun currency ->
                    { Id = dbSnapshot.Base.Id
                      Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                      Ticker = dbSnapshot.TickerId.ToFastTickerById()
                      MainCurrency =
                        { Id = 0
                          Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                          Ticker = dbSnapshot.TickerId.ToFastTickerById()
                          Currency = currency // Reactive currency lookup
                          TotalShares = 0m
                          Weight = 0.0m
                          CostBasis = 0.0m
                          RealCost = 0.0m
                          Dividends = 0.0m
                          DividendTaxes = 0.0m
                          Options = 0.0m
                          TotalIncomes = 0.0m
                          Unrealized = 0.0m
                          Realized = 0.0m
                          Performance = 0.0m
                          LatestPrice = 0.0m
                          OpenTrades = false
                          Commissions = 0.0m
                          Fees = 0.0m }
                      OtherCurrencies = [] })

        /// <summary>
        /// Fast version of tickerSnapshotToModel using O(1) currency lookup.
        /// This provides immediate results with improved performance compared to the original linear search.
        /// </summary>
        [<Extension>]
        static member tickerSnapshotToModelFast(dbSnapshot: Binnaculum.Core.Database.SnapshotsModel.TickerSnapshot) =
            { Id = dbSnapshot.Base.Id
              Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
              Ticker = dbSnapshot.TickerId.ToFastTickerById()
              MainCurrency =
                { Id = 0
                  Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                  Ticker = dbSnapshot.TickerId.ToFastTickerById()
                  Currency = "USD".ToFastCurrency() // Fast O(1) currency lookup
                  TotalShares = 0m
                  Weight = 0.0m
                  CostBasis = 0.0m
                  RealCost = 0.0m
                  Dividends = 0.0m
                  DividendTaxes = 0.0m
                  Options = 0.0m
                  TotalIncomes = 0.0m
                  Unrealized = 0.0m
                  Realized = 0.0m
                  Performance = 0.0m
                  LatestPrice = 0.0m
                  OpenTrades = false
                  Commissions = 0.0m
                  Fees = 0.0m }
              OtherCurrencies = [] }

        /// <summary>
        /// Converts a database TickerCurrencySnapshot to the domain model TickerCurrencySnapshot.
        /// Follows the established pattern from brokerFinancialSnapshotToModel.
        /// Uses fast O(1) lookups for ticker and currency references.
        /// </summary>
        /// <param name="dbCurrencySnapshot">The database TickerCurrencySnapshot to convert</param>
        /// <param name="ticker">Optional ticker model (use when ticker is already loaded)</param>
        /// <returns>A domain model TickerCurrencySnapshot with all fields populated</returns>
        [<Extension>]
        static member tickerCurrencySnapshotToModel
            (dbCurrencySnapshot: Binnaculum.Core.Database.SnapshotsModel.TickerCurrencySnapshot, ?ticker: Ticker)
            : Binnaculum.Core.Models.TickerCurrencySnapshot =
            let tickerModel =
                match ticker with
                | Some t -> t
                | None -> dbCurrencySnapshot.TickerId.ToFastTickerById()

            { Id = dbCurrencySnapshot.Base.Id
              Date = DateOnly.FromDateTime(dbCurrencySnapshot.Base.Date.Value)
              Ticker = tickerModel
              Currency = dbCurrencySnapshot.CurrencyId.ToFastCurrencyById()
              TotalShares = dbCurrencySnapshot.TotalShares
              Weight = dbCurrencySnapshot.Weight
              CostBasis = dbCurrencySnapshot.CostBasis.Value
              RealCost = dbCurrencySnapshot.RealCost.Value
              Dividends = dbCurrencySnapshot.Dividends.Value
              DividendTaxes = dbCurrencySnapshot.DividendTaxes.Value
              Options = dbCurrencySnapshot.Options.Value
              TotalIncomes = dbCurrencySnapshot.TotalIncomes.Value
              Unrealized = dbCurrencySnapshot.Unrealized.Value
              Realized = dbCurrencySnapshot.Realized.Value
              Performance = dbCurrencySnapshot.Performance
              LatestPrice = dbCurrencySnapshot.LatestPrice.Value
              OpenTrades = dbCurrencySnapshot.OpenTrades
              Commissions = dbCurrencySnapshot.Commissions.Value
              Fees = dbCurrencySnapshot.Fees.Value }

        /// <summary>
        /// Converts a database TickerSnapshot to the domain model TickerSnapshot,
        /// loading all associated TickerCurrencySnapshots and properly populating MainCurrency and OtherCurrencies.
        /// Follows the established pattern from brokerAccountSnapshotToOverviewSnapshot.
        /// </summary>
        /// <param name="dbSnapshot">The database TickerSnapshot to convert</param>
        /// <param name="currencySnapshots">List of TickerCurrencySnapshot records for this ticker snapshot</param>
        /// <param name="ticker">The Ticker model (should be pre-loaded for performance)</param>
        /// <returns>A domain model TickerSnapshot with all currency data properly populated</returns>
        [<Extension>]
        static member tickerSnapshotToModelWithCurrencies
            (
                dbSnapshot: Binnaculum.Core.Database.SnapshotsModel.TickerSnapshot,
                currencySnapshots: Binnaculum.Core.Database.SnapshotsModel.TickerCurrencySnapshot list,
                ticker: Ticker
            ) : Binnaculum.Core.Models.TickerSnapshot =
            let (mainCurrency, otherCurrencies) =
                if currencySnapshots.IsEmpty then
                    // Create empty currency snapshot if no data available
                    let emptySnapshot =
                        { Id = 0
                          Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                          Ticker = ticker
                          Currency = "USD".ToFastCurrency() // Default to USD
                          TotalShares = 0m
                          Weight = 0.0m
                          CostBasis = 0.0m
                          RealCost = 0.0m
                          Dividends = 0.0m
                          DividendTaxes = 0.0m
                          Options = 0.0m
                          TotalIncomes = 0.0m
                          Unrealized = 0.0m
                          Realized = 0.0m
                          Performance = 0.0m
                          LatestPrice = 0.0m
                          OpenTrades = false
                          Commissions = 0.0m
                          Fees = 0.0m }

                    (emptySnapshot, [])
                else
                    // Convert database snapshots to model snapshots
                    let modelSnapshots =
                        currencySnapshots
                        |> List.map (fun dbCurrencySnapshot ->
                            dbCurrencySnapshot.tickerCurrencySnapshotToModel (ticker = ticker))

                    // Use first currency snapshot as main, rest as others
                    match modelSnapshots with
                    | head :: tail -> (head, tail)
                    | [] ->
                        // This shouldn't happen since we checked for empty list above, but handle it gracefully
                        let emptySnapshot =
                            { Id = 0
                              Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                              Ticker = ticker
                              Currency = "USD".ToFastCurrency()
                              TotalShares = 0m
                              Weight = 0.0m
                              CostBasis = 0.0m
                              RealCost = 0.0m
                              Dividends = 0.0m
                              DividendTaxes = 0.0m
                              Options = 0.0m
                              TotalIncomes = 0.0m
                              Unrealized = 0.0m
                              Realized = 0.0m
                              Performance = 0.0m
                              LatestPrice = 0.0m
                              OpenTrades = false
                              Commissions = 0.0m
                              Fees = 0.0m }

                        (emptySnapshot, [])

            { Id = dbSnapshot.Base.Id
              Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
              Ticker = ticker
              MainCurrency = mainCurrency
              OtherCurrencies = otherCurrencies }
