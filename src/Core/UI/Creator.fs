namespace Binnaculum.Core.UI
open Binnaculum.Core.Database.DatabaseModel
open BankExtensions
open BrokerAccountExtensions
open BankAccountExtensions
open BrokerExtensions
open BrokerMovementExtensions
open System
open Binnaculum.Core.Patterns
open Binnacle.Core.Storage
open Microsoft.FSharp.Core

module Creator =
    
    let SaveBank(name, icon: string option) = task {
        let audit = { CreatedAt = Some(DateTimePattern.FromDateTime(DateTime.Now)); UpdatedAt = None }
        let bank = { Id = 0; Name = name; Image = icon; Audit = audit }
        do! bank.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.getOrRefreshBanks() |> Async.AwaitTask |> Async.Ignore
    }

    let SaveBroker(name, icon) = task {
        let broker = { Id = 0; Name = name; Image = icon; SupportedBroker = SupportedBroker.Unknown }
        do! broker.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.getOrRefreshAllBrokers() |> Async.AwaitTask |> Async.Ignore
    }

    let SaveBankAccount(bankId, name, currencyId) = task {
        let audit = { CreatedAt = Some(DateTimePattern.FromDateTime(DateTime.Now)); UpdatedAt = None }
        let account = { Id = 0; BankId = bankId; Name = name; Description = None; CurrencyId = currencyId; Audit = audit; }
        do! account.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.getOrRefreshAllAccounts() |> Async.AwaitTask |> Async.Ignore
    }

    let SaveBrokerAccount(brokerId: int, accountNumber: string) = task {
        let audit = { CreatedAt = Some(DateTimePattern.FromDateTime(DateTime.Now)); UpdatedAt = None }
        let account = { Id = 0; BrokerId = brokerId; AccountNumber = accountNumber; Audit = audit }
        do! account.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.getOrRefreshAllAccounts() |> Async.AwaitTask |> Async.Ignore
    }

    let SaveDeposit(uiDeposit: Binnaculum.Core.Models.UiDeposit) = task {
        let audit = { CreatedAt = Some(DateTimePattern.FromDateTime(DateTime.Now)); UpdatedAt = None }
        let timeStampPattern = DateTimePattern.FromDateTime(uiDeposit.Timestamp)
        let amountMoney = Money.FromAmount(uiDeposit.Amount)
        let commissionMoney = Money.FromAmount(uiDeposit.Commissions)
        let feeMoney = Money.FromAmount(uiDeposit.Fees)
        let brokerMovementType = ModelParser.fromMovementTypeToBrokerMoveventType(Binnaculum.Core.Models.MovementType.Deposit)
        let movement = 
            { 
                Id = 0; 
                TimeStamp = timeStampPattern; 
                Amount = amountMoney; 
                CurrencyId = uiDeposit.CurrencyId; 
                BrokerAccountId = uiDeposit.BrokerAccountId; 
                Commissions = commissionMoney; 
                Fees = feeMoney; 
                MovementType = brokerMovementType; 
                Audit = audit;
            }

        do! movement.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.loadMovementsFor(None) |> Async.AwaitTask 
    }

    let SaveBankIconChange(iconPath, bankId) = task {
        let! bank = BankExtensions.Do.getById bankId |> Async.AwaitTask
        match bank with
        | Some b ->
            let audit = { b.Audit with UpdatedAt = None }
            let bank = { b with Image = Some iconPath; Audit = audit }
            do! bank.save() |> Async.AwaitTask |> Async.Ignore
            do! DataLoader.refreshBankAccount(bankId) |> Async.AwaitTask |> Async.Ignore
        | None ->        
            return()
    }

    let SaveBankMovement(movement: Binnaculum.Core.Models.BankAccountMovement) = task {
        return()
    }