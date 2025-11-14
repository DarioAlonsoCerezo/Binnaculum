using Binnaculum.Core;

namespace Binnaculum.Controls;

public partial class TradeMovementTemplate
{
    public TradeMovementTemplate()
    {
        InitializeComponent();
    }

    protected override void StartLoad()
    {

    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is Models.Movement movement && movement.Type == Models.AccountMovementType.Trade)
        {
            // Commented out to reduce log noise
            //CoreLogger.logDebug("TradeMovementTemplate", "BindingContext changed for template");
            var trade = movement.Trade.Value;
            
            // Use pre-computed properties
            Title.SetLocalizedText(movement.FormattedTitle);
            if (movement.FormattedSubtitle != null)
                SubTitle.SetLocalizedText(movement.FormattedSubtitle.Value);
            if (movement.FormattedQuantity != null)
                Quantity.Text = movement.FormattedQuantity.Value;
            TimeStamp.DateTime = movement.TimeStamp;
            
            // Bind trade-specific values
            Icon.ImagePath = trade.Ticker.Image?.Value ?? string.Empty;
            Icon.PlaceholderText = trade.Ticker.Symbol;
            Amount.Amount = trade.TotalInvestedAmount;
            Amount.Money = trade.Currency;
        }
    }
}
