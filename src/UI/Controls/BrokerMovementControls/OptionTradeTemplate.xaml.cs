using Binnaculum.Core;

namespace Binnaculum.Controls;

public partial class OptionTradeTemplate
{
    public OptionTradeTemplate()
	{
		InitializeComponent();
	}

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if(BindingContext is Models.OptionTrade trade)
        {
            if(trade.Quantity > 1)
                Quantity.Text = $"x{trade.Quantity}";

            ExpirationDate.Text = trade.ExpirationDate.ToString("yyyy MMMM dd");
            StrikePrice.Text = trade.Strike.ToString("F2");
            Premium.Money = trade.Currency;
            Premium.Amount = trade.NetPremium;
            OptionType.SetLocalizedText(trade.OptionType.ToLocalized());
            OptionCode.SetLocalizedText(trade.Code.ToLocalized());
        }
    }
}