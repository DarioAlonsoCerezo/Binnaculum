using Binnaculum.Core;
using static Binnaculum.Core.Models;

namespace Binnaculum.Controls;

public partial class OptionTradeMovementTemplate
{
    public OptionTradeMovementTemplate()
    {
        InitializeComponent();
    }

    protected override void StartLoad()
    {

    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is Models.Movement movement && movement.Type == Models.AccountMovementType.OptionTrade)
        {
            var trade = movement.OptionTrade.Value;
            var toShow = trade.Code.ToShow();
            
            // Use pre-computed properties
            Title.SetLocalizedText(movement.FormattedTitle);
            TimeStamp.DateTime = movement.TimeStamp;
            
            // Option-specific display logic
            if (toShow && trade.ExpirationDate > DateTime.Today)
            {
                ExpirationDateLabel.IsVisible = true;
                ExpirationDate.Text = trade.ExpirationDate.ToString("d");
            }
            
            OptionStrike.IsVisible = trade.ExpirationDate > DateTime.Today;
            OptionStrikeValue.Text = trade.Strike.ToMoneyString();
            
            // Bind option trade values
            Icon.ImagePath = trade.Ticker.Image?.Value ?? string.Empty;
            Icon.PlaceholderText = trade.Ticker.Symbol;
            Amount.Amount = trade.NetPremium;
            Amount.Money = trade.Currency;
            Amount.ChangeColor = toShow;
            Amount.IsNegative = trade.Code.IsPaid();
            
            OptionType.SetLocalizedText(trade.OptionType.ToLocalized());
            OptionCode.SetLocalizedText(trade.Code.ToLocalized());
            if (trade.Quantity > 1)
                OptionQuantity.Text = $"x{trade.Quantity}";
        }
    }
}
