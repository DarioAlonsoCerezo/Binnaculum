namespace Binnaculum.Controls;

public partial class BrokerMovementDepositControl
{
    public event EventHandler<DepositControl> DepositChanged;

    private DepositControl _deposit;

	public BrokerMovementDepositControl()
	{
		InitializeComponent();

        _deposit = new DepositControl(
            TimeStamp: DateTime.Now,
            Amount: 0m,
            Currency: Core.UI.SavedPrefereces.UserPreferences.Value.Currency,
            Commissions: 0m,
            Fees: 0m);
    }

    protected override void StartLoad()
    {
        AmountEntry.Events().CurrencyChanged
            .Subscribe(x =>
            {
                _deposit = _deposit with { Currency = x };

                DepositChanged?.Invoke(this, _deposit);
            })
            .DisposeWith(Disposables);

        AmountEntry.Events().TextChanged
            .Subscribe(x =>
            {
                if (decimal.TryParse(x.NewTextValue, out var amount))
                {
                    _deposit = _deposit with { Amount = amount };
                    DepositChanged?.Invoke(this, _deposit);
                }
            })
            .DisposeWith(Disposables);

        DateTimePicker.Events().DateSelected
            .Subscribe(x =>
            {
                _deposit = _deposit with { TimeStamp = x };
                DepositChanged?.Invoke(this, _deposit);
            })
            .DisposeWith(Disposables);

        FeesAndCommissions.Events().FeeAndCommissionChanged
            .Subscribe(x =>
            {
                _deposit = _deposit with { Commissions = x.Commission, Fees = x.Fee };
                DepositChanged?.Invoke(this, _deposit);
            })
            .DisposeWith(Disposables);
    }
}

public record DepositControl(
    DateTime TimeStamp, 
    decimal Amount, 
    string Currency,
    decimal Commissions,
    decimal Fees);