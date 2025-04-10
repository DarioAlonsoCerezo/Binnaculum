module internal CommandExtensions

open System.Runtime.CompilerServices
open Microsoft.Data.Sqlite

    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member fillParameters(command: SqliteCommand, parameters: (string * obj) list) =
            for (key, value) in parameters do
                command.Parameters.AddWithValue(key, box value) |> ignore
            command