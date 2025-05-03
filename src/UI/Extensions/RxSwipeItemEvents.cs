namespace Binnaculum.Extensions;

public class RxSwipeItemEvents(SwipeItem data) : RxElementEvents(data)
{
    private readonly SwipeItem _data = data;

    public IObservable<EventArgs> Invoked
        => Observable
            .FromEvent<EventHandler<EventArgs>, EventArgs>(
                eventHandler => (_, e) => eventHandler(e),
                handler => _data.Invoked += handler,
                handler => _data.Invoked -= handler);
}
