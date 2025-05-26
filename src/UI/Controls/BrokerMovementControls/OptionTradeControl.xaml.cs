using Binnaculum.Core;
using Binnaculum.Popups;

namespace Binnaculum.Controls;

public partial class OptionTradeControl 
{
    public event EventHandler<List<Models.OptionTrade?>?> OptionTradesChanged;

    private string _currency, _ticker;
    private decimal _multiplier = 100.0m;

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

        UpdateMultiplier();

        OptionTradesChanged?.Invoke(this, GetTrades());

        Core.UI.SavedPrefereces.UserPreferences
            .Do(p =>
            {
                var ticker = Core.UI.Collections.GetTicker(p.Ticker);
                Icon.PlaceholderText = ticker.Symbol;
                Icon.ImagePath = ticker.Image?.Value ?? string.Empty;
                CurrencyLabel.Text = p.Currency;
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
                    CurrencyLabel.Text = currency.Code;
                    _currency = currency.Code;
                }
                return Unit.Default; // Return Unit.Default as a "void" equivalent
            }))
            .Subscribe()
            .DisposeWith(Disposables);

        MultiplierGesture.Events().Tapped
            .SelectMany(_ => Observable.FromAsync(async () =>
            {
                var result = await new DecimalInputPopup(_multiplier, ResourceKeys.Multiplier_Title).ShowAndWait();
                if (result is decimal multiplier)
                {
                    if(multiplier < 100m)
                        multiplier = 100m; // Ensure minimum multiplier is 100
                    _multiplier = multiplier;
                    UpdateMultiplier();
                }
                return Unit.Default; // Return Unit.Default as a "void" equivalent
            }))
            .Subscribe()
            .DisposeWith(Disposables);

        AddLeg.Events().AddClicked
            .SelectMany(_ => Observable.FromAsync(async () =>
            {
                var ticker = Core.UI.Collections.GetTicker(_ticker);
                var currency = Core.UI.Collections.GetCurrency(_currency);
                var result = await new OptionBuilderPopup(currency, BrokerAccount, ticker).ShowAndWait();
                
            }))
            .Subscribe()
            .DisposeWith(Disposables);
    }

    private void UpdateMultiplier()
    {
        var text = $"x{_multiplier:N0}";
        MultiplierText.Text = text;
    }

    private List<Models.OptionTrade?>? GetTrades()
    {
        return null;
    }
}