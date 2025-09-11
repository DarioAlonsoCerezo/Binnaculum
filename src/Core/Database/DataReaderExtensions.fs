module internal DataReaderExtensions

open System.Runtime.CompilerServices
open Microsoft.Data.Sqlite
open Binnaculum.Core.Patterns
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core

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
        static member getString(reader: SqliteDataReader, columnName: string) =
            reader.GetString(reader.GetOrdinal(columnName))

        [<Extension>]
        static member getBoolean(reader: SqliteDataReader, columnName: string) =
            reader.GetBoolean(reader.GetOrdinal(columnName))

        [<Extension>]
        static member getDateTimePattern(reader: SqliteDataReader, columName: string) =
            DateTimePattern.Parse(reader.GetString(reader.GetOrdinal(columName)))

        [<Extension>]
        static member getMoney(reader: SqliteDataReader, columName: string) =
            try
                let textValue = reader.getString columName
                System.Diagnostics.Debug.WriteLine($"[DataReaderExtensions] Reading Money from column {columName} - Raw text value: '{textValue}'")
                let decimalValue = System.Decimal.Parse(textValue, System.Globalization.CultureInfo.InvariantCulture)
                System.Diagnostics.Debug.WriteLine($"[DataReaderExtensions] Parsed decimal value: {decimalValue}")
                Money.FromAmount(decimalValue)
            with
            | ex ->
                System.Diagnostics.Debug.WriteLine($"[DataReaderExtensions] Error reading Money from column {columName}: {ex.Message}")
                let fallbackValue = reader.getDecimal columName
                System.Diagnostics.Debug.WriteLine($"[DataReaderExtensions] Fallback decimal read: {fallbackValue}")
                Money.FromAmount(decimal fallbackValue)

        [<Extension>]
        static member getMoneyOrNone(reader: SqliteDataReader, columName: string) =
            let ordinal = reader.GetOrdinal(columName)
            if reader.IsDBNull(ordinal) then
                None
            else
                let value = reader.getDecimal columName
                Some(Money.FromAmount(decimal value))

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

        [<Extension>]
        static member getDecimalOrNone(reader: SqliteDataReader, columName: string) =
            let ordinal = reader.GetOrdinal(columName)
            if reader.IsDBNull(ordinal) then
                None
            else
                Some(reader.GetDecimal(ordinal))

        [<Extension>]
        static member getDataTimeOrNone(reader: SqliteDataReader, columName: string) =
            let ordinal = reader.GetOrdinal(columName)
            if reader.IsDBNull(ordinal) then
                None
            else
                Some(reader.GetDateTime(ordinal))

        [<Extension>]
        static member getDateTimePatternOrNone(reader: SqliteDataReader, columName: string) =
            let ordinal = reader.GetOrdinal(columName)
            if reader.IsDBNull(ordinal) then
                None
            else
                Some(DateTimePattern.Parse(reader.GetString(ordinal)))

        [<Extension>]
        static member getAudit(reader: SqliteDataReader) =
            {
                CreatedAt = reader.getDateTimePatternOrNone FieldName.CreatedAt
                UpdatedAt = reader.getDateTimePatternOrNone FieldName.UpdatedAt
            }
        