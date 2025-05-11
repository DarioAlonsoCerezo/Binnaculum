namespace Binnaculum.Controls;

internal class MovementTemplateSelector : DataTemplateSelector
{
    private readonly DataTemplate EmptyMovementTemplate;

    public MovementTemplateSelector()
    {
        EmptyMovementTemplate = new DataTemplate(typeof(EmptyMovementTemplate));
    }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        return EmptyMovementTemplate;
    }
}