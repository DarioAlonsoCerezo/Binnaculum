using Binnaculum.Core;
using Binnaculum.Popups;

namespace Binnaculum.Controls;

public partial class BorderedConversionControl
{
    public event EventHandler<Conversion> ConversionChanged;

    public decimal AmountFrom { get; private set; }
    public decimal AmountTo { get; private set; }
    public string CurrencyFrom { get; private set; }
    public string CurrencyTo { get; private set; }

    public BorderedConversionControl()
	{
		InitializeComponent();

        AmountFrom = 0m;
        AmountTo = 0m;
        
        var currency = Core.UI.SavedPrefereces.UserPreferences.Value.Currency;

        // Initialize the labels and properties with the user's preferred currency
        CurrencyConvertingLabel.Text =
        CurrencyConvertedLabel.Text =
        CurrencyFrom =
        CurrencyTo = currency;        
    }

    protected override void StartLoad()
    {
        AmountConverting.Events().TextChanged
            .Subscribe(_ =>
            {
                AmountFrom = 0m;
                if (decimal.TryParse(AmountConverting.Text, out var amount))
                    AmountFrom = amount;

                Raise();
            })
            .DisposeWith(Disposables);

        AmountConverted.Events().TextChanged
            .Subscribe(_ =>
            {
                AmountTo = 0m;
                if (decimal.TryParse(AmountConverted.Text, out var amount))
                    AmountTo = amount;

                Raise();
            })
            .DisposeWith(Disposables);

        CurrencyConvertingGesture.Events().Tapped
            .SelectMany(_ => Observable.FromAsync(async () =>
            {
                var popupResult = await new CurrencySelectorPopup().ShowAndWait();
                if (popupResult.Result is Models.Currency currency)
                {
                    CurrencyConvertingLabel.Text = currency.Code;
                    CurrencyFrom = currency.Code;
                    Raise();
                }
                return Unit.Default; // Return Unit.Default as a "void" equivalent
            }))
            .Subscribe()
            .DisposeWith(Disposables);

        CurrencyConvertedGesture.Events().Tapped
            .SelectMany(_ => Observable.FromAsync(async () =>
            {
                var popupResult = await new CurrencySelectorPopup().ShowAndWait();
                if (popupResult.Result is Models.Currency currency)
                {
                    CurrencyConvertedLabel.Text = currency.Code;
                    CurrencyTo = currency.Code;
                    Raise();
                }
                return Unit.Default; // Return Unit.Default as a "void" equivalent
            }))
            .Subscribe()
            .DisposeWith(Disposables);
    }

    private void Raise()
    {
        var conversion = new Conversion(AmountFrom, AmountTo, CurrencyFrom, CurrencyTo);
        ConversionChanged?.Invoke(this, conversion);
    }
}

public record Conversion(
    decimal AmountFrom, 
    decimal AmountTo, 
    string CurrencyFrom,
    string CurrencyTo);