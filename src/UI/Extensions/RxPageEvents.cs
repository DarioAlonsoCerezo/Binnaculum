namespace Binnaculum.Extensions;

public class RxPageEvents(Page data) : RxVisualElementEvents(data)
{
    private readonly Page _data = data;

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
