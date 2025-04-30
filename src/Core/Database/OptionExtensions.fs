module internal OptionExtensions

open System
open System.Runtime.CompilerServices

[<Extension>]
type Do() =
    /// Converts an F# option value to a database value.
    /// Returns the unwrapped value if present, otherwise returns DBNull.Value.
    [<Extension>]
    static member ToDbValue(option: 'T option) : obj =
        match option with
        | Some value -> box value
        | None -> box DBNull.Value