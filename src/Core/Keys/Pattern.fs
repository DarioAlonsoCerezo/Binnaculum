namespace Binnaculum.Core

open System
open System.Globalization

module internal Patterns =

    type DateTimePattern =
        private | DateTimePattern of DateTime
        with
            /// Creates a DateTimePattern from a DateTime value
            static member FromDateTime(dateTime: DateTime) =
                DateTimePattern dateTime

            /// Parses a string into a DateTimePattern, ensuring it matches the ISO 8601 pattern
            static member Parse(value: string) =
                match DateTime.TryParseExact(value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None) with
                | true, dateTime -> DateTimePattern dateTime
                | false, _ -> failwith $"Invalid DateTime format: {value}. Expected format: yyyy-MM-ddTHH:mm:ss"

            /// Tries to parse a string into a DateTimePattern, returning an option
            static member TryParse(value: string) =
                match DateTime.TryParseExact(value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None) with
                | true, dateTime -> Some(DateTimePattern dateTime)
                | false, _ -> None

            /// Returns a new DateTimePattern with the same date but time set to 23:59:59
            member this.WithEndOfDay() =
                let (DateTimePattern dateTime) = this
                let endOfDay = DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 23, 59, 59, dateTime.Kind)
                DateTimePattern endOfDay

            /// Converts the DateTimePattern back to a string in ISO 8601 format
            override this.ToString() =
                let (DateTimePattern dateTime) = this
                dateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)

            /// Gets the underlying DateTime value
            member this.Value =
                let (DateTimePattern dateTime) = this
                dateTime

    type Money =
        private | Money of decimal
        with
            /// Creates a Money instance from a decimal value
            static member FromAmount(amount: decimal) =
                Money amount

            /// Parses a string into a Money instance, respecting the given culture
            static member Parse(value: string, culture: CultureInfo) =
                match Decimal.TryParse(value, NumberStyles.Number, culture) with
                | true, parsedDecimal -> Money.FromAmount(parsedDecimal)
                | false, _ -> failwith $"Invalid decimal format: {value} for culture {culture.Name}"

            /// Tries to parse a string into a Money instance, respecting the given culture
            static member TryParse(value: string, culture: CultureInfo) =
                match Decimal.TryParse(value, NumberStyles.Number, culture) with
                | true, parsedDecimal -> Some(Money.FromAmount(parsedDecimal))
                | false, _ -> None

            /// Gets the raw decimal value
            member this.Value =
                let (Money amount) = this
                amount
