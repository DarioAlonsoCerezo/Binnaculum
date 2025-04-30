namespace Binnaculum.Core.UI
open Binnaculum.Core.Database.DatabaseModel
open BankExtensions
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
        return()
    }

