namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns
open System
open System.Runtime.CompilerServices

module internal ModelsToDatabase =
    
    [<Extension>]
    type Do() =     
        
        [<Extension>]
        static member createBankToDatabase(bank: Binnaculum.Core.Models.Bank) =
            let audit = { CreatedAt = Some(DateTimePattern.FromDateTime(DateTime.Now)); UpdatedAt = None }
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
