namespace Binnaculum.Extensions;

public class RxItemsViewEvents(ItemsView data) : RxVisualElementEvents(data)
{
    private readonly ItemsView _data = data;

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
