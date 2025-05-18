namespace Binnaculum.Core.Storage

open System.Runtime.CompilerServices
open Binnaculum.Core.Models

module internal DiscriminatedToModel =
    
    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member bankMovementTypeToModel(movementType: Binnaculum.Core.Database.DatabaseModel.BankAccountMovementType) =
            match movementType with
            | Binnaculum.Core.Database.DatabaseModel.BankAccountMovementType.Balance -> BankAccountMovementType.Balance
            | Binnaculum.Core.Database.DatabaseModel.BankAccountMovementType.Interest -> BankAccountMovementType.Interest
            | Binnaculum.Core.Database.DatabaseModel.BankAccountMovementType.Fee -> BankAccountMovementType.Fee

