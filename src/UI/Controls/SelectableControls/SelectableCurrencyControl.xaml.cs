namespace Binnaculum.Controls;

public partial class SelectableCurrencyControl 
{
    public event EventHandler<Core.Models.Currency> CurrencySelected;
    private Core.Models.Currency _currency;

    public SelectableCurrencyControl()
	{
		InitializeComponent();
	}

    protected override void StartLoad()
    {
        CurrencyGesture.Events().Tapped
            .Subscribe(_ => CurrencySelected?.Invoke(this, _currency))
            .DisposeWith(Disposables);
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if(BindingContext is Core.Models.Currency currency)
        {
            _currency = currency;
            CurrencyTitle.SetLocalizedText(currency.Title);
            CurrencyCode.Text = currency.Code;
        }
    }
}