﻿



module internal DataReaderExtensions

open System.Runtime.CompilerServices
open Microsoft.Data.Sqlite

    [<Extension>]
    type Do() =

        [<Extension>]
        static member getStringOrNone(reader: SqliteDataReader, columName: string) =
            let ordinal = reader.GetOrdinal(columName)
            if reader.IsDBNull(ordinal) then
                None
            else
                Some(reader.GetString(ordinal))

        [<Extension>]
        static member getIntOrNone(reader: SqliteDataReader, columName: string) =
            let ordinal = reader.GetOrdinal(columName)
            if reader.IsDBNull(ordinal) then
                None
            else
                Some(reader.GetInt32(ordinal))