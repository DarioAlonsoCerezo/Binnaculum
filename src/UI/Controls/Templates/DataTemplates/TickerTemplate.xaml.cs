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

        if (BindingContext is Models.TickerSnapshot snapshot)
        {
            SetupIcon(snapshot.Ticker);           

            TickerName.Text = GetName(snapshot.Ticker);
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

    private string GetName(Models.Ticker ticker)
    {
        var name = "";
        if (ticker.Name != null && ticker.Name.Value != null)
        {
            name = ticker.Name.Value;
        }
        else
        {
            name = ticker.Symbol;
        }
        return name.Length > 8 ? name.Substring(0, 8) + "..." : name;
    }
}