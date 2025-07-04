namespace Binnaculum.Controls;

public record ConversionControl(
    DateTime TimeStamp,
    decimal AmountFrom,
    decimal AmountTo,
    string CurrencyFrom,
    string CurrencyTo,
    decimal Commissions,
    decimal Fees,
    string Note);