using Binnaculum.Controls;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using PropertyChangingEventArgs = Microsoft.Maui.Controls.PropertyChangingEventArgs;
using PropertyChangingEventHandler = Microsoft.Maui.Controls.PropertyChangingEventHandler;

namespace Binnaculum.Extensions;

public static class EventsExtensions
{
    public static RxBindableObjectEvents Events(this BindableObject data) => new(data);
    public static RxTapGestureRecognizerEvents Events(this TapGestureRecognizer data) => new(data);
    public static RxPanGestureRecognizerEvents Events(this PanGestureRecognizer data) => new(data);
    public static RxEntryEvents Events(this Entry item) => new(item);
    public static RxInputViewEvents Events(this InputView item) => new(item);
    public static RxImageEvents Events(this Image item) => new(item);
    public static RxListViewEvents Events(this ListView item) => new(item);
    public static RxCollectionViewEvents Events(this CollectionView item) => new(item);
    public static RxWebViewEvents Events(this WebView item) => new(item);
    public static RxDatePickerEvents Events(this DatePicker item) => new(item);
    public static RxVisualElementEvents Events(this VisualElement item) => new(item);
    public static RxButtonEvents Events(this Button item) => new(item);
    public static RxPageEvents Events(this Page item) => new(item);
    public static RxNavigationPageEvents Events(this NavigationPage item) => new(item);
    public static RxSliderEvents Events(this Slider item) => new(item);
    public static RxStepperEvents Events(this Stepper item) => new(item);
    public static RxSwipeViewEvents Events(this SwipeView item) => new(item);
    public static RxSwipeItemEvents Events(this SwipeItem item) => new(item);
    public static RxPickerEvents Events(this Picker item) => new(item);
    public static RxRefreshViewEvents Events(this RefreshView item) => new(item);
    public static RxRadioButtonEvents Events(this RadioButton item) => new(item);
    public static RxSelectableBrokerEvents Events(this SelectableBrokerControl item) => new(item);
    public static RxSelectableBankEvents Events(this SelectableBankControl item) => new(item);
    public static RxButtonSaveEvents Events(this ButtonSave item) => new(item);
    public static RxButtonDiscardEvents Events(this ButtonDiscard item) => new(item);
    public static RxButtonAddOrDiscardEvents Events(this ButtonSaveOrDiscard item) => new(item);
    public static RxCarouselViewEvents Events(this CarouselView item) => new(item);
    public static RxBorderedEntry Events(this BorderedEntry item) => new(item);
    public static RxExpanderEvents Events(this Expander item) => new(item);
    public static RxIconControlEvents Events(this IconControl item) => new(item);
    public static RxEditableIconControlEvents Events(this EditableIconControl item) => new(item);
}

public class RxBindableObjectEvents
{
    private readonly BindableObject _data;

    public RxBindableObjectEvents(BindableObject data) => _data = data;

    public IObservable<EventArgs> BindingContextChanged
        => Observable
            .FromEvent((Func<Action<EventArgs>, EventHandler>)
                (eventHandler => (_, e) => eventHandler(e)),
                x => _data.BindingContextChanged += x,
                x => _data.BindingContextChanged -= x);

    public IObservable<PropertyChangedEventArgs> PropertyChanged
        => Observable
            .FromEvent((Func<Action<PropertyChangedEventArgs>, PropertyChangedEventHandler>)
                (eventHandler => (_, e) => eventHandler(e)),
                x => _data.PropertyChanged += x,
                x => _data.PropertyChanged -= x);

    public IObservable<PropertyChangingEventArgs> PropertyChanging
        => Observable
            .FromEvent((Func<Action<PropertyChangingEventArgs>, PropertyChangingEventHandler>)
                (eventHandler => (_, e) => eventHandler(e)),
                x => _data.PropertyChanging += x,
                x => _data.PropertyChanging -= x);
}

public class RxGestureRecognizerEvents : RxBindableObjectEvents
{
    private readonly GestureRecognizer _data;

