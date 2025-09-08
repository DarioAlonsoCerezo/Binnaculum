namespace Binnaculum.Core

open System
open System.Runtime.CompilerServices
open System.Reactive.Linq
open Binnaculum.Core.Models
open Binnaculum.Core.UI
open DiscriminatedToModel
open Microsoft.Maui.Storage
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Keys

module internal DatabaseToModels =

    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member bankToModel(bank: Binnaculum.Core.Database.DatabaseModel.Bank) =
            {
                Id = bank.Id
                Name = bank.Name
                Image = bank.Image
                CreatedAt = 
                    match bank.Audit.CreatedAt with
                    | Some createdAt -> createdAt.Value
                    | None -> System.DateTime.Now
            }

        [<Extension>]
        static member banksToModel(banks: Binnaculum.Core.Database.DatabaseModel.Bank list) =
            banks |> List.map (fun b -> b.bankToModel())
        
        [<Extension>]
        static member currencyToModel(currency: Binnaculum.Core.Database.DatabaseModel.Currency) =
            {
                Id = currency.Id
                Title = currency.Name
                Code = currency.Code
                Symbol = currency.Symbol
            }

        [<Extension>]
        static member currenciesToModel(currencies: Binnaculum.Core.Database.DatabaseModel.Currency list) =
            currencies |> List.map (fun c -> c.currencyToModel())

        [<Extension>]
        static member bankAccountToModel(bankAccount: Binnaculum.Core.Database.DatabaseModel.BankAccount) = 
            {
                Id = bankAccount.Id
                Bank = bankAccount.BankId.ToFastBankById()
                Name = bankAccount.Name
                Description = bankAccount.Description
                Currency = bankAccount.CurrencyId.ToFastCurrencyById()
            }

        [<Extension>]
        static member bankAccountsToModel(bankAccounts: Binnaculum.Core.Database.DatabaseModel.BankAccount list) =
            bankAccounts |> List.map (fun b -> b.bankAccountToModel())

        [<Extension>]
        static member bankAccountToMovement(movement: Binnaculum.Core.Database.DatabaseModel.BankAccountMovement) =
            let bankMovement =
                {
                    Id = movement.Id
                    TimeStamp = movement.TimeStamp.Value
                    Amount = movement.Amount.Value
                    Currency = movement.CurrencyId.ToFastCurrencyById()
                    BankAccount = movement.BankAccountId.ToFastBankAccountById()
                    MovementType = movement.MovementType.bankMovementTypeToModel()
                }

            {
                Type = AccountMovementType.BankAccountMovement
                TimeStamp = bankMovement.TimeStamp
                Trade = None
                Dividend = None
                DividendTax = None
                DividendDate = None
                OptionTrade = None
                BrokerMovement = None
                BankAccountMovement = Some bankMovement
                TickerSplit = None
            }

        [<Extension>]
        static member bankAccountMovementsToMovements(movements: Binnaculum.Core.Database.DatabaseModel.BankAccountMovement list) =
            movements |> List.map (fun m -> m.bankAccountToMovement())

        [<Extension>]
        static member brokerToModel(broker: Binnaculum.Core.Database.DatabaseModel.Broker) =
            {
                Id = broker.Id
                Name = broker.Name
                Image = broker.Image
                SupportedBroker = broker.SupportedBroker.supportedBrokerToModel()
            }

        [<Extension>]
        static member brokersToModel(brokers: Binnaculum.Core.Database.DatabaseModel.Broker list) =
            brokers |> List.map (fun b -> b.brokerToModel())

        [<Extension>]
        static member brokerAccountToModel(brokerAccount: Binnaculum.Core.Database.DatabaseModel.BrokerAccount) =
            {
                Id = brokerAccount.Id
                Broker = brokerAccount.BrokerId.ToFastBrokerById()
                AccountNumber = brokerAccount.AccountNumber
            }

        [<Extension>]
        static member brokerAccountsToModel(brokerAccounts: Binnaculum.Core.Database.DatabaseModel.BrokerAccount list) =
            brokerAccounts |> List.map (fun b -> b.brokerAccountToModel())

        [<Extension>]
        static member brokerMovementToModel(movement: Binnaculum.Core.Database.DatabaseModel.BrokerMovement) =
            let brokerAccount = Binnaculum.Core.UI.Collections.getBrokerAccount(movement.BrokerAccountId)
            let currency = movement.CurrencyId.ToFastCurrencyById()
            let fromCurrency = movement.FromCurrencyId |> Option.map (fun id -> id.ToFastCurrencyById())
            let ticker = movement.TickerId |> Option.map (fun id -> id.ToFastTickerById())
            let brokerMovement =
                {
                    Id = movement.Id
                    TimeStamp = movement.TimeStamp.Value
                    Amount = movement.Amount.Value
                    Currency = currency
                    BrokerAccount = brokerAccount
                    Commissions = movement.Commissions.Value
                    Fees = movement.Fees.Value
                    MovementType = movement.MovementType.brokerMovementTypeToModel()
                    Notes = movement.Notes
                    FromCurrency = fromCurrency
                    AmountChanged = movement.AmountChanged |> Option.map (fun m -> m.Value)
                    Ticker = ticker
                    Quantity = movement.Quantity
                }
            {
                Type = AccountMovementType.BrokerMovement
                TimeStamp = brokerMovement.TimeStamp
                Trade = None
                Dividend = None
                DividendTax = None
                DividendDate = None
                OptionTrade = None
                BrokerMovement = Some brokerMovement
                BankAccountMovement = None
                TickerSplit = None
            }

        [<Extension>]
        static member brokerMovementsToModel(movements: Binnaculum.Core.Database.DatabaseModel.BrokerMovement list) =
            movements |> List.map (fun m -> m.brokerMovementToModel())

        [<Extension>]
        static member tickerToModel(ticker: Binnaculum.Core.Database.DatabaseModel.Ticker) =
            {
                Id = ticker.Id
                Symbol = ticker.Symbol
                Image = ticker.Image
                Name = ticker.Name
            }

        [<Extension>]
        static member tickersToModel(tikers: Binnaculum.Core.Database.DatabaseModel.Ticker list) =
            tikers |> List.map (fun t -> t.tickerToModel())

        [<Extension>]
        static member tickerSnapshotToModel(dbSnapshot: TickerSnapshot) =
            {
                Id = dbSnapshot.Base.Id
                Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                Ticker = dbSnapshot.TickerId.ToFastTickerById()
                MainCurrency = 
                    {
                        Id = 0
                        Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                        Ticker = dbSnapshot.TickerId.ToFastTickerById()
                        Currency = "USD".ToFastCurrency() // Updated to use fast O(1) lookup
                        TotalShares = 0m // Default value, not used in this context
                        Weight = 0.0m // Default value, not used in this context
                        CostBasis = 0.0m // Default value, not used in this context
                        RealCost = 0.0m // Default value, not used in this context
                        Dividends = 0.0m // Default value, not used in this context
                        Options = 0.0m // Default value, not used in this context
                        TotalIncomes = 0.0m // Default value, not used in this context
                        Unrealized = 0.0m // Default value, not used in this context
                        Realized = 0.0m // Default value, not used in this context
                        Performance = 0.0m // Default value, not used in this context
                        LatestPrice = 0.0m // Default value, not used in this context
                        OpenTrades = false // Default value, not used in this context
                    }
                OtherCurrencies = []
            }

        [<Extension>]
        static member tickerSnapshotsToModel(dbSnapshots: TickerSnapshot list) =
            dbSnapshots |> List.map (fun s -> s.tickerSnapshotToModel())

        [<Extension>]
        static member tradeToModel(trade: Binnaculum.Core.Database.DatabaseModel.Trade) =
            let amount = trade.Price.Value * trade.Quantity
            let commissions = trade.Fees.Value + trade.Commissions.Value
            let totalInvestedAmount = amount + commissions
            {
                Id = trade.Id
                TimeStamp = trade.TimeStamp.Value
                TotalInvestedAmount = totalInvestedAmount
                Ticker = trade.TickerId.ToFastTickerById()
                BrokerAccount = Binnaculum.Core.UI.Collections.getBrokerAccount(trade.BrokerAccountId)
                Currency = trade.CurrencyId.ToFastCurrencyById()
                Quantity = trade.Quantity
                Price = trade.Price.Value
                Commissions = trade.Commissions.Value
                Fees = trade.Fees.Value
                TradeCode = trade.TradeCode.databaseToTradeCode()
                TradeType = trade.TradeType.databaseToTradeType()
                Leveraged = trade.Leveraged
                Notes = trade.Notes
            }
        
        [<Extension>]
        static member tradesToModel(trades: Binnaculum.Core.Database.DatabaseModel.Trade list) =
            trades |> List.map (fun t -> t.tradeToModel())

        [<Extension>]
        static member tradeToMovement(trade: Binnaculum.Core.Database.DatabaseModel.Trade) =
            let tradeMovement = trade.tradeToModel()
            {
                Type = AccountMovementType.Trade
                TimeStamp = tradeMovement.TimeStamp
                Trade = Some tradeMovement
                Dividend = None
                DividendTax = None
                DividendDate = None
                OptionTrade = None
                BrokerMovement = None
                BankAccountMovement = None
                TickerSplit = None
            }

        [<Extension>]
        static member tradesToMovements(trades: Binnaculum.Core.Database.DatabaseModel.Trade list) =
            trades |> List.map (fun t -> t.tradeToMovement())

        [<Extension>]
        static member dividendReceivedToModel(dividend: Binnaculum.Core.Database.DatabaseModel.Dividend) =
            {
                Id = dividend.Id
                TimeStamp = dividend.TimeStamp.Value
                Amount = dividend.DividendAmount.Value
                Ticker = dividend.TickerId.ToFastTickerById()
                Currency = dividend.CurrencyId.ToFastCurrencyById()
                BrokerAccount = Binnaculum.Core.UI.Collections.getBrokerAccount(dividend.BrokerAccountId)
            }

        [<Extension>]
        static member dividendsReceivedToModel(dividends: Binnaculum.Core.Database.DatabaseModel.Dividend list) =
            dividends |> List.map (fun d -> d.dividendReceivedToModel())

        [<Extension>]
        static member dividendReceivedToMovement(dividend: Binnaculum.Core.Database.DatabaseModel.Dividend) =
            let model = dividend.dividendReceivedToModel()
            {
                Type = AccountMovementType.Dividend
                TimeStamp = model.TimeStamp
                Trade = None
                Dividend = Some model
                DividendTax = None
                DividendDate = None
                OptionTrade = None
                BrokerMovement = None
                BankAccountMovement = None
                TickerSplit = None
            }

        [<Extension>]
        static member dividendsReceivedToMovements(dividends: Binnaculum.Core.Database.DatabaseModel.Dividend list) =
            dividends |> List.map (fun d -> d.dividendReceivedToMovement())

        [<Extension>]
        static member dividendTaxToModel(dividend: Binnaculum.Core.Database.DatabaseModel.DividendTax) =
            {
                Id = dividend.Id
                TimeStamp = dividend.TimeStamp.Value
                TaxAmount = dividend.DividendTaxAmount.Value
                Ticker = dividend.TickerId.ToFastTickerById()
                Currency = dividend.CurrencyId.ToFastCurrencyById()
                BrokerAccount = Binnaculum.Core.UI.Collections.getBrokerAccount(dividend.BrokerAccountId)
            }

        [<Extension>]
        static member dividendTaxesToModel(dividends: Binnaculum.Core.Database.DatabaseModel.DividendTax list) =
            dividends |> List.map (fun d -> d.dividendTaxToModel())
        
        [<Extension>]
        static member dividendTaxToMovement(dividend: Binnaculum.Core.Database.DatabaseModel.DividendTax) =
            let model = dividend.dividendTaxToModel()
            {
                Type = AccountMovementType.DividendTax
                TimeStamp = model.TimeStamp
                Trade = None
                Dividend = None
                DividendTax = Some model
                DividendDate = None
                OptionTrade = None
                BrokerMovement = None
                BankAccountMovement = None
                TickerSplit = None
            }

        [<Extension>]
        static member dividendTaxesToMovements(dividends: Binnaculum.Core.Database.DatabaseModel.DividendTax list) =
            dividends |> List.map (fun d -> d.dividendTaxToMovement())

        [<Extension>]
        static member dividendDateToModel(dividend: Binnaculum.Core.Database.DatabaseModel.DividendDate) =
            {
                Id = dividend.Id
                TimeStamp = dividend.TimeStamp.Value
                Amount = dividend.Amount.Value
                Ticker = dividend.TickerId.ToFastTickerById()
                Currency = dividend.CurrencyId.ToFastCurrencyById()
                BrokerAccount = Binnaculum.Core.UI.Collections.getBrokerAccount(dividend.BrokerAccountId)
                DividendCode = dividend.DividendCode.databaseToDividendCode()
            }

        [<Extension>]
        static member dividendDatesToModel(dividends: Binnaculum.Core.Database.DatabaseModel.DividendDate list) =
            dividends |> List.map (fun d -> d.dividendDateToModel())
        
        [<Extension>]
        static member dividendDateToMovement(dividend: Binnaculum.Core.Database.DatabaseModel.DividendDate) =
            let model = dividend.dividendDateToModel()
            {
                Type = AccountMovementType.DividendDate
                TimeStamp = model.TimeStamp
                Trade = None
                Dividend = None
                DividendTax = None
                DividendDate = Some model
                OptionTrade = None
                BrokerMovement = None
                BankAccountMovement = None
                TickerSplit = None
            }

        [<Extension>]
        static member dividendDatesToMovements(dividends: Binnaculum.Core.Database.DatabaseModel.DividendDate list) =
            dividends |> List.map (fun d -> d.dividendDateToMovement())

        [<Extension>]
        static member optionTradeToModel(optionTrade: Binnaculum.Core.Database.DatabaseModel.OptionTrade) =
            {
                Id = optionTrade.Id
                TimeStamp = optionTrade.TimeStamp.Value
                ExpirationDate = optionTrade.ExpirationDate.Value
                Premium = optionTrade.Premium.Value
                NetPremium = optionTrade.NetPremium.Value
                Ticker = optionTrade.TickerId.ToFastTickerById()
                BrokerAccount = Binnaculum.Core.UI.Collections.getBrokerAccount(optionTrade.BrokerAccountId)
                Currency = optionTrade.CurrencyId.ToFastCurrencyById()
                OptionType = optionTrade.OptionType.databaseToOptionType()
                Code = optionTrade.Code.databaseToOptionCode()
                Strike = optionTrade.Strike.Value
                Commissions = optionTrade.Commissions.Value
                Fees = optionTrade.Fees.Value
                IsOpen = optionTrade.IsOpen
                ClosedWith = match optionTrade.ClosedWith with | Some c -> c | None -> 0
                Multiplier = optionTrade.Multiplier
                Quantity = 1
                Notes = optionTrade.Notes
                FeesPerOperation = false
            }

        [<Extension>]
        static member optionTradesToModel(optionTrades: Binnaculum.Core.Database.DatabaseModel.OptionTrade list) =
            optionTrades |> List.map (fun o -> o.optionTradeToModel())
        
        [<Extension>]
        static member optionTradesToMovements(optionTrades: Binnaculum.Core.Database.DatabaseModel.OptionTrade list) =
            // First convert all trades to models
            let optionTradeModels = optionTrades |> List.map (fun o -> o.optionTradeToModel())

            let groupOptions = Preferences.Get(GroupOptionsKey, true)
            
            if groupOptions then
            // Group trades by key characteristics (ticker ID, option type, strike price, expiration date)
                let groupedTrades = 
                    optionTradeModels 
                    |> List.groupBy (fun trade -> 
                        (trade.Ticker.Id, trade.OptionType, decimal trade.Strike, trade.ExpirationDate.Date))
                    |> List.map (fun ((tickerId, optionType, strike, expiration), trades) ->
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
                        {
                            Type = AccountMovementType.OptionTrade
                            TimeStamp = combinedTrade.TimeStamp
                            Trade = None
                            Dividend = None
                            DividendTax = None
                            DividendDate = None
                            OptionTrade = Some combinedTrade
                            BrokerMovement = None
                            BankAccountMovement = None
                            TickerSplit = None
                        })
            
                groupedTrades

            else
                optionTradeModels
                    |> List.map(fun optionTrade -> 
                        {
                            Type = AccountMovementType.OptionTrade
                            TimeStamp = optionTrade.TimeStamp
                            Trade = None
                            Dividend = None
                            DividendTax = None
                            DividendDate = None
                            OptionTrade = Some optionTrade
                            BrokerMovement = None
                            BankAccountMovement = None
                            TickerSplit = None
                        })

        // Snapshot conversion functions (backward compatible)
        [<Extension>]
        static member brokerSnapshotToOverviewSnapshot(dbSnapshot: BrokerSnapshot, broker: Broker) =
            dbSnapshot.brokerSnapshotToOverviewSnapshot(broker, [])

        [<Extension>]
        static member brokerSnapshotToOverviewSnapshot(dbSnapshot: BrokerSnapshot, broker: Broker, financialSnapshots: BrokerFinancialSnapshot list) =
            let (mainFinancial, otherFinancials) =
                if financialSnapshots.IsEmpty then
                    // Create empty financial snapshot if no data available
                    let emptySnapshot = {
                        Id = 0
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
                    }
                    (emptySnapshot, [])
                else
                    // Convert database snapshots to model snapshots
                    let modelSnapshots = 
                        financialSnapshots
                        |> List.map (fun dbFinancial -> {
                            Id = dbFinancial.Base.Id
                            Date = DateOnly.FromDateTime(dbFinancial.Base.Date.Value)
                            Broker = if dbFinancial.BrokerId = -1 then None else Some (dbFinancial.BrokerId.ToFastBrokerById())
                            BrokerAccount = if dbFinancial.BrokerAccountId = -1 then None else Some (Binnaculum.Core.UI.Collections.getBrokerAccount(dbFinancial.BrokerAccountId))
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
                        })
                    
                    // Find the snapshot with the highest MovementCounter
                    let sortedSnapshots = modelSnapshots |> List.sortByDescending (fun s -> s.MovementCounter)
                    match sortedSnapshots with
                    | head :: tail -> (head, tail)
                    | [] -> 
                        // This shouldn't happen since we checked for empty list above, but handle it gracefully
                        let emptySnapshot = {
                            Id = 0
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
                        }
                        (emptySnapshot, [])
            
            {
                Type = OverviewSnapshotType.Broker
                InvestmentOverview = None
                Broker = Some {
                    Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                    Broker = broker
                    PortfoliosValue = 0m // TODO: Calculate from currency snapshots at runtime
                    AccountCount = dbSnapshot.AccountCount
                    Financial = mainFinancial
                    FinancialOtherCurrencies = otherFinancials
                }
                Bank = None
                BrokerAccount = None
                BankAccount = None
            }

        [<Extension>]
        static member bankSnapshotToOverviewSnapshot(dbSnapshot: BankSnapshot, bank: Bank) =
            {
                Type = OverviewSnapshotType.Bank
                InvestmentOverview = None
                Broker = None
                Bank = Some {
                    Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                    Bank = bank
                    TotalBalance = dbSnapshot.TotalBalance.Value
                    InterestEarned = dbSnapshot.InterestEarned.Value
                    FeesPaid = dbSnapshot.FeesPaid.Value
                    AccountCount = dbSnapshot.AccountCount
                }
                BrokerAccount = None
                BankAccount = None
            }

        [<Extension>]
        static member brokerAccountSnapshotToOverviewSnapshot(dbSnapshot: BrokerAccountSnapshot, financialSnapshots: BrokerFinancialSnapshot list, brokerAccount: BrokerAccount) =
            let (mainFinancial, otherFinancials) =
                if financialSnapshots.IsEmpty then
                    // Create empty financial snapshot if no data available
                    let emptySnapshot = {
                        Id = 0
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
                    }
                    (emptySnapshot, [])
                else
                    // Convert database snapshots to model snapshots
                    let modelSnapshots = 
                        financialSnapshots
                        |> List.map (fun dbFinancial -> {
                            Id = dbFinancial.Base.Id
                            Date = DateOnly.FromDateTime(dbFinancial.Base.Date.Value)
                            Broker = None // For broker account snapshots, broker is not specific
                            BrokerAccount = Some brokerAccount // This is for a specific broker account
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
                        })
                    
                    // Find the snapshot with the highest MovementCounter
                    let sortedSnapshots = modelSnapshots |> List.sortByDescending (fun s -> s.MovementCounter)
                    match sortedSnapshots with
                    | head :: tail -> (head, tail)
                    | [] -> 
                        // This shouldn't happen since we checked for empty list above, but handle it gracefully
                        let emptySnapshot = {
                            Id = 0
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
                        }
                        (emptySnapshot, [])

            // Calculate portfolio value from financial snapshots (sum of invested + unrealized gains for all currencies)
            let portfolioValue = 
                (mainFinancial.Invested + mainFinancial.UnrealizedGains) + 
                (otherFinancials |> List.sumBy (fun f -> f.Invested + f.UnrealizedGains))
            
            {
                Type = OverviewSnapshotType.BrokerAccount
                InvestmentOverview = None
                Broker = None
                Bank = None
                BrokerAccount = Some {
                    Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                    BrokerAccount = brokerAccount
                    PortfolioValue = portfolioValue
                    Financial = mainFinancial
                    FinancialOtherCurrencies = otherFinancials
                }
                BankAccount = None
            }

        [<Extension>]
        static member bankAccountSnapshotToOverviewSnapshot(dbSnapshot: BankAccountSnapshot, bankAccount: BankAccount) =
            {
                Type = OverviewSnapshotType.BankAccount
                InvestmentOverview = None
                Broker = None
                Bank = None
                BrokerAccount = None
                BankAccount = Some {
                    Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                    BankAccount = bankAccount
                    Balance = dbSnapshot.Balance.Value
                    InterestEarned = dbSnapshot.InterestEarned.Value
                    FeesPaid = dbSnapshot.FeesPaid.Value
                }
            }

        [<Extension>]
        static member createEmptyOverviewSnapshot() =
            {
                Type = OverviewSnapshotType.Empty
                InvestmentOverview = None
                Broker = None
                Bank = None
                BrokerAccount = None
                BankAccount = None
            }

        // Reactive extension methods for ticker snapshot conversion
        
        /// <summary>
        /// Reactive version of tickerSnapshotToModel that automatically updates when currency data changes.
        /// Returns an observable that emits the updated ticker snapshot when the currency becomes available or changes.
        /// </summary>
        [<Extension>]
        static member tickerSnapshotToModelReactive(dbSnapshot: Binnaculum.Core.Database.SnapshotsModel.TickerSnapshot) : IObservable<Binnaculum.Core.Models.TickerSnapshot> =
            "USD".ToReactiveCurrency()
                .Select(fun currency -> 
                    {
                        Id = dbSnapshot.Base.Id
                        Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                        Ticker = dbSnapshot.TickerId.ToFastTickerById()
                        MainCurrency = 
                            {
                                Id = 0
                                Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                                Ticker = dbSnapshot.TickerId.ToFastTickerById()
                                Currency = currency // Reactive currency lookup
                                TotalShares = 0m
                                Weight = 0.0m
                                CostBasis = 0.0m
                                RealCost = 0.0m
                                Dividends = 0.0m
                                Options = 0.0m
                                TotalIncomes = 0.0m
                                Unrealized = 0.0m
                                Realized = 0.0m
                                Performance = 0.0m
                                LatestPrice = 0.0m
                                OpenTrades = false
                            }
                        OtherCurrencies = []
                    })

        /// <summary>
        /// Reactive version of tickerSnapshotToModel with custom currency code.
        /// Returns an observable that emits the updated ticker snapshot when the specified currency becomes available or changes.
        /// </summary>
        [<Extension>]
        static member tickerSnapshotToModelReactive(dbSnapshot: Binnaculum.Core.Database.SnapshotsModel.TickerSnapshot, currencyCode: string) : IObservable<Binnaculum.Core.Models.TickerSnapshot> =
            currencyCode.ToReactiveCurrency()
                .Select(fun currency -> 
                    {
                        Id = dbSnapshot.Base.Id
                        Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                        Ticker = dbSnapshot.TickerId.ToFastTickerById()
                        MainCurrency = 
                            {
                                Id = 0
                                Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                                Ticker = dbSnapshot.TickerId.ToFastTickerById()
                                Currency = currency // Reactive currency lookup
                                TotalShares = 0m
                                Weight = 0.0m
                                CostBasis = 0.0m
                                RealCost = 0.0m
                                Dividends = 0.0m
                                Options = 0.0m
                                TotalIncomes = 0.0m
                                Unrealized = 0.0m
                                Realized = 0.0m
                                Performance = 0.0m
                                LatestPrice = 0.0m
                                OpenTrades = false
                            }
                        OtherCurrencies = []
                    })

        /// <summary>
        /// Fast version of tickerSnapshotToModel using O(1) currency lookup.
        /// This provides immediate results with improved performance compared to the original linear search.
        /// </summary>
        [<Extension>]
        static member tickerSnapshotToModelFast(dbSnapshot: Binnaculum.Core.Database.SnapshotsModel.TickerSnapshot) =
            {
                Id = dbSnapshot.Base.Id
                Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                Ticker = dbSnapshot.TickerId.ToFastTickerById()
                MainCurrency = 
                    {
                        Id = 0
                        Date = DateOnly.FromDateTime(dbSnapshot.Base.Date.Value)
                        Ticker = dbSnapshot.TickerId.ToFastTickerById()
                        Currency = "USD".ToFastCurrency() // Fast O(1) currency lookup
                        TotalShares = 0m
                        Weight = 0.0m
                        CostBasis = 0.0m
                        RealCost = 0.0m
                        Dividends = 0.0m
                        Options = 0.0m
                        TotalIncomes = 0.0m
                        Unrealized = 0.0m
                        Realized = 0.0m
                        Performance = 0.0m
                        LatestPrice = 0.0m
                        OpenTrades = false
                    }
                OtherCurrencies = []
            }