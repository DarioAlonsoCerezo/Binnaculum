namespace Binnaculum.Extensions;

public class RxElementEvents(Element data) : RxBindableObjectEvents(data)
{
    private readonly Element _data = data;

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
