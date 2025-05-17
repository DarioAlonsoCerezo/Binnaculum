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
                Icon.ImagePath = movement.BankAccountMovement.Value.BankAccount.Bank.Image.Value;
                Amount.Amount = movement.BankAccountMovement.Value.Amount;
                Amount.Money = movement.BankAccountMovement.Value.Currency;
                TimeStamp.DateTime = movement.BankAccountMovement.Value.TimeStamp;
            }
        }
    }
}