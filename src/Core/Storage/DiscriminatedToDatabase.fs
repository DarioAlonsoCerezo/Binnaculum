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

        [<Extension>]
        static member brokerMoveventTypeToDatabase(movementType: Binnaculum.Core.Models.MovementType) =
            match movementType with
            | Binnaculum.Core.Models.MovementType.Deposit -> BrokerMovementType.Deposit
            | Binnaculum.Core.Models.MovementType.Withdrawal -> BrokerMovementType.Withdrawal
            | Binnaculum.Core.Models.MovementType.Fee -> BrokerMovementType.Fee
            | Binnaculum.Core.Models.MovementType.InterestsGained -> BrokerMovementType.InterestsGained
            | Binnaculum.Core.Models.MovementType.Lending -> BrokerMovementType.Lending
            | Binnaculum.Core.Models.MovementType.ACATMoneyTransfer -> BrokerMovementType.ACATMoneyTransfer
            | Binnaculum.Core.Models.MovementType.ACATSecuritiesTransfer -> BrokerMovementType.ACATSecuritiesTransfer
            | Binnaculum.Core.Models.MovementType.InterestsPaid -> BrokerMovementType.InterestsPaid
            | Binnaculum.Core.Models.MovementType.Conversion -> BrokerMovementType.Conversion
            | _ -> failwithf "MovementType %A is not a BrokerMovementType" movementType