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
                    ("@TimeStamp", brokerMovement.TimeStamp.ToString());
                    ("@Amount", brokerMovement.Amount);
                    ("@CurrencyId", brokerMovement.CurrencyId);
                    ("@BrokerAccountId", brokerMovement.BrokerAccountId);
                    ("@Commissions", brokerMovement.Commissions);
                    ("@Fees", brokerMovement.Fees);
                    ("@MovementType", fromMovementTypeToDatabase brokerMovement.MovementType);
                    ("@CreatedAt", brokerMovement.CreatedAt);
                    ("@UpdatedAt", brokerMovement.UpdatedAt);
                ])

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.getInt32 "Id"
                TimeStamp = reader.getDateTimePattern "TimeStamp"
                Amount = reader.getMoney "Amount"
                CurrencyId = reader.getInt32 "CurrencyId"
                BrokerAccountId = reader.getInt32 "BrokerAccountId"
                Commissions = reader.getMoney "Commissions"
                Fees = reader.getMoney "Fees"
                MovementType = reader.getString "MovementType" |> fromDataseToMovementType
                CreatedAt = reader.getDataTimeOrNone "CreatedAt"
                UpdatedAt = reader.getDataTimeOrNone "UpdatedAt"
            }

        [<Extension>]
        static member save(brokerMovement: BrokerMovement) = Database.Do.saveEntity brokerMovement (fun t c -> t.fill c) 

        [<Extension>]
        static member delete(brokerMovement: BrokerMovement) = Database.Do.deleteEntity brokerMovement

        static member getAll() = Database.Do.getAllEntities Do.read

        static member getById(id: int) = Database.Do.getById id Do.read

