namespace Binnaculum.Extensions;

public class RxSwipeViewEvents(SwipeView data) : RxElementEvents(data)
{
    private readonly SwipeView _data = data;

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
