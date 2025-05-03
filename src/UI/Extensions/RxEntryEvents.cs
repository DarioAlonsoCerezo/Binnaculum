namespace Binnaculum.Extensions;

public class RxEntryEvents(Entry data) : RxInputViewEvents(data)
{
    private readonly Entry _data = data;

    public IObservable<EventArgs> Completed
        => Observable
            .FromEvent<EventHandler, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Completed += handler,
                handler => _data.Completed -= handler);
}
