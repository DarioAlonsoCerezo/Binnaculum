namespace Binnaculum.Core.Storage

open System.Runtime.CompilerServices
open Binnaculum.Core.Models
open Binnaculum.Core.UI.Collections

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