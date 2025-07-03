namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns
open System
open System.Runtime.CompilerServices
open DiscriminatedToDatabase

module internal ModelsToDatabase =
    
    [<Extension>]
    type Do() = 
    
        [<Extension>]
        static member createBrokerToDatabase(broker: Binnaculum.Core.Models.Broker) =
            { 
                Id = broker.Id; 
                Name = broker.Name; 
                Image = broker.Image; 
                SupportedBroker = SupportedBroker.Unknown;
            }

        [<Extension>]
        static member updateBrokerToDatabase(broker: Binnaculum.Core.Models.Broker) = task {
            let! currentBroker = BrokerExtensions.Do.getById broker.Id |> Async.AwaitTask
            match currentBroker with
            | Some current -> 
                let updatedBroker = 
                    { current with 
                        Name = broker.Name; 
                        Image = broker.Image; 
                        SupportedBroker = SupportedBroker.Unknown;
                    }
                return updatedBroker
            | None ->
                return failwithf "Broker with ID %d not found" broker.Id
        }

        [<Extension>]
        static member brokerToDatabase(broker: Binnaculum.Core.Models.Broker) = task {
            if broker.Id = 0 then
                return broker.createBrokerToDatabase()
            else
                return! broker.updateBrokerToDatabase() |> Async.AwaitTask
        }
        
        [<Extension>]
        static member createBankToDatabase(bank: Binnaculum.Core.Models.Bank) =
            { 
                Id = bank.Id; 
                Name = bank.Name; 
                Image = bank.Image; 
                Audit = AuditableEntity.FromDateTime(DateTime.Now);
            }

        [<Extension>]
        static member updateBankToDatabase(bank: Binnaculum.Core.Models.Bank) = task {
            let! currentBank = BankExtensions.Do.getById bank.Id |> Async.AwaitTask
            match currentBank with
            | Some current -> 
                let updatedBank = 
                    { current with 
                        Name = bank.Name; 
                        Image = bank.Image; 
                        Audit = AuditableEntity.FromDateTime(DateTime.Now);
                    }
                return updatedBank
            | None ->
                return failwithf "Bank with ID %d not found" bank.Id
        }

        [<Extension>]
        static member bankToDatabase(bank: Binnaculum.Core.Models.Bank) = task {
            if bank.Id = 0 then
                return bank.createBankToDatabase()
            else
                return! bank.updateBankToDatabase() |> Async.AwaitTask
        }

        [<Extension>]
        static member createBankAccountToDatabase(bankAccount: Binnaculum.Core.Models.BankAccount) =
            { 
                Id = bankAccount.Id; 
                BankId = bankAccount.Bank.Id; 
                Name = bankAccount.Name; 
                Description = bankAccount.Description; 
                CurrencyId = bankAccount.Currency.Id; 
                Audit = AuditableEntity.FromDateTime(DateTime.Now);
            }

        [<Extension>]
        static member updateBankAccountToDatabase(bankAccount: Binnaculum.Core.Models.BankAccount) = task {
            let! currentBankAccount = BankAccountExtensions.Do.getById bankAccount.Id |> Async.AwaitTask
            match currentBankAccount with
            | Some current -> 
                let updatedBankAccount = 
                    { current with 
                        Name = bankAccount.Name; 
                        Description = bankAccount.Description; 
                        Audit = AuditableEntity.FromDateTime(DateTime.Now);
                    }
                return updatedBankAccount
            | None -> 
                return failwithf "BankAccount with ID %d not found" bankAccount.Id
        }

        [<Extension>]
        static member bankAccountToDatabase(bankAccount: Binnaculum.Core.Models.BankAccount) = task {
            if bankAccount.Id = 0 then
                return bankAccount.createBankAccountToDatabase()
            else
                return! bankAccount.updateBankAccountToDatabase() |> Async.AwaitTask
        }

        [<Extension>]
        static member bankAccountMovementToDatabase(movement: Binnaculum.Core.Models.BankAccountMovement) =
            let movementType = movement.MovementType.bankMovementTypeToDatabase()
            { 
                Id = movement.Id; 
                TimeStamp = DateTimePattern.FromDateTime(movement.TimeStamp); 
                Amount = Money.FromAmount(movement.Amount);
                BankAccountId = movement.BankAccount.Id;
                CurrencyId = movement.Currency.Id;
                MovementType = movementType;
                Audit = AuditableEntity.FromDateTime(movement.TimeStamp);
            }

        [<Extension>]
        static member brokerMovementToDatabase(movement: Binnaculum.Core.Models.BrokerMovement) =
            { 
                Id = movement.Id; 
                TimeStamp = DateTimePattern.FromDateTime(movement.TimeStamp); 
                Amount = Money.FromAmount(movement.Amount); 
                BrokerAccountId = movement.BrokerAccount.Id; 
                CurrencyId = movement.Currency.Id; 
                Commissions = Money.FromAmount(movement.Commissions); 
                Fees = Money.FromAmount(movement.Fees); 
                MovementType = movement.MovementType.brokerMovementTypeToDatabase();
                Notes = movement.Notes;
                FromCurrencyId = movement.FromCurrency |> Option.map (fun c -> c.Id);
                AmountChanged = movement.AmountChanged |> Option.map Money.FromAmount;
                Audit = AuditableEntity.FromDateTime(movement.TimeStamp);
            }

        [<Extension>]
        static member createTickerToDatabase(ticker: Binnaculum.Core.Models.Ticker) =
            { 
                Id = ticker.Id; 
                Symbol = ticker.Symbol; 
                Image = ticker.Image; 
                Name = ticker.Name; 
                Audit = AuditableEntity.FromDateTime(DateTime.Now);
            }

        [<Extension>]
        static member updateTickerToDatabase(ticker: Binnaculum.Core.Models.Ticker) = task {
            let! current = TickerExtensions.Do.getById ticker.Id |> Async.AwaitTask
            match current with
            | Some dbTicker ->
                let updated = 
                    { dbTicker with 
                        Symbol = ticker.Symbol; 
                        Image = ticker.Image; 
                        Name = ticker.Name; 
                        Audit = AuditableEntity.FromDateTime(DateTime.Now); 
                    }
                return updated
            | None -> return failwithf "Ticker with ID %d not found" ticker.Id
        }

        [<Extension>]
        static member tickerToDatabase(ticker: Binnaculum.Core.Models.Ticker) = task {
            if ticker.Id = 0 then
                return ticker.createTickerToDatabase()
            else
                return! ticker.updateTickerToDatabase() |> Async.AwaitTask
        }

        [<Extension>]
        static member tradeToDatabase(trade: Binnaculum.Core.Models.Trade) =
            { 
                Id = trade.Id
                TimeStamp = DateTimePattern.FromDateTime(trade.TimeStamp) 
                TickerId = trade.Ticker.Id
                BrokerAccountId = trade.BrokerAccount.Id
                CurrencyId = trade.Currency.Id
                Quantity = trade.Quantity
                Price = Money.FromAmount(trade.Price)
                Commissions = Money.FromAmount(trade.Commissions)
                Fees = Money.FromAmount(trade.Fees)
                TradeCode = trade.TradeCode.tradeCodeToDatabase()
                TradeType = trade.TradeType.tradeTypeToDatabase()
                Leveraged = trade.Leveraged
                Notes = trade.Notes
                Audit = AuditableEntity.FromDateTime(trade.TimeStamp)
            }

        [<Extension>]
        static member dividendReceivedToDatabase(dividend: Binnaculum.Core.Models.Dividend) =
            { 
                Id = dividend.Id 
                TimeStamp = DateTimePattern.FromDateTime(dividend.TimeStamp) 
                DividendAmount = Money.FromAmount(dividend.Amount) 
                TickerId = dividend.Ticker.Id 
                CurrencyId = dividend.Currency.Id 
                BrokerAccountId = dividend.BrokerAccount.Id 
                Audit = AuditableEntity.FromDateTime(dividend.TimeStamp)
            }

        [<Extension>]
        static member dividendTaxToDatabase(dividend: Binnaculum.Core.Models.DividendTax) =
            {
                Id = dividend.Id 
                TimeStamp = DateTimePattern.FromDateTime(dividend.TimeStamp) 
                DividendTaxAmount = Money.FromAmount(dividend.TaxAmount) 
                TickerId = dividend.Ticker.Id 
                CurrencyId = dividend.Currency.Id 
                BrokerAccountId = dividend.BrokerAccount.Id  
                Audit = AuditableEntity.FromDateTime(dividend.TimeStamp)
            }

        [<Extension>]
        static member dividendDateToDatabase(dividendDate: Binnaculum.Core.Models.DividendDate) =
            { 
                Id = dividendDate.Id
                TimeStamp = DateTimePattern.FromDateTime(dividendDate.TimeStamp)
                Amount = Money.FromAmount(dividendDate.Amount)
                TickerId = dividendDate.Ticker.Id
                CurrencyId = dividendDate.Currency.Id
                BrokerAccountId = dividendDate.BrokerAccount.Id
                DividendCode = dividendDate.DividendCode.dividendCodeToDatabase()
                Audit = AuditableEntity.FromDateTime(dividendDate.TimeStamp)
            }

        [<Extension>]
        static member optionTradeToDatabase(optionTrade: Binnaculum.Core.Models.OptionTrade) =
            { 
                Id = optionTrade.Id 
                TimeStamp = DateTimePattern.FromDateTime(optionTrade.TimeStamp) 
                ExpirationDate = DateTimePattern.FromDateTime(optionTrade.ExpirationDate)
                Premium = Money.FromAmount(optionTrade.Premium)
                NetPremium = Money.FromAmount(optionTrade.NetPremium)
                TickerId = optionTrade.Ticker.Id
                BrokerAccountId = optionTrade.BrokerAccount.Id
                CurrencyId = optionTrade.Currency.Id
                OptionType = optionTrade.OptionType.optionTypeToDatabase()
                Code = optionTrade.Code.optionCodeToDatabase()
                Strike = Money.FromAmount(optionTrade.Strike)
                Commissions = Money.FromAmount(optionTrade.Commissions)
                Fees = Money.FromAmount(optionTrade.Fees)
                IsOpen = optionTrade.IsOpen
                ClosedWith = Some optionTrade.ClosedWith
                Multiplier = optionTrade.Multiplier
                Notes = optionTrade.Notes
                Audit = AuditableEntity.FromDateTime(optionTrade.TimeStamp)
            }

        [<Extension>]
        static member optionTradesToDatabase(optionTrades: Binnaculum.Core.Models.OptionTrade seq) = 
            optionTrades |> Seq.map (fun trade -> trade.optionTradeToDatabase()) |> Seq.toList
