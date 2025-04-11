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

            /// Converts the DateTimePattern back to a string in ISO 8601 format
            override this.ToString() =
                let (DateTimePattern dateTime) = this
                dateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)

            /// Gets the underlying DateTime value
            member this.Value =
                let (DateTimePattern dateTime) = this
                dateTime

    type Money =
        private | Money of int
        with
            /// Creates a Money instance from an integer (cents)
            static member FromCents(cents: int) =
                Money cents

            /// Creates a Money instance from a decimal (dollars)
            static member FromAmount(dollars: decimal) =
                Money (int (Math.Round(dollars * 100M)))

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

            /// Converts the Money instance to cents
            member this.ToCents() =
                let (Money cents) = this
                cents

            /// Converts the Money instance to dollars
            member this.ToAmount() =
                let (Money cents) = this
                decimal cents / 100M

            /// Formats the Money instance as a string in the given culture
            member this.ToString(culture: CultureInfo) =
                let dollars = this.ToAmount()
                dollars.ToString("N2", culture)

            /// Adds two Money instances
            static member (+) (Money a, Money b) =
                Money (a + b)

            /// Subtracts one Money instance from another
            static member (-) (Money a, Money b) =
                Money (a - b)

            /// Multiplies a Money instance by a scalar
            static member (*) (Money a, multiplier: int) =
                Money (a * multiplier)

            /// Divides a Money instance by a scalar
            static member (/) (Money a, divisor: int) =
                Money (a / divisor)

            /// Default string representation (uses invariant culture)
            override this.ToString() =
                this.ToString(CultureInfo.InvariantCulture)
