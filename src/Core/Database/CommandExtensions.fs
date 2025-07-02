module internal CommandExtensions

open System.Runtime.CompilerServices
open Microsoft.Data.Sqlite
open Binnaculum.Core.Database.Do
open Binnaculum.Core
open System

    [<Extension>]
    type Do() =
        
        /// <summary>
        /// Fills the command with parameters from a list of tuples.
        /// </summary>
        [<Extension>]
        static member fillParameters(command: SqliteCommand, parameters: (string * obj) list) =
            for (key, value) in parameters do
                command.Parameters.AddWithValue(key, box value) |> ignore
            command

        /// <summary>
        /// Fills the command with parameters from a list of tuples, and adds an Id parameter.
        /// </summary>
        [<Extension>]
        static member fillEntity<'a when 'a :> IEntity>(command: SqliteCommand, parameters: (string * obj) list, entity: 'a) =
            command.fillParameters(parameters)
            |> fun c -> c.Parameters.AddWithValue(SQLParameterName.Id, box entity.Id) |> ignore
            command

        /// <summary>
        /// Fills the command with parameters from a list of tuples, and adds CreatedAt, UpdatedAt, and Id parameters.
        /// </summary>
        [<Extension>]
        static member fillEntityAuditable<'T when 'T :> IEntity and 'T :> IAuditEntity>(command: SqliteCommand, parameters: (string * obj) list, entity: 'T) =
            // Add existing parameters
            parameters |> List.iter (fun (name, value) -> command.Parameters.AddWithValue(name, value) |> ignore)
    
            // Add audit parameters with explicit conversion
            match entity.CreatedAt with
            | Some createdAt -> command.Parameters.AddWithValue("@CreatedAt", createdAt.ToString()) |> ignore
            | None -> command.Parameters.AddWithValue("@CreatedAt", DBNull.Value) |> ignore
    
            match entity.UpdatedAt with
            | Some updatedAt -> command.Parameters.AddWithValue("@UpdatedAt", updatedAt.ToString()) |> ignore 
            | None -> command.Parameters.AddWithValue("@UpdatedAt", DBNull.Value) |> ignore
    
            // Other parameters
            command.Parameters.AddWithValue("@Id", entity.Id) |> ignore
            command