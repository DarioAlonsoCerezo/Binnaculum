namespace Binnaculum.Controls;

public partial class SelectableItemControl
{
    public event EventHandler<SelectableItem>? ItemSelected;

    public static readonly BindableProperty SelectableItemProperty = BindableProperty.Create(
        nameof(SelectableItem),
        typeof(SelectableItem),
        typeof(SelectableItemControl),
        null,
        propertyChanged: (bindable, old, newValue) =>
        {
            if(bindable is SelectableItemControl control && newValue is SelectableItem item)
            {
                control.ItemName.Text = item.Title;
            }
        });

    public SelectableItem SelectableItem
    {
        get => (SelectableItem)GetValue(SelectableItemProperty);
        set => SetValue(SelectableItemProperty, value);
    }

    public SelectableItemControl()
	{
		InitializeComponent();
	}

    protected override void StartLoad()
    {
        ContentGesture.Events().Tapped
            .Where(_ => SelectableItem != null)
            .Subscribe(_ =>
            {
                ItemSelected?.Invoke(this, SelectableItem);
            }).DisposeWith(Disposables);
    }
}
