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
            Icon.ImagePath = snapshot.Ticker.Image.Value;
            TickerName.Text = snapshot.Ticker.Name.Value;
            Realized.Percentage = snapshot.MainCurrency.Performance;
        }
    }
}