using Binnaculum.Core;
using Binnaculum.Core.Logging;
using static Binnaculum.Core.Models;

namespace Binnaculum.Controls;

public partial class DividendDateMovementTemplate
{
    public DividendDateMovementTemplate()
    {
        InitializeComponent();
    }

    protected override void StartLoad()
    {

    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is Models.Movement movement && movement.Type == Models.AccountMovementType.DividendDate)
        {
            // Commented out to reduce log noise
            //CoreLogger.logDebug("DividendDateMovementTemplate", "BindingContext changed for template");
            var dividend = movement.DividendDate.Value;
            
            // Use pre-computed properties
            Title.SetLocalizedText(movement.FormattedTitle);
            if (movement.FormattedSubtitle != null)
                SubTitle.SetLocalizedText(movement.FormattedSubtitle.Value);
            TimeStamp.DateTime = movement.TimeStamp;
            
            // Bind dividend date values
            Icon.ImagePath = dividend.Ticker.Image?.Value ?? string.Empty;
            Icon.PlaceholderText = dividend.Ticker.Symbol;
            Amount.Amount = dividend.Amount;
            Amount.Money = dividend.Currency;
        }
    }
}
