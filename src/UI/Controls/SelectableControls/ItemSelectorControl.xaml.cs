using Binnaculum.Core;
using CommunityToolkit.Maui.Core;

namespace Binnaculum.Controls;

public partial class ItemSelectorControl
{
    public event EventHandler<SelectableItem>? ItemSelected;

    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(
        nameof(SelectedItem),
        typeof(SelectableItem),
        typeof(ItemSelectorControl),
        defaultValue: null);

    public SelectableItem? SelectedItem
    {
        get => (SelectableItem?)GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IEnumerable<SelectableItem>),
        typeof(ItemSelectorControl),
        defaultValue: null,
        propertyChanged: (bindable, old, newValue) =>
        {
            if (bindable is ItemSelectorControl control && newValue is IEnumerable<SelectableItem> items)
            {
                BindableLayout.SetItemsSource(control.SelectorLayout, items);
            }
        });

    public IEnumerable<SelectableItem>? ItemsSource
    {
        get => (IEnumerable<SelectableItem>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title),
        typeof(string),
        typeof(ItemSelectorControl),
        defaultValue: string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }


    public ItemSelectorControl()
    {
        InitializeComponent();
    }

    protected override void StartLoad()
    {
        this.WhenAnyValue(x => x.Title)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ObserveOn(UiThread)
            .BindTo(SelectorTitle, x => x.Text)
            .DisposeWith(Disposables);

        ControlExpander.Events().ExpandedChanged
            .Select(x => x as ExpandedChangedEventArgs)
            .Where(x => x!.IsExpanded && SelectedElement.IsVisible)
            .Do(_ =>
            {
                SelectedElement.IsVisible = false;
                ExpanderTitle.SetLocalizedText(ResourceKeys.ItemSelector_Select_Option);
            })
            .Subscribe()
            .DisposeWith(Disposables);
    }

    private void SelectableItemControl_ItemSelected(object sender, SelectableItem item)
    {
        SelectedElement.SelectableItem = item;
        ControlExpander.IsExpanded = false;
        ExpanderTitle.SetLocalizedText(ResourceKeys.ItemSelector_Change_Selection);
        SelectedElement.IsVisible = true;
        ItemSelected?.Invoke(this, item);
    }
}