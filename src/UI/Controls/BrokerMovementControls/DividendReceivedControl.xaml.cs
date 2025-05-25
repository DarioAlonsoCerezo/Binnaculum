using Binnaculum.Core;
using Binnaculum.Popups;

namespace Binnaculum.Controls;

public partial class DividendReceivedControl 
{
    public event EventHandler<Models.Dividend?> DividendChanged;

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

    public Models.Dividend? Dividend => GetDividend();

    public DividendReceivedControl()
	{
		InitializeComponent();

        Core.UI.SavedPrefereces.UserPreferences
            .Do(p =>
            {
                var ticker = Core.UI.Collections.GetTicker(p.Ticker);
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
                var result = await new TickerSelectorPopup().ShowAndWait();
                if (result is Models.Ticker ticker)
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
        .Select(_ => GetDividend())
        .Subscribe(dividend =>
        {
            DividendChanged?.Invoke(this, dividend);
        })
        .DisposeWith(Disposables);
    }

    private Models.Dividend? GetDividend()
    {
        if (_amount <= 0)
            return null;

        var ticker = Core.UI.Collections.GetTicker(_ticker);
        var currency = Core.UI.Collections.GetCurrency(AmountEntry.SelectedCurrencyText);
        var date = DateTimePicker.Date;

        return new Models.Dividend(
            0,
            date,
            _amount,
            ticker,
            currency,
            BrokerAccount);
    }
}