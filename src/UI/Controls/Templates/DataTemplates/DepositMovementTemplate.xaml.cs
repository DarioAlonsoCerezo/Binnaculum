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
            }
            
            if(movement.BankAccountMovement != null)
            {
                
            }
        }
    }
}