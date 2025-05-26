using Binnaculum.Core;
using Binnaculum.Popups;

namespace Binnaculum.Controls;

public partial class TradeControl
{
    public event EventHandler<Models.Trade?> TradeChanged;

    private string _currency, _ticker;

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

    public Models.Trade? Trade => GetTrade();

    public TradeControl()
	{
		InitializeComponent();
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

        BuySwitch.IsOn = true;
        LongSwitch.IsOn = true;
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
        
        SetupChanges();
    }

    private void SetupChanges()
    {
        Observable.Merge(
            Quantity.Events().TextChanged.Select(_ => Unit.Default),
            Price.Events().TextChanged.Select(_ => Unit.Default),
            DateTimePicker.Events().DateSelected.Select(_ => Unit.Default),
            FeesAndCommissions.Events().FeeAndCommissionChanged.Select(_ => Unit.Default),
            Icon.WhenAnyValue(x => x.PlaceholderText).Select(_ => Unit.Default),
            Currency.WhenAnyValue(x => x.Text).Select(_ => Unit.Default),
            BuySwitch.Events().Toggled.Select(_ => Unit.Default),
            LongSwitch.Events().Toggled.Select(_ => Unit.Default))
        .Where(x => IsVisible)
        .Select(_ => GetTrade())
        .Subscribe(trade =>
        {
            TradeChanged?.Invoke(this, trade);
        })
        .DisposeWith(Disposables);
    }

    private Models.Trade? GetTrade()
    {
        var quantity = Quantity.ToToDecimalOrZero();
        var price = Price.ToToDecimalOrZero();
        var date = DateTimePicker.Date;
        var fees = FeesAndCommissions.Fee;
        var commissions = FeesAndCommissions.Commission;
        var currency = Core.UI.Collections.GetCurrency(_currency);
        var ticker = Core.UI.Collections.GetTicker(_ticker);
        var totalInvested = quantity * price + fees + commissions;
        var notes = string.IsNullOrWhiteSpace(Notes.Text)
            ? Microsoft.FSharp.Core.FSharpOption<string>.None
            : Microsoft.FSharp.Core.FSharpOption<string>.Some(Notes.Text.Trim());

        var tradeCode = Models.TradeCode.BuyToOpen; // Default buy to open
        var tradeType = LongSwitch.IsOn ? Models.TradeType.Long : Models.TradeType.Short;

        if (LongSwitch.IsOn)
            tradeCode = BuySwitch.IsOn
                ? Models.TradeCode.BuyToOpen
                : Models.TradeCode.SellToClose;
        else
            tradeCode = BuySwitch.IsOn
                ? Models.TradeCode.SellToOpen
                : Models.TradeCode.BuyToClose;
        
        if (quantity <= 0)
            return null;
        
        return new Models.Trade(
            0,
            date,
            totalInvested,
            ticker,
            BrokerAccount,
            currency,
            quantity,
            price,
            commissions,
            fees,
            tradeCode,
            tradeType,
            1.0m,
            notes);
    }
}