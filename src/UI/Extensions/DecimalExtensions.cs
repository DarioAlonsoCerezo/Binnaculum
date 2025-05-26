using Markdig.Helpers;

namespace Binnaculum.Extensions;

public static class DecimalExtensions
{
    /// <summary>
    /// This method try to simplify a decimal number and return as less decimal places as possible. 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string Simplifyed(this decimal value)
    {
        if (value == 0)
            return "0";
        // Determine the number of decimal places to keep
        var decimalPlaces = 2; // Default to 2 decimal places
        if (value % 1 == 0) // If it's a whole number
            decimalPlaces = 0;
        else if (Math.Abs(value) < 1) // If it's less than 1
            decimalPlaces = 4; // More precision for small numbers
        return value.ToString($"F{decimalPlaces}");
    }

    public static decimal ToDecimalOrZero(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0m;
        if (decimal.TryParse(value, out var result))
            return result;
        return 0m; // or throw an exception, depending on your needs
    }

    /// <summary>
    /// Converts a string to a decimal value representing a monetary amount.
    /// The method uses invariant culture for parsing and ensures the result has exactly two decimal places.
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>A decimal value rounded to two decimal places, or 0 if parsing fails</returns>
    public static decimal ToMoney(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0m;
        
        if (decimal.TryParse(
            value,
            NumberStyles.Currency | NumberStyles.Number, 
            System.Globalization.CultureInfo.InvariantCulture,
            out var result))
        {
            // Round to two decimal places for monetary values
            return Math.Round(result, 2);
        }

        // If parsing fails, return zero
        return 0m;
    }
}
