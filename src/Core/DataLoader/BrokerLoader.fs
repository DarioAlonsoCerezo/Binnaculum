namespace Binnaculum.Core.DataLoader

open Binnaculum.Core.DatabaseToModels
open Binnaculum.Core.UI
open DynamicData

module internal BrokerLoader =
    let load() = task {
        do! BrokerExtensions.Do.insertIfNotExists() |> Async.AwaitTask
        let! databaseBrokers = BrokerExtensions.Do.getAll() |> Async.AwaitTask
        let brokers = databaseBrokers.brokersToModel()
        Collections.Brokers.EditDiff brokers

        //As we allow users create brokers, we add this default broker to recognize it in the UI (if not already present)
        let hasDefaultBroker = Collections.Brokers.Items |> Seq.exists (fun b -> b.Id = -1)
        if not hasDefaultBroker then
            Collections.Brokers.Add({ Id = -1; Name = "AccountCreator_Create_Broker"; Image = "broker"; SupportedBroker = "Unknown"; })
    }

