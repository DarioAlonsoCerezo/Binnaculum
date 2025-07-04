namespace Binnaculum.Controls;

public partial class BrokerMovementControl
{
    public event EventHandler<DepositControl> DepositChanged;
    public event EventHandler<ConversionControl> ConversionChanged;

    private DepositControl _deposit;
    private ConversionControl _conversion;

    public DepositControl DepositData => _deposit;
    public ConversionControl ConversionData => _conversion;

    public static readonly BindableProperty HideFeesAndCommissionsProperty =
        BindableProperty.Create(
            nameof(HideFeesAndCommissions), 
            typeof(bool), 
            typeof(BrokerMovementControl), 
            false,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is BrokerMovementControl control && newValue is bool hideFeesAndCommissions)
                {
                    control.FeesAndCommissions.IsVisible = !hideFeesAndCommissions;
                }
            });

    public bool HideFeesAndCommissions
    {
        get => (bool)GetValue(HideFeesAndCommissionsProperty);
        set => SetValue(HideFeesAndCommissionsProperty, value);
    }

    public static readonly BindableProperty ShowCurrencyProperty =
        BindableProperty.Create(
            nameof(ShowCurrency),
            typeof(bool),
            typeof(BrokerMovementControl),
            true, // Default to visible
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is BrokerMovementControl control && newValue is bool showCurrency)
                {
                    control.AmountEntry.IsCurrencyVisible = showCurrency;
                }
            });

    public bool ShowCurrency
    {
        get => (bool)GetValue(ShowCurrencyProperty);
        set => SetValue(ShowCurrencyProperty, value);
    }

    public static readonly BindableProperty HideAmountProperty =
        BindableProperty.Create(
            nameof(HideAmount),
            typeof(bool),
            typeof(BrokerMovementControl),
            false,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is BrokerMovementControl control && newValue is bool hideAmount)
                {
                    control.AmountEntry.IsVisible = !hideAmount;
                    control.Conversion.IsVisible = hideAmount;
                }
            });

    public bool HideAmount
    {
        get => (bool)GetValue(HideAmountProperty);
        set => SetValue(HideAmountProperty, value);
    }

    public BrokerMovementControl()
	{
		InitializeComponent();

        // Set default currency visibility (true by default from the bindable property)
        AmountEntry.IsCurrencyVisible = ShowCurrency;

        _deposit = new DepositControl(
            TimeStamp: DateTime.Now,
            Amount: 0m,
            Currency: Core.UI.SavedPrefereces.UserPreferences.Value.Currency,
            Commissions: 0m,
            Fees: 0m,
            Note: string.Empty);

        _conversion = new ConversionControl(
            TimeStamp: DateTime.Now,
            AmountFrom: 0m,
            AmountTo: 0m,
            CurrencyFrom: string.Empty,
            CurrencyTo: string.Empty, 
            Commissions: 0m,
            Fees: 0m, 
            Note: string.Empty);
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

        Conversion.Events().ConversionChanged
            .Subscribe(x =>
            {
                _conversion = _conversion with
                {
                    AmountFrom = x.AmountFrom,
                    AmountTo = x.AmountTo,
                    CurrencyFrom = x.CurrencyFrom,
                    CurrencyTo = x.CurrencyTo
                };
                ConversionChanged?.Invoke(this, _conversion);
            })
            .DisposeWith(Disposables);

        DateTimePicker.Events().DateSelected
            .Subscribe(x =>
            {
                _deposit = _deposit with { TimeStamp = x };
                _conversion = _conversion with { TimeStamp = x };
                DepositChanged?.Invoke(this, _deposit);
                ConversionChanged?.Invoke(this, _conversion);
            })
            .DisposeWith(Disposables);

        FeesAndCommissions.Events().FeeAndCommissionChanged
            .Subscribe(x =>
            {
                _deposit = _deposit with { Commissions = x.Commission, Fees = x.Fee };
                _conversion = _conversion with { Commissions = x.Commission, Fees = x.Fee };
                DepositChanged?.Invoke(this, _deposit);
                ConversionChanged?.Invoke(this, _conversion);
            })
            .DisposeWith(Disposables);

        Notes.Events().TextChanged
            .Subscribe(x =>
            {
                _deposit = _deposit with { Note = x.NewTextValue };
                _conversion = _conversion with { Note = x.NewTextValue };
                DepositChanged?.Invoke(this, _deposit);
                ConversionChanged?.Invoke(this, _conversion);
            })
            .DisposeWith(Disposables);
    }
}