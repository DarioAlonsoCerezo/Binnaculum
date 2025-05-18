namespace Binnaculum.Core.Storage

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel

module internal DiscriminatedToDatabase =
    
    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member bankMovementTypeToDatabase(movementType: Binnaculum.Core.Models.BankAccountMovementType) =
            match movementType with
            | Binnaculum.Core.Models.BankAccountMovementType.Balance -> BankAccountMovementType.Balance
            | Binnaculum.Core.Models.BankAccountMovementType.Interest -> BankAccountMovementType.Interest
            | Binnaculum.Core.Models.BankAccountMovementType.Fee -> BankAccountMovementType.Fee
