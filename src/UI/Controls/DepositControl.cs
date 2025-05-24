namespace Binnaculum.Controls;

public record DepositControl(
    DateTime TimeStamp,
    decimal Amount,
    string Currency,
    decimal Commissions,
    decimal Fees,
    string Note);