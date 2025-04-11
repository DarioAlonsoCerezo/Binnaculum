module internal BrokerMovementExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.Database.TypeParser
open DataReaderExtensions
open CommandExtensions

    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member fill(brokerMovement: BrokerMovement, command: SqliteCommand) =
            command.fillParameters(
                [
                    ("@Id", brokerMovement.Id);
                    ("@TimeStamp", brokerMovement.TimeStamp);
                    ("@Amount", brokerMovement.Amount);
                    ("@CurrencyId", brokerMovement.CurrencyId);
                    ("@BrokerAccountId", brokerMovement.BrokerAccountId);
                    ("@Commissions", brokerMovement.Commissions);
                    ("@Fees", brokerMovement.Fees);
                    ("@MovementType", fromMovementTypeToDatabase brokerMovement.MovementType);
                ])

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.getInt32 "Id"
                TimeStamp = reader.getDateTime "TimeStamp"
                Amount = reader.getDecimal "Amount"
                CurrencyId = reader.getInt32 "CurrencyId"
                BrokerAccountId = reader.getInt32 "BrokerAccountId"
                Commissions = reader.getDecimal "Commissions"
                Fees = reader.getDecimal "Fees"
                MovementType = reader.getString "MovementType" |> fromDataseToMovementType
            }

        [<Extension>]
        static member save(brokerMovement: BrokerMovement) = Database.Do.saveEntity brokerMovement (fun t c -> t.fill c) 

        [<Extension>]
        static member delete(brokerMovement: BrokerMovement) = Database.Do.deleteEntity brokerMovement

        static member getAll() = Database.Do.getAllEntities Do.read

        static member getById(id: int) = Database.Do.getById id Do.read

