module internal CommandExtensions

open System.Runtime.CompilerServices
open Microsoft.Data.Sqlite
open Binnaculum.Core.Database.Do
open Binnaculum.Core

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
        /// Fills the command with parameters from a list of tuples, and adds CreatedAt and UpdatedAt parameters.
        /// </summary>
        [<Extension>]
        static member fillAuditable<'a when 'a :> IAuditEntity>(command: SqliteCommand, parameters: (string * obj) list, entity: 'a) =
            let filled = command.fillParameters(parameters)
            filled.Parameters.AddWithValue(SQLParameterName.CreatedAt, box entity.CreatedAt) |> ignore
            filled.Parameters.AddWithValue(SQLParameterName.UpdatedAt, box entity.UpdatedAt) |> ignore
            filled

        /// <summary>
        /// Fills the command with parameters from a list of tuples, and adds CreatedAt, UpdatedAt, and Id parameters.
        /// </summary>
        [<Extension>]
        static member fillEntityAuditable<'a when 'a :> IEntity and 'a :> IAuditEntity>(command: SqliteCommand, parameters: (string * obj) list, entity: 'a) =
            command.fillEntity<'a>(parameters, entity)
            |> fun c -> c.fillAuditable<'a>(parameters, entity)