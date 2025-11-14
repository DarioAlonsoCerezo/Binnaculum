using Binnaculum.Core;
using Binnaculum.Core.UI;
using Binnaculum.Popups;

namespace Binnaculum.Controls;

public partial class BrokerMovementControl
{
    public event EventHandler<DepositControl> DepositChanged;
    public event EventHandler<ConversionControl> ConversionChanged;
    public event EventHandler<ACATControl> ACATChanged;

    private DepositControl _deposit;
    private ConversionControl _conversion;
    private ACATControl _acat;

    public DepositControl DepositData => _deposit;
    public ConversionControl ConversionData => _conversion;
    public ACATControl ACATData => _acat;

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

    public static readonly BindableProperty ShowTickerProperty =
        BindableProperty.Create(
            nameof(ShowTicker),
            typeof(bool),
            typeof(BrokerMovementControl),
            true, // Default to visible
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is BrokerMovementControl control && newValue is bool showTicker)
                {
                    control.TickerIcon.IsVisible = showTicker;
                }
            });

    public bool ShowTicker
    {
        get => (bool)GetValue(ShowTickerProperty);
        set => SetValue(ShowTickerProperty, value);
    }

    public BrokerMovementControl()
	{
		InitializeComponent();

        // Set default currency visibility (true by default from the bindable property)
        AmountEntry.IsCurrencyVisible = ShowCurrency;

        Core.UI.SavedPrefereces.UserPreferences
            .Do(p =>
            {
                var ticker = p.Ticker.ToFastTicker();
                TickerIcon.PlaceholderText = ticker.Symbol;
                TickerIcon.ImagePath = ticker.Image?.Value ?? string.Empty;
            })
            .Subscribe()
            .DisposeWith(Disposables);

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

        _acat = new ACATControl(
            TimeStamp: DateTime.Now,
            Ticker: Core.UI.SavedPrefereces.UserPreferences.Value.Ticker.ToFastTicker(),
            Quantity: 0m,
            Commissions: 0m,
            Fees: 0m,
            Note: string.Empty);

        TickerIcon.WhenAnyValue(x => x.IsVisible)
            .Select(x => x ? ResourceKeys.Placeholder_Quantity : ResourceKeys.Placeholder_Amount)
            .Do(t =>
            {
                AmountEntry.SetPlaceholderLocalizedText(t);
            })
            .Subscribe()
            .DisposeWith(Disposables);
    }

    protected override void StartLoad()
    {
        TickerIconGesture.Events().Tapped
            .SelectMany(_ => Observable.FromAsync(async () =>
            {
                var popupResult = await new TickerSelectorPopup().ShowAndWait();
                if (popupResult.Result is Models.Ticker ticker)
                {
                    TickerIcon.PlaceholderText = ticker.Symbol;
                    TickerIcon.ImagePath = ticker.Image?.Value ?? string.Empty;
                    _acat = _acat with { Ticker = ticker };
                    ACATChanged?.Invoke(this, _acat);
                }
                return Unit.Default; // Return Unit.Default as a "void" equivalent
            }))
            .Subscribe()
            .DisposeWith(Disposables);

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
                    _acat = _acat with { Quantity = amount };
                    DepositChanged?.Invoke(this, _deposit);
                    ACATChanged?.Invoke(this, _acat);
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
                _acat = _acat with { TimeStamp = x };
                DepositChanged?.Invoke(this, _deposit);
                ConversionChanged?.Invoke(this, _conversion);
                ACATChanged?.Invoke(this, _acat);
            })
            .DisposeWith(Disposables);

        FeesAndCommissions.Events().FeeAndCommissionChanged
            .Subscribe(x =>
            {
                _deposit = _deposit with { Commissions = x.Commission, Fees = x.Fee };
                _conversion = _conversion with { Commissions = x.Commission, Fees = x.Fee };
                _acat = _acat with { Commissions = x.Commission, Fees = x.Fee };
                DepositChanged?.Invoke(this, _deposit);
                ConversionChanged?.Invoke(this, _conversion);
                ACATChanged?.Invoke(this, _acat);
            })
            .DisposeWith(Disposables);

        Notes.Events().TextChanged
            .Subscribe(x =>
            {
                _deposit = _deposit with { Note = x.NewTextValue };
                _conversion = _conversion with { Note = x.NewTextValue };
                _acat = _acat with { Note = x.NewTextValue };
                DepositChanged?.Invoke(this, _deposit);
                ConversionChanged?.Invoke(this, _conversion);
                ACATChanged?.Invoke(this, _acat);
            })
            .DisposeWith(Disposables);
    }
}