    public RxGestureRecognizerEvents(GestureRecognizer data) : base(data) => _data = data;

}

public class RxTapGestureRecognizerEvents : RxGestureRecognizerEvents
{
    private readonly TapGestureRecognizer _data;

    public RxTapGestureRecognizerEvents(TapGestureRecognizer data) : base(data) => _data = data;

    public IObservable<TappedEventArgs> Tapped
        => Observable
            .FromEvent((Func<Action<TappedEventArgs>, EventHandler<TappedEventArgs>>)
                (eventHandler => (_, e) => eventHandler(e)),
                x => _data.Tapped += x,
                x => _data.Tapped -= x);
}

public class RxPanGestureRecognizerEvents : RxGestureRecognizerEvents
{
    private readonly PanGestureRecognizer _data;

    public RxPanGestureRecognizerEvents(PanGestureRecognizer data) : base(data) => _data = data;

    public IObservable<PanUpdatedEventArgs> PanUpdated
        => Observable
            .FromEvent((Func<Action<PanUpdatedEventArgs>, EventHandler<PanUpdatedEventArgs>>)
                (eventHandler => (_, e) => eventHandler(e)),
                x => _data.PanUpdated += x,
                x => _data.PanUpdated -= x);
}

public class RxElementEvents : RxBindableObjectEvents
{
    private readonly Element _data;

    public RxElementEvents(Element data) : base(data) => _data = data;

    public IObservable<ElementEventArgs> ChildAdded
        => Observable
            .FromEvent<EventHandler<ElementEventArgs>, ElementEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.ChildAdded += handler,
                handler => _data.ChildAdded -= handler);

    public IObservable<ElementEventArgs> ChildRemoved
        => Observable
            .FromEvent<EventHandler<ElementEventArgs>, ElementEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.ChildRemoved += handler,
                handler => _data.ChildRemoved -= handler);

    public IObservable<ElementEventArgs> DescendantAdded
        => Observable
            .FromEvent<EventHandler<ElementEventArgs>, ElementEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.DescendantAdded += handler,
                handler => _data.DescendantAdded -= handler);

    public IObservable<ElementEventArgs> DescendantRemoved
        => Observable
            .FromEvent<EventHandler<ElementEventArgs>, ElementEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.DescendantRemoved += handler,
                handler => _data.DescendantRemoved -= handler);

    public IObservable<ParentChangingEventArgs> ParentChanging
        => Observable
            .FromEvent<EventHandler<ParentChangingEventArgs>, ParentChangingEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.ParentChanging += handler,
                handler => _data.ParentChanging -= handler);

    public IObservable<EventArgs> ParentChanged
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.ParentChanged += handler,
                handler => _data.ParentChanged -= handler);

    public IObservable<HandlerChangingEventArgs> HandlerChanging
        => Observable
            .FromEvent<EventHandler<HandlerChangingEventArgs>, HandlerChangingEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.HandlerChanging += handler,
                handler => _data.HandlerChanging -= handler);

    public IObservable<EventArgs> HandlerChanged
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.HandlerChanged += handler,
                handler => _data.HandlerChanged -= handler);

}

public class RxVisualElementEvents : RxElementEvents
{
    private readonly VisualElement _data;

    public RxVisualElementEvents(VisualElement data) : base(data) => _data = data;

