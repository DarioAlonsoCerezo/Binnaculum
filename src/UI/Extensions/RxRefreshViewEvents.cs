namespace Binnaculum.Extensions;

public class RxRefreshViewEvents(RefreshView data) : RxVisualElementEvents(data)
{
    private readonly RefreshView _data = data;

    public IObservable<EventArgs> Refreshing
        => Observable
            .FromEvent<EventHandler, EventArgs>(
            eventHandler => (_, e) => eventHandler(e),
            handler => _data.Refreshing += handler,
            handler => _data.Refreshing -= handler);
}
