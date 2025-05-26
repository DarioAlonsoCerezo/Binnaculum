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
            ExpirationDate.Text = trade.ExpirationDate.ToString("yyyy MMMM dd");
            StrikePrice.Text = trade.Strike.ToString("F2");

            OptionType.Text = GetOptionTypeString(trade.OptionType);
            OptionCode.Text = GetOptionCodeString(trade.Code);
        }
    }

    private string GetOptionTypeString(Models.OptionType optionType)
    {
        if (optionType.IsPut)
            return "PUT: ";

        return "CALL: ";
    }

    private string GetOptionCodeString(Models.OptionCode code)
    {
        if (code.IsSellToOpen)
            return "Sell to open";

        if(code.IsSellToClose)
            return "Sell to close";

        if(code.IsBuyToOpen)
            return "Buy to open";

        if(code.IsBuyToClose)
            return "Buy to close";

        if(code.IsAssigned)
            return "Assigned";

        return "Expired";
    }
}