    public IObservable<EventArgs> ChildrenReordered
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.ChildrenReordered += handler,
                handler => _data.ChildrenReordered -= handler);

    public IObservable<FocusEventArgs> Focused
        => Observable
            .FromEvent<EventHandler<FocusEventArgs>, FocusEventArgs>(
                eventHandler => (_, args) => eventHandler(args),
                handler => _data.Focused += handler,
                handler => _data.Focused -= handler);

    public IObservable<EventArgs> MeasureInvalidated
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.MeasureInvalidated += handler,
                handler => _data.MeasureInvalidated -= handler);

    public IObservable<EventArgs> SizeChanged
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.SizeChanged += handler,
                handler => _data.SizeChanged -= handler);

    public IObservable<FocusEventArgs> Unfocused
        => Observable
            .FromEvent<EventHandler<FocusEventArgs>, FocusEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Unfocused += handler,
                handler => _data.Unfocused -= handler);

    public IObservable<EventArgs> Loaded
        => Observable
            .FromEvent((Func<Action<EventArgs>, EventHandler>)
                (eventHandler => (_, e) => eventHandler(e)),
                x => _data.Loaded += x,
                x => _data.Loaded -= x);

    public IObservable<EventArgs> Unloaded
        => Observable
            .FromEvent((Func<Action<EventArgs>, EventHandler>)
                (eventHandler => (_, e) => eventHandler(e)),
                x => _data.Unloaded += x,
                x => _data.Unloaded -= x);
}

public class RxInputViewEvents : RxVisualElementEvents
{
    private readonly InputView _data;

    public RxInputViewEvents(InputView data) : base(data)
    {
        _data = data;
    }

    public IObservable<TextChangedEventArgs> TextChanged
        => Observable
            .FromEvent<EventHandler<TextChangedEventArgs>, TextChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.TextChanged += handler,
                handler => _data.TextChanged -= handler);
}

public class RxEntryEvents : RxInputViewEvents
{
    private readonly Entry _data;

    public RxEntryEvents(Entry data) : base(data)
    {
        _data = data;
    }

    public IObservable<EventArgs> Completed
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Completed += handler,
                handler => _data.Completed -= handler);
}

public class RxImageEvents : RxVisualElementEvents
{
    private readonly Image _data;

    public RxImageEvents(Image data) : base(data)
    {
        _data = data;
    }
}

public class RxListViewEvents : RxVisualElementEvents
{
    private readonly ListView _data;

    public RxListViewEvents(ListView data) : base(data)
    {
        _data = data;
    }

    public IObservable<ScrolledEventArgs> Scrolled
        => Observable
            .FromEvent<EventHandler<ScrolledEventArgs>, ScrolledEventArgs>(
                eventHandler => (_, e) => eventHandler(e)
                , handler => _data.Scrolled += handler,
                handler => _data.Scrolled -= handler);
}

public class RxCollectionViewEvents : RxReorderableItemsViewEvents
{
    public RxCollectionViewEvents(CollectionView data) : base(data) { }
}

public class RxReorderableItemsViewEvents : RxSelectableItemsViewEvents
{
    private readonly ReorderableItemsView _data;
    public RxReorderableItemsViewEvents(ReorderableItemsView data) : base(data) => _data = data;

    public IObservable<EventArgs> ReorderCompleted
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.ReorderCompleted += handler,
                handler => _data.ReorderCompleted -= handler);
}

public class RxSelectableItemsViewEvents : RxItemsViewEvents
{
    private readonly SelectableItemsView _data;

    public RxSelectableItemsViewEvents(SelectableItemsView data) : base(data) => _data = data;

    public IObservable<SelectionChangedEventArgs> SelectionChanged =>
        Observable
            .FromEvent<EventHandler<SelectionChangedEventArgs>, SelectionChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.SelectionChanged += handler,
                handler => _data.SelectionChanged -= handler);
}

public class RxItemsViewEvents : RxVisualElementEvents
{
    private readonly ItemsView _data;

    public RxItemsViewEvents(ItemsView data) : base(data) => _data = data;

    public IObservable<ScrollToRequestEventArgs> ScrollToRequested
        => Observable.FromEvent<EventHandler<ScrollToRequestEventArgs>, ScrollToRequestEventArgs>(
            eventHandler => (_, e) => eventHandler(e),
            handler => _data.ScrollToRequested += handler,
            handler => _data.ScrollToRequested -= handler);

    public IObservable<ItemsViewScrolledEventArgs> Scrolled
        => Observable.FromEvent<EventHandler<ItemsViewScrolledEventArgs>, ItemsViewScrolledEventArgs>(
            eventHandler => (_, e) => eventHandler(e),
            handler => _data.Scrolled += handler,
            handler => _data.Scrolled -= handler);

