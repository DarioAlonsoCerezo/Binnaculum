namespace Binnaculum.Extensions;

public class RxNavigationPageEvents(NavigationPage data) : RxPageEvents(data)
{
    private readonly NavigationPage _data = data;

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
