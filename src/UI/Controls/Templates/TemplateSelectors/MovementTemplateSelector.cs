using Binnaculum.Core;

namespace Binnaculum.Controls;

internal class MovementTemplateSelector : DataTemplateSelector
{
    private readonly DataTemplate EmptyMovementTemplate;
    private readonly DataTemplate DepositMovementTemplate;

    public MovementTemplateSelector()
    {
        EmptyMovementTemplate = new DataTemplate(typeof(EmptyMovementTemplate));
        DepositMovementTemplate = new DataTemplate(typeof(DepositMovementTemplate));
    }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is Models.Movement movement)
        {
            if(movement.BrokerMovement != null)
            {
                if (movement.BrokerMovement.Value.MovementType.IsDeposit)
                    return DepositMovementTemplate;
                
            }
            if(movement.BankAccountMovement != null)
            {
                if (movement.BankAccountMovement.Value.MovementType.IsBalance)
                    return DepositMovementTemplate;
                
            }
        }
        return EmptyMovementTemplate;
    }
}