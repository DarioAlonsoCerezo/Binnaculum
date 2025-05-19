using Binnaculum.Core;

namespace Binnaculum.Controls;

public partial class DepositMovementTemplate
{
	public DepositMovementTemplate()
	{
		InitializeComponent();
	}

    protected override void StartLoad()
    {
        
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if(BindingContext is Models.Movement movement)
        {
            if (movement.BrokerMovement != null)
            {
                Icon.ImagePath = movement.BrokerMovement.Value.BrokerAccount.Broker.Image;
                Amount.Amount = movement.BrokerMovement.Value.Amount;
                Amount.Money = movement.BrokerMovement.Value.Currency;
                TimeStamp.DateTime = movement.BrokerMovement.Value.TimeStamp;
            }
            
            if(movement.BankAccountMovement != null)
            {
                FillBankAccountMovement(movement.BankAccountMovement.Value);
                return;
            }
        }
    }

    private void FillBankAccountMovement(Models.BankAccountMovement movement)
    {
        Icon.ImagePath = movement.BankAccount.Bank.Image.Value;
        Amount.Amount = movement.Amount;
        Amount.Money = movement.Currency;
        TimeStamp.DateTime = movement.TimeStamp;
        Title.SetLocalizedText(GetTitleFromBankAccountMovementType(movement.MovementType));
    }

    private string GetTitleFromBankAccountMovementType(Models.BankAccountMovementType movementType)
    {
        var resourceKey = ResourceKeys.MovementType_Bank_Fees;
        if (movementType.IsBalance)
            resourceKey = ResourceKeys.MovementType_Bank_Balance;
        if (movementType.IsInterest)
            resourceKey = ResourceKeys.MovementType_Bank_Interest;

        return resourceKey;
    }
}