    public IObservable<EventArgs> RemainingItemsThresholdReached
        => Observable.FromEvent<EventHandler, EventArgs>(
            eventHandler => (_, e) => eventHandler(e),
            handler => _data.RemainingItemsThresholdReached += handler,
            handler => _data.RemainingItemsThresholdReached -= handler);
}

public class RxWebViewEvents : RxVisualElementEvents
{
    private readonly WebView _data;

    public RxWebViewEvents(WebView data) : base(data)
    {
        _data = data;
    }

    public IObservable<WebNavigatingEventArgs> Navigating
        => Observable
            .FromEvent<EventHandler<WebNavigatingEventArgs>, WebNavigatingEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Navigating += handler,
                handler => _data.Navigating -= handler);

    public IObservable<WebNavigatedEventArgs> Navigated
        => Observable
            .FromEvent<EventHandler<WebNavigatedEventArgs>, WebNavigatedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Navigated += handler,
                handler => _data.Navigated -= handler);
}

public class RxDatePickerEvents : RxVisualElementEvents
{
    private readonly DatePicker _data;

    public RxDatePickerEvents(DatePicker data) : base(data)
    {
        _data = data;
    }

    public IObservable<DateChangedEventArgs> DateSelected
        => Observable
            .FromEvent<EventHandler<DateChangedEventArgs>, DateChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.DateSelected += handler,
                handler => _data.DateSelected -= handler);
}

public class RxPickerEvents : RxVisualElementEvents
{
    private readonly Picker _data;

    public RxPickerEvents(Picker data) : base(data) => _data = data;

    public IObservable<EventArgs> SelectedIndexChanged
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.SelectedIndexChanged += handler,
                handler => _data.SelectedIndexChanged -= handler);
}

public class RxButtonEvents : RxElementEvents
{
    private readonly Button _data;

    public RxButtonEvents(Button data) : base(data) => _data = data;

    public IObservable<EventArgs> Clicked
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Clicked += handler,
                handler => _data.Clicked -= handler);

    public IObservable<EventArgs> Pressed
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Pressed += handler,
                handler => _data.Pressed -= handler);

    public IObservable<EventArgs> Released
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Released += handler,
                handler => _data.Released -= handler);
}

public class RxPageEvents : RxVisualElementEvents
{
    private readonly Page _data;

    public RxPageEvents(Page data) : base(data) => _data = data;

    public IObservable<EventArgs> LayoutChanged
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.LayoutChanged += handler,
                handler => _data.LayoutChanged -= handler);

    public IObservable<EventArgs> Appearing
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Appearing += handler,
                handler => _data.Appearing -= handler);

    public IObservable<EventArgs> Disappearing
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Disappearing += handler,
                handler => _data.Disappearing -= handler);

    public IObservable<NavigatedToEventArgs> NavigatedTo
        => Observable
            .FromEvent<EventHandler<NavigatedToEventArgs>, NavigatedToEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.NavigatedTo += handler,
                handler => _data.NavigatedTo -= handler);

    public IObservable<NavigatingFromEventArgs> NavigatingFrom
        => Observable
            .FromEvent<EventHandler<NavigatingFromEventArgs>, NavigatingFromEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.NavigatingFrom += handler,
                handler => _data.NavigatingFrom -= handler);

    public IObservable<NavigatedFromEventArgs> NavigatedFrom
        => Observable
            .FromEvent<EventHandler<NavigatedFromEventArgs>, NavigatedFromEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.NavigatedFrom += handler,
                handler => _data.NavigatedFrom -= handler);
}

public class RxNavigationPageEvents : RxPageEvents
{
    private readonly NavigationPage _data;

    public RxNavigationPageEvents(NavigationPage data) : base(data) => _data = data;

