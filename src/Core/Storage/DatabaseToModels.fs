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
                }
            {
                Type = AccountMovementType.BrokerMovement
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