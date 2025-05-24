using Binnaculum.Core;

namespace Binnaculum.Controls;

internal class MovementTemplateSelector : DataTemplateSelector
{
    private readonly DataTemplate EmptyMovementTemplate;
    private readonly DataTemplate DepositMovementTemplate;

    public MovementTemplateSelector()
    {
        EmptyMovementTemplate = new DataTemplate(typeof(EmptyMovementTemplate));
        DepositMovementTemplate = new DataTemplate(typeof(MovementTemplate));
    }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is Models.Movement)
        {
            return DepositMovementTemplate;
        }
        return EmptyMovementTemplate;
    }
}