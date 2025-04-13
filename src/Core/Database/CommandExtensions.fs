module internal CommandExtensions

open System.Runtime.CompilerServices
open Microsoft.Data.Sqlite
open Binnaculum.Core.Database.Do
open Binnaculum.Core

    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member fillParameters(command: SqliteCommand, parameters: (string * obj) list) =
            for (key, value) in parameters do
                command.Parameters.AddWithValue(key, box value) |> ignore
            command

        [<Extension>]
        static member fillEntity<'a when 'a :> IEntity>(command: SqliteCommand, parameters: (string * obj) list) =
            command.fillParameters(parameters)
            |> fun c -> c.Parameters.AddWithValue(SQLParameterName.Id, box Unchecked.defaultof<'a>.Id) |> ignore
            command

        [<Extension>]
        static member fillAuditable<'T when 'T :> IAuditEntity>(command: SqliteCommand, parameters: (string * obj) list) =
            let filled = command.fillParameters(parameters)
            filled.Parameters.AddWithValue(SQLParameterName.CreatedAt, box Unchecked.defaultof<'T>.CreatedAt) |> ignore
            filled.Parameters.AddWithValue(SQLParameterName.UpdatedAt, box Unchecked.defaultof<'T>.UpdatedAt) |> ignore
            filled

        [<Extension>]
        static member fillEntityAuditable<'a when 'a :> IEntity and 'a :> IAuditEntity>(command: SqliteCommand, parameters: (string * obj) list) =
            command.fillEntity<'a>(parameters)
            |> fun c -> c.fillAuditable<'a>(parameters)