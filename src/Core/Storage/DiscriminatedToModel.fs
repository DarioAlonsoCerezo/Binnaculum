namespace Binnaculum.Core.Storage

open System.Runtime.CompilerServices
open Binnaculum.Core.Models
open Binnaculum.Core

module internal DiscriminatedToModel =
    
    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member bankMovementTypeToModel(movementType: Binnaculum.Core.Database.DatabaseModel.BankAccountMovementType) =
            match movementType with
            | Binnaculum.Core.Database.DatabaseModel.BankAccountMovementType.Balance -> BankAccountMovementType.Balance
            | Binnaculum.Core.Database.DatabaseModel.BankAccountMovementType.Interest -> BankAccountMovementType.Interest
            | Binnaculum.Core.Database.DatabaseModel.BankAccountMovementType.Fee -> BankAccountMovementType.Fee

        [<Extension>]
        static member supportedBrokerToModel(databaseSupportedBroker: Binnaculum.Core.Database.DatabaseModel.SupportedBroker) =
            match databaseSupportedBroker with
            | Binnaculum.Core.Database.DatabaseModel.SupportedBroker.IBKR -> Keys.Broker_IBKR
            | Binnaculum.Core.Database.DatabaseModel.SupportedBroker.Tastytrade -> Keys.Broker_Tastytrade
            | Binnaculum.Core.Database.DatabaseModel.SupportedBroker.SigmaTrade -> Keys.Broker_SigmaTrade
            | Binnaculum.Core.Database.DatabaseModel.SupportedBroker.Unknown -> Keys.Broker_Unknown
