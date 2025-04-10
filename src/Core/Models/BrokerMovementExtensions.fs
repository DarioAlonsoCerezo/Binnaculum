module internal BrokerMovementExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.SQL
open Binnaculum.Core.Database.TypeParser

    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member fill(brokerMovement: BrokerMovement, command: SqliteCommand) =
            command.Parameters.AddWithValue("@TimeStamp", brokerMovement.TimeStamp) |> ignore
            command.Parameters.AddWithValue("@Amount", brokerMovement.Amount) |> ignore
            command.Parameters.AddWithValue("@CurrencyId", brokerMovement.CurrencyId) |> ignore
            command.Parameters.AddWithValue("@BrokerAccountId", brokerMovement.BrokerAccountId) |> ignore
            command.Parameters.AddWithValue("@Commissions", brokerMovement.Commissions) |> ignore
            command.Parameters.AddWithValue("@Fees", brokerMovement.Fees) |> ignore
            command.Parameters.AddWithValue("@MovementType", fromMovementTypeToDatabase brokerMovement.MovementType) |> ignore
            command

        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id"))
                TimeStamp = reader.GetDateTime(reader.GetOrdinal("TimeStamp"))
                Amount = reader.GetDecimal(reader.GetOrdinal("Amount"))
                CurrencyId = reader.GetInt32(reader.GetOrdinal("CurrencyId"))
                BrokerAccountId = reader.GetInt32(reader.GetOrdinal("BrokerAccountId"))
                Commissions = reader.GetDecimal(reader.GetOrdinal("Commissions"))
                Fees = reader.GetDecimal(reader.GetOrdinal("Fees"))
                MovementType = reader.GetString(reader.GetOrdinal("MovementType")) |> fromDataseToMovementType
            }

        [<Extension>]
        static member save(brokerMovement: BrokerMovement) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- 
                match brokerMovement.Id with
                | 0 -> BrokerMovementQuery.insert
                | _ -> BrokerMovementQuery.update
            do! Database.Do.executeNonQuery(brokerMovement.fill command) |> Async.AwaitTask |> Async.Ignore
        }

        [<Extension>]
        static member delete(brokerMovement: BrokerMovement) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BrokerMovementQuery.delete
            command.Parameters.AddWithValue("@Id", brokerMovement.Id) |> ignore
            do! Database.Do.executeNonQuery(command) |> Async.AwaitTask |> Async.Ignore
        }

        static member getAll() = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BrokerMovementQuery.getAll
            let! brokerMovements = Database.Do.readAll<BrokerMovement>(command, Do.read)
            return brokerMovements
        }

        static member getById(id: int) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- BrokerMovementQuery.getById
            command.Parameters.AddWithValue("@Id", id) |> ignore
            let! brokerMovements = Database.Do.readAll<BrokerMovement>(command, Do.read)
            return brokerMovements |> List.tryHead
        }

