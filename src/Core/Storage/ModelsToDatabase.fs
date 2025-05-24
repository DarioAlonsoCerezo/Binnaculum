namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns
open System
open System.Runtime.CompilerServices
open DiscriminatedToDatabase
open PatternExtensions

module internal ModelsToDatabase =
    
    [<Extension>]
    type Do() =     
        
        [<Extension>]
        static member createBankToDatabase(bank: Binnaculum.Core.Models.Bank) =
            let audit = { CreatedAt = Some(DateTime.Now.fromDateTime()); UpdatedAt = None }
            { 
                Id = 0; 
                Name = bank.Name; 
                Image = bank.Image; 
                Audit = audit
            }

        [<Extension>]
        static member updateBankToDatabase(bank: Binnaculum.Core.Models.Bank) = task {
            let! currentBank = BankExtensions.Do.getById bank.Id |> Async.AwaitTask
            match currentBank with
            | Some current -> 
                let audit = { current.Audit with UpdatedAt = Some(DateTimePattern.FromDateTime(DateTime.Now)) }
                let updatedBank = { current with Name = bank.Name; Image = bank.Image; Audit = audit }
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
            let audit = { CreatedAt = Some(DateTimePattern.FromDateTime(DateTime.Now)); UpdatedAt = None }
            { 
                Id = 0; 
                BankId = bankAccount.Bank.Id; 
                Name = bankAccount.Name; 
                Description = bankAccount.Description; 
                CurrencyId = bankAccount.Currency.Id; 
                Audit = audit
            }

        [<Extension>]
        static member updateBankAccountToDatabase(bankAccount: Binnaculum.Core.Models.BankAccount) = task {
            let! currentBankAccount = BankAccountExtensions.Do.getById bankAccount.Id |> Async.AwaitTask
            match currentBankAccount with
            | Some current -> 
                let audit = { current.Audit with UpdatedAt = Some(DateTimePattern.FromDateTime(DateTime.Now)) }
                let updatedBankAccount = { current with Name = bankAccount.Name; Description = bankAccount.Description; Audit = audit }
                return updatedBankAccount
            | None -> 
                return failwithf "BankAccount with ID %d not found" bankAccount.Id
        }

        [<Extension>]
        static member bankAccountMovementToDatabase(movement: Binnaculum.Core.Models.BankAccountMovement) =
            let movementType = movement.MovementType.bankMovementTypeToDatabase()
            let audit = { CreatedAt = Some(DateTimePattern.FromDateTime(DateTime.Now)); UpdatedAt = None }
            
            { 
                Id = 0; 
                TimeStamp = DateTimePattern.FromDateTime(movement.TimeStamp); 
                Amount = Money.FromAmount(movement.Amount);
                BankAccountId = movement.BankAccount.Id;
                CurrencyId = movement.Currency.Id;
                MovementType = movementType;
                Audit = audit;
            }

        [<Extension>]
        static member brokerMovementToDatabase(movement: Binnaculum.Core.Models.BrokerMovement) =
            let audit = { CreatedAt = Some(DateTimePattern.FromDateTime(movement.TimeStamp)); UpdatedAt = None }
            let timeStampPattern = DateTimePattern.FromDateTime(movement.TimeStamp)
            let amountMoney = Money.FromAmount(movement.Amount)
            let commissionMoney = Money.FromAmount(movement.Commissions)
            let feeMoney = Money.FromAmount(movement.Fees)
            let brokerMovementType = movement.MovementType.brokerMovementTypeToDatabase()            
            { 
                Id = 0; 
                TimeStamp = timeStampPattern; 
                Amount = amountMoney; 
                BrokerAccountId = movement.BrokerAccount.Id; 
                CurrencyId = movement.Currency.Id; 
                Commissions = commissionMoney; 
                Fees = feeMoney; 
                MovementType = brokerMovementType; 
                Audit = audit;
            }
