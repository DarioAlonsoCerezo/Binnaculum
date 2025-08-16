namespace Binnaculum.Core.DataLoader

open Binnaculum.Core.DatabaseToModels
open Binnaculum.Core.UI
open DynamicData
open System

module internal BankLoader =
    let load() = task {
        let! databaseBanks = BankExtensions.Do.getAll() |> Async.AwaitTask
        let banks = databaseBanks.banksToModel()
                
        Collections.Banks.EditDiff banks            

        //As we allow users create banks, we add this default bank to recognize it in the UI (if not already present)
        let hasDefaultBank = Collections.Banks.Items |> Seq.exists (fun b -> b.Id = -1)
        if not hasDefaultBank then
            Collections.Banks.Add({ Id = -1; Name = "AccountCreator_Create_Bank"; Image = Some "bank"; CreatedAt = DateTime.Now; })
    }