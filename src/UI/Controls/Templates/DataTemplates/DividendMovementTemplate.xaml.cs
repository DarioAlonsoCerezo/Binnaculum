using Binnaculum.Core;
using Binnaculum.Core.Logging;
using static Binnaculum.Core.Models;

namespace Binnaculum.Controls;

public partial class DividendMovementTemplate
{
    public DividendMovementTemplate()
    {
        InitializeComponent();
    }

    protected override void StartLoad()
    {

    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is Models.Movement movement && movement.Type == Models.AccountMovementType.Dividend)
        {
            // Commented out to reduce log noise
            //CoreLogger.logDebug("DividendMovementTemplate", "BindingContext changed for template");
            var dividend = movement.Dividend.Value;
            
            // Use pre-computed properties
            Title.SetLocalizedText(movement.FormattedTitle);
            TimeStamp.DateTime = movement.TimeStamp;
            
            // Bind dividend values
            Icon.ImagePath = dividend.Ticker.Image?.Value ?? string.Empty;
            Icon.PlaceholderText = dividend.Ticker.Symbol;
            Amount.Amount = dividend.Amount;
            Amount.Money = dividend.Currency;
        }
    }
}
