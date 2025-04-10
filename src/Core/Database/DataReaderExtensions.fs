



module internal DataReaderExtensions

open System.Runtime.CompilerServices
open Microsoft.Data.Sqlite

    [<Extension>]
    type Do() =
        [<Extension>]
        static member getInt32(reader: SqliteDataReader, columnName: string) =
            reader.GetInt32(reader.GetOrdinal(columnName))

        [<Extension>]
        static member getDateTime(reader: SqliteDataReader, columnName: string) =
            reader.GetDateTime(reader.GetOrdinal(columnName))

        [<Extension>]
        static member getDecimal(reader: SqliteDataReader, columnName: string) =
            reader.GetDecimal(reader.GetOrdinal(columnName))

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