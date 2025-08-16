namespace Binnaculum.Core

open System.Runtime.CompilerServices
open Binnaculum.Core.Patterns

module internal PatternExtensions =

    [<Extension>]
    type Do() = 
        
        [<Extension>]
        static member fromDateTime(dateTime: System.DateTime) =
            DateTimePattern.FromDateTime(dateTime)

        [<Extension>]
        static member fromDateTimeToSome(dateTime: System.DateTime) =
            Some(dateTime.fromDateTime)
