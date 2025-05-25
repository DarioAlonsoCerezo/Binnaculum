namespace Binnaculum.Extensions;

public static class EntryExtensions
{
    public static decimal ToToDecimalOrZero(this Entry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Text))
            return 0m;
        if (decimal.TryParse(entry.Text, out var value))
            return value;
        return 0m; // or throw an exception, depending on your needs
    }
}
