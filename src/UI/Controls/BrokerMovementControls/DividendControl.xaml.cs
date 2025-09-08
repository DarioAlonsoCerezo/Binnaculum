using Binnaculum.Core;
using Binnaculum.Core.UI;
using Binnaculum.Popups;

namespace Binnaculum.Controls;

public enum DividenEditor
{
    Received,
    ExDividendDate,
    PayDate,
    Taxes
}

public partial class DividendControl 
{
    public event EventHandler<Models.Dividend?> DividendChanged;
    public event EventHandler<Models.DividendDate?> DividendDateChanged;
    public event EventHandler<Models.DividendTax?> DividendTaxChanged;

    private string _ticker;
    private decimal _amount;

    public static readonly BindableProperty BrokerAccountProperty =
        BindableProperty.Create(
            nameof(BrokerAccount),
            typeof(Models.BrokerAccount),
            typeof(TradeControl),
            null);

    public Models.BrokerAccount BrokerAccount
    {
        get => (Models.BrokerAccount)GetValue(BrokerAccountProperty);
        set => SetValue(BrokerAccountProperty, value);
    }

    public static readonly BindableProperty DividendEditorProperty =
        BindableProperty.Create(
            nameof(DividendEditor),
            typeof(DividenEditor),
            typeof(DividendControl),
            DividenEditor.Received);

    public DividenEditor DividendEditor
    {
        get => (DividenEditor)GetValue(DividendEditorProperty);
        set => SetValue(DividendEditorProperty, value);
    }

    public Models.Dividend? Dividend => GetDividend();
    public Models.DividendDate? DividendDate => GetDividendDate();
    public Models.DividendTax? DividendTax => GetDividendTax();

    public DividendControl()
	{
		InitializeComponent();
        _amount = 0m;

        Core.UI.SavedPrefereces.UserPreferences
            .Do(p =>
            {
                var ticker = p.Ticker.ToFastTicker();
                Icon.PlaceholderText = ticker.Symbol;
                Icon.ImagePath = ticker.Image?.Value ?? string.Empty;
                _ticker = ticker.Symbol;
            })
            .Subscribe()
            .DisposeWith(Disposables);
    }

    protected override void StartLoad()
    {
        IconGesture.Events().Tapped
            .SelectMany(_ => Observable.FromAsync(async () =>
            {
                var popupResult = await new TickerSelectorPopup().ShowAndWait();
                if (popupResult.Result is Models.Ticker ticker)
                {
                    Icon.PlaceholderText = ticker.Symbol;
                    Icon.ImagePath = ticker.Image?.Value ?? string.Empty;
                    _ticker = ticker.Symbol;
                }
                return Unit.Default; // Return Unit.Default as a "void" equivalent
            }))
            .Subscribe()
            .DisposeWith(Disposables);

        AmountEntry.Events().TextChanged
            .Select(x => x.NewTextValue.ToDecimalOrZero())
            .Where(x => x > 0)
            .Do(x => _amount = x)
            .Subscribe()
            .DisposeWith(Disposables);

        Observable.Merge(
            AmountEntry.Events().TextChanged.Select(_ => Unit.Default),
            DateTimePicker.Events().DateSelected.Select(_ => Unit.Default),
            Icon.WhenAnyValue(x => x.PlaceholderText).Select(_ => Unit.Default))
        .Where(x => IsVisible)
        .Subscribe(_ =>
        {
            switch (DividendEditor)
            {
                case DividenEditor.Received:
                    DividendChanged?.Invoke(this, Dividend);
                    break;
                case DividenEditor.ExDividendDate:
                case DividenEditor.PayDate:
                    DividendDateChanged?.Invoke(this, DividendDate);
                    break;
                case DividenEditor.Taxes:
                    DividendTaxChanged?.Invoke(this, DividendTax);
                    break;
            }
        })
        .DisposeWith(Disposables);
    }

    private Models.Dividend? GetDividend()
    {
        if (_amount <= 0)
            return null;

        var ticker = _ticker.ToFastTicker();
        var currency = AmountEntry.SelectedCurrencyText.ToFastCurrency();
        var date = DateTimePicker.Date;

        return new Models.Dividend(
            0,
            date,
            _amount,
            ticker,
            currency,
            BrokerAccount);
    }

    private Models.DividendDate? GetDividendDate()
    {
        if(_amount <= 0) 
            return null;

        var ticker = _ticker.ToFastTicker();
        var currency = AmountEntry.SelectedCurrencyText.ToFastCurrency();
        var date = DateTimePicker.Date;
        var code = DividendEditor == DividenEditor.ExDividendDate
            ? Models.DividendCode.ExDividendDate
            : Models.DividendCode.PayDividendDate;

        return new Models.DividendDate(
            0,
            date,
            _amount,
            ticker,
            currency,
            BrokerAccount,
            code);
    }

    private Models.DividendTax? GetDividendTax()
    {
        if (_amount <= 0)
            return null;

        var ticker = _ticker.ToFastTicker();
        var currency = AmountEntry.SelectedCurrencyText.ToFastCurrency();
        var date = DateTimePicker.Date;
        
        return new Models.DividendTax(
            0,
            date,
            _amount,
            ticker,
            currency,
            BrokerAccount);
    }
}