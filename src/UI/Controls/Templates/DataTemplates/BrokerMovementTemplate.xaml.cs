using Binnaculum.Core;
using Binnaculum.Core.Logging;
using static Binnaculum.Core.Models;

namespace Binnaculum.Controls;

public partial class BrokerMovementTemplate
{
    private Color _redColor = (Color)Application.Current!.Resources["RedState"];
    private Color _greenColor = (Color)Application.Current!.Resources["GreenState"];

    public BrokerMovementTemplate()
    {
        InitializeComponent();
    }

    protected override void StartLoad()
    {

    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is Models.Movement movement && movement.Type == Models.AccountMovementType.BrokerMovement)
        {
            // Commented out to reduce log noise
            //CoreLogger.logDebug("BrokerMovementTemplate", "BindingContext changed for template");
            var bm = movement.BrokerMovement.Value;

            // Use pre-computed properties
            Title.SetLocalizedText(movement.FormattedTitle);
            TimeStamp.DateTime = movement.TimeStamp;

            if (movement.FormattedSubtitle != null)
            {
                SubTitle.SetLocalizedText(movement.FormattedSubtitle.Value);
                SubTitle.IsVisible = true;
            }

            // Bind broker movement values
            Icon.ImagePath = bm.BrokerAccount.Broker.Image;
            Amount.Amount = bm.Amount;
            Amount.Money = bm.Currency;

            // Handle Conversion type
            if (bm.MovementType.IsConversion)
            {
                AmountConverted.Amount = bm.AmountChanged.Value;
                AmountConverted.Money = bm.FromCurrency.Value;
                AmountConverted.IsVisible = true;
            }

            // Handle ACAT Securities transfers
            if (bm.MovementType.IsACATSecuritiesTransferReceived || bm.MovementType.IsACATSecuritiesTransferSent)
            {
                Icon.ImagePath = bm.Ticker.Value.Image.Value;
                if (movement.FormattedQuantity != null)
                    ACATQuantity.Text = movement.FormattedQuantity.Value;
                ACATQuantity.TextColor = bm.MovementType.IsACATSecuritiesTransferSent ? _redColor : _greenColor;
                ACAT.IsVisible = true;
                Amount.IsVisible = false;
            }
        }
    }
}
