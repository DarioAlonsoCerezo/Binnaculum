using Binnaculum.Core;

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

public record ACATControl(
    DateTime TimeStamp,
    Models.Ticker Ticker,
    decimal Quantity,
    decimal Commissions,
    decimal Fees,
    string Note);

public record DepositControl(
    DateTime TimeStamp,
    decimal Amount,
    string Currency,
    decimal Commissions,
    decimal Fees,
    string Note);