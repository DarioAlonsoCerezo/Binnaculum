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
}
