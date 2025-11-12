using Binnaculum.Core;

namespace Binnaculum.Controls;

public partial class TickerTemplate
{
	public TickerTemplate()
	{
		InitializeComponent();
	}

    protected override void StartLoad()
    {
        
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if(BindingContext is Models.TickerSnapshot snapshot)
        {
            SetupIcon(snapshot.Ticker);

            var name = snapshot.Ticker.Name.Value;
            TickerName.Text = name.Length > 8 ? name.Substring(0, 8) + "..." : name;
            Realized.Percentage = snapshot.MainCurrency.Performance;
        }
    }

    private void SetupIcon(Models.Ticker ticker)
    {
        if (ticker.Image == null)
            Icon.PlaceholderText = ticker.Symbol;
        else
            Icon.ImagePath = ticker.Image.Value;
    }
}