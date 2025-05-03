namespace Binnaculum.Extensions;

public class RxListViewEvents(ListView data) : RxVisualElementEvents(data)
{
    private readonly ListView _data = data;

    public IObservable<ScrolledEventArgs> Scrolled
        => Observable
            .FromEvent<EventHandler<ScrolledEventArgs>, ScrolledEventArgs>(
                eventHandler => (_, e) => eventHandler(e)
                , handler => _data.Scrolled += handler,
                handler => _data.Scrolled -= handler);
}
