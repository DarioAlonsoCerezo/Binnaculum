namespace Binnaculum.Extensions;

public class RxSelectableItemsViewEvents(SelectableItemsView data) 
    : RxItemsViewEvents(data)
{
    private readonly SelectableItemsView _data = data;

    public IObservable<SelectionChangedEventArgs> SelectionChanged =>
        Observable
            .FromEvent<EventHandler<SelectionChangedEventArgs>, SelectionChangedEventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.SelectionChanged += handler,
                handler => _data.SelectionChanged -= handler);
}
