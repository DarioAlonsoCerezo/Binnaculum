using Binnaculum.Core;
using Binnaculum.Popups;

namespace Binnaculum.Controls;

public partial class OptionTradeControl 
{
    public event EventHandler<List<Models.OptionTrade?>?> OptionTradesChanged;

    private string _currency, _ticker;
    //private decimal _multiplier = 100.0m;

    public static readonly BindableProperty BrokerAccountProperty =
        BindableProperty.Create(
            nameof(BrokerAccount),
            typeof(Models.BrokerAccount),
            typeof(OptionTradeControl),
            null);

    public Models.BrokerAccount BrokerAccount
    {
        get => (Models.BrokerAccount)GetValue(BrokerAccountProperty);
        set => SetValue(BrokerAccountProperty, value);
    }

    public OptionTradeControl()
	{
		InitializeComponent();

        OptionTradesChanged?.Invoke(this, GetTrades());

        Core.UI.SavedPrefereces.UserPreferences
            .Do(p =>
            {
                var ticker = Core.UI.Collections.GetTicker(p.Ticker);
                Icon.PlaceholderText = ticker.Symbol;
                Icon.ImagePath = ticker.Image?.Value ?? string.Empty;
                Currency.Text = p.Currency;
                _ticker = ticker.Symbol;
                _currency = p.Currency;
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

        CurrencyGesture.Events().Tapped
            .SelectMany(_ => Observable.FromAsync(async () =>
            {
                var result = await new CurrencySelectorPopup().ShowAndWait();
                if (result is Models.Currency currency)
                {
                    Currency.Text = currency.Code;
                    _currency = currency.Code;
                }
                return Unit.Default; // Return Unit.Default as a "void" equivalent
            }))
            .Subscribe()
            .DisposeWith(Disposables);

        
    }

    private List<Models.OptionTrade?>? GetTrades()
    {
        return null;
    }
}