using Binnaculum.Core;
using static Binnaculum.Core.Models;

namespace Binnaculum.Controls;

public partial class DividendTaxMovementTemplate
{
    public DividendTaxMovementTemplate()
    {
        InitializeComponent();
    }

    protected override void StartLoad()
    {

    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is Models.Movement movement && movement.Type == Models.AccountMovementType.DividendTax)
        {
            var dividend = movement.DividendTax.Value;
            
            // Use pre-computed properties
            Title.SetLocalizedText(movement.FormattedTitle);
            TimeStamp.DateTime = movement.TimeStamp;
            
            // Bind dividend tax values
            Icon.ImagePath = dividend.Ticker.Image?.Value ?? string.Empty;
            Icon.PlaceholderText = dividend.Ticker.Symbol;
            Amount.Amount = dividend.TaxAmount;
            Amount.Money = dividend.Currency;
        }
    }
}
