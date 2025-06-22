using Binnaculum.Core;
using Binnaculum.Popups;
using Microsoft.FSharp.Collections;

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

    public FSharpList<Models.OptionTrade> Trades => GetTrades();

    public OptionTradeControl()
	{
		InitializeComponent();

        UpdateMultiplier();

        // Get the current item source to pass to the event
        var currentLegs = BindableLayout.GetItemsSource(LegsLayout) as List<Models.OptionTrade?>;
        OptionTradesChanged?.Invoke(this, currentLegs);

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

        CurrencyGesture.Events().Tapped
            .SelectMany(_ => Observable.FromAsync(async () =>
            {
                var popupResult = await new CurrencySelectorPopup().ShowAndWait();
                if (popupResult.Result is Models.Currency currency)
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
                var popupResult = await new DecimalInputPopup(_multiplier, ResourceKeys.Multiplier_Title).ShowAndWait();
                if (popupResult.Result is decimal multiplier)
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

        AddLegGesture.Events().Tapped
            .SelectMany(_ => Observable.FromAsync(async () =>
            {
                var ticker = Core.UI.Collections.GetTicker(_ticker);
                var currency = Core.UI.Collections.GetCurrency(_currency);
                var popupResult = await new OptionBuilderPopup(
                        currency, 
                        BrokerAccount, 
                        ticker, 
                        _multiplier, 
                        FeesForOperation.IsVisible)
                    .ShowAndWait();
                if(popupResult.Result is Models.OptionTrade trade)
                {
                    FeesForOperation.IsVisible = trade.FeesPerOperation;

                    if (!LegsLayout.IsVisible)
                    {
                        var legs = new List<Models.OptionTrade?>
                        {
                            trade
                        };
                        LegsLayout.IsVisible = true;
                        BindableLayout.SetItemsSource(LegsLayout, legs);
                        OptionTradesChanged?.Invoke(this, legs);
                        return;
                    }

                    var currentLegs = BindableLayout.GetItemsSource(LegsLayout) as List<Models.OptionTrade?>;
                    // Here we create a new list to insert the new trade coming from popup
                    // and then we set the new list as ItemSource for LegsLayout
                    var newLegs = currentLegs?.ToList() ?? [];
                    newLegs.Add(trade);
                    AddLegText.IsVisible = newLegs.Count < 4; // Hide "Add Leg" if we have 4 or more legs
                    BindableLayout.SetItemsSource(LegsLayout, newLegs);
                    OptionTradesChanged?.Invoke(this, newLegs);
                }
            }))
            .Subscribe()
            .DisposeWith(Disposables);
    }

    private void UpdateMultiplier()
    {
        var text = $"x{_multiplier:N0}";
        MultiplierText.Text = text;
    }

    private FSharpList<Models.OptionTrade> GetTrades()
    {
        var csharpList = BindableLayout.GetItemsSource(LegsLayout) as List<Models.OptionTrade?>;
        if (csharpList == null || csharpList.Count == 0)
        {
            // Return an empty F# list if there are no trades
            return ListModule.Empty<Models.OptionTrade>();
        }

        // Filter out null values and convert to non-nullable OptionTrade sequence
        var nonNullTrades = csharpList
            .Where(trade => trade != null)
            .Cast<Models.OptionTrade>();
        
        // Convert the C# sequence to an F# list
        return ListModule.OfSeq(nonNullTrades);
    }
}