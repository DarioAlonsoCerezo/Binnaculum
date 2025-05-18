namespace Binnaculum.Core.UI
open Binnaculum.Core.Database.DatabaseModel
open BankExtensions
open BrokerAccountExtensions
open BankAccountExtensions
open BrokerExtensions
open BrokerMovementExtensions
open BankAccountBalanceExtensions
open Binnaculum.Core.Storage.ModelsToDatabase
open System
open Binnaculum.Core.Patterns
open Binnacle.Core.Storage
open Microsoft.FSharp.Core

module Creator =
    
    let SaveBank(bank: Binnaculum.Core.Models.Bank) = task {
        let! databaseBank = bank.bankToDatabase() |> Async.AwaitTask
        do! databaseBank.save() |> Async.AwaitTask |> Async.Ignore
        if bank.Id = 0 then
            do! DataLoader.getOrRefreshBanks() |> Async.AwaitTask |> Async.Ignore
        else
            do! DataLoader.refreshBankAccount(bank.Id) |> Async.AwaitTask |> Async.Ignore        
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

    let SaveBankMovement(movement: Binnaculum.Core.Models.BankAccountMovement) = task {
        let audit = { CreatedAt = Some(DateTimePattern.FromDateTime(movement.TimeStamp)); UpdatedAt = None }
        let timeStampPattern = DateTimePattern.FromDateTime(movement.TimeStamp)
        let amountMoney = Money.FromAmount(movement.Amount)
        let movementType = ModelParser.fromBankMovementTypeToDatabase(movement.MovementType)
        let bankMovement = 
            { 
                Id = 0
                TimeStamp = timeStampPattern
                Amount = amountMoney
                BankAccountId = movement.BankAccount.Id
                CurrencyId = movement.Currency.Id
                MovementType = movementType
                Audit = audit
            }
        do! bankMovement.save() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.loadMovementsFor(None) |> Async.AwaitTask |> Async.Ignore
    }