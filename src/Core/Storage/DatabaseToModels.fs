namespace Binnaculum.Core.Storage

open System.Runtime.CompilerServices
open Binnaculum.Core.Models
open DiscriminatedToModel

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
        static member bankAccountToModel(bankAccount: Binnaculum.Core.Database.DatabaseModel.BankAccount, bank: Binnaculum.Core.Models.Bank, currency: Binnaculum.Core.Models.Currency) =
            {
                Id = bankAccount.Id
                Bank = bank
                Name = bankAccount.Name
                Description = bankAccount.Description
                Currency = currency
            }
            
        [<Extension>]
        static member bankAccountToModel(bankAccount: Binnaculum.Core.Database.DatabaseModel.BankAccount) = 
            {
                Id = bankAccount.Id
                Bank = Binnaculum.Core.UI.Collections.getBank(bankAccount.BankId)
                Name = bankAccount.Name
                Description = bankAccount.Description
                Currency = Binnaculum.Core.UI.Collections.getCurrency(bankAccount.CurrencyId)
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
                    Currency = Binnaculum.Core.UI.Collections.getCurrency(movement.CurrencyId)
                    BankAccount = Binnaculum.Core.UI.Collections.getBankAccount(movement.BankAccountId)
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
                Broker = Binnaculum.Core.UI.Collections.getBroker(brokerAccount.BrokerId)
                AccountNumber = brokerAccount.AccountNumber
            }

        [<Extension>]
        static member brokerAccountsToModel(brokerAccounts: Binnaculum.Core.Database.DatabaseModel.BrokerAccount list) =
            brokerAccounts |> List.map (fun b -> b.brokerAccountToModel())

        [<Extension>]
        static member brokerMovementToModel(movement: Binnaculum.Core.Database.DatabaseModel.BrokerMovement) =
            let brokerAccount = Binnaculum.Core.UI.Collections.getBrokerAccount(movement.BrokerAccountId)
            let currency = Binnaculum.Core.UI.Collections.getCurrency(movement.CurrencyId)
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
        static member tradeToModel(trade: Binnaculum.Core.Database.DatabaseModel.Trade) =
            let amount = trade.Price.Value * trade.Quantity
            let commissions = trade.Fees.Value + trade.Commissions.Value
            let totalInvestedAmount = amount + commissions
            {
                Id = trade.Id
                TimeStamp = trade.TimeStamp.Value
                TotalInvestedAmount = totalInvestedAmount
                Ticker = Binnaculum.Core.UI.Collections.getTickerById(trade.TickerId)
                BrokerAccount = Binnaculum.Core.UI.Collections.getBrokerAccount(trade.BrokerAccountId)
                Currency = Binnaculum.Core.UI.Collections.getCurrency(trade.CurrencyId)
                Quantity = trade.Quantity
                Price = trade.Price.Value
                Commissions = trade.Commissions.Value
                Fees = trade.Fees.Value
                TradeCode = trade.TradeCode.databaseToTradeCode()
                TradeType = trade.TradeType.databaseToTradeType()
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
                Ticker = Binnaculum.Core.UI.Collections.getTickerById(dividend.TickerId)
                Currency = Binnaculum.Core.UI.Collections.getCurrency(dividend.CurrencyId)
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
                Ticker = Binnaculum.Core.UI.Collections.getTickerById(dividend.TickerId)
                Currency = Binnaculum.Core.UI.Collections.getCurrency(dividend.CurrencyId)
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
                Ticker = Binnaculum.Core.UI.Collections.getTickerById(dividend.TickerId)
                Currency = Binnaculum.Core.UI.Collections.getCurrency(dividend.CurrencyId)
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