    public IObservable<NavigationEventArgs> Popped
        => Observable
            .FromEvent<EventHandler<NavigationEventArgs>, NavigationEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Popped += handler,
                handler => _data.Popped -= handler);

    public IObservable<NavigationEventArgs> PoppedToRoot
        => Observable
            .FromEvent<EventHandler<NavigationEventArgs>, NavigationEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.PoppedToRoot += handler,
                handler => _data.PoppedToRoot -= handler);
}

public class RxSliderEvents : RxVisualElementEvents
{
    private readonly Slider _data;

    public RxSliderEvents(Slider data) : base(data)
    {
        _data = data;
    }

    public IObservable<ValueChangedEventArgs> ValueChanged
        => Observable
            .FromEvent<EventHandler<ValueChangedEventArgs>, ValueChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.ValueChanged += handler,
                handler => _data.ValueChanged -= handler);
}

public class RxStepperEvents : RxVisualElementEvents
{
    private readonly Stepper _data;

    public RxStepperEvents(Stepper data) : base(data)
    {
        _data = data;
    }

    public IObservable<ValueChangedEventArgs> ValueChanged
        => Observable
            .FromEvent<EventHandler<ValueChangedEventArgs>, ValueChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.ValueChanged += handler,
                handler => _data.ValueChanged -= handler);
}

public class RxSwipeViewEvents : RxElementEvents
{
    private readonly SwipeView _data;

    public RxSwipeViewEvents(SwipeView data) : base(data) => _data = data;

    public IObservable<SwipeStartedEventArgs> SwipeStarted
        => Observable
            .FromEvent<EventHandler<SwipeStartedEventArgs>, SwipeStartedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.SwipeStarted += handler,
                handler => _data.SwipeStarted -= handler);

    public IObservable<SwipeChangingEventArgs> SwipeChanging
        => Observable
            .FromEvent<EventHandler<SwipeChangingEventArgs>, SwipeChangingEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.SwipeChanging += handler,
                handler => _data.SwipeChanging -= handler);

    public IObservable<SwipeEndedEventArgs> SwipeEnded
        => Observable
            .FromEvent<EventHandler<SwipeEndedEventArgs>, SwipeEndedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.SwipeEnded += handler,
                handler => _data.SwipeEnded -= handler);
}

public class RxSwipeItemEvents : RxElementEvents
{
    private readonly SwipeItem _data;

    public RxSwipeItemEvents(SwipeItem data) : base(data) => _data = data;

    public IObservable<EventArgs> Invoked
        => Observable
            .FromEvent<EventHandler<EventArgs>, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Invoked += handler,
                handler => _data.Invoked -= handler);
}

public class RxRefreshViewEvents : RxVisualElementEvents
{
    private readonly RefreshView _data;

    public RxRefreshViewEvents(RefreshView data) : base(data)
        => _data = data;

    public IObservable<EventArgs> Refreshing
        => Observable
            .FromEvent<EventHandler, EventArgs>(
            eventHandler => (_, e) => eventHandler(e),
            handler => _data.Refreshing += handler,
            handler => _data.Refreshing -= handler);
}

public class RxRadioButtonEvents : RxVisualElementEvents
{
    private readonly RadioButton _data;

    public RxRadioButtonEvents(RadioButton data) : base(data)
    {
        _data = data;
    }

    public IObservable<CheckedChangedEventArgs> CheckedChanged
        => Observable
            .FromEvent<EventHandler<CheckedChangedEventArgs>, CheckedChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.CheckedChanged += handler,
                handler => _data.CheckedChanged -= handler);
}

public class RxSelectableBrokerEvents : RxBindableObjectEvents
{
    private readonly SelectableBrokerControl _data;
    public RxSelectableBrokerEvents(SelectableBrokerControl data) : base(data)
    {
        _data = data;
    }
    public IObservable<Core.Models.Broker> BrokerSelected
        => Observable
            .FromEvent<EventHandler<Core.Models.Broker>, Core.Models.Broker>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.BrokerSelected += handler,
                handler => _data.BrokerSelected -= handler);
}

