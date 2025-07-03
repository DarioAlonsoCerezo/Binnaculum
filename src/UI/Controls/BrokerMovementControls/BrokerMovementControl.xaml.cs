namespace Binnaculum.Controls;

public partial class BrokerMovementControl
{
    public event EventHandler<DepositControl> DepositChanged;

    private DepositControl _deposit;

    public DepositControl DepositData => _deposit;

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

    public static readonly BindableProperty ShowFromCurrencyProperty =
        BindableProperty.Create(
            nameof(ShowFromCurrency),
            typeof(bool),
            typeof(BrokerMovementControl),
            false, // Default to hidden
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is BrokerMovementControl control && newValue is bool showFromCurrency)
                {
                    control.FromCurrencyEntry.IsVisible = showFromCurrency;
                }
            });

    public bool ShowFromCurrency
    {
        get => (bool)GetValue(ShowFromCurrencyProperty);
        set => SetValue(ShowFromCurrencyProperty, value);
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

    public BrokerMovementControl()
	{
		InitializeComponent();

        // Set default currency visibility (true by default from the bindable property)
        AmountEntry.IsCurrencyVisible = ShowCurrency;
        FromCurrencyEntry.IsVisible = ShowFromCurrency;

        _deposit = new DepositControl(
            TimeStamp: DateTime.Now,
            Amount: 0m,
            Currency: Core.UI.SavedPrefereces.UserPreferences.Value.Currency,
            Commissions: 0m,
            Fees: 0m,
            Note: string.Empty,
            FromCurrency: null);
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

        Notes.Events().TextChanged
            .Subscribe(x =>
            {
                _deposit = _deposit with { Note = x.NewTextValue };
                DepositChanged?.Invoke(this, _deposit);
            })
            .DisposeWith(Disposables);

        FromCurrencyEntry.Events().CurrencyChanged
            .Subscribe(x =>
            {
                _deposit = _deposit with { FromCurrency = x };
                DepositChanged?.Invoke(this, _deposit);
            })
            .DisposeWith(Disposables);
    }
}