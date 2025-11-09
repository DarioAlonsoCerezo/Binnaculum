using Binnaculum.Core;
using static Binnaculum.Core.Models;

namespace Binnaculum.Controls;

public partial class BankMovementTemplate
{
    public BankMovementTemplate()
    {
        InitializeComponent();
    }

    protected override void StartLoad()
    {

    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is Models.Movement movement && movement.Type == Models.AccountMovementType.BankAccountMovement)
        {
            var bankMovement = movement.BankAccountMovement.Value;
            
            // Use pre-computed properties
            Title.SetLocalizedText(movement.FormattedTitle);
            TimeStamp.DateTime = movement.TimeStamp;
            
            // Bind bank account movement values
            Icon.ImagePath = bankMovement.BankAccount.Bank.Image.Value;
            Amount.Amount = bankMovement.Amount;
            Amount.Money = bankMovement.Currency;
        }
    }
}