public class RxSelectableBankEvents : RxBindableObjectEvents
{
    private readonly SelectableBankControl _data;
    public RxSelectableBankEvents(SelectableBankControl data) : base(data)
    {
        _data = data;
    }

    public IObservable<Core.Models.Bank> BankSelected
        => Observable
            .FromEvent<EventHandler<Core.Models.Bank>, Core.Models.Bank>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.BankSelected += handler,
                handler => _data.BankSelected -= handler);
}

public class RxButtonSaveEvents : RxBindableObjectEvents
{
    private readonly ButtonSave _data;
    public RxButtonSaveEvents(ButtonSave data) : base(data)
    {
        _data = data;
    }
    public IObservable<EventArgs> SaveClicked
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.SaveClicked += handler,
                handler => _data.SaveClicked -= handler);
}

public class RxButtonDiscardEvents : RxBindableObjectEvents
{
    private readonly ButtonDiscard _data;
    public RxButtonDiscardEvents(ButtonDiscard data) : base(data)
    {
        _data = data;
    }
    public IObservable<EventArgs> DiscardClicked
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.DiscardClicked += handler,
                handler => _data.DiscardClicked -= handler);
}

public class RxButtonAddOrDiscardEvents : RxBindableObjectEvents
{
    private readonly ButtonSaveOrDiscard _data;
    public RxButtonAddOrDiscardEvents(ButtonSaveOrDiscard data) : base(data)
    {
        _data = data;
    }
    public IObservable<EventArgs> SaveClicked
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.SaveClicked += handler,
                handler => _data.SaveClicked -= handler);
    public IObservable<EventArgs> DiscardClicked
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.DiscardClicked += handler,
                handler => _data.DiscardClicked -= handler);
}

public class RxCarouselViewEvents : RxItemsViewEvents
{
    private readonly CarouselView _data;
    public RxCarouselViewEvents(CarouselView data) : base(data) => _data = data;
    public IObservable<CurrentItemChangedEventArgs> CurrentItemChanged
        => Observable
            .FromEvent<EventHandler<CurrentItemChangedEventArgs>, CurrentItemChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.CurrentItemChanged += handler,
                handler => _data.CurrentItemChanged -= handler);

    public IObservable<EventArgs> PositionSelected
        => Observable
            .FromEvent<EventHandler<PositionChangedEventArgs>, PositionChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.PositionChanged += handler,
                handler => _data.PositionChanged -= handler);
}

public class RxBorderedEntry : RxVisualElementEvents
{
    private readonly BorderedEntry _data;
    public RxBorderedEntry(BorderedEntry data) : base(data)
    {
        _data = data;
    }

    public IObservable<TextChangedEventArgs> TextChanged
        => Observable
            .FromEvent<EventHandler<TextChangedEventArgs>, TextChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.TextChanged += handler,
                handler => _data.TextChanged -= handler);

    public IObservable<EventArgs> Completed
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Completed += handler,
                handler => _data.Completed -= handler);
}

public class RxExpanderEvents : RxVisualElementEvents
{
    private readonly Expander _data;

    public RxExpanderEvents(Expander data) : base(data)
    {
        _data = data;
    }

    public IObservable<EventArgs> ExpandedChanged
        => Observable
            .FromEvent<EventHandler<ExpandedChangedEventArgs>, ExpandedChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.ExpandedChanged += handler,
                handler => _data.ExpandedChanged -= handler);
}

public class RxIconControlEvents : RxVisualElementEvents
{
    private readonly IconControl _data;
    
    public RxIconControlEvents(IconControl data) : base(data)
    {
        _data = data;
    }

    public IObservable<EventArgs> IconClicked
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.IconClicked += handler,
                handler => _data.IconClicked -= handler);
}

public class RxEditableIconControlEvents : RxVisualElementEvents
{
    private readonly EditableIconControl _data;

    public RxEditableIconControlEvents(EditableIconControl data) : base(data)
    {
        _data = data;
    }
    public IObservable<EventArgs> IconClicked
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.IconClicked += handler,
                handler => _data.IconClicked -= handler);
}